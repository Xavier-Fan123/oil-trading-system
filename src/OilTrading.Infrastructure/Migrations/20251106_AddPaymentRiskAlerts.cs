using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OilTrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentRiskAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentRiskAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TradingPartnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AlertType = table.Column<int>(type: "INTEGER", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ResolvedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsResolved = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    DaysOverdue = table.Column<int>(type: "INTEGER", nullable: true),
                    DaysUntilDue = table.Column<int>(type: "INTEGER", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRiskAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRiskAlerts_TradingPartners_TradingPartnerId",
                        column: x => x.TradingPartnerId,
                        principalTable: "TradingPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRiskAlerts_TradingPartnerId",
                table: "PaymentRiskAlerts",
                column: "TradingPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRiskAlerts_TradingPartnerId_IsResolved",
                table: "PaymentRiskAlerts",
                columns: new[] { "TradingPartnerId", "IsResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRiskAlerts_Severity",
                table: "PaymentRiskAlerts",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRiskAlerts_AlertType",
                table: "PaymentRiskAlerts",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRiskAlerts_CreatedDate",
                table: "PaymentRiskAlerts",
                column: "CreatedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentRiskAlerts");
        }
    }
}
