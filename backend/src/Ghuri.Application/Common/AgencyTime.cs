namespace Ghuri.Application.Common;

/// <summary>
/// "Today" for the agency, in Bangladesh - not on the server's UTC clock.
/// Between midnight and 6 a.m. in Dhaka, UTC is still on yesterday; a
/// departure "for today" must already count as today.
/// </summary>
/// <remarks>
/// A fixed UTC+6 offset rather than a time-zone lookup: Bangladesh has no
/// daylight-saving time (last used in 2009), and a fixed offset behaves the
/// same on Windows, Linux and in Docker images without time-zone data.
/// </remarks>
public static class AgencyTime
{
    public static readonly TimeSpan UtcOffset = TimeSpan.FromHours(6);

    public static DateOnly Today(this TimeProvider clock) =>
        DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(UtcOffset).DateTime);
}

/// <summary>A Bangladesh calendar day as UTC times - for "today's bookings" or a date filter on UTC columns.</summary>
public static class AgencyDay
{
    /// <summary>Midnight in Dhaka, in UTC: 7 Oct 00:00 (UTC+6) = 6 Oct 18:00 UTC.</summary>
    public static DateTime StartUtc(DateOnly day) =>
        DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue) - AgencyTime.UtcOffset, DateTimeKind.Utc);
}
