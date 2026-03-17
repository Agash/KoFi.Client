using System.Text.Json.Serialization;

namespace KoFi.Client.Models;

/// <summary>
/// Represents a Ko-fi shop line item within a shop order webhook payload.
/// </summary>
public sealed record KoFiShopItem
{
    /// <summary>
    /// Gets the direct link code for the ordered item.
    /// </summary>
    [JsonPropertyName("direct_link_code")]
    public required string DirectLinkCode { get; init; }

    /// <summary>
    /// Gets the selected variation name, if present.
    /// </summary>
    [JsonPropertyName("variation_name")]
    public string? VariationName { get; init; }

    /// <summary>
    /// Gets the ordered quantity.
    /// </summary>
    [JsonPropertyName("quantity")]
    public required int Quantity { get; init; }
}