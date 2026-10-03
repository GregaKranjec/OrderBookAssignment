using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using OrderBook.Api.Contracts;
using OrderBook.Api.Exchanges.Bitstamp;
using OrderBook.Api.Hubs;

namespace OrderBook.Api.OrderBooks;

/// <summary>
/// Acquires snapshots serially, updates the shared store, and broadcasts complete snapshots.
/// </summary>
public sealed class OrderBookWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OrderBookSnapshotStore _snapshots;
    private readonly IHubContext<OrderBookHub> _hub;
    private readonly ILogger<OrderBookWorker> _logger;

    // intervals
    private readonly TimeSpan _pollingInterval;
    private readonly TimeSpan _maxRetryDelay;

    /// <summary>
    /// Creates the acquisition worker
    /// </summary>
    /// <param name="scopeFactory">Creates an acquisition scope for the exchange client and future audit services.</param>
    /// <param name="snapshots">Stores the latest published snapshot.</param>
    /// <param name="hub">Broadcasts snapshots to connected SignalR clients.</param>
    /// <param name="options">Configures polling and retry delays.</param>
    /// <param name="logger">Records acquisition and delivery failures.</param>
    public OrderBookWorker(
        IServiceScopeFactory scopeFactory,
        OrderBookSnapshotStore snapshots,
        IHubContext<OrderBookHub> hub,
        IOptions<OrderBookPollingOptions> options,
        ILogger<OrderBookWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _snapshots = snapshots;
        _hub = hub;
        _logger = logger;
        _pollingInterval = TimeSpan.FromMilliseconds(options.Value.PollingIntervalMilliseconds);
        _maxRetryDelay = TimeSpan.FromMilliseconds(options.Value.MaxRetryDelayMilliseconds);
    }

    /// <summary>
    /// Acquires immediately, then repeats at the configured interval with exponential error backoff.
    /// Shutdown cancels both in-flight work and scheduled delays.
    /// </summary>
    /// <param name="stoppingToken">Canceled when the API is shutting down.</param>
    /// <returns>A task representing the worker lifetime.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan retryDelay = _pollingInterval;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                long cycleStarted = Stopwatch.GetTimestamp();
                bool acquired = await AcquireAndPublishAsync(stoppingToken);
                TimeSpan delay;

                if (acquired)
                {
                    // poll again on interval if successful
                    retryDelay = _pollingInterval;
                    TimeSpan remaining = _pollingInterval - Stopwatch.GetElapsedTime(cycleStarted);
                    delay = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
                }
                else
                {
                    // retry after delay if unsuccessful
                    delay = retryDelay;
                    retryDelay = TimeSpan.FromMilliseconds(Math.Min(
                        retryDelay.TotalMilliseconds * 2,
                        _maxRetryDelay.TotalMilliseconds));
                }

                await Task.Delay(delay, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Cancellation is the normal shutdown path - no error thrown
        }
    }

    private async Task<bool> AcquireAndPublishAsync(CancellationToken stoppingToken)
    {
        OrderBookResponse snapshot;

        try
        {
            // The worker lives for the entire API's lifetime, so each acquisition gets its own scope.
            // The future EF Core DbContext must be resolved from this scope, rather than injected
            // into the worker, to keeping it scoped to one cycle and disposing it when this block ends.
            using IServiceScope scope = _scopeFactory.CreateScope();
            BitstampOrderBookClient client = scope.ServiceProvider.GetRequiredService<BitstampOrderBookClient>();
            snapshot = await client.GetOrderBookAsync(stoppingToken);

            // TODO Audit logic will be added here using this acquisition scope.
        }
        catch (Exception exception) when (
            !stoppingToken.IsCancellationRequested &&
            exception is HttpRequestException or JsonException or OperationCanceledException)
        {
            _logger.LogWarning(exception, "Bitstamp acquisition failed. Retaining the last snapshot.");
            return false;
        }

        stoppingToken.ThrowIfCancellationRequested();

        OrderBookResponse published = _snapshots.Publish(snapshot);

        // 5-second delivery timeout (does not stop the worker)
        using CancellationTokenSource deliveryCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        deliveryCancellation.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            await _hub.Clients.All.SendAsync(OrderBookHub.SnapshotEvent, published, deliveryCancellation.Token);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception,
                "SignalR delivery failed for snapshot {SnapshotId}. The snapshot remains available through HTTP.",
                published.SnapshotId);
        }

        return true;
    }
}
