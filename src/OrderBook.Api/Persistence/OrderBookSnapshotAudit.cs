namespace OrderBook.Api.Persistence;

/// <summary>
/// Records an acquired order book before it becomes available to clients.
/// The application inserts these records without updating or deleting them.
/// </summary>
public sealed class OrderBookSnapshotAudit
{
    /// <summary>
    /// Gets the snapshot identifier shared with HTTP and SignalR responses.
    /// </summary>
    public Guid SnapshotId { get; init; }

    /// <summary>
    /// Gets the trading pair represented by the snapshot.
    /// </summary>
    public required string Market { get; init; }

    /// <summary>
    /// Gets the UTC time when the API received the exchange response body.
    /// </summary>
    public DateTime AcquiredAtUtc { get; init; }

    /// <summary>
    /// Gets the complete exchange JSON, preserving price and quantity strings.
    /// </summary>
    public required string OrderBookJson { get; init; }
}
