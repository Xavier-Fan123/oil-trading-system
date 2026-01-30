using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OilTrading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DataLineageEnhancement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Settlements_SettlementId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Settlements_SettlementId1",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "SettlementAdjustments");

            migrationBuilder.DropTable(
                name: "Settlements");

            migrationBuilder.DropIndex(
                name: "IX_Payments_SettlementId1",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_MarketPrices_ProductCode_PriceDate",
                table: "MarketPrices");

            migrationBuilder.DropColumn(
                name: "SettlementId1",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "EstimatedPaymentDate",
                table: "SalesContracts",
                newName: "LastPricingDate");

            migrationBuilder.RenameColumn(
                name: "EstimatedPaymentDate",
                table: "PurchaseContracts",
                newName: "LastPricingDate");

            migrationBuilder.RenameColumn(
                name: "ActualPaymentDate",
                table: "ContractSettlements",
                newName: "SupersededDate");

            migrationBuilder.RenameColumn(
                name: "ActualPayableDueDate",
                table: "ContractSettlements",
                newName: "PreviousSettlementId");

            migrationBuilder.AddColumn<string>(
                name: "DealReferenceId",
                table: "ShippingOperations",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSplit",
                table: "ShippingOperations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPlannedQuantity",
                table: "ShippingOperations",
                type: "TEXT",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalPlannedQuantityUnit",
                table: "ShippingOperations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentShippingOperationId",
                table: "ShippingOperations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SplitReasonNotes",
                table: "ShippingOperations",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SplitReasonType",
                table: "ShippingOperations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SplitSequence",
                table: "ShippingOperations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AmendmentReason",
                table: "SalesSettlements",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AmendmentType",
                table: "SalesSettlements",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "DealReferenceId",
                table: "SalesSettlements",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLatestVersion",
                table: "SalesSettlements",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalSettlementId",
                table: "SalesSettlements",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreviousSettlementId",
                table: "SalesSettlements",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SettlementSequence",
                table: "SalesSettlements",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "SupersededDate",
                table: "SalesSettlements",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DealReferenceId",
                table: "SalesContracts",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FixedPercentage",
                table: "SalesContracts",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FixedQuantity",
                table: "SalesContracts",
                type: "TEXT",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PriceSource",
                table: "SalesContracts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricingStatus",
                table: "SalesContracts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "UnfixedQuantity",
                table: "SalesContracts",
                type: "TEXT",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AmendmentReason",
                table: "PurchaseSettlements",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AmendmentType",
                table: "PurchaseSettlements",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "DealReferenceId",
                table: "PurchaseSettlements",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLatestVersion",
                table: "PurchaseSettlements",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalSettlementId",
                table: "PurchaseSettlements",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreviousSettlementId",
                table: "PurchaseSettlements",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SettlementSequence",
                table: "PurchaseSettlements",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "SupersededDate",
                table: "PurchaseSettlements",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DealReferenceId",
                table: "PurchaseContracts",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FixedPercentage",
                table: "PurchaseContracts",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FixedQuantity",
                table: "PurchaseContracts",
                type: "TEXT",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PriceSource",
                table: "PurchaseContracts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricingStatus",
                table: "PurchaseContracts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "UnfixedQuantity",
                table: "PurchaseContracts",
                type: "TEXT",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "HedgeDesignationDate",
                table: "PaperContracts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HedgeEffectiveness",
                table: "PaperContracts",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HedgeRatio",
                table: "PaperContracts",
                type: "TEXT",
                precision: 8,
                scale: 4,
                nullable: false,
                defaultValue: 1.0m);

            migrationBuilder.AddColumn<Guid>(
                name: "HedgedContractId",
                table: "PaperContracts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HedgedContractType",
                table: "PaperContracts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDesignatedHedge",
                table: "PaperContracts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "MarketPrices",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "X'00'",
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldRowVersion: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "MarketPrices",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AmendmentReason",
                table: "ContractSettlements",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AmendmentType",
                table: "ContractSettlements",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "DealReferenceId",
                table: "ContractSettlements",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLatestVersion",
                table: "ContractSettlements",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalSettlementId",
                table: "ContractSettlements",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SettlementSequence",
                table: "ContractSettlements",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

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
                    table.PrimaryKey("PK_PaymentRiskAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRiskAlerts_TradingPartners_TradingPartnerId",
                        column: x => x.TradingPartnerId,
                        principalTable: "TradingPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ReportType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FilterJson = table.Column<string>(type: "TEXT", nullable: true),
                    ColumnsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ExportFormat = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "CSV"),
                    IncludeMetadata = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportConfigurations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReportConfigurations_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SettlementAutomationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    RuleType = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "Normal"),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 3),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    RuleVersion = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    Scope = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    ScopeFilter = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Trigger = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    ScheduleExpression = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    OrchestrationStrategy = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    MaxSettlementsPerExecution = table.Column<int>(type: "INTEGER", nullable: true),
                    GroupingDimension = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ExecutionCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    SuccessCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    FailureCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    LastExecutedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastExecutionSettlementCount = table.Column<int>(type: "INTEGER", nullable: true),
                    LastExecutionError = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DisabledDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DisabledReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_SettlementAutomationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SettlementTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    TemplateConfiguration = table.Column<string>(type: "TEXT", nullable: false),
                    TimesUsed = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportDistributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReportConfigId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChannelName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ChannelType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ChannelConfiguration = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastTestedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastTestStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    LastTestMessage = table.Column<string>(type: "TEXT", nullable: true),
                    MaxRetries = table.Column<int>(type: "INTEGER", nullable: false),
                    RetryDelaySeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDistributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportDistributions_ReportConfigurations_ReportConfigId",
                        column: x => x.ReportConfigId,
                        principalTable: "ReportConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReportConfigId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExecutionStartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExecutionEndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    OutputFilePath = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    OutputFileName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    OutputFileFormat = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "CSV"),
                    RecordsProcessed = table.Column<int>(type: "INTEGER", nullable: true),
                    TotalRecords = table.Column<int>(type: "INTEGER", nullable: true),
                    DurationSeconds = table.Column<double>(type: "REAL", precision: 18, scale: 2, nullable: true),
                    SuccessfulDistributions = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedDistributions = table.Column<int>(type: "INTEGER", nullable: false),
                    ExecutedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsScheduled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportExecutions_ReportConfigurations_ReportConfigId",
                        column: x => x.ReportConfigId,
                        principalTable: "ReportConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportExecutions_Users_ExecutedBy",
                        column: x => x.ExecutedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ReportSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReportConfigId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Frequency = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: true),
                    DayOfMonth = table.Column<int>(type: "INTEGER", nullable: true),
                    Time = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Timezone = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NextRunDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastRunDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportSchedules_ReportConfigurations_ReportConfigId",
                        column: x => x.ReportConfigId,
                        principalTable: "ReportConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleExecutionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TriggerSource = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ExecutionStartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExecutionEndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExecutionDurationMs = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    SettlementCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    ConditionsEvaluated = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    ActionsExecuted = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    DetailedLog = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                    AffectedSettlementIds = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_RuleExecutionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleExecutionRecords_SettlementAutomationRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "SettlementAutomationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SettlementRuleActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SequenceNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Parameters = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    StopOnFailure = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    NotificationTemplateId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_SettlementRuleActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettlementRuleActions_SettlementAutomationRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "SettlementAutomationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SettlementRuleConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Field = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OperatorType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    SequenceNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    LogicalOperator = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false, defaultValue: "AND"),
                    GroupReference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_SettlementRuleConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettlementRuleConditions_SettlementAutomationRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "SettlementAutomationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SettlementTemplatePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TemplateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PermissionLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GrantedBy = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementTemplatePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettlementTemplatePermissions_SettlementTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "SettlementTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SettlementTemplateUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TemplateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SettlementId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AppliedBy = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementTemplateUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettlementTemplateUsages_SettlementTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "SettlementTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportArchives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ArchiveDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RetentionDays = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 90),
                    ExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StorageLocation = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    IsCompressed = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportArchives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportArchives_ReportExecutions_ExecutionId",
                        column: x => x.ExecutionId,
                        principalTable: "ReportExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOperations_DealReferenceId",
                table: "ShippingOperations",
                column: "DealReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOperations_IsSplit",
                table: "ShippingOperations",
                column: "IsSplit");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOperations_ParentId_SplitSequence",
                table: "ShippingOperations",
                columns: new[] { "ParentShippingOperationId", "SplitSequence" });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOperations_ParentShippingOperationId",
                table: "ShippingOperations",
                column: "ParentShippingOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesSettlements_DealReferenceId",
                table: "SalesSettlements",
                column: "DealReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesSettlements_IsLatestVersion",
                table: "SalesSettlements",
                column: "IsLatestVersion");

            migrationBuilder.CreateIndex(
                name: "IX_SalesSettlements_OriginalSettlementId",
                table: "SalesSettlements",
                column: "OriginalSettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesSettlements_OriginalSettlementId_Sequence",
                table: "SalesSettlements",
                columns: new[] { "OriginalSettlementId", "SettlementSequence" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesSettlements_PreviousSettlementId",
                table: "SalesSettlements",
                column: "PreviousSettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_DealReferenceId",
                table: "SalesContracts",
                column: "DealReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_DealReferenceId_Status",
                table: "SalesContracts",
                columns: new[] { "DealReferenceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_PricingStatus",
                table: "SalesContracts",
                column: "PricingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSettlements_DealReferenceId",
                table: "PurchaseSettlements",
                column: "DealReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSettlements_IsLatestVersion",
                table: "PurchaseSettlements",
                column: "IsLatestVersion");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSettlements_OriginalSettlementId",
                table: "PurchaseSettlements",
                column: "OriginalSettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSettlements_OriginalSettlementId_Sequence",
                table: "PurchaseSettlements",
                columns: new[] { "OriginalSettlementId", "SettlementSequence" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSettlements_PreviousSettlementId",
                table: "PurchaseSettlements",
                column: "PreviousSettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_DealReferenceId",
                table: "PurchaseContracts",
                column: "DealReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_DealReferenceId_Status",
                table: "PurchaseContracts",
                columns: new[] { "DealReferenceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_PricingStatus",
                table: "PurchaseContracts",
                column: "PricingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PaperContracts_HedgedContractId",
                table: "PaperContracts",
                column: "HedgedContractId");

            migrationBuilder.CreateIndex(
                name: "IX_PaperContracts_HedgedContractId_Type",
                table: "PaperContracts",
                columns: new[] { "HedgedContractId", "HedgedContractType" });

            migrationBuilder.CreateIndex(
                name: "IX_PaperContracts_IsDesignatedHedge",
                table: "PaperContracts",
                column: "IsDesignatedHedge");

            migrationBuilder.CreateIndex(
                name: "IX_PaperContracts_IsDesignatedHedge_Status",
                table: "PaperContracts",
                columns: new[] { "IsDesignatedHedge", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_ProductCode_ContractMonth_PriceDate_PriceType",
                table: "MarketPrices",
                columns: new[] { "ProductCode", "ContractMonth", "PriceDate", "PriceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_ProductCode_Region_PriceDate_PriceType",
                table: "MarketPrices",
                columns: new[] { "ProductCode", "Region", "PriceDate", "PriceType" });

            migrationBuilder.CreateIndex(
                name: "IX_ContractSettlements_DealReferenceId",
                table: "ContractSettlements",
                column: "DealReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractSettlements_IsLatestVersion",
                table: "ContractSettlements",
                column: "IsLatestVersion");

            migrationBuilder.CreateIndex(
                name: "IX_ContractSettlements_OriginalSettlementId",
                table: "ContractSettlements",
                column: "OriginalSettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractSettlements_OriginalSettlementId_Sequence",
                table: "ContractSettlements",
                columns: new[] { "OriginalSettlementId", "SettlementSequence" });

            migrationBuilder.CreateIndex(
                name: "IX_ContractSettlements_PreviousSettlementId",
                table: "ContractSettlements",
                column: "PreviousSettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRiskAlerts_AlertType",
                table: "PaymentRiskAlerts",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRiskAlerts_CreatedDate",
                table: "PaymentRiskAlerts",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRiskAlerts_Severity",
                table: "PaymentRiskAlerts",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRiskAlerts_TradingPartnerId",
                table: "PaymentRiskAlerts",
                column: "TradingPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRiskAlerts_TradingPartnerId_IsResolved",
                table: "PaymentRiskAlerts",
                columns: new[] { "TradingPartnerId", "IsResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportArchives_ArchiveDate",
                table: "ReportArchives",
                column: "ArchiveDate");

            migrationBuilder.CreateIndex(
                name: "IX_ReportArchives_ExecutionId",
                table: "ReportArchives",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportArchives_ExpiryDate",
                table: "ReportArchives",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_ReportArchives_ExpiryDate_IsDeleted",
                table: "ReportArchives",
                columns: new[] { "ExpiryDate", "IsDeleted" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ReportConfigurations_CreatedBy",
                table: "ReportConfigurations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReportConfigurations_CreatedDate",
                table: "ReportConfigurations",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_ReportConfigurations_IsActive_IsDeleted",
                table: "ReportConfigurations",
                columns: new[] { "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportConfigurations_Name",
                table: "ReportConfigurations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ReportConfigurations_ReportType",
                table: "ReportConfigurations",
                column: "ReportType");

            migrationBuilder.CreateIndex(
                name: "IX_ReportConfigurations_UpdatedBy",
                table: "ReportConfigurations",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDistributions_ChannelType",
                table: "ReportDistributions",
                column: "ChannelType");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDistributions_IsEnabled",
                table: "ReportDistributions",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDistributions_ReportConfigId",
                table: "ReportDistributions",
                column: "ReportConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDistributions_ReportConfigId_ChannelType",
                table: "ReportDistributions",
                columns: new[] { "ReportConfigId", "ChannelType" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportExecutions_ExecutedBy",
                table: "ReportExecutions",
                column: "ExecutedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReportExecutions_ExecutionStartTime_Active",
                table: "ReportExecutions",
                column: "ExecutionStartTime",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ReportExecutions_IsScheduled",
                table: "ReportExecutions",
                column: "IsScheduled");

            migrationBuilder.CreateIndex(
                name: "IX_ReportExecutions_ReportConfigId",
                table: "ReportExecutions",
                column: "ReportConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportExecutions_ReportConfigId_Status",
                table: "ReportExecutions",
                columns: new[] { "ReportConfigId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportExecutions_Status",
                table: "ReportExecutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSchedules_Frequency_IsEnabled",
                table: "ReportSchedules",
                columns: new[] { "Frequency", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportSchedules_IsEnabled",
                table: "ReportSchedules",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSchedules_NextRunDate",
                table: "ReportSchedules",
                column: "NextRunDate",
                filter: "[IsEnabled] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSchedules_ReportConfigId",
                table: "ReportSchedules",
                column: "ReportConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionRuleId",
                table: "RuleExecutionRecords",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionStartTime",
                table: "RuleExecutionRecords",
                column: "ExecutionStartTime");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionStatus",
                table: "RuleExecutionRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RuleExecutionTime",
                table: "RuleExecutionRecords",
                columns: new[] { "RuleId", "ExecutionStartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_CreatedDate",
                table: "SettlementAutomationRules",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_LastExecuted",
                table: "SettlementAutomationRules",
                column: "LastExecutedDate");

            migrationBuilder.CreateIndex(
                name: "IX_RuleTypeEnabled",
                table: "SettlementAutomationRules",
                columns: new[] { "RuleType", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_SettlementAutomationRules_IsEnabled",
                table: "SettlementAutomationRules",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementAutomationRules_Name",
                table: "SettlementAutomationRules",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SettlementAutomationRules_Status",
                table: "SettlementAutomationRules",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ActionType",
                table: "SettlementRuleActions",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_RuleActionSequence",
                table: "SettlementRuleActions",
                columns: new[] { "RuleId", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_RuleConditionSequence",
                table: "SettlementRuleConditions",
                columns: new[] { "RuleId", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_SettlementTemplatePermissions_PermissionLevel",
                table: "SettlementTemplatePermissions",
                column: "PermissionLevel");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementTemplatePermissions_TemplateId_UserId",
                table: "SettlementTemplatePermissions",
                columns: new[] { "TemplateId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SettlementTemplatePermissions_UserId",
                table: "SettlementTemplatePermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementTemplateUsages_AppliedAt",
                table: "SettlementTemplateUsages",
                column: "AppliedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementTemplateUsages_SettlementId",
                table: "SettlementTemplateUsages",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementTemplateUsages_TemplateId",
                table: "SettlementTemplateUsages",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementTemplates_CreatedByUserId",
                table: "SettlementTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementTemplates_IsActive",
                table: "SettlementTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementTemplates_IsPublic",
                table: "SettlementTemplates",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementTemplates_Name",
                table: "SettlementTemplates",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_ContractSettlements_ContractSettlements_OriginalSettlementId",
                table: "ContractSettlements",
                column: "OriginalSettlementId",
                principalTable: "ContractSettlements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractSettlements_ContractSettlements_PreviousSettlementId",
                table: "ContractSettlements",
                column: "PreviousSettlementId",
                principalTable: "ContractSettlements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseSettlements_PurchaseSettlements_OriginalSettlementId",
                table: "PurchaseSettlements",
                column: "OriginalSettlementId",
                principalTable: "PurchaseSettlements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseSettlements_PurchaseSettlements_PreviousSettlementId",
                table: "PurchaseSettlements",
                column: "PreviousSettlementId",
                principalTable: "PurchaseSettlements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesSettlements_SalesSettlements_OriginalSettlementId",
                table: "SalesSettlements",
                column: "OriginalSettlementId",
                principalTable: "SalesSettlements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesSettlements_SalesSettlements_PreviousSettlementId",
                table: "SalesSettlements",
                column: "PreviousSettlementId",
                principalTable: "SalesSettlements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingOperations_ShippingOperations_ParentShippingOperationId",
                table: "ShippingOperations",
                column: "ParentShippingOperationId",
                principalTable: "ShippingOperations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractSettlements_ContractSettlements_OriginalSettlementId",
                table: "ContractSettlements");

            migrationBuilder.DropForeignKey(
                name: "FK_ContractSettlements_ContractSettlements_PreviousSettlementId",
                table: "ContractSettlements");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseSettlements_PurchaseSettlements_OriginalSettlementId",
                table: "PurchaseSettlements");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseSettlements_PurchaseSettlements_PreviousSettlementId",
                table: "PurchaseSettlements");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesSettlements_SalesSettlements_OriginalSettlementId",
                table: "SalesSettlements");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesSettlements_SalesSettlements_PreviousSettlementId",
                table: "SalesSettlements");

            migrationBuilder.DropForeignKey(
                name: "FK_ShippingOperations_ShippingOperations_ParentShippingOperationId",
                table: "ShippingOperations");

            migrationBuilder.DropTable(
                name: "PaymentRiskAlerts");

            migrationBuilder.DropTable(
                name: "ReportArchives");

            migrationBuilder.DropTable(
                name: "ReportDistributions");

            migrationBuilder.DropTable(
                name: "ReportSchedules");

            migrationBuilder.DropTable(
                name: "RuleExecutionRecords");

            migrationBuilder.DropTable(
                name: "SettlementRuleActions");

            migrationBuilder.DropTable(
                name: "SettlementRuleConditions");

            migrationBuilder.DropTable(
                name: "SettlementTemplatePermissions");

            migrationBuilder.DropTable(
                name: "SettlementTemplateUsages");

            migrationBuilder.DropTable(
                name: "ReportExecutions");

            migrationBuilder.DropTable(
                name: "SettlementAutomationRules");

            migrationBuilder.DropTable(
                name: "SettlementTemplates");

            migrationBuilder.DropTable(
                name: "ReportConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_ShippingOperations_DealReferenceId",
                table: "ShippingOperations");

            migrationBuilder.DropIndex(
                name: "IX_ShippingOperations_IsSplit",
                table: "ShippingOperations");

            migrationBuilder.DropIndex(
                name: "IX_ShippingOperations_ParentId_SplitSequence",
                table: "ShippingOperations");

            migrationBuilder.DropIndex(
                name: "IX_ShippingOperations_ParentShippingOperationId",
                table: "ShippingOperations");

            migrationBuilder.DropIndex(
                name: "IX_SalesSettlements_DealReferenceId",
                table: "SalesSettlements");

            migrationBuilder.DropIndex(
                name: "IX_SalesSettlements_IsLatestVersion",
                table: "SalesSettlements");

            migrationBuilder.DropIndex(
                name: "IX_SalesSettlements_OriginalSettlementId",
                table: "SalesSettlements");

            migrationBuilder.DropIndex(
                name: "IX_SalesSettlements_OriginalSettlementId_Sequence",
                table: "SalesSettlements");

            migrationBuilder.DropIndex(
                name: "IX_SalesSettlements_PreviousSettlementId",
                table: "SalesSettlements");

            migrationBuilder.DropIndex(
                name: "IX_SalesContracts_DealReferenceId",
                table: "SalesContracts");

            migrationBuilder.DropIndex(
                name: "IX_SalesContracts_DealReferenceId_Status",
                table: "SalesContracts");

            migrationBuilder.DropIndex(
                name: "IX_SalesContracts_PricingStatus",
                table: "SalesContracts");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseSettlements_DealReferenceId",
                table: "PurchaseSettlements");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseSettlements_IsLatestVersion",
                table: "PurchaseSettlements");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseSettlements_OriginalSettlementId",
                table: "PurchaseSettlements");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseSettlements_OriginalSettlementId_Sequence",
                table: "PurchaseSettlements");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseSettlements_PreviousSettlementId",
                table: "PurchaseSettlements");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseContracts_DealReferenceId",
                table: "PurchaseContracts");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseContracts_DealReferenceId_Status",
                table: "PurchaseContracts");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseContracts_PricingStatus",
                table: "PurchaseContracts");

            migrationBuilder.DropIndex(
                name: "IX_PaperContracts_HedgedContractId",
                table: "PaperContracts");

            migrationBuilder.DropIndex(
                name: "IX_PaperContracts_HedgedContractId_Type",
                table: "PaperContracts");

            migrationBuilder.DropIndex(
                name: "IX_PaperContracts_IsDesignatedHedge",
                table: "PaperContracts");

            migrationBuilder.DropIndex(
                name: "IX_PaperContracts_IsDesignatedHedge_Status",
                table: "PaperContracts");

            migrationBuilder.DropIndex(
                name: "IX_MarketPrices_ProductCode_ContractMonth_PriceDate_PriceType",
                table: "MarketPrices");

            migrationBuilder.DropIndex(
                name: "IX_MarketPrices_ProductCode_Region_PriceDate_PriceType",
                table: "MarketPrices");

            migrationBuilder.DropIndex(
                name: "IX_ContractSettlements_DealReferenceId",
                table: "ContractSettlements");

            migrationBuilder.DropIndex(
                name: "IX_ContractSettlements_IsLatestVersion",
                table: "ContractSettlements");

            migrationBuilder.DropIndex(
                name: "IX_ContractSettlements_OriginalSettlementId",
                table: "ContractSettlements");

            migrationBuilder.DropIndex(
                name: "IX_ContractSettlements_OriginalSettlementId_Sequence",
                table: "ContractSettlements");

            migrationBuilder.DropIndex(
                name: "IX_ContractSettlements_PreviousSettlementId",
                table: "ContractSettlements");

            migrationBuilder.DropColumn(
                name: "DealReferenceId",
                table: "ShippingOperations");

            migrationBuilder.DropColumn(
                name: "IsSplit",
                table: "ShippingOperations");

            migrationBuilder.DropColumn(
                name: "OriginalPlannedQuantity",
                table: "ShippingOperations");

            migrationBuilder.DropColumn(
                name: "OriginalPlannedQuantityUnit",
                table: "ShippingOperations");

            migrationBuilder.DropColumn(
                name: "ParentShippingOperationId",
                table: "ShippingOperations");

            migrationBuilder.DropColumn(
                name: "SplitReasonNotes",
                table: "ShippingOperations");

            migrationBuilder.DropColumn(
                name: "SplitReasonType",
                table: "ShippingOperations");

            migrationBuilder.DropColumn(
                name: "SplitSequence",
                table: "ShippingOperations");

            migrationBuilder.DropColumn(
                name: "AmendmentReason",
                table: "SalesSettlements");

            migrationBuilder.DropColumn(
                name: "AmendmentType",
                table: "SalesSettlements");

            migrationBuilder.DropColumn(
                name: "DealReferenceId",
                table: "SalesSettlements");

            migrationBuilder.DropColumn(
                name: "IsLatestVersion",
                table: "SalesSettlements");

            migrationBuilder.DropColumn(
                name: "OriginalSettlementId",
                table: "SalesSettlements");

            migrationBuilder.DropColumn(
                name: "PreviousSettlementId",
                table: "SalesSettlements");

            migrationBuilder.DropColumn(
                name: "SettlementSequence",
                table: "SalesSettlements");

            migrationBuilder.DropColumn(
                name: "SupersededDate",
                table: "SalesSettlements");

            migrationBuilder.DropColumn(
                name: "DealReferenceId",
                table: "SalesContracts");

            migrationBuilder.DropColumn(
                name: "FixedPercentage",
                table: "SalesContracts");

            migrationBuilder.DropColumn(
                name: "FixedQuantity",
                table: "SalesContracts");

            migrationBuilder.DropColumn(
                name: "PriceSource",
                table: "SalesContracts");

            migrationBuilder.DropColumn(
                name: "PricingStatus",
                table: "SalesContracts");

            migrationBuilder.DropColumn(
                name: "UnfixedQuantity",
                table: "SalesContracts");

            migrationBuilder.DropColumn(
                name: "AmendmentReason",
                table: "PurchaseSettlements");

            migrationBuilder.DropColumn(
                name: "AmendmentType",
                table: "PurchaseSettlements");

            migrationBuilder.DropColumn(
                name: "DealReferenceId",
                table: "PurchaseSettlements");

            migrationBuilder.DropColumn(
                name: "IsLatestVersion",
                table: "PurchaseSettlements");

            migrationBuilder.DropColumn(
                name: "OriginalSettlementId",
                table: "PurchaseSettlements");

            migrationBuilder.DropColumn(
                name: "PreviousSettlementId",
                table: "PurchaseSettlements");

            migrationBuilder.DropColumn(
                name: "SettlementSequence",
                table: "PurchaseSettlements");

            migrationBuilder.DropColumn(
                name: "SupersededDate",
                table: "PurchaseSettlements");

            migrationBuilder.DropColumn(
                name: "DealReferenceId",
                table: "PurchaseContracts");

            migrationBuilder.DropColumn(
                name: "FixedPercentage",
                table: "PurchaseContracts");

            migrationBuilder.DropColumn(
                name: "FixedQuantity",
                table: "PurchaseContracts");

            migrationBuilder.DropColumn(
                name: "PriceSource",
                table: "PurchaseContracts");

            migrationBuilder.DropColumn(
                name: "PricingStatus",
                table: "PurchaseContracts");

            migrationBuilder.DropColumn(
                name: "UnfixedQuantity",
                table: "PurchaseContracts");

            migrationBuilder.DropColumn(
                name: "HedgeDesignationDate",
                table: "PaperContracts");

            migrationBuilder.DropColumn(
                name: "HedgeEffectiveness",
                table: "PaperContracts");

            migrationBuilder.DropColumn(
                name: "HedgeRatio",
                table: "PaperContracts");

            migrationBuilder.DropColumn(
                name: "HedgedContractId",
                table: "PaperContracts");

            migrationBuilder.DropColumn(
                name: "HedgedContractType",
                table: "PaperContracts");

            migrationBuilder.DropColumn(
                name: "IsDesignatedHedge",
                table: "PaperContracts");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "MarketPrices");

            migrationBuilder.DropColumn(
                name: "AmendmentReason",
                table: "ContractSettlements");

            migrationBuilder.DropColumn(
                name: "AmendmentType",
                table: "ContractSettlements");

            migrationBuilder.DropColumn(
                name: "DealReferenceId",
                table: "ContractSettlements");

            migrationBuilder.DropColumn(
                name: "IsLatestVersion",
                table: "ContractSettlements");

            migrationBuilder.DropColumn(
                name: "OriginalSettlementId",
                table: "ContractSettlements");

            migrationBuilder.DropColumn(
                name: "SettlementSequence",
                table: "ContractSettlements");

            migrationBuilder.RenameColumn(
                name: "LastPricingDate",
                table: "SalesContracts",
                newName: "EstimatedPaymentDate");

            migrationBuilder.RenameColumn(
                name: "LastPricingDate",
                table: "PurchaseContracts",
                newName: "EstimatedPaymentDate");

            migrationBuilder.RenameColumn(
                name: "SupersededDate",
                table: "ContractSettlements",
                newName: "ActualPaymentDate");

            migrationBuilder.RenameColumn(
                name: "PreviousSettlementId",
                table: "ContractSettlements",
                newName: "ActualPayableDueDate");

            migrationBuilder.AddColumn<Guid>(
                name: "SettlementId1",
                table: "Payments",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "MarketPrices",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldRowVersion: true,
                oldDefaultValueSql: "X'00'");

            migrationBuilder.CreateTable(
                name: "Settlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PayeePartyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PayerPartyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PurchaseContractId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SalesContractId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CancellationReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CompletedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProcessedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ProcessedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false),
                    SettlementNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "USD"),
                    TermsCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "USD"),
                    DiscountRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: true),
                    EarlyPaymentDays = table.Column<int>(type: "INTEGER", nullable: true),
                    EnableAutomaticProcessing = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    LateFeeRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: true),
                    SettlementMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentTerms = table.Column<int>(type: "INTEGER", nullable: false)
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
                name: "SettlementAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AdjustedBy = table.Column<Guid>(type: "TEXT", nullable: false),
                    AdjustmentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false),
                    SettlementId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "USD")
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

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SettlementId1",
                table: "Payments",
                column: "SettlementId1");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_ProductCode_PriceDate",
                table: "MarketPrices",
                columns: new[] { "ProductCode", "PriceDate" },
                unique: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Settlements_SettlementId",
                table: "Payments",
                column: "SettlementId",
                principalTable: "Settlements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Settlements_SettlementId1",
                table: "Payments",
                column: "SettlementId1",
                principalTable: "Settlements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
