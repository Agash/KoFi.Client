using Agash.Webhook.Abstractions;
using KoFi.Client.Abstractions;
using KoFi.Client.Events;
using KoFi.Client.Internal;
using KoFi.Client.Json;
using KoFi.Client.Models;
using KoFi.Client.Options;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace KoFi.Client.Webhooks;

/// <summary>
/// Provides the default transport-neutral implementation for processing Ko-fi webhook requests.
/// </summary>
/// <remarks>
/// This handler is intentionally stateless. All per-request configuration, including the
/// expected verification token, is supplied by the caller through
/// <see cref="KoFiWebhookOptions"/>.
/// </remarks>
public sealed class KoFiWebhookHandler : IKoFiWebhookHandler
{
    private const string FormUrlEncodedContentType = "application/x-www-form-urlencoded";
    private const string DataFieldName = "data";

    /// <inheritdoc />
    public Task<WebhookHandleResult<KoFiWebhookEvent>> HandleAsync(
        WebhookRequest request,
        KoFiWebhookOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(options.VerificationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(request.Method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new WebhookHandleResult<KoFiWebhookEvent>
            {
                Response = WebhookResponse.Empty(405),
                IsAuthenticated = false,
                IsKnownEvent = false,
                Event = null,
                FailureReason = "Unsupported HTTP method. Ko-fi webhooks must use POST.",
            });
        }

        if (!request.HasContentType(FormUrlEncodedContentType))
        {
            return Task.FromResult(new WebhookHandleResult<KoFiWebhookEvent>
            {
                Response = WebhookResponse.Empty(400),
                IsAuthenticated = false,
                IsKnownEvent = false,
                Event = null,
                FailureReason = "Unsupported content type. Expected application/x-www-form-urlencoded.",
            });
        }

        string formBody;
        try
        {
            formBody = Encoding.UTF8.GetString(request.Body);
        }
        catch (Exception ex)
        {
            return Task.FromResult(new WebhookHandleResult<KoFiWebhookEvent>
            {
                Response = WebhookResponse.Empty(400),
                IsAuthenticated = false,
                IsKnownEvent = false,
                Event = null,
                FailureReason = $"Unable to decode request body as UTF-8: {ex.Message}",
            });
        }

        Dictionary<string, string> formValues;
        try
        {
            formValues = ParseFormUrlEncoded(formBody);
        }
        catch (Exception ex)
        {
            return Task.FromResult(new WebhookHandleResult<KoFiWebhookEvent>
            {
                Response = WebhookResponse.Empty(400),
                IsAuthenticated = false,
                IsKnownEvent = false,
                Event = null,
                FailureReason = $"Malformed form body: {ex.Message}",
            });
        }

        if (!formValues.TryGetValue(DataFieldName, out string? rawJson) || string.IsNullOrWhiteSpace(rawJson))
        {
            return Task.FromResult(new WebhookHandleResult<KoFiWebhookEvent>
            {
                Response = WebhookResponse.Empty(400),
                IsAuthenticated = false,
                IsKnownEvent = false,
                Event = null,
                FailureReason = "The form body did not contain a non-empty 'data' field.",
            });
        }

        KoFiPayload? payload;
        JsonDocument? rawJsonDocument;
        try
        {
            payload = JsonSerializer.Deserialize(rawJson, KoFiJsonSerializerContext.Default.KoFiPayload);
            rawJsonDocument = JsonDocument.Parse(rawJson);
        }
        catch (JsonException ex)
        {
            return Task.FromResult(new WebhookHandleResult<KoFiWebhookEvent>
            {
                Response = WebhookResponse.Empty(400),
                IsAuthenticated = false,
                IsKnownEvent = false,
                Event = null,
                FailureReason = $"The 'data' field did not contain valid Ko-fi JSON: {ex.Message}",
            });
        }

        if (payload is null)
        {
            return Task.FromResult(new WebhookHandleResult<KoFiWebhookEvent>
            {
                Response = WebhookResponse.Empty(400),
                IsAuthenticated = false,
                IsKnownEvent = false,
                Event = null,
                FailureReason = "The Ko-fi payload could not be deserialized.",
            });
        }

        if (!ConstantTimeStringComparer.Equals(payload.VerificationToken, options.VerificationToken))
        {
            rawJsonDocument?.Dispose();

            return Task.FromResult(new WebhookHandleResult<KoFiWebhookEvent>
            {
                Response = WebhookResponse.Empty(401),
                IsAuthenticated = false,
                IsKnownEvent = false,
                Event = null,
                FailureReason = "The Ko-fi verification token did not match the expected value.",
            });
        }

        if (!decimal.TryParse(payload.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount))
        {
            rawJsonDocument?.Dispose();

            return Task.FromResult(new WebhookHandleResult<KoFiWebhookEvent>
            {
                Response = WebhookResponse.Empty(400),
                IsAuthenticated = true,
                IsKnownEvent = false,
                Event = null,
                FailureReason = $"The Ko-fi amount value '{payload.Amount}' could not be parsed as a decimal.",
            });
        }

        KoFiWebhookEvent normalizedEvent = MapEvent(payload, amount, rawJsonDocument?.RootElement.Clone());
        bool isKnownEvent = normalizedEvent is not KoFiUnknownWebhookEvent;

        rawJsonDocument?.Dispose();

        return Task.FromResult(new WebhookHandleResult<KoFiWebhookEvent>
        {
            Response = WebhookResponse.Empty(200),
            IsAuthenticated = true,
            IsKnownEvent = isKnownEvent,
            Event = normalizedEvent,
            FailureReason = null,
        });
    }

    private static Dictionary<string, string> ParseFormUrlEncoded(string formBody)
    {
        Dictionary<string, string> result = new(StringComparer.Ordinal);

        if (string.IsNullOrEmpty(formBody))
        {
            return result;
        }

        ReadOnlySpan<char> span = formBody.AsSpan();

        while (!span.IsEmpty)
        {
            int ampersandIndex = span.IndexOf('&');
            ReadOnlySpan<char> pair;

            if (ampersandIndex < 0)
            {
                pair = span;
                span = default;
            }
            else
            {
                pair = span[..ampersandIndex];
                span = span[(ampersandIndex + 1)..];
            }

            if (pair.IsEmpty)
            {
                continue;
            }

            int equalsIndex = pair.IndexOf('=');
            string encodedKey;
            string encodedValue;

            if (equalsIndex < 0)
            {
                encodedKey = pair.ToString();
                encodedValue = string.Empty;
            }
            else
            {
                encodedKey = pair[..equalsIndex].ToString();
                encodedValue = pair[(equalsIndex + 1)..].ToString();
            }

            string key = UrlDecodeFormComponent(encodedKey);
            string value = UrlDecodeFormComponent(encodedValue);

            result[key] = value;
        }

        return result;
    }

    private static string UrlDecodeFormComponent(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        string plusNormalized = value.Replace('+', ' ');
        return Uri.UnescapeDataString(plusNormalized);
    }

    private static KoFiWebhookEvent MapEvent(KoFiPayload payload, decimal amount, JsonElement? rawPayload)
    {
        return payload.Type switch
        {
            "Donation" => CreateDonationEvent(payload, amount),
            "Subscription" when payload.IsSubscriptionPayment && payload.IsFirstSubscriptionPayment =>
                CreateSubscriptionStartedEvent(payload, amount),
            "Subscription" when payload.IsSubscriptionPayment =>
                CreateSubscriptionRenewedEvent(payload, amount),
            "Shop Order" => CreateShopOrderEvent(payload, amount),
            "Commission" => CreateCommissionEvent(payload, amount),
            "Referral" => CreateReferralEvent(payload, amount),
            _ => CreateUnknownEvent(payload, amount, rawPayload ?? default),
        };
    }

    private static KoFiDonationEvent CreateDonationEvent(KoFiPayload payload, decimal amount)
    {
        return new()
        {
            RawType = payload.Type,
            MessageId = payload.MessageId,
            Timestamp = payload.Timestamp,
            IsPublic = payload.IsPublic,
            FromName = payload.FromName,
            Message = payload.Message,
            Amount = amount,
            Currency = payload.Currency,
            KofiTransactionId = payload.KofiTransactionId,
            Email = payload.Email,
            DiscordUsername = payload.DiscordUsername,
            DiscordUserId = payload.DiscordUserId,
            TierName = payload.TierName,
        };
    }

    private static KoFiSubscriptionStartedEvent CreateSubscriptionStartedEvent(KoFiPayload payload, decimal amount)
    {
        return new()
        {
            RawType = payload.Type,
            MessageId = payload.MessageId,
            Timestamp = payload.Timestamp,
            IsPublic = payload.IsPublic,
            FromName = payload.FromName,
            Message = payload.Message,
            Amount = amount,
            Currency = payload.Currency,
            KofiTransactionId = payload.KofiTransactionId,
            Email = payload.Email,
            DiscordUsername = payload.DiscordUsername,
            DiscordUserId = payload.DiscordUserId,
            TierName = payload.TierName,
        };
    }

    private static KoFiSubscriptionRenewedEvent CreateSubscriptionRenewedEvent(KoFiPayload payload, decimal amount)
    {
        return new()
        {
            RawType = payload.Type,
            MessageId = payload.MessageId,
            Timestamp = payload.Timestamp,
            IsPublic = payload.IsPublic,
            FromName = payload.FromName,
            Message = payload.Message,
            Amount = amount,
            Currency = payload.Currency,
            KofiTransactionId = payload.KofiTransactionId,
            Email = payload.Email,
            DiscordUsername = payload.DiscordUsername,
            DiscordUserId = payload.DiscordUserId,
            TierName = payload.TierName,
        };
    }

    private static KoFiReferralEvent CreateReferralEvent(KoFiPayload payload, decimal amount)
    {
        return new()
        {
            RawType = payload.Type,
            MessageId = payload.MessageId,
            Timestamp = payload.Timestamp,
            IsPublic = payload.IsPublic,
            FromName = payload.FromName,
            Message = payload.Message,
            Amount = amount,
            Currency = payload.Currency,
            KofiTransactionId = payload.KofiTransactionId,
            Email = payload.Email,
            DiscordUsername = payload.DiscordUsername,
            DiscordUserId = payload.DiscordUserId,
            TierName = payload.TierName,
        };
    }

    private static KoFiShopOrderEvent CreateShopOrderEvent(KoFiPayload payload, decimal amount)
    {
        return new()
        {
            RawType = payload.Type,
            MessageId = payload.MessageId,
            Timestamp = payload.Timestamp,
            IsPublic = payload.IsPublic,
            FromName = payload.FromName,
            Message = payload.Message,
            Amount = amount,
            Currency = payload.Currency,
            KofiTransactionId = payload.KofiTransactionId,
            Email = payload.Email,
            DiscordUsername = payload.DiscordUsername,
            DiscordUserId = payload.DiscordUserId,
            ShopItems = payload.ShopItems,
            Shipping = payload.Shipping,
        };
    }

    private static KoFiCommissionEvent CreateCommissionEvent(KoFiPayload payload, decimal amount)
    {
        return new()
        {
            RawType = payload.Type,
            MessageId = payload.MessageId,
            Timestamp = payload.Timestamp,
            IsPublic = payload.IsPublic,
            FromName = payload.FromName,
            Message = payload.Message,
            Amount = amount,
            Currency = payload.Currency,
            KofiTransactionId = payload.KofiTransactionId,
            Email = payload.Email,
            DiscordUsername = payload.DiscordUsername,
            DiscordUserId = payload.DiscordUserId,
            ShopItems = payload.ShopItems,
            Shipping = payload.Shipping,
        };
    }

    private static KoFiUnknownWebhookEvent CreateUnknownEvent(KoFiPayload payload, decimal amount, JsonElement rawPayload)
    {
        return new()
        {
            RawType = payload.Type,
            MessageId = payload.MessageId,
            Timestamp = payload.Timestamp,
            IsPublic = payload.IsPublic,
            FromName = payload.FromName,
            Message = payload.Message,
            Amount = amount,
            Currency = payload.Currency,
            KofiTransactionId = payload.KofiTransactionId,
            Email = payload.Email,
            DiscordUsername = payload.DiscordUsername,
            DiscordUserId = payload.DiscordUserId,
            RawPayload = rawPayload,
        };
    }
}