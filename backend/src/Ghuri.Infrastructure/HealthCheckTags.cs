namespace Ghuri.Infrastructure;

/// <summary>
/// Shared tag names, so the place that REGISTERS a health check
/// (AddInfrastructure) and the place that FILTERS by it (Program.cs) can't
/// drift apart through a typo in a plain string.
/// </summary>
public static class HealthCheckTags
{
    /// <summary>Checks that must pass before the app should receive traffic (e.g. the database is reachable).</summary>
    public const string Ready = "ready";
}
