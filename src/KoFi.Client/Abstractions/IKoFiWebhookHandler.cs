using Agash.Webhook.Abstractions;
using KoFi.Client.Events;
using KoFi.Client.Options;

namespace KoFi.Client.Abstractions;

/// <summary>
/// Defines a transport-neutral webhook handler for Ko-fi webhook payloads.
/// </summary>
/// <remarks>
/// The handler is intentionally stateless. Callers provide the per-request verification
/// options so the same handler instance can process multiple Ko-fi provider instances with
/// different verification tokens.
/// </remarks>
public interface IKoFiWebhookHandler
{
    /// <summary>
    /// Processes an inbound Ko-fi webhook request.
    /// </summary>
    /// <param name="request">The inbound webhook request.</param>
    /// <param name="options">
    /// The per-request Ko-fi webhook options, including the expected verification token.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to observe cancellation.
    /// </param>
    /// <returns>
    /// A task that completes with the webhook processing result, including the HTTP response
    /// to return and the normalized Ko-fi event when processing succeeds.
    /// </returns>
    Task<WebhookHandleResult<KoFiWebhookEvent>> HandleAsync(
        WebhookRequest request,
        KoFiWebhookOptions options,
        CancellationToken cancellationToken = default);
}