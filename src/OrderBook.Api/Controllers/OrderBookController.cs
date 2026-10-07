using Microsoft.AspNetCore.Mvc;
using OrderBook.Api.Contracts;
using OrderBook.Api.OrderBooks;

namespace OrderBook.Api.Controllers;

/// <summary>
/// Provides read-only access to the latest published snapshot for testing and inspection.
/// </summary>
[ApiController]
[Route("api/order-book")]
public sealed class OrderBookController : ControllerBase
{
    private readonly OrderBookSnapshotStore _snapshots;

    /// <summary>
    /// Creates a controller using the shared snapshot store.
    /// </summary>
    /// <param name="snapshots">The store populated by the background acquisition worker.</param>
    public OrderBookController(OrderBookSnapshotStore snapshots)
    {
        _snapshots = snapshots;
    }

    /// <summary>
    /// Reads the latest published BTC/EUR snapshot for testing and inspection.
    /// </summary>
    /// <remarks>
    /// This read-only endpoint never starts an exchange request or changes the acquisition schedule.
    /// The frontend receives initial snapshots and live updates through SignalR at /hubs/order-book.
    /// HTTP caching is disabled. The last successful snapshot remains available during exchange or audit save failures;
    /// its acquisition time must be checked for freshness. No snapshot is available until the worker's
    /// first successful acquisition and audit save. Purchase estimates are calculated in the frontend from the snapshot's asks.
    /// </remarks>
    /// <returns>The latest snapshot, or Problem Details when no snapshot is available.</returns>
    /// <response code="200">Returns the latest snapshot, which may be stale during an exchange outage.</response>
    /// <response code="503">No snapshot has been acquired and audited since the API started.</response>
    [HttpGet(Name = "GetOrderBook")]
    [ProducesResponseType(typeof(OrderBookResponse), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public ActionResult<OrderBookResponse> GetOrderBook()
    {
        // prevent http cache
        Response.Headers.CacheControl = "no-store";

        OrderBookResponse? snapshot = _snapshots.GetLatest();

        if (snapshot is null)
        {
            return Problem(
                title: "Order book unavailable",
                detail: "No audited order book snapshot is available yet. Try again shortly.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Ok(snapshot);
    }
}
