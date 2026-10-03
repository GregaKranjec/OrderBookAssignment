using System.Text.Json;
using OrderBook.Api.Contracts;

namespace OrderBook.Api.Exchanges.Bitstamp;

/// <summary>
/// Acquires grouped BTC/EUR order book snapshots from Bitstamp's public REST API.
/// </summary>
public sealed class BitstampOrderBookClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Creates an exchange client using the configured HTTP client.
    /// </summary>
    /// <param name="httpClient">The HTTP client containing the exchange base address and timeout.</param>
    public BitstampOrderBookClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Requests one BTC/EUR snapshot, validates its basic JSON structure, and records acquisition time.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the request and response parsing.</param>
    /// <returns>The exchange JSON with the market identifier and UTC acquisition time.</returns>
    /// <exception cref="HttpRequestException">The exchange request fails or returns an unsuccessful HTTP status.</exception>
    /// <exception cref="JsonException">The response is not valid JSON or has an unexpected structure.</exception>
    /// <exception cref="OperationCanceledException">The operation is canceled or the HTTP request times out.</exception>
    public async Task<OrderBookResponse> GetOrderBookAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(
            "api/v2/order_book/btceur/?group=1",
            cancellationToken);

        response.EnsureSuccessStatusCode();
        DateTime acquiredAtUtc = DateTime.UtcNow;

        JsonElement orderBook = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: cancellationToken);

        // Validate the response against the expected shape
        if (orderBook.ValueKind != JsonValueKind.Object
            || !orderBook.TryGetProperty("timestamp", out JsonElement timestamp)
            || timestamp.ValueKind != JsonValueKind.String
            || !HasArrayProperty(orderBook, "bids")
            || !HasArrayProperty(orderBook, "asks"))
        {
            throw new JsonException("Bitstamp returned an unexpected order book structure.");
        }

        return new OrderBookResponse(Guid.NewGuid(), "BTC/EUR", acquiredAtUtc, orderBook);
    }

    private static bool HasArrayProperty(JsonElement orderBook, string propertyName)
    {
        return orderBook.TryGetProperty(propertyName, out JsonElement property)
            && property.ValueKind == JsonValueKind.Array;
    }
}
