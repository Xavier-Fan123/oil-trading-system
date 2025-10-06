using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OilTrading.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Migration to add enhanced trading features including:
    /// - Settlement entities (Settlement, Payment, SettlementAdjustment)
    /// - Inventory reservation system (InventoryReservation)
    /// - Trade chain tracking (TradeChain with operations and events)
    /// - Enhanced audit and transaction tracking
    /// </summary>
    public partial class AddEnhancedTradingFeatures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create Settlements table
            migrationBuilder.CreateTable(
                name: "Settlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentTerms = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProcessedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settlements", x => x.Id);
                });

            // Create Payments table
            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BankReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProcessedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Settlements_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "Settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create SettlementAdjustments table
            migrationBuilder.CreateTable(
                name: "SettlementAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettlementAdjustments_Settlements_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "Settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create InventoryReservations table
            migrationBuilder.CreateTable(
                name: "InventoryReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReservedQuantityValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ReservedQuantityUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ReleasedQuantityValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ReleasedQuantityUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReservationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedUsageDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualUsageDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReservedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReleasedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReleaseReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReservations", x => x.Id);
                });

            // Create TradingChains table
            migrationBuilder.CreateTable(
                name: "TradingChains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChainId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChainName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PurchaseContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurchaseAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PurchaseCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    SalesAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    SalesCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    RealizedPnLAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    RealizedPnLCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    UnrealizedPnLAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    UnrealizedPnLCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    PurchaseQuantityValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PurchaseQuantityUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SalesQuantityValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    SalesQuantityUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    RemainingQuantityValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    RemainingQuantityUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TradeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedDeliveryStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedDeliveryEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualDeliveryStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualDeliveryEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingChains", x => x.Id);
                });

            // Create TradeChainOperations table
            migrationBuilder.CreateTable(
                name: "TradeChainOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TradeChainId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeChainOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeChainOperations_TradingChains_TradeChainId",
                        column: x => x.TradeChainId,
                        principalTable: "TradingChains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create TradeChainEvents table
            migrationBuilder.CreateTable(
                name: "TradeChainEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TradeChainId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeChainEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeChainEvents_TradingChains_TradeChainId",
                        column: x => x.TradeChainId,
                        principalTable: "TradingChains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for Settlements
            migrationBuilder.CreateIndex(
                name: "IX_Settlements_ContractId",
                table: "Settlements",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_Status",
                table: "Settlements",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_Type",
                table: "Settlements",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_DueDate",
                table: "Settlements",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_CreatedBy",
                table: "Settlements",
                column: "CreatedBy");

            // Create indexes for Payments
            migrationBuilder.CreateIndex(
                name: "IX_Payments_SettlementId",
                table: "Payments",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentReference",
                table: "Payments",
                column: "PaymentReference",
                unique: true);

            // Create indexes for SettlementAdjustments
            migrationBuilder.CreateIndex(
                name: "IX_SettlementAdjustments_SettlementId",
                table: "SettlementAdjustments",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementAdjustments_Type",
                table: "SettlementAdjustments",
                column: "Type");

            // Create indexes for InventoryReservations
            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_ContractId",
                table: "InventoryReservations",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_ProductId",
                table: "InventoryReservations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_LocationId",
                table: "InventoryReservations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_Status",
                table: "InventoryReservations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_ExpiryDate",
                table: "InventoryReservations",
                column: "ExpiryDate");

            // Create indexes for TradingChains
            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_ChainId",
                table: "TradingChains",
                column: "ChainId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_Status",
                table: "TradingChains",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_Type",
                table: "TradingChains",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_PurchaseContractId",
                table: "TradingChains",
                column: "PurchaseContractId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_SalesContractId",
                table: "TradingChains",
                column: "SalesContractId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_SupplierId",
                table: "TradingChains",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_CustomerId",
                table: "TradingChains",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_ProductId",
                table: "TradingChains",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_TradeDate",
                table: "TradingChains",
                column: "TradeDate");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_ExpectedDeliveryStart",
                table: "TradingChains",
                column: "ExpectedDeliveryStart");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_ExpectedDeliveryEnd",
                table: "TradingChains",
                column: "ExpectedDeliveryEnd");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_CreatedBy",
                table: "TradingChains",
                column: "CreatedBy");

            // Composite indexes for TradingChains
            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_Status_Type",
                table: "TradingChains",
                columns: new[] { "Status", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_CreatedAt_Status",
                table: "TradingChains",
                columns: new[] { "CreatedAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_SupplierId_Status",
                table: "TradingChains",
                columns: new[] { "SupplierId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_CustomerId_Status",
                table: "TradingChains",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_ProductId_TradeDate",
                table: "TradingChains",
                columns: new[] { "ProductId", "TradeDate" });

            // Create indexes for TradeChainOperations
            migrationBuilder.CreateIndex(
                name: "IX_TradeChainOperations_TradeChainId",
                table: "TradeChainOperations",
                column: "TradeChainId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeChainOperations_OperationType",
                table: "TradeChainOperations",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_TradeChainOperations_PerformedAt",
                table: "TradeChainOperations",
                column: "PerformedAt");

            // Create indexes for TradeChainEvents
            migrationBuilder.CreateIndex(
                name: "IX_TradeChainEvents_TradeChainId",
                table: "TradeChainEvents",
                column: "TradeChainId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeChainEvents_EventType",
                table: "TradeChainEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_TradeChainEvents_PerformedAt",
                table: "TradeChainEvents",
                column: "PerformedAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop tables in reverse order to avoid foreign key constraint issues
            migrationBuilder.DropTable(name: "TradeChainEvents");
            migrationBuilder.DropTable(name: "TradeChainOperations");
            migrationBuilder.DropTable(name: "TradingChains");
            migrationBuilder.DropTable(name: "InventoryReservations");
            migrationBuilder.DropTable(name: "SettlementAdjustments");
            migrationBuilder.DropTable(name: "Payments");
            migrationBuilder.DropTable(name: "Settlements");
        }
    }
}