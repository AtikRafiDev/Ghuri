using Ghuri.Application.Abstractions.Ports;

namespace Ghuri.Api.Authentication;

/// <summary>IClientInfo, answered from the current HTTP request.</summary>
/// <remarks>
/// Behind a reverse proxy (Nginx/IIS), RemoteIpAddress will be the proxy's
/// own address unless the forwarded-headers middleware is switched on - a
/// deployment task, done when the finished project goes to a server.
/// </remarks>
internal sealed class HttpClientInfo(IHttpContextAccessor httpContextAccessor) : IClientInfo
{
    // Column sizes of iam.RefreshTokens (CreatedByIp, UserAgent).
    private const int MaxUserAgentLength = 300;

    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
    {
        get
        {
            var userAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
            if (string.IsNullOrEmpty(userAgent))
                return null;

            // Browsers send whatever they like; cut it to fit the column
            // instead of letting one odd browser make the INSERT fail.
            return userAgent.Length > MaxUserAgentLength ? userAgent[..MaxUserAgentLength] : userAgent;
        }
    }
}
