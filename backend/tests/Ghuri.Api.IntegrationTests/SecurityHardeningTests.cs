using System.Net;
using System.Net.Http.Json;

namespace Ghuri.Api.IntegrationTests;

/// <summary>
/// The Day 16 security pass, checked over real HTTP. In its own class: its
/// own app instance, so its own rate-limit counters.
/// </summary>
public class SecurityHardeningTests(GhuriApiFactory factory) : IClassFixture<GhuriApiFactory>
{
    [Fact]
    public async Task EveryAnswer_CarriesTheSecurityHeaders()
    {
        var response = await factory.CreateClient().GetAsync("/health/live", TestContext.Current.CancellationToken);

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
    }

    [Fact]
    public async Task BookingInALoop_IsStoppedAfter20AMinute()
    {
        var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        // Not logged in on purpose: the limiter counts a request before login is
        // checked, so even a script without an account is stopped.
        for (var i = 1; i <= 20; i++)
        {
            var allowed = await client.PostAsJsonAsync("/api/v1/bookings", new { }, ct);
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        var next = await client.PostAsJsonAsync("/api/v1/bookings", new { }, ct);

        Assert.Equal(HttpStatusCode.TooManyRequests, next.StatusCode);
    }
}
