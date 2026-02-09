using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OilTrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddXGroupMarketDataFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add X-group format support fields to MarketPrices table

            // ContractSpecificationId: "SG380 Apr26" (futures) or "SG380" (spot)
            migrationBuilder.AddColumn<string>(
                name: "ContractSpecificationId",
                table: "MarketPrices",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            // SettlementPrice: Futures settlement price (null for spot rows)
            migrationBuilder.AddColumn<decimal>(
                name: "SettlementPrice",
                table: "MarketPrices",
                type: "TEXT",
                precision: 18,
                scale: 4,
                nullable: true);

            // SpotPrice: Spot market price (null for futures rows)
            migrationBuilder.AddColumn<decimal>(
                name: "SpotPrice",
                table: "MarketPrices",
                type: "TEXT",
                precision: 18,
                scale: 4,
                nullable: true);

            // Create index for ContractSpecificationId queries
            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_ContractSpecId_PriceDate",
                table: "MarketPrices",
                columns: new[] { "ContractSpecificationId", "PriceDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop index first
            migrationBuilder.DropIndex(
                name: "IX_MarketPrices_ContractSpecId_PriceDate",
                table: "MarketPrices");

            // Drop columns
            migrationBuilder.DropColumn(
                name: "ContractSpecificationId",
                table: "MarketPrices");

            migrationBuilder.DropColumn(
                name: "SettlementPrice",
                table: "MarketPrices");

            migrationBuilder.DropColumn(
                name: "SpotPrice",
                table: "MarketPrices");
        }
    }
}
