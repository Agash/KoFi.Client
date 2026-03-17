using Agash.Webhook.Abstractions;
using KoFi.Client.Events;
using KoFi.Client.Options;
using Microsoft.AspNetCore.Http;

namespace KoFi.Client.AspNetCore;

/// <summary>
/// Represents configuration callbacks used by the ASP.NET Core Ko-fi webhook endpoint mapper.
/// </summary>
public sealed class KoFiWebhookEndpointOptions
{
    /// <summary>
    /// Gets or sets the callback used to resolve the effective Ko-fi webhook options for the
    /// current HTTP request.
    /// </summary>
    /// <remarks>
    /// This is intentionally request-based so host applications can resolve tokens and other
    /// configuration from instance-scoped settings, route values, or custom stores.
    /// </remarks>
    public required Func<HttpContext, CancellationToken, Task<KoFiWebhookOptions>> ResolveWebhookOptionsAsync { get; set; }

    /// <summary>
    /// Gets or sets an optional callback invoked after a normalized Ko-fi event has been
    /// produced successfully.
    /// </summary>
    /// <remarks>
    /// This callback is not invoked when the handler returns no event, for example when the
    /// payload is invalid or authentication fails.
    /// </remarks>
    public Func<KoFiWebhookEvent, HttpContext, CancellationToken, Task>? OnEventAsync { get; set; }

    /// <summary>
    /// Gets or sets an optional callback invoked after the Ko-fi handler completes, regardless
    /// of whether it produced a normalized event.
    /// </summary>
    public Func<WebhookHandleResult<KoFiWebhookEvent>, HttpContext, CancellationToken, Task>? OnResultAsync { get; set; }
}