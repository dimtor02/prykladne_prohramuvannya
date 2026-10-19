using Microsoft.EntityFrameworkCore;
using SecureLab.Api.Data;
using SecureLab.Api.Data.Entities;
using SecureLab.Api.Presentation.Contracts;

namespace SecureLab.Api.Scaffolding;

// Навчальний старт ЛР 02. Запускати лише з локальними штучними даними.
public static class Lab02Endpoints
{
    public static void MapLab02Endpoints(this WebApplication app)
    {
        app.MapGet("/api/incidents/search", async (string? q, string? sortBy, SecureLabDbContext db, CancellationToken ct) =>
        {
            var order = sortBy switch
            {
                null or "" or "createdAtUtc" => "created_at_utc DESC",
                "severity" => "severity", "status" => "status", _ => sortBy
            };
            var sql = "SELECT * FROM incidents WHERE title ILIKE '%" + (q ?? "")
                + "%' OR description ILIKE '%" + (q ?? "") + "%' ORDER BY " + order + " LIMIT 50";
            var rows = await db.Incidents.FromSqlRaw(sql).AsNoTracking().ToListAsync(ct);
            return Results.Ok(rows.Select(row => new
            {
                row.Id, row.Title, row.Description,
                Severity = row.Severity.ToString(), Status = row.Status.ToString(), row.CreatedAtUtc
            }));
        });
        app.MapPost("/api/incidents", CreateIncidentAsync)
            .WithName("CreateIncident")
            .Produces<CreatedIncidentResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> CreateIncidentAsync(
        CreateIncidentRequest request,
        SecureLabDbContext db,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var title = request.Title?.Trim() ?? "";
        var description = request.Description?.Trim() ?? "";
        var errors = new Dictionary<string, string[]>();

        if (title.Length == 0)
            errors["title"] = ["Заголовок є обов'язковим."];
        else if (request.Title!.Length > 160)
            errors["title"] = ["Заголовок має містити не більше 160 символів до обрізання пробілів."];

        if (description.Length == 0)
            errors["description"] = ["Опис є обов'язковим."];
        else if (request.Description!.Length > 4000)
            errors["description"] = ["Опис має містити не більше 4000 символів до обрізання пробілів."];

        var severityIsValid = Enum.TryParse<IncidentSeverity>(request.Severity, true, out var severity)
            && Enum.IsDefined(severity);
        if (!severityIsValid)
            errors["severity"] = ["Допустимі значення: Low, Medium, High, Critical."];

        if (request.OccurredAtUtc is null)
            errors["occurredAtUtc"] = ["Час інциденту є обов'язковим."];
        else if (request.OccurredAtUtc.Value > now.AddMinutes(5))
            errors["occurredAtUtc"] = ["Час інциденту не може випереджати час сервера більш ніж на 5 хвилин."];

        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        if (severity is IncidentSeverity.High or IncidentSeverity.Critical && description.Length < 40)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["description"] = ["Для High або Critical опис після обрізання пробілів має містити щонайменше 40 символів."]
            });

        // This check teaches a business conflict; concurrent requests still need a DB constraint or transaction.
        if (await db.Incidents.AnyAsync(item => item.Title == title && item.Status != IncidentStatus.Closed, ct))
            return Results.Problem(
                title: "Конфлікт інциденту",
                detail: "Інцидент із таким заголовком уже існує та ще не закритий.",
                statusCode: StatusCodes.Status409Conflict);

        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            OwnerUserId = DbSeeder.AliceId,
            Title = title,
            Description = description,
            Severity = severity,
            Status = IncidentStatus.New,
            OccurredAtUtc = request.OccurredAtUtc!.Value.ToUniversalTime(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        db.Incidents.Add(incident);
        await db.SaveChangesAsync(ct);

        var response = new CreatedIncidentResponse(
            incident.Id, incident.Title, incident.Description, incident.Severity.ToString(),
            incident.Status.ToString(), incident.OccurredAtUtc, incident.CreatedAtUtc);
        return Results.Created($"/api/incidents/{incident.Id}", response);
    }
}
