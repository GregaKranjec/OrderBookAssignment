using Microsoft.AspNetCore.SignalR;
using OrderBook.Api.Contracts;
using OrderBook.Api.OrderBooks;

namespace OrderBook.Api.Hubs;

/// <summary>
/// Delivers the latest snapshot on connection and subsequent worker updates at /hubs/order-book.
/// Connecting clients never initiate an exchange acquisition.
/// </summary>
public sealed class OrderBookHub : Hub
{
    /// <summary>
    /// The client event containing a complete OrderBookResponse snapshot.
    /// </summary>
    public const string SnapshotEvent = "OrderBookUpdated";

    private readonly OrderBookSnapshotStore _snapshots;

    /// <summary>
    /// Creates a hub using the shared snapshot store.
    /// </summary>
    /// <param name="snapshots">The store populated by the background worker.</param>
    public OrderBookHub(OrderBookSnapshotStore snapshots)
    {
        _snapshots = snapshots;
    }

    /// <summary>
    /// Sends the latest available snapshot to a newly connected or reconnected client.
    /// If no snapshot exists, the client receives the next successful worker broadcast.
    /// Initial delivery can race with a broadcast; clients must discard older or duplicate sequences.
    /// </summary>
    /// <returns>A task representing initial snapshot delivery and connection initialization.</returns>
    public override async Task OnConnectedAsync()
    {
        OrderBookResponse? snapshot = _snapshots.GetLatest();

        if (snapshot is not null)
        {
            using CancellationTokenSource deliveryCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(Context.ConnectionAborted);
            deliveryCancellation.CancelAfter(TimeSpan.FromSeconds(5));

            await Clients.Caller.SendAsync(SnapshotEvent, snapshot, deliveryCancellation.Token);
        }

        await base.OnConnectedAsync();
    }
}
