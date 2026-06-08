using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Wex.Purchase.API.Authentication;

/// <summary>
/// Custom authentication handler for Bearer token validation.
/// Validates incoming requests with "Authorization: Bearer {token}" header.
/// Token is read from configuration key 'Authentication:BearerToken' with fallback to 'secret'.
/// </summary>
public class BearerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string AuthorizationHeaderName = "Authorization";
    private const string BearerScheme = "Bearer";
    private readonly string _validToken;

    public BearerAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        // Read token from configuration; fallback to 'secret' if not set
        _validToken = configuration["Authentication:BearerToken"] ?? "secret";
    }

    /// <summary>
    /// Validates the Bearer token from the Authorization header.
    /// </summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(AuthorizationHeaderName, out var authHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authHeaderValue = authHeader.ToString();
        if (!authHeaderValue.StartsWith(BearerScheme + " ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = authHeaderValue.Substring(BearerScheme.Length).Trim();

        if (!string.Equals(token, _validToken, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid token"));
        }

        // Create claims for the authenticated user
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "BearerUser"),
            new Claim(ClaimTypes.Name, "Bearer User")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
