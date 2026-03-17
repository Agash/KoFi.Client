using KoFi.Client.Models;
using System.Text.Json.Serialization;

namespace KoFi.Client.Json;

/// <summary>
/// Provides source-generated JSON serialization metadata for Ko-fi payload models.
/// </summary>
/// <remarks>
/// This context enables trimming- and AOT-friendly JSON processing without relying on
/// runtime reflection-based serialization metadata generation.
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    WriteIndented = false)]
[JsonSerializable(typeof(KoFiPayload))]
[JsonSerializable(typeof(KoFiShopItem))]
[JsonSerializable(typeof(KoFiShipping))]
internal sealed partial class KoFiJsonSerializerContext : JsonSerializerContext;