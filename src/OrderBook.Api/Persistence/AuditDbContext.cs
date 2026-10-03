using Microsoft.EntityFrameworkCore;

namespace OrderBook.Api.Persistence;

/// <summary>
/// Provides EF Core access to persisted order book snapshots.
/// Each acquisition cycle resolves and disposes its own scoped context.
/// </summary>
public sealed class AuditDbContext : DbContext
{
    /// <summary>
    /// Creates a context using the SQLite database.
    /// </summary>
    /// <param name="options">The database provider and connection settings.</param>
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets the audit records saved before snapshot publication.
    /// </summary>
    public DbSet<OrderBookSnapshotAudit> OrderBookSnapshots => Set<OrderBookSnapshotAudit>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var snapshots = modelBuilder.Entity<OrderBookSnapshotAudit>();
        snapshots.ToTable("OrderBookSnapshots");
        snapshots.HasKey(snapshot => snapshot.SnapshotId);
        snapshots.Property(snapshot => snapshot.SnapshotId).ValueGeneratedNever();
        snapshots.Property(snapshot => snapshot.Market).IsRequired();
        snapshots.Property(snapshot => snapshot.OrderBookJson).IsRequired();

        // SQLite does not preserve DateTime.Kind. Normalize writes and restore UTC on reads.
        snapshots.Property(snapshot => snapshot.AcquiredAtUtc).HasConversion(
            value => value.ToUniversalTime(),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
