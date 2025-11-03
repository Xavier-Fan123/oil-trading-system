using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OilTrading.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseAndSalesSettlementTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractSettlements_PurchaseContracts",
                table: "ContractSettlements");

            migrationBuilder.DropForeignKey(
                name: "FK_ContractSettlements_SalesContracts",
                table: "ContractSettlements");

            migrationBuilder.CreateTable(
                name: "PurchaseSettlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PurchaseContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ExternalContractNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DocumentNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DocumentType = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualQuantityMT = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    ActualQuantityBBL = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    CalculationQuantityMT = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    CalculationQuantityBBL = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    QuantityCalculationNote = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    BenchmarkPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    BenchmarkPriceFormula = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PricingStartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PricingEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BenchmarkPriceCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "USD"),
                    BenchmarkAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AdjustmentAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CargoValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalCharges = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalSettlementAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SettlementCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "USD"),
                    ExchangeRate = table.Column<decimal>(type: "TEXT", precision: 10, scale: 6, nullable: true),
                    ExchangeRateNote = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsFinalized = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FinalizedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinalizedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false, defaultValueSql: "X'00000000000000000000000000000001'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseSettlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseSettlements_PurchaseContracts",
                        column: x => x.PurchaseContractId,
                        principalTable: "PurchaseContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesSettlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SalesContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ExternalContractNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DocumentNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DocumentType = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualQuantityMT = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    ActualQuantityBBL = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    CalculationQuantityMT = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    CalculationQuantityBBL = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    QuantityCalculationNote = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    BenchmarkPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    BenchmarkPriceFormula = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PricingStartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PricingEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BenchmarkPriceCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "USD"),
                    BenchmarkAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AdjustmentAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CargoValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalCharges = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalSettlementAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SettlementCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "USD"),
                    ExchangeRate = table.Column<decimal>(type: "TEXT", precision: 10, scale: 6, nullable: true),
                    ExchangeRateNote = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsFinalized = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FinalizedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinalizedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false, defaultValueSql: "X'00000000000000000000000000000001'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesSettlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesSettlements_SalesContracts",
                        column: x => x.SalesContractId,
                        principalTable: "SalesContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSettlements_CreatedDate",
                table: "PurchaseSettlements",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSettlements_DocumentDate",
                table: "PurchaseSettlements",
                column: "DocumentDate");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSettlements_DocumentNumber",
                table: "PurchaseSettlements",
                column: "DocumentNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSettlements_ExternalContractNumber",
                table: "PurchaseSettlements",
                column: "ExternalContractNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSettlements_IsFinalized",
                table: "PurchaseSettlements",
                column: "IsFinalized");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSettlements_IsFinalized_CreatedDate",
                table: "PurchaseSettlements",
                columns: new[] { "IsFinalized", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSettlements_PurchaseContractId",
                table: "PurchaseSettlements",
                column: "PurchaseContractId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSettlements_PurchaseContractId_Status",
                table: "PurchaseSettlements",
                columns: new[] { "PurchaseContractId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSettlements_Status",
                table: "PurchaseSettlements",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSettlements_Status_CreatedDate",
                table: "PurchaseSettlements",
                columns: new[] { "Status", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesSettlements_CreatedDate",
                table: "SalesSettlements",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_SalesSettlements_DocumentDate",
                table: "SalesSettlements",
                column: "DocumentDate");

            migrationBuilder.CreateIndex(
                name: "IX_SalesSettlements_DocumentNumber",
                table: "SalesSettlements",
                column: "DocumentNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SalesSettlements_ExternalContractNumber",
                table: "SalesSettlements",
                column: "ExternalContractNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SalesSettlements_IsFinalized",
                table: "SalesSettlements",
                column: "IsFinalized");

            migrationBuilder.CreateIndex(
                name: "IX_SalesSettlements_IsFinalized_CreatedDate",
                table: "SalesSettlements",
                columns: new[] { "IsFinalized", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesSettlements_SalesContractId",
                table: "SalesSettlements",
                column: "SalesContractId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesSettlements_SalesContractId_Status",
                table: "SalesSettlements",
                columns: new[] { "SalesContractId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesSettlements_Status",
                table: "SalesSettlements",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SalesSettlements_Status_CreatedDate",
                table: "SalesSettlements",
                columns: new[] { "Status", "CreatedDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_SettlementCharges_PurchaseSettlements",
                table: "SettlementCharges",
                column: "SettlementId",
                principalTable: "PurchaseSettlements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SettlementCharges_SalesSettlements",
                table: "SettlementCharges",
                column: "SettlementId",
                principalTable: "SalesSettlements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SettlementCharges_PurchaseSettlements",
                table: "SettlementCharges");

            migrationBuilder.DropForeignKey(
                name: "FK_SettlementCharges_SalesSettlements",
                table: "SettlementCharges");

            migrationBuilder.DropTable(
                name: "PurchaseSettlements");

            migrationBuilder.DropTable(
                name: "SalesSettlements");

            migrationBuilder.AddForeignKey(
                name: "FK_ContractSettlements_PurchaseContracts",
                table: "ContractSettlements",
                column: "ContractId",
                principalTable: "PurchaseContracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractSettlements_SalesContracts",
                table: "ContractSettlements",
                column: "ContractId",
                principalTable: "SalesContracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
