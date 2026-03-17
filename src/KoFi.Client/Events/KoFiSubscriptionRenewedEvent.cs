namespace KoFi.Client.Events;

/// <summary>
/// Represents a recurring renewal payment for an existing Ko-fi subscription.
/// </summary>
public sealed record KoFiSubscriptionRenewedEvent : KoFiSupporterEvent;