using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OilTrading.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_RowVersion_Config_ContractSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "ContractSettlements",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "X'00000000000000000000000000000001'",
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldRowVersion: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_ExternalContractNumber",
                table: "SalesContracts",
                column: "ExternalContractNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesContracts_ExternalContractNumber",
                table: "SalesContracts");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "ContractSettlements",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldRowVersion: true,
                oldDefaultValueSql: "X'00000000000000000000000000000001'");
        }
    }
}
