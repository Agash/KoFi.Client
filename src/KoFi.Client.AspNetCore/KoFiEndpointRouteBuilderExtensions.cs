using Agash.Webhook.Abstractions;
using KoFi.Client.Abstractions;
using KoFi.Client.Events;
using KoFi.Client.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace KoFi.Client.AspNetCore;

/// <summary>
/// Provides endpoint mapping extensions for exposing <see cref="IKoFiWebhookHandler"/>
/// through ASP.NET Core minimal APIs.
/// </summary>
public static class KoFiEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps a Ko-fi webhook endpoint using the supplied endpoint options.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <param name="pattern">The route pattern to map.</param>
    /// <param name="configure">
    /// A callback used to configure token resolution and optional event/result callbacks.
    /// </param>
    /// <returns>
    /// An endpoint convention builder that can be further configured with metadata,
    /// filters, authorization, or endpoint conventions.
    /// </returns>
    public static IEndpointConventionBuilder MapKoFiWebhook(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Action<KoFiWebhookEndpointOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrEmpty(pattern);
        ArgumentNullException.ThrowIfNull(configure);

        KoFiWebhookEndpointOptions options = new()
        {
            ResolveWebhookOptionsAsync = static (_, _) => Task.FromResult(new KoFiWebhookOptions
            {
                VerificationToken = string.Empty,
            }),
        };

        configure(options);

        IEndpointConventionBuilder builder = endpoints.MapPost(pattern, async context =>
        {
            IKoFiWebhookHandler handler = context.RequestServices.GetRequiredService<IKoFiWebhookHandler>();

            KoFiWebhookOptions webhookOptions =
                await options.ResolveWebhookOptionsAsync(context, context.RequestAborted).ConfigureAwait(false);

            WebhookRequest request =
                await HttpContextWebhookRequestMapper.FromHttpContextAsync(context, context.RequestAborted)
                    .ConfigureAwait(false);

            WebhookHandleResult<KoFiWebhookEvent> result =
                await handler.HandleAsync(request, webhookOptions, context.RequestAborted)
                    .ConfigureAwait(false);

            if (result.Event is KoFiWebhookEvent evt && options.OnEventAsync is not null)
            {
                await options.OnEventAsync(evt, context, context.RequestAborted).ConfigureAwait(false);
            }

            if (options.OnResultAsync is not null)
            {
                await options.OnResultAsync(result, context, context.RequestAborted).ConfigureAwait(false);
            }

            await WebhookResponseHttpContextWriter.WriteAsync(context, result.Response, context.RequestAborted)
                .ConfigureAwait(false);
        });

        return builder;
    }

    /// <summary>
    /// Maps a Ko-fi webhook endpoint using a direct webhook options resolver delegate.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <param name="pattern">The route pattern to map.</param>
    /// <param name="resolveWebhookOptionsAsync">
    /// A callback that resolves the Ko-fi webhook options for the current request.
    /// </param>
    /// <param name="onEventAsync">
    /// An optional callback invoked when a normalized Ko-fi event is produced.
    /// </param>
    /// <param name="onResultAsync">
    /// An optional callback invoked after the handler completes.
    /// </param>
    /// <returns>
    /// An endpoint convention builder that can be further configured by the caller.
    /// </returns>
    public static IEndpointConventionBuilder MapKoFiWebhook(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<HttpContext, CancellationToken, Task<KoFiWebhookOptions>> resolveWebhookOptionsAsync,
        Func<KoFiWebhookEvent, HttpContext, CancellationToken, Task>? onEventAsync = null,
        Func<WebhookHandleResult<KoFiWebhookEvent>, HttpContext, CancellationToken, Task>? onResultAsync = null)
    {
        ArgumentNullException.ThrowIfNull(resolveWebhookOptionsAsync);

        return endpoints.MapKoFiWebhook(
            pattern,
            options =>
            {
                options.ResolveWebhookOptionsAsync = resolveWebhookOptionsAsync;
                options.OnEventAsync = onEventAsync;
                options.OnResultAsync = onResultAsync;
            });
    }

    /// <summary>
    /// Maps a Ko-fi webhook endpoint using a fixed verification token for all requests.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <param name="pattern">The route pattern to map.</param>
    /// <param name="verificationToken">
    /// The verification token that all incoming requests must match.
    /// </param>
    /// <param name="onEventAsync">
    /// An optional callback invoked when a normalized Ko-fi event is produced.
    /// </param>
    /// <param name="onResultAsync">
    /// An optional callback invoked after the handler completes.
    /// </param>
    /// <returns>
    /// An endpoint convention builder that can be further configured by the caller.
    /// </returns>
    public static IEndpointConventionBuilder MapKoFiWebhook(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        string verificationToken,
        Func<KoFiWebhookEvent, HttpContext, CancellationToken, Task>? onEventAsync = null,
        Func<WebhookHandleResult<KoFiWebhookEvent>, HttpContext, CancellationToken, Task>? onResultAsync = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrEmpty(pattern);
        ArgumentException.ThrowIfNullOrEmpty(verificationToken);

        IEndpointConventionBuilder builder = endpoints.MapPost(pattern, async context =>
        {
            IKoFiWebhookHandler handler = context.RequestServices.GetRequiredService<IKoFiWebhookHandler>();

            WebhookRequest request =
                await HttpContextWebhookRequestMapper.FromHttpContextAsync(context, context.RequestAborted)
                    .ConfigureAwait(false);

            WebhookHandleResult<KoFiWebhookEvent> result =
                await handler.HandleAsync(
                        request,
                        new KoFiWebhookOptions
                        {
                            VerificationToken = verificationToken,
                        },
                        context.RequestAborted)
                    .ConfigureAwait(false);

            if (result.Event is KoFiWebhookEvent evt && onEventAsync is not null)
            {
                await onEventAsync(evt, context, context.RequestAborted).ConfigureAwait(false);
            }

            if (onResultAsync is not null)
            {
                await onResultAsync(result, context, context.RequestAborted).ConfigureAwait(false);
            }

            await WebhookResponseHttpContextWriter.WriteAsync(context, result.Response, context.RequestAborted)
                .ConfigureAwait(false);
        });

        _ = builder.WithMetadata(new KoFiFixedVerificationTokenMetadata(verificationToken));

        return builder;
    }

    private sealed record KoFiFixedVerificationTokenMetadata(string VerificationToken);
}