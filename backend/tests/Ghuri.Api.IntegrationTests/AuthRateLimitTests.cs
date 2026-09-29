using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Ghuri.Api.IntegrationTests;

/// <summary>
/// In its own class on purpose: each test class gets its own app instance,
/// and so its own rate-limit counters - other tests' requests can't eat
/// into this test's 5 permits.
/// </summary>
public class AuthRateLimitTests(GhuriApiFactory factory) : IClassFixture<GhuriApiFactory>
{
    [Fact]
    public async Task Login_SixthAttemptWithinAMinute_Is429()
    {
        var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        // Invalid input on purpose: the validator answers 400 without a
        // database - but every attempt still counts against the limit.
        var attempt = new { phoneOrEmail = "01711000000", password = new string('x', 129) };

        for (var i = 1; i <= 5; i++)
        {
            var allowed = await client.PostAsJsonAsync("/api/v1/auth/login", attempt, ct);
            Assert.Equal(HttpStatusCode.BadRequest, allowed.StatusCode);
        }

        var sixth = await client.PostAsJsonAsync("/api/v1/auth/login", attempt, ct);

        Assert.Equal(HttpStatusCode.TooManyRequests, sixth.StatusCode);
        var body = await sixth.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal("too_many_requests", body.GetProperty("code").GetString());
        Assert.True(sixth.Headers.Contains("Retry-After"));
    }
}
