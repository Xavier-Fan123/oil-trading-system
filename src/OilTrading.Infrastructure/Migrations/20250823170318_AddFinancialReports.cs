using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OilTrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TradingPartnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReportStartDate = table.Column<DateTime>(type: "date", nullable: false),
                    ReportEndDate = table.Column<DateTime>(type: "date", nullable: false),
                    TotalAssets = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    TotalLiabilities = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    NetAssets = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    CurrentAssets = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    CurrentLiabilities = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Revenue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    NetProfit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    OperatingCashFlow = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialReports_TradingPartners_TradingPartnerId",
                        column: x => x.TradingPartnerId,
                        principalTable: "TradingPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReports_ReportStartDate",
                table: "FinancialReports",
                column: "ReportStartDate");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReports_TradingPartnerId",
                table: "FinancialReports",
                column: "TradingPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReports_TradingPartnerId_ReportStartDate",
                table: "FinancialReports",
                columns: new[] { "TradingPartnerId", "ReportStartDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialReports");
        }
    }
}
