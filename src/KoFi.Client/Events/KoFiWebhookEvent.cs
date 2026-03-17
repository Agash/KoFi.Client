namespace KoFi.Client.Events;

/// <summary>
/// Represents the normalized base type for all Ko-fi webhook events.
/// </summary>
/// <remarks>
/// The event hierarchy intentionally normalizes Ko-fi's raw webhook payload into
/// strongly-typed categories while still preserving important source fields.
/// </remarks>
public abstract record KoFiWebhookEvent
{
    /// <summary>
    /// Gets the raw Ko-fi event type, for example <c>Donation</c> or <c>Shop Order</c>.
    /// </summary>
    public required string RawType { get; init; }

    /// <summary>
    /// Gets the unique message identifier for the delivery.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets the time at which the event occurred according to Ko-fi.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets a value indicating whether the event is public.
    /// </summary>
    public required bool IsPublic { get; init; }

    /// <summary>
    /// Gets the supporter or customer display name, if present.
    /// </summary>
    public string? FromName { get; init; }

    /// <summary>
    /// Gets the supporter or customer message, if present.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the event amount.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Gets the ISO currency code or provider currency string.
    /// </summary>
    public required string Currency { get; init; }

    /// <summary>
    /// Gets the Ko-fi transaction identifier, if present.
    /// </summary>
    public string? KofiTransactionId { get; init; }

    /// <summary>
    /// Gets the email address associated with the event, if present.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets the Discord username associated with the event, if present.
    /// </summary>
    public string? DiscordUsername { get; init; }

    /// <summary>
    /// Gets the Discord user identifier associated with the event, if present.
    /// </summary>
    public string? DiscordUserId { get; init; }
}