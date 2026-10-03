using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderBook.Api.Contracts;

namespace OrderBook.Api.Persistence;

/// <summary>
/// Maps acquired snapshots to audit records and saves them through a scoped EF Core context.
/// </summary>
public sealed class OrderBookAuditWriter
{
    private readonly AuditDbContext _database;

    /// <summary>
    /// Creates a writer using the current acquisition cycle's database context.
    /// </summary>
    /// <param name="database">The scoped context used to insert audit records.</param>
    public OrderBookAuditWriter(AuditDbContext database)
    {
        _database = database;
    }

    /// <summary>
    /// Saves the snapshot's identifier, market, acquisition time, and complete exchange JSON.
    /// </summary>
    /// <param name="snapshot">The acquired and validated snapshot to audit before publication.</param>
    /// <param name="cancellationToken">Cancels the save when the API is shutting down.</param>
    /// <returns>A task that completes after the audit record has been saved.</returns>
    /// <exception cref="DbUpdateException">EF Core cannot save the audit record.</exception>
    /// <exception cref="SqliteException">A SQLite operation fails.</exception>
    /// <exception cref="OperationCanceledException">The save is canceled.</exception>
    public async Task SaveAsync(OrderBookResponse snapshot, CancellationToken cancellationToken)
    {
        _database.OrderBookSnapshots.Add(new OrderBookSnapshotAudit
        {
            SnapshotId = snapshot.SnapshotId,
            Market = snapshot.Market,
            AcquiredAtUtc = snapshot.AcquiredAtUtc,
            OrderBookJson = snapshot.OrderBook.GetRawText()
        });

        await _database.SaveChangesAsync(cancellationToken);
    }
}
