using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ghuri.Api.RateLimiting;

/// <summary>
/// Limits how often one IP address may call the sensitive endpoints
/// (blueprint section 8: "strict on /auth (5/min per IP)").
/// </summary>
/// <remarks>
/// Why this matters even with account lockout: lockout protects ONE
/// account, but someone could try one password against 10,000 different
/// phone numbers - no single account ever reaches 5 failures. Limiting per
/// IP stops that, and also stops flooding "forgot password" emails.
/// The numbers are constants in code, not in appsettings, on purpose: a
/// security floor should only ever change through a reviewed code change.
/// Behind Nginx (Day 7) every request would seem to come from Nginx's IP -
/// the forwarded-headers middleware must be switched on then.
/// </remarks>
public static class RateLimitPolicies
{
    /// <summary>Login, register, forgot/reset/change password: 5 per minute per IP.</summary>
    public const string AuthStrict = "auth-strict";

    /// <summary>
    /// Refreshing a session: 30 per minute per IP. Looser because every
    /// open tab refreshes on page load, and an office or mobile network
    /// can put many customers behind one IP.
    /// </summary>
    public const string AuthRefresh = "auth-refresh";

    /// <summary>
    /// What customers DO - book, pay, cancel, ask for or accept a custom trip
    /// (Day 16 security pass): 20 per minute per IP. Far more than a person
    /// clicks; stops a script from booking (and so holding seats) in a loop.
    /// Per IP, not per user: the limiter runs before login is checked
    /// (Program.cs), so a flood is refused before any work is spent on it.
    /// </summary>
    public const string CustomerWrites = "customer-writes";

    public static IServiceCollection AddRateLimitPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(AuthStrict, context => PerIp(context, permitsPerMinute: 5));
            options.AddPolicy(AuthRefresh, context => PerIp(context, permitsPerMinute: 30));
            options.AddPolicy(CustomerWrites, context => PerIp(context, permitsPerMinute: 20));

            // "Too many requests" as ProblemDetails with a code, like every
            // other error - the frontend handles all errors the same way.
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                var problemDetails = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many attempts. Please wait a minute and try again.",
                        Extensions = { ["code"] = "too_many_requests" },
                    },
                });
            };
        });

        return services;
    }

    // "Fixed window": each IP gets N permits per minute; the count resets
    // when the minute is over. QueueLimit 0 = refuse immediately instead
    // of making the extra request wait.
    private static RateLimitPartition<string> PerIp(HttpContext context, int permitsPerMinute) =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitsPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            });
}
