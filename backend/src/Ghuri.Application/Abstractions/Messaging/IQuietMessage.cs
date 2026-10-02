namespace Ghuri.Application.Abstractions.Messaging;

/// <summary>
/// Marks a query or command that runs on a timer, e.g. the expiry job's
/// "any expired holds?" every minute. LoggingBehavior logs its success at
/// Debug (hidden at our Information level) instead of Information: ~2,900
/// "Handling … / Handled …" lines a day that say nothing would bury the
/// logs that matter - the same reasoning as the quiet /health checks in
/// Program.cs. A FAILURE is still logged as a warning.
/// </summary>
public interface IQuietMessage;
