using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderBook.Api.Contracts;
using OrderBook.Api.Persistence;

namespace OrderBook.Api.IntegrationTests.Persistence;

public sealed class OrderBookAuditWriterTests
{
    [Fact]
    public async Task SaveAsync_PersistsSnapshotAndRestoresUtcTimestamp()
    {
        // SQLite database exists only while its connection stays open.
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        DbContextOptions<AuditDbContext> options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseSqlite(connection)
            .Options;

        using JsonDocument orderBook = JsonDocument.Parse("""
            {
                "timestamp": "1791201600",
                "bids": [["75000.10", "0.12345678"]],
                "asks": [["75010.20", "0.20000000"]]
            }
            """);

        var snapshot = new OrderBookResponse(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "BTC/EUR",
            new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc).AddTicks(1234567),
            orderBook.RootElement);

        await using (var writeContext = new AuditDbContext(options))
        {
            // Apply the same migrations used by the API instead of creating a separate test schema.
            await writeContext.Database.MigrateAsync();

            // save through audit writer
            var writer = new OrderBookAuditWriter(writeContext);
            await writer.SaveAsync(snapshot, CancellationToken.None);
        }

        // read saved row from the db
        await using var readContext = new AuditDbContext(options);
        OrderBookSnapshotAudit saved = Assert.Single(
            await readContext.OrderBookSnapshots.AsNoTracking().ToListAsync());

        Assert.Equal(snapshot.SnapshotId, saved.SnapshotId);
        Assert.Equal(snapshot.Market, saved.Market);
        Assert.Equal(snapshot.AcquiredAtUtc, saved.AcquiredAtUtc);
        Assert.Equal(DateTimeKind.Utc, saved.AcquiredAtUtc.Kind);
        Assert.Equal(snapshot.OrderBook.GetRawText(), saved.OrderBookJson);
    }
}