using Microsoft.AspNetCore.Identity;
using System.Globalization;
using System.Net.Mail;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using SecureLab.Api.Data;
using SecureLab.Api.Data.Entities;
using SecureLab.Api.Presentation.Contracts;

namespace SecureLab.Api.Scaffolding;

public sealed class LoginRateLimitOptions
{
    public int PermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 60;
}

public static class Lab03Identity
{
    public const string LoginPolicy = "login";
    public static void AddLab03Identity(this WebApplicationBuilder builder)
    {
        builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 10;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;
        }).AddEntityFrameworkStores<SecureLabDbContext>().AddDefaultTokenProviders();
        builder.Services.AddAuthorization();
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });
        builder.Services.AddOptions<LoginRateLimitOptions>()
            .BindConfiguration("Authentication:LoginRateLimit")
            .Validate(options => options.PermitLimit > 0 && options.WindowSeconds > 0,
                "Login limit and window must be positive.").ValidateOnStart();
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(LoginPolicy, context =>
            {
                var settings = context.RequestServices.GetRequiredService<IOptions<LoginRateLimitOptions>>().Value;
                // Only the transport IP is trusted, never a supplied user name or forwarding header.
                return RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                        QueueLimit = 0, AutoReplenishment = true
                    });
            });
            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter =
                        Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(Lab03Identity).FullName!);
                LogEvent(logger, context.HttpContext, "login", "rate_limited", "anonymous");
                await Results.Problem(statusCode: 429, title: "Забагато спроб входу",
                    detail: "Повторіть спробу після завершення вікна обмеження.")
                    .ExecuteAsync(context.HttpContext);
            };
        });
    }

    public static void MapLab03Identity(this WebApplication app)
    {
        app.MapPost("/api/auth/register", RegisterAsync)
            .Produces<UserProfileResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem().ProducesProblem(StatusCodes.Status409Conflict);
        app.MapPost("/api/auth/login", LoginAsync).RequireRateLimiting(LoginPolicy);
        app.MapPost("/api/auth/logout", async (HttpContext context,
            SignInManager<ApplicationUser> signIn, ILoggerFactory logs) =>
        {
            await signIn.SignOutAsync();
            LogEvent(logs.CreateLogger(typeof(Lab03Identity).FullName!), context, "logout", "success", "anonymous");
            return Results.NoContent();
        });
        app.MapGet("/api/me", async (HttpContext context, UserManager<ApplicationUser> users) =>
        {
            // The lookup key is the verified NameIdentifier claim, never a request header.
            var user = await users.GetUserAsync(context.User);
            return user is null ? Results.Unauthorized() : Results.Ok(Profile(user));
        }).RequireAuthorization();
        app.MapGet("/api/auth/session-check", () => Results.NoContent()).RequireAuthorization();
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest input,
        UserManager<ApplicationUser> users, SecureLabDbContext db)
    {
        var errors = new Dictionary<string, string[]>();
        var name = input.UserName?.Trim() ?? "";
        var email = input.Email?.Trim() ?? "";
        var display = input.DisplayName?.Trim() ?? "";
        if (name.Length == 0 || input.UserName!.Length > 256 ||
            name.Any(value => !(char.IsAsciiLetterOrDigit(value) || "-._@+".Contains(value))))
            errors["userName"] = ["Ім’я: 1–256 символів до Trim; латинські літери, цифри або -._@+."];
        if (email.Length == 0 || input.Email!.Length > 256 ||
            !MailAddress.TryCreate(email, out var address) || address.Address != email)
            errors["email"] = ["Потрібна коректна email-адреса, не більше 256 символів до Trim."];
        if (display.Length == 0 || input.DisplayName!.Length > 120 || display.Any(char.IsControl))
            errors["displayName"] = ["Ім’я профілю: 1–120 символів до Trim, без керівних символів."];
        // Password is never trimmed or otherwise normalized.
        if (string.IsNullOrEmpty(input.Password) || input.Password.Length is < 10 or > 128)
            errors["password"] = ["Пароль: 10–128 символів, велика й мала латинські літери, цифра та спецсимвол."];
        if (errors.Count > 0) return Results.ValidationProblem(errors);

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(), UserName = name, Email = email, DisplayName = display
            };
            var result = await users.CreateAsync(user, input.Password!);
            if (!result.Succeeded)
            {
                var codes = result.Errors.Select(error => error.Code).ToArray();
                if (codes.Any(code => code is "DuplicateUserName" or "DuplicateEmail"))
                    return DuplicateRegistration();
                foreach (var code in codes)
                {
                    var field = code.StartsWith("Password", StringComparison.Ordinal) ? "password"
                        : code == "InvalidEmail" ? "email" : "userName";
                    errors[field] = ["Значення не відповідає вимогам реєстрації."];
                }
                return Results.ValidationProblem(errors);
            }
            var role = await users.AddToRoleAsync(user, DbSeeder.ReporterRole);
            if (!role.Succeeded) throw new InvalidOperationException("Default role assignment failed.");
            await transaction.CommitAsync();
            return Results.Created("/api/me", Profile(user));
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return DuplicateRegistration();
        }
    }

    private static async Task<IResult> LoginAsync(LoginRequest input,
        HttpContext context, SignInManager<ApplicationUser> signIn, ILoggerFactory logs)
    {
        var logger = logs.CreateLogger(typeof(Lab03Identity).FullName!);
        var validInput = !string.IsNullOrWhiteSpace(input.UserName) && input.UserName.Length <= 256
            && !string.IsNullOrEmpty(input.Password) && input.Password.Length <= 128;
        var succeeded = validInput && (await signIn.PasswordSignInAsync(
            input.UserName!.Trim(), input.Password!, isPersistent: false, lockoutOnFailure: false)).Succeeded;
        LogEvent(logger, context, "login", succeeded ? "success" : "failure",
            succeeded ? "authenticated" : "anonymous");
        return succeeded ? Results.NoContent() : Results.Problem(statusCode: 401,
            title: "Вхід не виконано", detail: "Неправильне ім’я користувача або пароль.");
    }

    private static IResult DuplicateRegistration() => Results.Problem(statusCode: 409,
        title: "Реєстрація недоступна", detail: "Ім’я користувача або email уже використовується.");

    private static UserProfileResponse Profile(ApplicationUser user) =>
        new(user.Id, user.UserName!, user.DisplayName);

    private static void LogEvent(ILogger logger, HttpContext context, string eventType, string result, string subject) =>
        logger.LogInformation("Authentication {EventType}: time {OccurredAtUtc}; result {Result}; subject {Subject}; trace {TraceId}",
            eventType, DateTimeOffset.UtcNow, result, subject, context.TraceIdentifier);
}
