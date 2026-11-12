using System.Text.Json;
using System.Text.Json.Serialization;
using KoFi.Client.Events;

namespace KoFi.Client.Json;

/// <summary>
/// Source-generated JSON metadata for the parsed webhook events, so a consumer can store and restore them
/// (an event journal, a queue) without reflection-based serialization, under trimming and Native AOT.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(KoFiDonationEvent))]
[JsonSerializable(typeof(KoFiSubscriptionStartedEvent))]
[JsonSerializable(typeof(KoFiSubscriptionRenewedEvent))]
[JsonSerializable(typeof(KoFiReferralEvent))]
[JsonSerializable(typeof(KoFiShopOrderEvent))]
[JsonSerializable(typeof(KoFiCommissionEvent))]
[JsonSerializable(typeof(KoFiUnknownWebhookEvent))]
public sealed partial class KoFiEventJsonContext : JsonSerializerContext;
