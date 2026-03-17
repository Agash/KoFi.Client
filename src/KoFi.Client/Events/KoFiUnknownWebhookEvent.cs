using System.Text.Json;

namespace KoFi.Client.Events;

/// <summary>
/// Represents a Ko-fi webhook event whose <c>type</c> value is not yet recognized by the client.
/// </summary>
/// <remarks>
/// Unknown events are still surfaced so consuming applications can observe them, log them,
/// or persist them without breaking when Ko-fi introduces new event types.
/// </remarks>
public sealed record KoFiUnknownWebhookEvent : KoFiWebhookEvent
{
    /// <summary>
    /// Gets the raw payload as received from Ko-fi.
    /// </summary>
    public required JsonElement RawPayload { get; init; }
}