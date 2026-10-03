using System.ComponentModel.DataAnnotations;

namespace OrderBook.Api.OrderBooks;

/// <summary>
/// Configures the server-owned order book acquisition schedule.
/// </summary>
public sealed class OrderBookPollingOptions
{
    /// <summary>
    /// Gets or sets the minimum interval between successful acquisition cycle starts, in milliseconds.
    /// Slow cycles run sequentially without attempting to catch up on missed intervals.
    /// </summary>
    [Range(100, 60000)]
    public int PollingIntervalMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the upper limit for exponential acquisition error backoff, in milliseconds.
    /// </summary>
    [Range(100, 300000)]
    public int MaxRetryDelayMilliseconds { get; set; } = 30000;
}
