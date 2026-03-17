namespace KoFi.Client.Events;

/// <summary>
/// Represents the first successful payment for a Ko-fi subscription.
/// </summary>
public sealed record KoFiSubscriptionStartedEvent : KoFiSupporterEvent;