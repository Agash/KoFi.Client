using System.Text.Json.Serialization;

namespace KoFi.Client.Models;

/// <summary>
/// Represents the raw Ko-fi webhook payload as delivered inside the <c>data</c> form field.
/// </summary>
/// <remarks>
/// The Ko-fi webhook request body is <c>application/x-www-form-urlencoded</c> and contains
/// one form field named <c>data</c>, whose value is a JSON-encoded payload object.
/// </remarks>
public sealed record KoFiPayload
{
    /// <summary>
    /// Gets the verification token included by Ko-fi in the payload.
    /// </summary>
    [JsonPropertyName("verification_token")]
    public required string VerificationToken { get; init; }

    /// <summary>
    /// Gets the unique message identifier for the delivery.
    /// </summary>
    [JsonPropertyName("message_id")]
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets the event timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the Ko-fi event type.
    /// </summary>
    /// <remarks>
    /// Known values currently include <c>Donation</c>, <c>Subscription</c>,
    /// <c>Shop Order</c>, <c>Commission</c>, and <c>Referral</c>.
    /// </remarks>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Gets a value indicating whether the contribution or order is public.
    /// </summary>
    [JsonPropertyName("is_public")]
    public required bool IsPublic { get; init; }

    /// <summary>
    /// Gets the supporter or customer display name, if present.
    /// </summary>
    [JsonPropertyName("from_name")]
    public string? FromName { get; init; }

    /// <summary>
    /// Gets the supporter message, if present.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>
    /// Gets the amount string as provided by Ko-fi.
    /// </summary>
    /// <remarks>
    /// This value is intentionally kept as a string to avoid floating-point precision issues.
    /// Parse it as <see cref="decimal"/> using invariant culture.
    /// </remarks>
    [JsonPropertyName("amount")]
    public required string Amount { get; init; }

    /// <summary>
    /// Gets a provider URL related to the event, if present.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    /// <summary>
    /// Gets the supporter or customer email address, if present.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    /// <summary>
    /// Gets the currency code.
    /// </summary>
    [JsonPropertyName("currency")]
    public required string Currency { get; init; }

    /// <summary>
    /// Gets a value indicating whether the event is a subscription payment.
    /// </summary>
    [JsonPropertyName("is_subscription_payment")]
    public required bool IsSubscriptionPayment { get; init; }

    /// <summary>
    /// Gets a value indicating whether the event is the first payment for a subscription.
    /// </summary>
    [JsonPropertyName("is_first_subscription_payment")]
    public required bool IsFirstSubscriptionPayment { get; init; }

    /// <summary>
    /// Gets the Ko-fi transaction identifier, if present.
    /// </summary>
    [JsonPropertyName("kofi_transaction_id")]
    public string? KofiTransactionId { get; init; }

    /// <summary>
    /// Gets the ordered shop items, if present.
    /// </summary>
    [JsonPropertyName("shop_items")]
    public IReadOnlyList<KoFiShopItem>? ShopItems { get; init; }

    /// <summary>
    /// Gets the membership tier name, if present.
    /// </summary>
    [JsonPropertyName("tier_name")]
    public string? TierName { get; init; }

    /// <summary>
    /// Gets the shipping address, if present.
    /// </summary>
    [JsonPropertyName("shipping")]
    public KoFiShipping? Shipping { get; init; }

    /// <summary>
    /// Gets the Discord username associated with the event, if present.
    /// </summary>
    [JsonPropertyName("discord_username")]
    public string? DiscordUsername { get; init; }

    /// <summary>
    /// Gets the Discord user identifier associated with the event, if present.
    /// </summary>
    [JsonPropertyName("discord_user_id")]
    public string? DiscordUserId { get; init; }
}