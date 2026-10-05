using System.Text.Json;
using OrderBook.Api.Contracts;
using OrderBook.Api.OrderBooks;

namespace OrderBook.Api.UnitTests.OrderBooks;

public sealed class OrderBookSnapshotStoreTests
{
    [Fact]
    public void Publish_ReplacesLatestSnapshotAndIncrementsSequence()
    {
        // Arrange: two acquisitions with distinct identities and acquisition times.
        var store = new OrderBookSnapshotStore();
        using JsonDocument orderBook = JsonDocument.Parse("""
            {
                "timestamp": "1791201600",
                "bids": [["75000.00", "0.10000000"]],
                "asks": [["75010.00", "0.20000000"]]
            }
            """);

        var firstSnapshot = new OrderBookResponse(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "BTC/EUR",
            new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc),
            orderBook.RootElement);

        OrderBookResponse secondSnapshot = firstSnapshot with
        {
            SnapshotId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            AcquiredAtUtc = firstSnapshot.AcquiredAtUtc.AddSeconds(1)
        };

        // Act: publish both acquisitions in order.
        OrderBookResponse firstPublished = store.Publish(firstSnapshot);
        OrderBookResponse secondPublished = store.Publish(secondSnapshot);

        // Assert: readers see the second acquisition with the next publication sequence.
        Assert.Equal(1, firstPublished.Sequence);
        Assert.Equal(2, secondPublished.Sequence);
        Assert.Equal(secondSnapshot with { Sequence = 2 }, secondPublished);
        Assert.Equal(secondPublished, store.GetLatest());
        Assert.Equal(0, secondSnapshot.Sequence);
    }
}
