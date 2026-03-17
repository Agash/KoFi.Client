using KoFi.Client.Abstractions;
using KoFi.Client.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KoFi.Client.DependencyInjection;

/// <summary>
/// Provides dependency injection registration helpers for <c>KoFi.Client</c>.
/// </summary>
public static class KoFiClientServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core Ko-fi client services to the specified service collection.
    /// </summary>
    /// <param name="services">The service collection to modify.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddKoFiClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IKoFiWebhookHandler, KoFiWebhookHandler>();
        return services;
    }
}