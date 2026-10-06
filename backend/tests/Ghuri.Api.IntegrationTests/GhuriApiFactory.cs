using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ghuri.Api.IntegrationTests;

/// <summary>
/// Boots the real Api for tests, plus the settings a real machine would
/// supply from outside git. Every integration test class uses this instead
/// of a bare WebApplicationFactory.
/// </summary>
/// <remarks>
/// The Api refuses to start without Jwt:SigningKey (ValidateOnStart). On a
/// developer PC that key comes from user-secrets, but the CI server has
/// none - so tests provide a throwaway key of their own. It signs nothing
/// real and is useless anywhere else.
/// Not sealed: SqlServerFixture builds on it to point the Api at a test
/// database container.
/// </remarks>
public class GhuriApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:SigningKey", "integration-tests-only-signing-key-not-a-secret");

        // The booking expiry job would otherwise start with every test app
        // and run on its own timer. Tests call BookingExpiryJob.RunOnceAsync
        // themselves, at the exact moment they want.
        builder.UseSetting("Jobs:BookingExpiry:Enabled", "false");
        builder.UseSetting("Jobs:Outbox:Enabled", "false"); // tests call OutboxDispatcherJob.RunOnceAsync themselves
        builder.UseSetting("Jobs:Outbox:RetryDelaySeconds", "0"); // ...and retry at once, without waiting

        // Development sends through smtp4dev - a test must never need a mail
        // server running. (SqlServerFixture goes further: a FakeEmailSender
        // that keeps every email for the test to read.)
        builder.UseSetting("Email:Sender", "Log");
    }
}
