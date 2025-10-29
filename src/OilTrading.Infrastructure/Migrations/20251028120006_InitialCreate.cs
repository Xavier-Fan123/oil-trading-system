using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OilTrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContractEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EventDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OldValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                    NewValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                    OldStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    NewStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    AdditionalData = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LocationName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    LocationType = table.Column<int>(type: "INTEGER", nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Coordinates = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    OperatorName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ContactInfo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TotalCapacity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalCapacityUnit = table.Column<int>(type: "INTEGER", nullable: false),
                    AvailableCapacity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AvailableCapacityUnit = table.Column<int>(type: "INTEGER", nullable: false),
                    UsedCapacity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    UsedCapacityUnit = table.Column<int>(type: "INTEGER", nullable: false),
                    SupportedProducts = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    HandlingServices = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    HasRailAccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasRoadAccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasSeaAccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasPipelineAccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Exchange = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Volume = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    High = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Low = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Open = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Close = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "USD"),
                    DataSource = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PriceDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PriceType = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ContractMonth = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    DataSource = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsSettlement = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ImportedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketPrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OperationAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OperationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OperationName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OperationType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    Data = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    InitiatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceIndices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    IndexName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "USD"),
                    Region = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Grade = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Change = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    ChangePercent = table.Column<decimal>(type: "TEXT", precision: 8, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceIndices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    ProductType = table.Column<int>(type: "INTEGER", nullable: false),
                    Grade = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Specification = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Density = table.Column<decimal>(type: "TEXT", precision: 10, scale: 4, nullable: false),
                    Origin = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false, defaultValueSql: "X'00000000000000000000000000000001'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false, defaultValue: "#6B7280"),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MutuallyExclusiveTags = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    MaxUsagePerEntity = table.Column<int>(type: "INTEGER", nullable: true),
                    AllowedContractStatuses = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TradeGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroupName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StrategyType = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpectedRiskLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxAllowedLoss = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TargetProfit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TradingChains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChainId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ChainName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PurchaseContractId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SalesContractId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PurchaseAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    PurchaseCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    SalesAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    SalesCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    RealizedPnLAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    RealizedPnLCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    UnrealizedPnLAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    UnrealizedPnLCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    PurchaseQuantityValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    PurchaseQuantityUnit = table.Column<int>(type: "INTEGER", maxLength: 10, nullable: true),
                    SalesQuantityValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    SalesQuantityUnit = table.Column<int>(type: "INTEGER", maxLength: 10, nullable: true),
                    RemainingQuantityValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    RemainingQuantityUnit = table.Column<int>(type: "INTEGER", maxLength: 10, nullable: true),
                    TradeDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpectedDeliveryStart = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpectedDeliveryEnd = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ActualDeliveryStart = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ActualDeliveryEnd = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SupplierId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingChains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TradingPartners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CompanyName = table.Column<string>(type: "TEXT", nullable: false),
                    CompanyCode = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    PartnerType = table.Column<int>(type: "INTEGER", nullable: false),
                    ContactPerson = table.Column<string>(type: "TEXT", nullable: true),
                    ContactEmail = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ContactPhone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TaxId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TaxNumber = table.Column<string>(type: "TEXT", nullable: true),
                    ContactInfo = table.Column<string>(type: "TEXT", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    CreditLimitValidUntil = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PaymentTermDays = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentExposure = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreditRating = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    TotalPurchaseAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalSalesAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalTransactions = table.Column<int>(type: "INTEGER", nullable: false),
                    LastTransactionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    IsBlocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    BlockReason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false, defaultValueSql: "X'00000000000000000000000000000001'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingPartners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false, defaultValueSql: "X'00000000000000000000000000000001'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "price_benchmarks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    benchmark_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    benchmark_type = table.Column<int>(type: "INTEGER", nullable: false),
                    product_category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    unit = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    data_source = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_benchmarks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryPositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    QuantityUnit = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Grade = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    BatchReference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Sulfur = table.Column<decimal>(type: "TEXT", precision: 10, scale: 4, nullable: true),
                    API = table.Column<decimal>(type: "TEXT", precision: 10, scale: 4, nullable: true),
                    Viscosity = table.Column<decimal>(type: "TEXT", precision: 10, scale: 4, nullable: true),
                    QualityNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StatusNotes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryPositions_InventoryLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryPositions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaperContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractNumber = table.Column<string>(type: "TEXT", nullable: false),
                    ContractMonth = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ProductType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    LotSize = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    EntryPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    TradeDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SettlementDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RealizedPnL = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    UnrealizedPnL = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    DailyPnL = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    LastMTMDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TradeGroupId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsSpread = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Leg1Product = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Leg2Product = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SpreadValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    VaRValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Volatility = table.Column<decimal>(type: "TEXT", precision: 8, scale: 4, nullable: true),
                    TradeReference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CounterpartyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaperContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaperContracts_TradeGroups_TradeGroupId",
                        column: x => x.TradeGroupId,
                        principalTable: "TradeGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TradeGroupTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TradeGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AssignedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TagId1 = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeGroupTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeGroupTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TradeGroupTags_Tags_TagId1",
                        column: x => x.TagId1,
                        principalTable: "Tags",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TradeGroupTags_TradeGroups_TradeGroupId",
                        column: x => x.TradeGroupId,
                        principalTable: "TradeGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TradeChainEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PerformedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TradeChainId = table.Column<Guid>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "TradeChainOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OperationType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PerformedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: true),
                    TradeChainId = table.Column<Guid>(type: "TEXT", nullable: false)
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
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
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

            migrationBuilder.CreateTable(
                name: "PhysicalContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ContractType = table.Column<int>(type: "INTEGER", nullable: false),
                    ContractDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TradingPartnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    QuantityUnit = table.Column<int>(type: "INTEGER", maxLength: 10, nullable: false),
                    ProductSpec = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PricingType = table.Column<int>(type: "INTEGER", nullable: false),
                    FixedPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    PricingFormula = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PricingBasis = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Premium = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "USD"),
                    ContractValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    DeliveryTerms = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    LoadPort = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DischargePort = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LaycanStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LaycanEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PaymentTerms = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PrepaymentPercentage = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    CreditDays = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 30),
                    PaymentDueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsAgencyTrade = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    PrincipalName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AgencyFee = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DeliveredQuantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: true),
                    DeliveryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InvoicedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    PaidAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    OutstandingAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    IsFullySettled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    ProformaInvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ProformaInvoiceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CommercialInvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CommercialInvoiceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    InternalNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhysicalContracts_TradingPartners_TradingPartnerId",
                        column: x => x.TradingPartnerId,
                        principalTable: "TradingPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ContractNumber_Year = table.Column<string>(type: "TEXT", nullable: false),
                    ContractNumber_Type = table.Column<int>(type: "INTEGER", nullable: false),
                    ContractNumber_SerialNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalContractNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ContractType = table.Column<int>(type: "INTEGER", nullable: false),
                    TradingPartnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TraderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PriceBenchmarkId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ContractQuantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    ContractQuantityUnit = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchedQuantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    TonBarrelRatio = table.Column<decimal>(type: "TEXT", precision: 8, scale: 4, nullable: false, defaultValue: 7.6m),
                    PriceFormula = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PricingMethod = table.Column<int>(type: "INTEGER", nullable: true),
                    PriceIndexName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FixedPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    FormulaPremium = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    FormulaPremiumCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    FormulaDiscount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    FormulaDiscountCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    PriceCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    PriceUnit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PricingDays = table.Column<int>(type: "INTEGER", nullable: true),
                    FormulaPricingStart = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FormulaPricingEnd = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BenchmarkUnit = table.Column<int>(type: "INTEGER", nullable: true),
                    AdjustmentUnit = table.Column<int>(type: "INTEGER", nullable: true),
                    FormulaAdjustment = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    FormulaAdjustmentCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    CalculationMode = table.Column<int>(type: "INTEGER", nullable: true),
                    ContractualConversionRatio = table.Column<decimal>(type: "TEXT", precision: 10, scale: 4, nullable: true),
                    ContractValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    ContractValueCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    PricingPeriodStart = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PricingPeriodEnd = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsPriceFinalized = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    BenchmarkContractId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Premium = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    PremiumCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    Discount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    DiscountCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    LaycanStart = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LaycanEnd = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LoadPort = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DischargePort = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DeliveryTerms = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    PaymentTerms = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreditPeriodDays = table.Column<int>(type: "INTEGER", nullable: true),
                    SettlementType = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    PrepaymentPercentage = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    Incoterms = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    QualitySpecifications = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    InspectionAgency = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    TradeGroupId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false, defaultValueSql: "X'00000000000000000000000000000001'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseContracts_BenchmarkContract",
                        column: x => x.BenchmarkContractId,
                        principalTable: "PurchaseContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurchaseContracts_PriceBenchmark",
                        column: x => x.PriceBenchmarkId,
                        principalTable: "price_benchmarks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurchaseContracts_Products",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseContracts_TradeGroups_TradeGroupId",
                        column: x => x.TradeGroupId,
                        principalTable: "TradeGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurchaseContracts_TradingPartners",
                        column: x => x.TradingPartnerId,
                        principalTable: "TradingPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseContracts_Users",
                        column: x => x.TraderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Consider partitioning by CreatedAt for large datasets");

            migrationBuilder.CreateTable(
                name: "daily_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    benchmark_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    price_date = table.Column<DateTime>(type: "date", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    open_price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    high_price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    low_price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    close_price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    volume = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    premium = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    discount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                    IsHoliday = table.Column<bool>(type: "INTEGER", nullable: false),
                    is_published = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    DataSource = table.Column<string>(type: "TEXT", nullable: true),
                    data_quality = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_daily_prices_benchmark",
                        column: x => x.benchmark_id,
                        principalTable: "price_benchmarks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FuturesDeals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DealNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TradeDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ContractMonth = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Direction = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    QuantityUnit = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "USD"),
                    PriceUnit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TotalValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Trader = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Broker = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Exchange = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ClearingHouse = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SettlementDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsCleared = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClearingReference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MarketPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    UnrealizedPnL = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    RealizedPnL = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    DataSource = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ImportedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    PaperContractId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuturesDeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuturesDeals_PaperContracts_PaperContractId",
                        column: x => x.PaperContractId,
                        principalTable: "PaperContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SalesContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ContractNumber_Year = table.Column<string>(type: "TEXT", nullable: false),
                    ContractNumber_Type = table.Column<int>(type: "INTEGER", nullable: false),
                    ContractNumber_SerialNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalContractNumber = table.Column<string>(type: "TEXT", nullable: true),
                    ContractType = table.Column<int>(type: "INTEGER", nullable: false),
                    TradingPartnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TraderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LinkedPurchaseContractId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PriceBenchmarkId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ContractQuantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    ContractQuantityUnit = table.Column<int>(type: "INTEGER", nullable: false),
                    TonBarrelRatio = table.Column<decimal>(type: "TEXT", precision: 8, scale: 4, nullable: false, defaultValue: 7.6m),
                    PriceFormula = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PricingMethod = table.Column<int>(type: "INTEGER", nullable: true),
                    PriceIndexName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FixedPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    FormulaPremium = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    FormulaPremiumCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    FormulaDiscount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    FormulaDiscountCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    PriceCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    PriceUnit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PricingDays = table.Column<int>(type: "INTEGER", nullable: true),
                    FormulaPricingStart = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FormulaPricingEnd = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BenchmarkUnit = table.Column<int>(type: "INTEGER", nullable: true),
                    AdjustmentUnit = table.Column<int>(type: "INTEGER", nullable: true),
                    FormulaAdjustment = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    FormulaAdjustmentCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    CalculationMode = table.Column<int>(type: "INTEGER", nullable: true),
                    ContractualConversionRatio = table.Column<decimal>(type: "TEXT", precision: 10, scale: 4, nullable: true),
                    ContractValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    ContractValueCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    ProfitMargin = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    ProfitMarginCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    PricingPeriodStart = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PricingPeriodEnd = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsPriceFinalized = table.Column<bool>(type: "INTEGER", nullable: false),
                    Premium = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    PremiumCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    Discount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    DiscountCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    LaycanStart = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LaycanEnd = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LoadPort = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DischargePort = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DeliveryTerms = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    PaymentTerms = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreditPeriodDays = table.Column<int>(type: "INTEGER", nullable: true),
                    SettlementType = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    PrepaymentPercentage = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    Incoterms = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    QualitySpecifications = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    InspectionAgency = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    TradeGroupId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false, defaultValueSql: "X'00000000000000000000000000000001'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesContracts_Products",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesContracts_PurchaseContracts",
                        column: x => x.LinkedPurchaseContractId,
                        principalTable: "PurchaseContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesContracts_TradeGroups_TradeGroupId",
                        column: x => x.TradeGroupId,
                        principalTable: "TradeGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesContracts_TradingPartners",
                        column: x => x.TradingPartnerId,
                        principalTable: "TradingPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesContracts_Users",
                        column: x => x.TraderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesContracts_price_benchmarks_PriceBenchmarkId",
                        column: x => x.PriceBenchmarkId,
                        principalTable: "price_benchmarks",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ContractMatchings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PurchaseContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SalesContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MatchedQuantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    MatchedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MatchedBy = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractMatchings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractMatchings_PurchaseContracts_PurchaseContractId",
                        column: x => x.PurchaseContractId,
                        principalTable: "PurchaseContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractMatchings_SalesContracts_SalesContractId",
                        column: x => x.SalesContractId,
                        principalTable: "SalesContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractSettlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
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
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractSettlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractSettlements_PurchaseContracts",
                        column: x => x.ContractId,
                        principalTable: "PurchaseContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractSettlements_SalesContracts",
                        column: x => x.ContractId,
                        principalTable: "SalesContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContractTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TagId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    AssignedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractTags_PurchaseContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "PurchaseContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractTags_SalesContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "SalesContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LocationCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    QuantityUnit = table.Column<int>(type: "INTEGER", nullable: false),
                    ReleasedQuantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    ReleasedQuantityUnit = table.Column<int>(type: "INTEGER", nullable: false),
                    ReservationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReleasedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ReservedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ReleasedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReleaseReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryReservations_PurchaseContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "PurchaseContracts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryReservations_SalesContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "SalesContracts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Settlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SettlementNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "USD"),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PayerPartyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PayeePartyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentTerms = table.Column<int>(type: "INTEGER", nullable: false),
                    SettlementMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    TermsCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "USD"),
                    DiscountRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: true),
                    EarlyPaymentDays = table.Column<int>(type: "INTEGER", nullable: true),
                    LateFeeRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: true),
                    EnableAutomaticProcessing = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProcessedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CompletedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CancellationReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PurchaseContractId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SalesContractId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Settlements_PurchaseContracts_PurchaseContractId",
                        column: x => x.PurchaseContractId,
                        principalTable: "PurchaseContracts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Settlements_SalesContracts_SalesContractId",
                        column: x => x.SalesContractId,
                        principalTable: "SalesContracts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Settlements_TradingPartners_PayeePartyId",
                        column: x => x.PayeePartyId,
                        principalTable: "TradingPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Settlements_TradingPartners_PayerPartyId",
                        column: x => x.PayerPartyId,
                        principalTable: "TradingPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShippingOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShippingNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VesselName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    PlannedQuantityUnit = table.Column<int>(type: "INTEGER", nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                    ActualQuantityUnit = table.Column<int>(type: "INTEGER", nullable: true),
                    LoadPortETA = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DischargePortETA = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LoadPort = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DischargePort = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    LoadPortATA = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DischargePortATA = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BillOfLadingDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NoticeOfReadinessDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CertificateOfDischargeDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ChartererName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IMONumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    VesselCapacity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    ShippingAgent = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    PurchaseContractId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SalesContractId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShippingOperations_PurchaseContracts_PurchaseContractId",
                        column: x => x.PurchaseContractId,
                        principalTable: "PurchaseContracts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShippingOperations_SalesContracts_SalesContractId",
                        column: x => x.SalesContractId,
                        principalTable: "SalesContracts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "contract_pricing_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    contract_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_type = table.Column<int>(type: "INTEGER", nullable: false),
                    event_date = table.Column<DateTime>(type: "date", nullable: false),
                    pricing_start_date = table.Column<DateTime>(type: "date", nullable: false),
                    pricing_end_date = table.Column<DateTime>(type: "date", nullable: false),
                    average_price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                    is_finalized = table.Column<bool>(type: "INTEGER", nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    pricing_benchmark = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    pricing_days_count = table.Column<int>(type: "INTEGER", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_pricing_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_pricing_events_purchase_contract",
                        column: x => x.contract_id,
                        principalTable: "PurchaseContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contract_pricing_events_sales_contract",
                        column: x => x.contract_id,
                        principalTable: "SalesContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SettlementCharges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SettlementId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChargeType = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "USD"),
                    IncurredDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReferenceDocument = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettlementCharges_ContractSettlements",
                        column: x => x.SettlementId,
                        principalTable: "ContractSettlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SettlementId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentReference = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PayerAccountNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PayerBankName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PayerSwiftCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PayerIBAN = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PayerAccountHolderName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PayerCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true, defaultValue: "USD"),
                    PayerRoutingNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PayerBranchCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PayeeAccountNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PayeeBankName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PayeeSwiftCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PayeeIBAN = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PayeeAccountHolderName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PayeeCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true, defaultValue: "USD"),
                    PayeeRoutingNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PayeeBranchCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    BankReference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Instructions = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    InitiatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SettlementId1 = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
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
                    table.ForeignKey(
                        name: "FK_Payments_Settlements_SettlementId1",
                        column: x => x.SettlementId1,
                        principalTable: "Settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SettlementAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "USD"),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AdjustmentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AdjustedBy = table.Column<Guid>(type: "TEXT", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SettlementId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
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
                    table.ForeignKey(
                        name: "FK_SettlementAdjustments_Users_AdjustedBy",
                        column: x => x.AdjustedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromLocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToLocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    QuantityUnit = table.Column<int>(type: "INTEGER", nullable: false),
                    MovementType = table.Column<int>(type: "INTEGER", nullable: false),
                    MovementDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PlannedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    MovementReference = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TransportMode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    VesselName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    TransportReference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TransportCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    TransportCostCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    HandlingCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    HandlingCostCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    TotalCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    TotalCostCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    InitiatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ApprovedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PurchaseContractId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SalesContractId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ShippingOperationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FromInventoryPositionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ToInventoryPositionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_InventoryLocations_FromLocationId",
                        column: x => x.FromLocationId,
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_InventoryLocations_ToLocationId",
                        column: x => x.ToLocationId,
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_InventoryPositions_FromInventoryPositionId",
                        column: x => x.FromInventoryPositionId,
                        principalTable: "InventoryPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_InventoryPositions_ToInventoryPositionId",
                        column: x => x.ToInventoryPositionId,
                        principalTable: "InventoryPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_PurchaseContracts_PurchaseContractId",
                        column: x => x.PurchaseContractId,
                        principalTable: "PurchaseContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_SalesContracts_SalesContractId",
                        column: x => x.SalesContractId,
                        principalTable: "SalesContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_ShippingOperations_ShippingOperationId",
                        column: x => x.ShippingOperationId,
                        principalTable: "ShippingOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PricingEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<int>(type: "INTEGER", nullable: false),
                    EventDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BeforeDays = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    AfterDays = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    HasIndexOnEventDay = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    PricingPeriodStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PricingPeriodEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalPricingDays = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ActualEventDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsEventConfirmed = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    PurchaseContractId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SalesContractId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ShippingOperationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricingEvents_PurchaseContracts_PurchaseContractId",
                        column: x => x.PurchaseContractId,
                        principalTable: "PurchaseContracts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PricingEvents_SalesContracts_SalesContractId",
                        column: x => x.SalesContractId,
                        principalTable: "SalesContracts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PricingEvents_ShippingOperations_ShippingOperationId",
                        column: x => x.ShippingOperationId,
                        principalTable: "ShippingOperations",
                        principalColumn: "Id");
                },
                comment: "Partitioned by EventDate (monthly partitions for performance)");

            migrationBuilder.CreateIndex(
                name: "IX_ContractEvents_ContractId",
                table: "ContractEvents",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractEvents_EventType",
                table: "ContractEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_ContractEvents_Timestamp_ContractId",
                table: "ContractEvents",
                columns: new[] { "Timestamp", "ContractId" });

            migrationBuilder.CreateIndex(
                name: "IX_ContractEvents_UserId",
                table: "ContractEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractMatchings_PurchaseContractId",
                table: "ContractMatchings",
                column: "PurchaseContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractMatchings_SalesContractId",
                table: "ContractMatchings",
                column: "SalesContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractSettlements_ContractId",
                table: "ContractSettlements",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractSettlements_ContractId_Status",
                table: "ContractSettlements",
                columns: new[] { "ContractId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ContractSettlements_CreatedDate",
                table: "ContractSettlements",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_ContractSettlements_DocumentDate",
                table: "ContractSettlements",
                column: "DocumentDate");

            migrationBuilder.CreateIndex(
                name: "IX_ContractSettlements_DocumentNumber",
                table: "ContractSettlements",
                column: "DocumentNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ContractSettlements_ExternalContractNumber",
                table: "ContractSettlements",
                column: "ExternalContractNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ContractSettlements_IsFinalized",
                table: "ContractSettlements",
                column: "IsFinalized");

            migrationBuilder.CreateIndex(
                name: "IX_ContractSettlements_IsFinalized_CreatedDate",
                table: "ContractSettlements",
                columns: new[] { "IsFinalized", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ContractSettlements_Status",
                table: "ContractSettlements",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ContractSettlements_Status_CreatedDate",
                table: "ContractSettlements",
                columns: new[] { "Status", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ContractTags_AssignedAt",
                table: "ContractTags",
                column: "AssignedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContractTags_Contract",
                table: "ContractTags",
                columns: new[] { "ContractId", "ContractType" });

            migrationBuilder.CreateIndex(
                name: "IX_ContractTags_Contract_Tag_Unique",
                table: "ContractTags",
                columns: new[] { "ContractId", "ContractType", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractTags_TagId",
                table: "ContractTags",
                column: "TagId");

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

            migrationBuilder.CreateIndex(
                name: "IX_FuturesDeals_ContractMonth",
                table: "FuturesDeals",
                column: "ContractMonth");

            migrationBuilder.CreateIndex(
                name: "IX_FuturesDeals_DealNumber",
                table: "FuturesDeals",
                column: "DealNumber");

            migrationBuilder.CreateIndex(
                name: "IX_FuturesDeals_DealNumber_TradeDate",
                table: "FuturesDeals",
                columns: new[] { "DealNumber", "TradeDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FuturesDeals_ImportedAt",
                table: "FuturesDeals",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FuturesDeals_PaperContractId",
                table: "FuturesDeals",
                column: "PaperContractId");

            migrationBuilder.CreateIndex(
                name: "IX_FuturesDeals_ProductCode",
                table: "FuturesDeals",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_FuturesDeals_Product_Contract",
                table: "FuturesDeals",
                columns: new[] { "ProductCode", "ContractMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_FuturesDeals_Status",
                table: "FuturesDeals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FuturesDeals_TradeDate",
                table: "FuturesDeals",
                column: "TradeDate");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLocations_Country",
                table: "InventoryLocations",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLocations_IsActive",
                table: "InventoryLocations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLocations_LocationCode",
                table: "InventoryLocations",
                column: "LocationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLocations_LocationType",
                table: "InventoryLocations",
                column: "LocationType");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_FromInventoryPositionId",
                table: "InventoryMovements",
                column: "FromInventoryPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_FromLocationId",
                table: "InventoryMovements",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_MovementDate",
                table: "InventoryMovements",
                column: "MovementDate");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_MovementReference",
                table: "InventoryMovements",
                column: "MovementReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_MovementType",
                table: "InventoryMovements",
                column: "MovementType");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_PlannedDate",
                table: "InventoryMovements",
                column: "PlannedDate");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ProductId",
                table: "InventoryMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_PurchaseContractId",
                table: "InventoryMovements",
                column: "PurchaseContractId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_SalesContractId",
                table: "InventoryMovements",
                column: "SalesContractId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ShippingOperationId",
                table: "InventoryMovements",
                column: "ShippingOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_Status",
                table: "InventoryMovements",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ToInventoryPositionId",
                table: "InventoryMovements",
                column: "ToInventoryPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ToLocationId",
                table: "InventoryMovements",
                column: "ToLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryPositions_LocationId",
                table: "InventoryPositions",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryPositions_Location_Product",
                table: "InventoryPositions",
                columns: new[] { "LocationId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryPositions_ProductId",
                table: "InventoryPositions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryPositions_ReceivedDate",
                table: "InventoryPositions",
                column: "ReceivedDate");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryPositions_Status",
                table: "InventoryPositions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_ContractId",
                table: "InventoryReservations",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_LocationCode",
                table: "InventoryReservations",
                column: "LocationCode");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_ProductCode",
                table: "InventoryReservations",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_Product_Location_Status",
                table: "InventoryReservations",
                columns: new[] { "ProductCode", "LocationCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_ReservationDate",
                table: "InventoryReservations",
                column: "ReservationDate");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_Status",
                table: "InventoryReservations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MarketData_Exchange",
                table: "MarketData",
                column: "Exchange");

            migrationBuilder.CreateIndex(
                name: "IX_MarketData_Symbol",
                table: "MarketData",
                column: "Symbol");

            migrationBuilder.CreateIndex(
                name: "IX_MarketData_Timestamp_Symbol",
                table: "MarketData",
                columns: new[] { "Timestamp", "Symbol" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_ContractMonth",
                table: "MarketPrices",
                column: "ContractMonth");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_DataSource",
                table: "MarketPrices",
                column: "DataSource");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_PriceDate",
                table: "MarketPrices",
                column: "PriceDate");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_PriceType",
                table: "MarketPrices",
                column: "PriceType");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_ProductCode",
                table: "MarketPrices",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_ProductCode_PriceDate",
                table: "MarketPrices",
                columns: new[] { "ProductCode", "PriceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationAuditLogs_CreatedAt",
                table: "OperationAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OperationAuditLogs_IsSuccess",
                table: "OperationAuditLogs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_OperationAuditLogs_OperationId",
                table: "OperationAuditLogs",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationAuditLogs_OperationType",
                table: "OperationAuditLogs",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_OperationAuditLogs_Timestamp",
                table: "OperationAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_OperationAuditLogs_TransactionId",
                table: "OperationAuditLogs",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationAuditLogs_Type_Success_Timestamp",
                table: "OperationAuditLogs",
                columns: new[] { "OperationType", "IsSuccess", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PaperContracts_ContractMonth",
                table: "PaperContracts",
                column: "ContractMonth");

            migrationBuilder.CreateIndex(
                name: "IX_PaperContracts_Position",
                table: "PaperContracts",
                column: "Position");

            migrationBuilder.CreateIndex(
                name: "IX_PaperContracts_ProductType",
                table: "PaperContracts",
                column: "ProductType");

            migrationBuilder.CreateIndex(
                name: "IX_PaperContracts_ProductType_ContractMonth",
                table: "PaperContracts",
                columns: new[] { "ProductType", "ContractMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_PaperContracts_Status",
                table: "PaperContracts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaperContracts_TradeDate",
                table: "PaperContracts",
                column: "TradeDate");

            migrationBuilder.CreateIndex(
                name: "IX_PaperContracts_TradeGroupId",
                table: "PaperContracts",
                column: "TradeGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentDate",
                table: "Payments",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentReference",
                table: "Payments",
                column: "PaymentReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SettlementId",
                table: "Payments",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SettlementId1",
                table: "Payments",
                column: "SettlementId1");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalContracts_ContractDate",
                table: "PhysicalContracts",
                column: "ContractDate");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalContracts_ContractNumber",
                table: "PhysicalContracts",
                column: "ContractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalContracts_Laycan",
                table: "PhysicalContracts",
                columns: new[] { "LaycanStart", "LaycanEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalContracts_Status",
                table: "PhysicalContracts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalContracts_TradingPartnerId",
                table: "PhysicalContracts",
                column: "TradingPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceIndex_IndexName",
                table: "PriceIndices",
                column: "IndexName");

            migrationBuilder.CreateIndex(
                name: "IX_PriceIndex_Region",
                table: "PriceIndices",
                column: "Region");

            migrationBuilder.CreateIndex(
                name: "IX_PriceIndex_Timestamp_IndexName",
                table: "PriceIndices",
                columns: new[] { "Timestamp", "IndexName" });

            migrationBuilder.CreateIndex(
                name: "IX_PricingEvents_ContractId",
                table: "PricingEvents",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingEvents_Contract_Date_DESC",
                table: "PricingEvents",
                columns: new[] { "ContractId", "EventDate" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_PricingEvents_CreatedAt",
                table: "PricingEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PricingEvents_EventDate_Recent",
                table: "PricingEvents",
                column: "EventDate",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_PricingEvents_EventType",
                table: "PricingEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_PricingEvents_IsEventConfirmed",
                table: "PricingEvents",
                column: "IsEventConfirmed");

            migrationBuilder.CreateIndex(
                name: "IX_PricingEvents_PricingPeriod",
                table: "PricingEvents",
                columns: new[] { "PricingPeriodStart", "PricingPeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_PricingEvents_PurchaseContractId",
                table: "PricingEvents",
                column: "PurchaseContractId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingEvents_SalesContractId",
                table: "PricingEvents",
                column: "SalesContractId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingEvents_ShippingOperationId",
                table: "PricingEvents",
                column: "ShippingOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingEvents_Type_Contract_Date",
                table: "PricingEvents",
                columns: new[] { "EventType", "ContractId", "EventDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PricingEvents_Type_Date",
                table: "PricingEvents",
                columns: new[] { "EventType", "EventDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Code",
                table: "Products",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive",
                table: "Products",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Origin",
                table: "Products",
                column: "Origin");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductType",
                table: "Products",
                column: "ProductType");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Type",
                table: "Products",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Type_Active",
                table: "Products",
                columns: new[] { "Type", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_BenchmarkContractId",
                table: "PurchaseContracts",
                column: "BenchmarkContractId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_ContractNumber",
                table: "PurchaseContracts",
                column: "ContractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_CreatedAt",
                table: "PurchaseContracts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_ExternalContractNumber",
                table: "PurchaseContracts",
                column: "ExternalContractNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_Laycan_Period",
                table: "PurchaseContracts",
                columns: new[] { "LaycanStart", "LaycanEnd" },
                filter: "Status IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_PriceBenchmarkId",
                table: "PurchaseContracts",
                column: "PriceBenchmarkId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_ProductId",
                table: "PurchaseContracts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_Product_Status",
                table: "PurchaseContracts",
                columns: new[] { "ProductId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_Status_Covering",
                table: "PurchaseContracts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_Status_CreatedAt",
                table: "PurchaseContracts",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_Supplier_Status",
                table: "PurchaseContracts",
                columns: new[] { "TradingPartnerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_TradeGroupId",
                table: "PurchaseContracts",
                column: "TradeGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_TraderId",
                table: "PurchaseContracts",
                column: "TraderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_Trader_Date",
                table: "PurchaseContracts",
                columns: new[] { "TraderId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_TradingPartnerId",
                table: "PurchaseContracts",
                column: "TradingPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_ContractNumber",
                table: "SalesContracts",
                column: "ContractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_CreatedAt",
                table: "SalesContracts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_Customer_Status",
                table: "SalesContracts",
                columns: new[] { "TradingPartnerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_LaycanDates",
                table: "SalesContracts",
                columns: new[] { "LaycanStart", "LaycanEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_LinkedPurchaseContractId",
                table: "SalesContracts",
                column: "LinkedPurchaseContractId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_PriceBenchmarkId",
                table: "SalesContracts",
                column: "PriceBenchmarkId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_ProductId",
                table: "SalesContracts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_Product_Delivery",
                table: "SalesContracts",
                columns: new[] { "ProductId", "LaycanEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_Status",
                table: "SalesContracts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_Status_CreatedAt",
                table: "SalesContracts",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_TradeGroupId",
                table: "SalesContracts",
                column: "TradeGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_TraderId",
                table: "SalesContracts",
                column: "TraderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_TradingPartnerId",
                table: "SalesContracts",
                column: "TradingPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementAdjustments_AdjustedBy",
                table: "SettlementAdjustments",
                column: "AdjustedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementAdjustments_AdjustmentDate",
                table: "SettlementAdjustments",
                column: "AdjustmentDate");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementAdjustments_SettlementId",
                table: "SettlementAdjustments",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementAdjustments_Type",
                table: "SettlementAdjustments",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementCharges_ChargeType",
                table: "SettlementCharges",
                column: "ChargeType");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementCharges_ChargeType_CreatedDate",
                table: "SettlementCharges",
                columns: new[] { "ChargeType", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SettlementCharges_CreatedDate",
                table: "SettlementCharges",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementCharges_IncurredDate",
                table: "SettlementCharges",
                column: "IncurredDate");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementCharges_SettlementId",
                table: "SettlementCharges",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementCharges_SettlementId_ChargeType",
                table: "SettlementCharges",
                columns: new[] { "SettlementId", "ChargeType" });

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_ContractId",
                table: "Settlements",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_DueDate",
                table: "Settlements",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_PayeePartyId",
                table: "Settlements",
                column: "PayeePartyId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_PayerPartyId",
                table: "Settlements",
                column: "PayerPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_PurchaseContractId",
                table: "Settlements",
                column: "PurchaseContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_SalesContractId",
                table: "Settlements",
                column: "SalesContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_SettlementNumber",
                table: "Settlements",
                column: "SettlementNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_Status",
                table: "Settlements",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_Status_DueDate",
                table: "Settlements",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_Type",
                table: "Settlements",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOperations_ContractId",
                table: "ShippingOperations",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOperations_CreatedAt",
                table: "ShippingOperations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOperations_Port_Discharge",
                table: "ShippingOperations",
                columns: new[] { "DischargePort", "DischargePortETA" });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOperations_Port_Loading",
                table: "ShippingOperations",
                columns: new[] { "LoadPort", "LoadPortETA" });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOperations_PurchaseContractId",
                table: "ShippingOperations",
                column: "PurchaseContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOperations_SalesContractId",
                table: "ShippingOperations",
                column: "SalesContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOperations_Schedule",
                table: "ShippingOperations",
                columns: new[] { "LoadPortETA", "DischargePortETA" });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOperations_ShippingNumber",
                table: "ShippingOperations",
                column: "ShippingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOperations_Status",
                table: "ShippingOperations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOperations_VesselName",
                table: "ShippingOperations",
                column: "VesselName");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOperations_Vessel_Status",
                table: "ShippingOperations",
                columns: new[] { "VesselName", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Category",
                table: "Tags",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Category_IsActive",
                table: "Tags",
                columns: new[] { "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_LastUsedAt",
                table: "Tags",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UsageCount",
                table: "Tags",
                column: "UsageCount");

            migrationBuilder.CreateIndex(
                name: "IX_TradeChainEvents_TradeChainId",
                table: "TradeChainEvents",
                column: "TradeChainId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeChainOperations_TradeChainId",
                table: "TradeChainOperations",
                column: "TradeChainId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeGroupTags_AssignedAt",
                table: "TradeGroupTags",
                column: "AssignedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TradeGroupTags_IsActive",
                table: "TradeGroupTags",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TradeGroupTags_TagId",
                table: "TradeGroupTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeGroupTags_TagId1",
                table: "TradeGroupTags",
                column: "TagId1");

            migrationBuilder.CreateIndex(
                name: "IX_TradeGroupTags_TradeGroupId",
                table: "TradeGroupTags",
                column: "TradeGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeGroupTags_TradeGroupId_TagId",
                table: "TradeGroupTags",
                columns: new[] { "TradeGroupId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradeGroups_CreatedAt",
                table: "TradeGroups",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TradeGroups_GroupName",
                table: "TradeGroups",
                column: "GroupName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradeGroups_Status",
                table: "TradeGroups",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TradeGroups_StrategyType",
                table: "TradeGroups",
                column: "StrategyType");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_ChainId",
                table: "TradingChains",
                column: "ChainId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_CreatedAt_Status",
                table: "TradingChains",
                columns: new[] { "CreatedAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_CreatedBy",
                table: "TradingChains",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_CustomerId",
                table: "TradingChains",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_CustomerId_Status",
                table: "TradingChains",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_ExpectedDeliveryEnd",
                table: "TradingChains",
                column: "ExpectedDeliveryEnd");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_ExpectedDeliveryStart",
                table: "TradingChains",
                column: "ExpectedDeliveryStart");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_ProductId",
                table: "TradingChains",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_ProductId_TradeDate",
                table: "TradingChains",
                columns: new[] { "ProductId", "TradeDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_PurchaseContractId",
                table: "TradingChains",
                column: "PurchaseContractId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_SalesContractId",
                table: "TradingChains",
                column: "SalesContractId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_Status",
                table: "TradingChains",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_Status_Type",
                table: "TradingChains",
                columns: new[] { "Status", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_SupplierId",
                table: "TradingChains",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_SupplierId_Status",
                table: "TradingChains",
                columns: new[] { "SupplierId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_TradeDate",
                table: "TradingChains",
                column: "TradeDate");

            migrationBuilder.CreateIndex(
                name: "IX_TradingChains_Type",
                table: "TradingChains",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_TradingPartners_Code",
                table: "TradingPartners",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradingPartners_Country",
                table: "TradingPartners",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "IX_TradingPartners_Country_Active",
                table: "TradingPartners",
                columns: new[] { "Country", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingPartners_IsActive",
                table: "TradingPartners",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TradingPartners_Name",
                table: "TradingPartners",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_TradingPartners_Rating_Active",
                table: "TradingPartners",
                columns: new[] { "CreditRating", "IsActive" },
                filter: "CreditRating IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TradingPartners_Type",
                table: "TradingPartners",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_TradingPartners_Type_Active",
                table: "TradingPartners",
                columns: new[] { "PartnerType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_Active",
                table: "Users",
                columns: new[] { "Email", "IsActive" },
                unique: true,
                filter: "IsActive = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Name",
                table: "Users",
                columns: new[] { "FirstName", "LastName" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role_Active2",
                table: "Users",
                columns: new[] { "Role", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_contract_pricing_events_contract_id",
                table: "contract_pricing_events",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_pricing_events_contract_type_date",
                table: "contract_pricing_events",
                columns: new[] { "contract_id", "event_type", "event_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contract_pricing_events_event_date",
                table: "contract_pricing_events",
                column: "event_date");

            migrationBuilder.CreateIndex(
                name: "ix_contract_pricing_events_is_finalized",
                table: "contract_pricing_events",
                column: "is_finalized");

            migrationBuilder.CreateIndex(
                name: "ix_contract_pricing_events_status",
                table: "contract_pricing_events",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_daily_prices_benchmark_date",
                table: "daily_prices",
                columns: new[] { "benchmark_id", "price_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_daily_prices_is_published",
                table: "daily_prices",
                column: "is_published");

            migrationBuilder.CreateIndex(
                name: "ix_daily_prices_price_date",
                table: "daily_prices",
                column: "price_date");

            migrationBuilder.CreateIndex(
                name: "ix_price_benchmarks_benchmark_name",
                table: "price_benchmarks",
                column: "benchmark_name");

            migrationBuilder.CreateIndex(
                name: "ix_price_benchmarks_is_active",
                table: "price_benchmarks",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_price_benchmarks_type_category",
                table: "price_benchmarks",
                columns: new[] { "benchmark_type", "product_category" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractEvents");

            migrationBuilder.DropTable(
                name: "ContractMatchings");

            migrationBuilder.DropTable(
                name: "ContractTags");

            migrationBuilder.DropTable(
                name: "FinancialReports");

            migrationBuilder.DropTable(
                name: "FuturesDeals");

            migrationBuilder.DropTable(
                name: "InventoryMovements");

            migrationBuilder.DropTable(
                name: "InventoryReservations");

            migrationBuilder.DropTable(
                name: "MarketData");

            migrationBuilder.DropTable(
                name: "MarketPrices");

            migrationBuilder.DropTable(
                name: "OperationAuditLogs");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PhysicalContracts");

            migrationBuilder.DropTable(
                name: "PriceIndices");

            migrationBuilder.DropTable(
                name: "PricingEvents");

            migrationBuilder.DropTable(
                name: "SettlementAdjustments");

            migrationBuilder.DropTable(
                name: "SettlementCharges");

            migrationBuilder.DropTable(
                name: "TradeChainEvents");

            migrationBuilder.DropTable(
                name: "TradeChainOperations");

            migrationBuilder.DropTable(
                name: "TradeGroupTags");

            migrationBuilder.DropTable(
                name: "contract_pricing_events");

            migrationBuilder.DropTable(
                name: "daily_prices");

            migrationBuilder.DropTable(
                name: "PaperContracts");

            migrationBuilder.DropTable(
                name: "InventoryPositions");

            migrationBuilder.DropTable(
                name: "ShippingOperations");

            migrationBuilder.DropTable(
                name: "Settlements");

            migrationBuilder.DropTable(
                name: "ContractSettlements");

            migrationBuilder.DropTable(
                name: "TradingChains");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "InventoryLocations");

            migrationBuilder.DropTable(
                name: "SalesContracts");

            migrationBuilder.DropTable(
                name: "PurchaseContracts");

            migrationBuilder.DropTable(
                name: "price_benchmarks");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "TradeGroups");

            migrationBuilder.DropTable(
                name: "TradingPartners");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
