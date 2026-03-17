using System.Text.Json.Serialization;

namespace KoFi.Client.Models;

/// <summary>
/// Represents the shipping address information included in a Ko-fi commerce webhook payload.
/// </summary>
public sealed record KoFiShipping
{
    /// <summary>
    /// Gets the recipient full name, if present.
    /// </summary>
    [JsonPropertyName("full_name")]
    public string? FullName { get; init; }

    /// <summary>
    /// Gets the street address, if present.
    /// </summary>
    [JsonPropertyName("street_address")]
    public string? StreetAddress { get; init; }

    /// <summary>
    /// Gets the city, if present.
    /// </summary>
    [JsonPropertyName("city")]
    public string? City { get; init; }

    /// <summary>
    /// Gets the state or province, if present.
    /// </summary>
    [JsonPropertyName("state_or_province")]
    public string? StateOrProvince { get; init; }

    /// <summary>
    /// Gets the postal code, if present.
    /// </summary>
    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; init; }

    /// <summary>
    /// Gets the country, if present.
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; init; }

    /// <summary>
    /// Gets the ISO country code, if present.
    /// </summary>
    [JsonPropertyName("country_code")]
    public string? CountryCode { get; init; }

    /// <summary>
    /// Gets the telephone number, if present.
    /// </summary>
    [JsonPropertyName("telephone")]
    public string? Telephone { get; init; }
}