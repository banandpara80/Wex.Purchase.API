using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Wex.Purchase.API.Authentication;

namespace Wex.Purchase.API.Extensions;

/// <summary>
/// Extension methods for configuring Bearer token authentication.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds Bearer token authentication to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddBearerAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "Bearer";
            options.DefaultChallengeScheme = "Bearer";
        })
        .AddScheme<AuthenticationSchemeOptions, BearerAuthenticationHandler>("Bearer", null);

        return services;
    }
}
