using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.ArchitectureTests;

/// <summary>
/// Blueprint section 11 / 14.2: "controllers only depend on ISender".
/// A controller that asks for a DbContext, a repository or a service has
/// started doing business logic - this test fails the build the moment
/// that happens, even in a controller written months from now.
/// </summary>
public class ControllerTests
{
    private static readonly Type[] Controllers = typeof(Program).Assembly.GetTypes()
        .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
        .ToArray();

    [Fact]
    public void ThereAreControllersToCheck() => Assert.NotEmpty(Controllers);

    [Fact]
    public void Controllers_OnlyDependOnISender()
    {
        var offenders = Controllers
            .SelectMany(c => c.GetConstructors().SelectMany(ctor => ctor.GetParameters())
                .Where(p => p.ParameterType != typeof(ISender))
                .Select(p => $"{c.Name} asks for {p.ParameterType.Name}"))
            .ToList();

        Assert.Empty(offenders);
    }
}
