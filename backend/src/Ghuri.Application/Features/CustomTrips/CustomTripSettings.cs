namespace Ghuri.Application.Features.CustomTrips;

/// <summary>
/// "Site" in appsettings: the public website's address, for links inside
/// emails ("see your quote: https://…/account/trips/CT1001"). Locally
/// http://localhost:5173 (or the tunnel address).
/// </summary>
public sealed class SiteOptions
{
    public const string SectionName = "Site";

    public string PublicUrl { get; init; } = string.Empty;

    /// <summary>"https://site/" + "account/trips/CT1001" - never a double slash.</summary>
    public string Link(string path) => $"{PublicUrl.TrimEnd('/')}/{path.TrimStart('/')}";
}

/// <summary>
/// "Agency:BookingsEmail": the shared inbox that hears about every new
/// custom trip request (decided 2026-10-06 - one address, not every staff
/// account). Empty = no staff alert (logged as a warning).
/// </summary>
public sealed class StaffAlertOptions
{
    public const string SectionName = "Agency";

    public string? BookingsEmail { get; init; }
}
