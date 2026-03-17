namespace KoFi.Client.Events;

/// <summary>
/// Represents a normalized Ko-fi supporter event.
/// </summary>
/// <remarks>
/// Supporter events include audience support actions such as donations, subscriptions,
/// subscription renewals, and referrals.
/// </remarks>
public abstract record KoFiSupporterEvent : KoFiWebhookEvent
{
    /// <summary>
    /// Gets the Ko-fi membership tier name, if present.
    /// </summary>
    public string? TierName { get; init; }
}