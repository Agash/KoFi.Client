using System.Text;
using Agash.Webhook.Abstractions;
using KoFi.Client.Events;
using KoFi.Client.Options;
using KoFi.Client.Webhooks;

namespace KoFi.Client.Tests.Webhooks;

[TestClass]
public sealed class KoFiWebhookHandlerTests
{
    private const string Token = "kofi-verification-token";

    private readonly KoFiWebhookHandler _handler = new();

    private static KoFiWebhookOptions Options => new() { VerificationToken = Token };

    private static string PayloadJson(
        string type = "Donation",
        string amount = "5.00",
        string token = Token,
        bool isSubscriptionPayment = false,
        bool isFirstSubscriptionPayment = false) =>
        $$"""
        {
          "verification_token": "{{token}}",
          "message_id": "msg-1",
          "timestamp": "2026-01-01T12:00:00Z",
          "type": "{{type}}",
          "is_public": true,
          "from_name": "Jane",
          "message": "Thanks!",
          "amount": "{{amount}}",
          "currency": "USD",
          "kofi_transaction_id": "txn-1",
          "is_subscription_payment": {{(isSubscriptionPayment ? "true" : "false")}},
          "is_first_subscription_payment": {{(isFirstSubscriptionPayment ? "true" : "false")}}
        }
        """;

    private static WebhookRequest Request(
        string? json = null,
        string method = "POST",
        string contentType = "application/x-www-form-urlencoded",
        string? rawBody = null)
    {
        string body = rawBody ?? $"data={Uri.EscapeDataString(json ?? PayloadJson())}";

        return new WebhookRequest
        {
            Method = method,
            Path = "/webhooks/kofi",
            ContentType = contentType,
            Body = Encoding.UTF8.GetBytes(body),
        };
    }

    [TestMethod]
    public async Task HandleAsync_WhenMethodIsNotPost_Returns405()
    {
        WebhookHandleResult<KoFiWebhookEvent> result =
            await _handler.HandleAsync(Request(method: "GET"), Options);

        Assert.AreEqual(405, result.Response.StatusCode);
        Assert.IsFalse(result.IsAuthenticated);
        Assert.IsNull(result.Event);
    }

    [TestMethod]
    public async Task HandleAsync_WhenContentTypeIsNotFormUrlEncoded_Returns400()
    {
        WebhookHandleResult<KoFiWebhookEvent> result =
            await _handler.HandleAsync(Request(contentType: "application/json"), Options);

        Assert.AreEqual(400, result.Response.StatusCode);
        Assert.IsFalse(result.IsAuthenticated);
    }

    [TestMethod]
    public async Task HandleAsync_WhenDataFieldIsMissing_Returns400()
    {
        WebhookHandleResult<KoFiWebhookEvent> result =
            await _handler.HandleAsync(Request(rawBody: "other=value"), Options);

        Assert.AreEqual(400, result.Response.StatusCode);
        Assert.IsNotNull(result.FailureReason);
        Assert.Contains("data", result.FailureReason);
    }

    [TestMethod]
    public async Task HandleAsync_WhenDataIsNotValidJson_Returns400()
    {
        WebhookHandleResult<KoFiWebhookEvent> result =
            await _handler.HandleAsync(Request(rawBody: "data=not-json"), Options);

        Assert.AreEqual(400, result.Response.StatusCode);
        Assert.IsFalse(result.IsAuthenticated);
    }

    [TestMethod]
    public async Task HandleAsync_WhenVerificationTokenDoesNotMatch_Returns401()
    {
        WebhookHandleResult<KoFiWebhookEvent> result =
            await _handler.HandleAsync(Request(PayloadJson(token: "wrong-token")), Options);

        Assert.AreEqual(401, result.Response.StatusCode);
        Assert.IsFalse(result.IsAuthenticated);
        Assert.IsNull(result.Event);
    }

    [TestMethod]
    public async Task HandleAsync_WhenAmountIsNotParseable_Returns400ButIsAuthenticated()
    {
        WebhookHandleResult<KoFiWebhookEvent> result =
            await _handler.HandleAsync(Request(PayloadJson(amount: "not-a-number")), Options);

        Assert.AreEqual(400, result.Response.StatusCode);
        Assert.IsTrue(result.IsAuthenticated);
        Assert.IsFalse(result.IsKnownEvent);
    }

    [TestMethod]
    public async Task HandleAsync_WhenPayloadIsAValidDonation_ReturnsDonationEvent()
    {
        WebhookHandleResult<KoFiWebhookEvent> result =
            await _handler.HandleAsync(Request(), Options);

        Assert.AreEqual(200, result.Response.StatusCode);
        Assert.IsTrue(result.IsAuthenticated);
        Assert.IsTrue(result.IsKnownEvent);

        _ = Assert.IsInstanceOfType<KoFiDonationEvent>(result.Event);
        var donation = (KoFiDonationEvent)result.Event;
        Assert.AreEqual(5.00m, donation.Amount);
        Assert.AreEqual("USD", donation.Currency);
        Assert.AreEqual("Jane", donation.FromName);
        Assert.AreEqual("txn-1", donation.KofiTransactionId);
    }

    [TestMethod]
    public async Task HandleAsync_WhenFirstSubscriptionPayment_ReturnsSubscriptionStarted()
    {
        WebhookHandleResult<KoFiWebhookEvent> result = await _handler.HandleAsync(
            Request(PayloadJson("Subscription", isSubscriptionPayment: true, isFirstSubscriptionPayment: true)),
            Options);

        _ = Assert.IsInstanceOfType<KoFiSubscriptionStartedEvent>(result.Event);
    }

    [TestMethod]
    public async Task HandleAsync_WhenRecurringSubscriptionPayment_ReturnsSubscriptionRenewed()
    {
        WebhookHandleResult<KoFiWebhookEvent> result = await _handler.HandleAsync(
            Request(PayloadJson("Subscription", isSubscriptionPayment: true)),
            Options);

        _ = Assert.IsInstanceOfType<KoFiSubscriptionRenewedEvent>(result.Event);
    }

    [TestMethod]
    [DataRow("Shop Order", typeof(KoFiShopOrderEvent))]
    [DataRow("Commission", typeof(KoFiCommissionEvent))]
    [DataRow("Referral", typeof(KoFiReferralEvent))]
    public async Task HandleAsync_MapsKnownPayloadTypes(string payloadType, Type expected)
    {
        WebhookHandleResult<KoFiWebhookEvent> result =
            await _handler.HandleAsync(Request(PayloadJson(payloadType)), Options);

        Assert.IsTrue(result.IsKnownEvent);
        Assert.IsInstanceOfType(result.Event, expected);
    }

    [TestMethod]
    public async Task HandleAsync_WhenPayloadTypeIsUnrecognised_ReturnsUnknownEventStill200()
    {
        WebhookHandleResult<KoFiWebhookEvent> result =
            await _handler.HandleAsync(Request(PayloadJson("Something New")), Options);

        Assert.AreEqual(200, result.Response.StatusCode);
        Assert.IsTrue(result.IsAuthenticated);
        Assert.IsFalse(result.IsKnownEvent);
        _ = Assert.IsInstanceOfType<KoFiUnknownWebhookEvent>(result.Event);
    }

    [TestMethod]
    public async Task HandleAsync_WhenCancelled_Throws()
    {
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => _handler.HandleAsync(Request(), Options, cts.Token));
    }

    [TestMethod]
    public async Task HandleAsync_WhenRequestIsNull_Throws()
        => await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => _handler.HandleAsync(null!, Options));

    [TestMethod]
    public async Task HandleAsync_WhenOptionsAreNull_Throws()
        => await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => _handler.HandleAsync(Request(), null!));
}
