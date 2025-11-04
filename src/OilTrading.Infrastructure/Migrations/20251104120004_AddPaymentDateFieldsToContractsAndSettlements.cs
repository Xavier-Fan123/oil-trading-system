using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OilTrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentDateFieldsToContractsAndSettlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add EstimatedPaymentDate to PurchaseContracts
            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedPaymentDate",
                table: "PurchaseContracts",
                type: "TEXT",
                nullable: true,
                comment: "Estimated payment date - filled by user when creating contract based on payment terms");

            // Add EstimatedPaymentDate to SalesContracts
            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedPaymentDate",
                table: "SalesContracts",
                type: "TEXT",
                nullable: true,
                comment: "Estimated collection date - filled by user when creating contract based on payment terms");

            // Add ActualPayableDueDate to ContractSettlements
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualPayableDueDate",
                table: "ContractSettlements",
                type: "TEXT",
                nullable: true,
                comment: "Actual payable/receivable due date - filled by user when creating/editing settlement");

            // Add ActualPaymentDate to ContractSettlements
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualPaymentDate",
                table: "ContractSettlements",
                type: "TEXT",
                nullable: true,
                comment: "Actual payment/collection date - filled by finance department after payment is made");

            // Add ActualPayableDueDate to PurchaseSettlements (inherited from ContractSettlements)
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualPayableDueDate",
                table: "PurchaseSettlements",
                type: "TEXT",
                nullable: true,
                comment: "Actual payment due date for purchases");

            // Add ActualPaymentDate to PurchaseSettlements (inherited from ContractSettlements)
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualPaymentDate",
                table: "PurchaseSettlements",
                type: "TEXT",
                nullable: true,
                comment: "Actual payment date for purchases");

            // Add ActualPayableDueDate to SalesSettlements (inherited from ContractSettlements)
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualPayableDueDate",
                table: "SalesSettlements",
                type: "TEXT",
                nullable: true,
                comment: "Actual collection due date for sales");

            // Add ActualPaymentDate to SalesSettlements (inherited from ContractSettlements)
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualPaymentDate",
                table: "SalesSettlements",
                type: "TEXT",
                nullable: true,
                comment: "Actual collection date for sales");

            // Create indexes for better query performance on payment date fields
            migrationBuilder.CreateIndex(
                name: "IX_ContractSettlements_ActualPayableDueDate",
                table: "ContractSettlements",
                column: "ActualPayableDueDate");

            migrationBuilder.CreateIndex(
                name: "IX_ContractSettlements_ActualPaymentDate",
                table: "ContractSettlements",
                column: "ActualPaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_EstimatedPaymentDate",
                table: "PurchaseContracts",
                column: "EstimatedPaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_EstimatedPaymentDate",
                table: "SalesContracts",
                column: "EstimatedPaymentDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_SalesContracts_EstimatedPaymentDate",
                table: "SalesContracts");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseContracts_EstimatedPaymentDate",
                table: "PurchaseContracts");

            migrationBuilder.DropIndex(
                name: "IX_ContractSettlements_ActualPaymentDate",
                table: "ContractSettlements");

            migrationBuilder.DropIndex(
                name: "IX_ContractSettlements_ActualPayableDueDate",
                table: "ContractSettlements");

            // Drop columns from SalesSettlements
            migrationBuilder.DropColumn(
                name: "ActualPaymentDate",
                table: "SalesSettlements");

            migrationBuilder.DropColumn(
                name: "ActualPayableDueDate",
                table: "SalesSettlements");

            // Drop columns from PurchaseSettlements
            migrationBuilder.DropColumn(
                name: "ActualPaymentDate",
                table: "PurchaseSettlements");

            migrationBuilder.DropColumn(
                name: "ActualPayableDueDate",
                table: "PurchaseSettlements");

            // Drop columns from ContractSettlements
            migrationBuilder.DropColumn(
                name: "ActualPaymentDate",
                table: "ContractSettlements");

            migrationBuilder.DropColumn(
                name: "ActualPayableDueDate",
                table: "ContractSettlements");

            // Drop columns from SalesContracts
            migrationBuilder.DropColumn(
                name: "EstimatedPaymentDate",
                table: "SalesContracts");

            // Drop columns from PurchaseContracts
            migrationBuilder.DropColumn(
                name: "EstimatedPaymentDate",
                table: "PurchaseContracts");
        }
    }
}
