using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderBook.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAuditSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderBookSnapshots",
                columns: table => new
                {
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Market = table.Column<string>(type: "TEXT", nullable: false),
                    AcquiredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OrderBookJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderBookSnapshots", x => x.SnapshotId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderBookSnapshots");
        }
    }
}
