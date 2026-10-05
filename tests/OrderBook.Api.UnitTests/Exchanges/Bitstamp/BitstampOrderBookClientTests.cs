using System.Net;
using System.Text;
using System.Text.Json;
using OrderBook.Api.Contracts;
using OrderBook.Api.Exchanges.Bitstamp;

namespace OrderBook.Api.UnitTests.Exchanges.Bitstamp;

public sealed class BitstampOrderBookClientTests
{
    [Fact]
    public async Task GetOrderBookAsync_ValidResponse_PreservesExchangeJsonAndAddsMetadata()
    {
        // Arrange: price and quantity strings must survive acquisition unchanged.
        // Timestamp - 5. 10. 2026, 12:00:00 UTC
        const string responseBody = """
            {
                "timestamp": "1791201600",
                "bids": [["75000.10", "0.12345678"]],
                "asks": [["75010.20", "0.20000000"]]
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(responseBody))
        {
            BaseAddress = new Uri("https://www.bitstamp.net/")
        };
        var client = new BitstampOrderBookClient(httpClient);

        OrderBookResponse snapshot = await client.GetOrderBookAsync(CancellationToken.None);

        // Payload is preserved
        Assert.Equal(responseBody, snapshot.OrderBook.GetRawText());
        Assert.Equal("BTC/EUR", snapshot.Market);
        Assert.NotEqual(Guid.Empty, snapshot.SnapshotId);
        Assert.NotEqual(default, snapshot.AcquiredAtUtc);
        Assert.Equal(DateTimeKind.Utc, snapshot.AcquiredAtUtc.Kind);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("[]")]
    [InlineData("""{"timestamp":123,"bids":[],"asks":[]}""")]
    [InlineData("""{"timestamp":"1791201600","bids":{},"asks":[]}""")]
    [InlineData("""{"timestamp":"1791201600","bids":[]}""")]
    public async Task GetOrderBookAsync_InvalidResponse_ThrowsJsonException(string responseBody)
    {
        // Arrange: HTTP succeeds, but the response is invalid
        using var httpClient = new HttpClient(new StubHttpMessageHandler(responseBody))
        {
            BaseAddress = new Uri("https://www.bitstamp.net/")
        };
        var client = new BitstampOrderBookClient(httpClient);

        // response should be rejected
        await Assert.ThrowsAsync<JsonException>(() => client.GetOrderBookAsync(CancellationToken.None));
    }

    // Returns the JSON supplied by the test instead of making a real request to Bitstamp.
    private sealed class StubHttpMessageHandler(string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
        }
    }
}
