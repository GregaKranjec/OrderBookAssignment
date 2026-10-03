using OrderBook.Api.Contracts;

namespace OrderBook.Api.OrderBooks;

/// <summary>
/// Shares the latest immutable snapshot between the worker, HTTP endpoint, and SignalR connections.
/// The store and its publication sequence are reset when the API process restarts.
/// </summary>
public sealed class OrderBookSnapshotStore
{
    // Readers and the worker use the same lock to access the latest snapshot safely.
    private readonly Lock _snapshotLock = new();
    private OrderBookResponse? _latest;
    private long _sequence;

    /// <summary>
    /// Reads the latest published snapshot without starting an exchange request.
    /// </summary>
    /// <returns>The latest snapshot, or null before the first successful publication.</returns>
    public OrderBookResponse? GetLatest()
    {
        lock (_snapshotLock)
        {
            return _latest;
        }
    }

    /// <summary>
    /// Assigns a publication sequence and atomically replaces the latest snapshot.
    /// Audit persistence must succeed before this method is called.
    /// </summary>
    /// <param name="snapshot">The successfully acquired, validated, and audited snapshot.</param>
    /// <returns>The published snapshot containing its sequence.</returns>
    public OrderBookResponse Publish(OrderBookResponse snapshot)
    {
        lock (_snapshotLock)
        {
            _latest = snapshot with { Sequence = ++_sequence };
            return _latest;
        }
    }
}
