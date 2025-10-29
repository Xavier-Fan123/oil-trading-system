using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OilTrading.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_RowVersion_Config_ShippingOperation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "ShippingOperations",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[] { 0 },
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldRowVersion: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "ShippingOperations",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldRowVersion: true,
                oldDefaultValue: new byte[] { 0 });
        }
    }
}
