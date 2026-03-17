using KoFi.Client.Models;

namespace KoFi.Client.Events;

/// <summary>
/// Represents a normalized Ko-fi commerce event.
/// </summary>
/// <remarks>
/// Commerce events include shop orders and commissions and may contain line items and
/// shipping information.
/// </remarks>
public abstract record KoFiCommerceEvent : KoFiWebhookEvent
{
    /// <summary>
    /// Gets the ordered shop items, if present.
    /// </summary>
    public IReadOnlyList<KoFiShopItem>? ShopItems { get; init; }

    /// <summary>
    /// Gets the shipping address, if present.
    /// </summary>
    public KoFiShipping? Shipping { get; init; }
}