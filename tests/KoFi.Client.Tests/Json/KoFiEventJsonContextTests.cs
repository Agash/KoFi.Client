using System.Text.Json;
using KoFi.Client.Events;
using KoFi.Client.Json;
using KoFi.Client.Models;

namespace KoFi.Client.Tests.Json;

[TestClass]
public sealed class KoFiEventJsonContextTests
{
    [TestMethod]
    public void Serialize_ShopOrder_RoundTripsThroughTheContext()
    {
        KoFiShopOrderEvent order = new()
        {
            RawType = "Shop Order",
            MessageId = "msg-1",
            Timestamp = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero),
            IsPublic = true,
            FromName = "Ava",
            Amount = 12.50m,
            Currency = "EUR",
            KofiTransactionId = "tx-1",
            ShopItems =
            [
                new KoFiShopItem
                {
                    DirectLinkCode = "abc",
                    VariationName = "Large",
                    Quantity = 2,
                },
            ],
        };

        string json = JsonSerializer.Serialize(
            order,
            KoFiEventJsonContext.Default.KoFiShopOrderEvent
        );
        KoFiShopOrderEvent? read = JsonSerializer.Deserialize(
            json,
            KoFiEventJsonContext.Default.KoFiShopOrderEvent
        );

        Assert.IsNotNull(read);
        Assert.AreEqual(12.50m, read.Amount);
        Assert.AreEqual("EUR", read.Currency);
        Assert.AreEqual("abc", read.ShopItems?[0].DirectLinkCode);
        Assert.AreEqual(2, read.ShopItems?[0].Quantity);
    }
}
