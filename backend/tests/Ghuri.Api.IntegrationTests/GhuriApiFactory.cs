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
    }
}
