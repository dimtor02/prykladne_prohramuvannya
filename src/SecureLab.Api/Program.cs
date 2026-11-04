using SecureLab.Api.Scaffolding;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SecureLab.Api.Application.Incidents;
using SecureLab.Api.Data;
using SecureLab.Api.Presentation.Endpoints;
using SecureLab.Api.Security;
using Microsoft.AspNetCore.Antiforgery;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "Client"
});
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

var connectionString = builder.Configuration.GetConnectionString("SecureLab")
    ?? throw new InvalidOperationException(
        "Connection string 'SecureLab' не налаштовано.");

builder.Services.AddDbContext<SecureLabDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<IncidentQueries>();
builder.Services.AddScoped<IncidentAccessPolicy>();

builder.AddLab03Identity();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = AntiforgeryFilter.HeaderName;
    options.Cookie.Name = "SecureLab.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.AddOptions<SearchRateLimitOptions>()
    .BindConfiguration("Security:SearchRateLimit")
    .Validate(o => o.PermitLimit > 0 && o.WindowSeconds > 0, "Search quota and window must be positive.")
    .ValidateOnStart();
builder.Services.AddRateLimiter(options => options.AddPolicy<string, SearchRateLimitPolicy>(SearchRateLimitPolicy.Name));

// Local teaching probe only; the normal client uses same-origin API requests.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Lab05BrowserProbe", policy =>
            policy.WithOrigins("http://localhost:4173")
                .WithMethods("GET")
                .WithHeaders("X-Lab-Cors-Probe")
                .AllowCredentials());
    });
}

var app = builder.Build();

var resetRequested = args.Contains("--reset-database", StringComparer.Ordinal);
await DatabaseBootstrap.InitializeAsync(app.Services, app.Configuration, app.Environment, resetRequested);

if (resetRequested)
{
    return;
}

var csp = "default-src 'self'; script-src 'self'; style-src 'self'; "
    + "connect-src 'self'; img-src 'self'; object-src 'none'; "
    + "base-uri 'self'; frame-ancestors 'self'";
app.Use(async (context, next) =>
{
    // Scalar is a separate Development-only documentation UI with its own resources.
    var isDevelopmentApiReference = app.Environment.IsDevelopment()
        && context.Request.Path.StartsWithSegments("/scalar");
    // Register outside the exception handler and write after a possible Response.Clear().
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        if (!isDevelopmentApiReference)
            context.Response.Headers["Content-Security-Policy"] = csp;
        return Task.CompletedTask;
    });
    await next(context);
});
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseRouting();
if (app.Environment.IsDevelopment())
{
    app.UseCors("Lab05BrowserProbe");
}
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", async (SecureLabDbContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ready" })
        : Results.Problem(
            title: "База даних недоступна",
            statusCode: StatusCodes.Status503ServiceUnavailable))
    .WithName("GetHealth")
    .Produces(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

app.MapIncidentEndpoints();
app.MapIncidentMutationEndpoints();
app.MapLab03Identity();
var tokenEndpoint = app.MapGet(AntiforgeryFilter.TokenRoute, (IAntiforgery antiforgery, HttpContext context) =>
    Results.Ok(new { token = antiforgery.GetAndStoreTokens(context).RequestToken }))
    .RequireAuthorization();
if (app.Environment.IsDevelopment())
    tokenEndpoint.WithMetadata(new Microsoft.AspNetCore.Cors.DisableCorsAttribute());
app.MapLab02Endpoints();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
