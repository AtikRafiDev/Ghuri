using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ghuri.Api.IntegrationTests;

/// <summary>
/// Boots the REAL Api (Program.cs, every DI registration, the whole
/// middleware pipeline) in memory and sends it a real HTTP request.
/// If any registration is broken - a missing service, a bad pipeline
/// order - the app fails to start and this test fails.
/// </summary>
/// <remarks>
/// Tests /health/live on purpose: it runs no database check, so this test
/// works on the CI server, which has no SQL Server. Database-backed tests
/// come later, with a throwaway database (the blueprint's Testcontainers).
/// </remarks>
public class HealthEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Live_ReturnsHealthy_WithoutNeedingADatabase()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
