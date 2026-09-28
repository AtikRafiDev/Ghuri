using System.Reflection;
using Ghuri.Domain.Common;

namespace Ghuri.ArchitectureTests;

/// <summary>
/// Enforces the Clean Architecture dependency rule (blueprint section 3.1):
/// dependencies point INWARD only. These fail the build if someone adds a
/// forbidden reference - so the rule can't be broken by accident, even
/// months from now by someone who never read the blueprint.
/// </summary>
/// <remarks>
/// Uses GetReferencedAssemblies(), which lists only assemblies the compiled
/// code ACTUALLY uses - so a single "using EF Core" line that sneaks into
/// Domain is caught, not just an extra line in a .csproj.
/// </remarks>
public class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(BaseEntity).Assembly;
    private static readonly Assembly Application = typeof(Ghuri.Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(Ghuri.Infrastructure.DependencyInjection).Assembly;

    private static string[] ReferencedNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();

    [Theory]
    [InlineData("Ghuri.Application")]
    [InlineData("Ghuri.Infrastructure")]
    [InlineData("Ghuri.Api")]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Mediator")]
    public void Domain_DependsOnNothing(string forbidden)
    {
        // Domain is the centre: pure C#, no other layer, no framework.
        Assert.DoesNotContain(ReferencedNames(Domain), name => name.StartsWith(forbidden, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Ghuri.Infrastructure")]
    [InlineData("Ghuri.Api")]
    [InlineData("Microsoft.EntityFrameworkCore.SqlServer")]
    public void Application_DoesNotDependOnOuterLayers(string forbidden)
    {
        // Application may use core EF (async LINQ for queries) but never
        // the SQL Server provider or anything from the outer layers.
        Assert.DoesNotContain(ReferencedNames(Application), name => name == forbidden);
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        Assert.DoesNotContain("Ghuri.Api", ReferencedNames(Infrastructure));
    }
}
