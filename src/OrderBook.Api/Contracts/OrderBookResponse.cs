using System.Text.Json;

namespace OrderBook.Api.Contracts;

/// <summary>
/// An acquired order book snapshot and the metadata recorded by this API.
/// </summary>
/// <param name="SnapshotId">The unique identifier shared by HTTP and SignalR deliveries of this snapshot.</param>
/// <param name="Market">The trading pair represented by the snapshot.</param>
/// <param name="AcquiredAtUtc">The UTC time when the backend received the exchange response body.</param>
/// <param name="OrderBook">The exchange JSON, preserving its price and quantity strings.</param>
public sealed record OrderBookResponse(
    Guid SnapshotId,
    string Market,
    DateTime AcquiredAtUtc,
    JsonElement OrderBook)
{
    /// <summary>
    /// Gets the publication sequence within the current API process.
    /// Consumers can ignore older or duplicate deliveries and must reset comparison when reconnecting.
    /// Zero means the acquired snapshot has not yet been published.
    /// </summary>
    public long Sequence { get; init; }
}
