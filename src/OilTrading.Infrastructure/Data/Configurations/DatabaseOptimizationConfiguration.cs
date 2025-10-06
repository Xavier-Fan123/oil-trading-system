using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

/// <summary>
/// Database optimization configuration for performance enhancement
/// </summary>
public static class DatabaseOptimizationConfiguration
{
    /// <summary>
    /// Apply database optimization configurations
    /// </summary>
    public static void ApplyOptimizations(ModelBuilder modelBuilder)
    {
        // Configure indexes for high-frequency queries
        ConfigureIndexes(modelBuilder);
        
        // Configure table partitioning strategies
        ConfigurePartitioning(modelBuilder);
        
        // Configure query optimization hints
        ConfigureQueryOptimizations(modelBuilder);
        
        // Configure materialized views for reporting
        ConfigureMaterializedViews(modelBuilder);
    }

    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Purchase Contracts - High frequency query patterns
        modelBuilder.Entity<PurchaseContract>(entity =>
        {
            // Composite index for status and date-based queries
            entity.HasIndex(e => new { e.Status, e.CreatedAt })
                .HasDatabaseName("IX_PurchaseContracts_Status_CreatedAt")
                .IncludeProperties(e => new { e.TradingPartnerId, e.ProductId });

            // Index for supplier-based queries
            entity.HasIndex(e => new { e.TradingPartnerId, e.Status })
                .HasDatabaseName("IX_PurchaseContracts_Supplier_Status");

            // Index for product-based queries
            entity.HasIndex(e => new { e.ProductId, e.Status })
                .HasDatabaseName("IX_PurchaseContracts_Product_Status");

            // Index for trader performance queries
            entity.HasIndex(e => new { e.TraderId, e.CreatedAt })
                .HasDatabaseName("IX_PurchaseContracts_Trader_Date");

            // Index for laycan period queries (shipping optimization)
            entity.HasIndex(e => new { e.LaycanStart, e.LaycanEnd })
                .HasDatabaseName("IX_PurchaseContracts_Laycan_Period")
                .HasFilter("Status IN (1, 2)"); // Only for Draft and Active

            // Covering index for contract summaries
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_PurchaseContracts_Status_Covering")
                .IncludeProperties(e => new 
                { 
                    e.TradingPartnerId, 
                    e.ProductId, 
                    e.TraderId, 
                    e.CreatedAt,
                    e.UpdatedAt
                });
        });

        // Sales Contracts - Similar optimization patterns
        modelBuilder.Entity<SalesContract>(entity =>
        {
            entity.HasIndex(e => new { e.Status, e.CreatedAt })
                .HasDatabaseName("IX_SalesContracts_Status_CreatedAt");

            entity.HasIndex(e => new { e.TradingPartnerId, e.Status })
                .HasDatabaseName("IX_SalesContracts_Customer_Status");

            entity.HasIndex(e => new { e.ProductId, e.LaycanEnd })
                .HasDatabaseName("IX_SalesContracts_Product_Delivery");
        });

        // Pricing Events - Time-series data optimization
        modelBuilder.Entity<PricingEvent>(entity =>
        {
            // Primary time-series index
            entity.HasIndex(e => new { e.ContractId, e.EventDate })
                .HasDatabaseName("IX_PricingEvents_Contract_Date_DESC")
                .IsDescending(false, true); // ContractId ASC, EventDate DESC

            // Index for recent price queries
            entity.HasIndex(e => e.EventDate)
                .HasDatabaseName("IX_PricingEvents_EventDate_Recent")
                .HasFilter("EventDate >= DATEADD(month, -3, GETDATE())")
                .IncludeProperties(e => new { e.ContractId, e.EventType });

            // Index for event type and date
            entity.HasIndex(e => new { e.EventType, e.EventDate })
                .HasDatabaseName("IX_PricingEvents_Type_Date");

            // Index for event type and contract filtering
            entity.HasIndex(e => new { e.EventType, e.ContractId, e.EventDate })
                .HasDatabaseName("IX_PricingEvents_Type_Contract_Date");
        });

        // Shipping Operations - Logistics optimization
        modelBuilder.Entity<ShippingOperation>(entity =>
        {
            // Index for vessel tracking
            entity.HasIndex(e => new { e.VesselName, e.Status })
                .HasDatabaseName("IX_ShippingOperations_Vessel_Status");

            // Index for port operations
            entity.HasIndex(e => new { e.LoadPort, e.LoadPortETA })
                .HasDatabaseName("IX_ShippingOperations_Port_Loading");

            entity.HasIndex(e => new { e.DischargePort, e.DischargePortETA })
                .HasDatabaseName("IX_ShippingOperations_Port_Discharge");

            // Index for contract linking
            entity.HasIndex(e => e.ContractId)
                .HasDatabaseName("IX_ShippingOperations_ContractId");
        });

        // Trading Partners - Counterparty analysis
        modelBuilder.Entity<TradingPartner>(entity =>
        {
            // Index for partner type and status
            entity.HasIndex(e => new { e.PartnerType, e.IsActive })
                .HasDatabaseName("IX_TradingPartners_Type_Active")
                .IncludeProperties(e => new { e.Name, e.Country, e.CreditRating });

            // Index for geographical analysis
            entity.HasIndex(e => new { e.Country, e.IsActive })
                .HasDatabaseName("IX_TradingPartners_Country_Active");

            // Index for credit rating queries
            entity.HasIndex(e => new { e.CreditRating, e.IsActive })
                .HasDatabaseName("IX_TradingPartners_Rating_Active")
                .HasFilter("CreditRating IS NOT NULL");
        });

        // Products - Reference data optimization
        modelBuilder.Entity<Product>(entity =>
        {
            // Index for product type queries
            entity.HasIndex(e => new { e.Type, e.IsActive })
                .HasDatabaseName("IX_Products_Type_Active")
                .IncludeProperties(e => new { e.Name, e.Specification });

            // Index for product code
            entity.HasIndex(e => e.Code)
                .HasDatabaseName("IX_Products_Code")
                .IsUnique();
        });

        // Users - Authentication and authorization
        modelBuilder.Entity<User>(entity =>
        {
            // Index for authentication
            entity.HasIndex(e => new { e.Email, e.IsActive })
                .HasDatabaseName("IX_Users_Email_Active")
                .IsUnique()
                .HasFilter("IsActive = 1");

            // Index for role-based access
            entity.HasIndex(e => new { e.Role, e.IsActive })
                .HasDatabaseName("IX_Users_Role_Active");

            // Index for role queries
            entity.HasIndex(e => new { e.Role, e.IsActive })
                .HasDatabaseName("IX_Users_Role_Active2");
        });
    }

    private static void ConfigurePartitioning(ModelBuilder modelBuilder)
    {
        // Configure partitioning for large tables
        // Note: EF Core doesn't directly support partitioning, but we can configure it via raw SQL

        // Pricing Events - Partition by date (monthly partitions)
        modelBuilder.Entity<PricingEvent>(entity =>
        {
            entity.ToTable("PricingEvents", tb =>
            {
                // This would be implemented via migration SQL
                tb.HasComment("Partitioned by EventDate (monthly partitions for performance)");
            });
        });

        // Purchase Contracts - Partition by created date (yearly partitions)
        modelBuilder.Entity<PurchaseContract>(entity =>
        {
            entity.ToTable("PurchaseContracts", tb =>
            {
                tb.HasComment("Consider partitioning by CreatedAt for large datasets");
            });
        });

        // Audit logs - Partition by timestamp (monthly partitions)
        // Note: This would be for a dedicated audit table in production
    }

    private static void ConfigureQueryOptimizations(ModelBuilder modelBuilder)
    {
        // Configure query filters for active entities
        // Note: PurchaseContract and SalesContract use Status field instead of IsDeleted

        modelBuilder.Entity<TradingPartner>()
            .HasQueryFilter(e => e.IsActive);

        modelBuilder.Entity<Product>()
            .HasQueryFilter(e => e.IsActive);

        modelBuilder.Entity<User>()
            .HasQueryFilter(e => e.IsActive);

        // Configure default ordering for time-series data
        modelBuilder.Entity<PricingEvent>()
            .HasIndex(e => e.EventDate)
            .IsDescending();
    }

    private static void ConfigureMaterializedViews(ModelBuilder modelBuilder)
    {
        // Configure views for complex reporting queries
        // These would be implemented as actual views or indexed views in the database

        // Contract Summary View
        modelBuilder.Entity<ContractSummaryView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_ContractSummary");
            
            entity.Property(e => e.ContractId);
            entity.Property(e => e.ContractNumber);
            entity.Property(e => e.ContractType);
            entity.Property(e => e.Status);
            entity.Property(e => e.SupplierName);
            entity.Property(e => e.ProductName);
            entity.Property(e => e.TraderName);
            entity.Property(e => e.ContractValue);
            entity.Property(e => e.CreatedAt);
            entity.Property(e => e.LaycanStart);
            entity.Property(e => e.LaycanEnd);
        });

        // Price Analytics View
        modelBuilder.Entity<PriceAnalyticsView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_PriceAnalytics");
            
            entity.Property(e => e.ProductId);
            entity.Property(e => e.ProductName);
            entity.Property(e => e.Date);
            entity.Property(e => e.OpenPrice);
            entity.Property(e => e.HighPrice);
            entity.Property(e => e.LowPrice);
            entity.Property(e => e.ClosePrice);
            entity.Property(e => e.Volume);
            entity.Property(e => e.MovingAverage7);
            entity.Property(e => e.MovingAverage30);
            entity.Property(e => e.Volatility);
        });

        // Risk Summary View
        modelBuilder.Entity<RiskSummaryView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_RiskSummary");
            
            entity.Property(e => e.Date);
            entity.Property(e => e.PortfolioValue);
            entity.Property(e => e.VaR95);
            entity.Property(e => e.VaR99);
            entity.Property(e => e.ExpectedShortfall95);
            entity.Property(e => e.ExpectedShortfall99);
            entity.Property(e => e.MaxDrawdown);
            entity.Property(e => e.Volatility);
        });

        // Trading Performance View
        modelBuilder.Entity<TradingPerformanceView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_TradingPerformance");
            
            entity.Property(e => e.TraderId);
            entity.Property(e => e.TraderName);
            entity.Property(e => e.Month);
            entity.Property(e => e.Year);
            entity.Property(e => e.TotalContracts);
            entity.Property(e => e.TotalVolume);
            entity.Property(e => e.TotalValue);
            entity.Property(e => e.ProfitLoss);
            entity.Property(e => e.SuccessRate);
        });
    }

    /// <summary>
    /// Get SQL scripts for creating database partitions
    /// </summary>
    public static class PartitioningScripts
    {
        public const string CreatePricingEventsPartitionFunction = @"
            -- Create partition function for pricing events (monthly partitions)
            CREATE PARTITION FUNCTION PF_PricingEvents_EventDate (datetime2)
            AS RANGE RIGHT FOR VALUES (
                '2024-01-01', '2024-02-01', '2024-03-01', '2024-04-01',
                '2024-05-01', '2024-06-01', '2024-07-01', '2024-08-01',
                '2024-09-01', '2024-10-01', '2024-11-01', '2024-12-01',
                '2025-01-01', '2025-02-01', '2025-03-01', '2025-04-01',
                '2025-05-01', '2025-06-01', '2025-07-01', '2025-08-01',
                '2025-09-01', '2025-10-01', '2025-11-01', '2025-12-01'
            );";

        public const string CreatePricingEventsPartitionScheme = @"
            -- Create partition scheme for pricing events
            CREATE PARTITION SCHEME PS_PricingEvents_EventDate
            AS PARTITION PF_PricingEvents_EventDate
            ALL TO ([PRIMARY]);";

        public const string CreateContractSummaryView = @"
            -- Create materialized view for contract summary
            CREATE VIEW vw_ContractSummary
            WITH SCHEMABINDING
            AS
            SELECT 
                pc.Id as ContractId,
                pc.ContractNumber_Value as ContractNumber,
                'Purchase' as ContractType,
                pc.Status,
                tp.Name as SupplierName,
                p.Name as ProductName,
                u.FirstName + ' ' + u.LastName as TraderName,
                pc.Quantity_Value * ISNULL(pc.PriceFormula_FixedPrice, 75.0) as ContractValue,
                pc.CreatedAt,
                pc.LaycanStart,
                pc.LaycanEnd
            FROM dbo.PurchaseContracts pc
            INNER JOIN dbo.TradingPartners tp ON pc.TradingPartnerId = tp.Id
            INNER JOIN dbo.Products p ON pc.ProductId = p.Id
            INNER JOIN dbo.Users u ON pc.TraderId = u.Id
            WHERE pc.IsDeleted = 0;

            -- Create clustered index on the view
            CREATE UNIQUE CLUSTERED INDEX IX_vw_ContractSummary_ContractId
            ON vw_ContractSummary (ContractId);

            -- Create additional indexes for common queries
            CREATE NONCLUSTERED INDEX IX_vw_ContractSummary_Status_Date
            ON vw_ContractSummary (Status, CreatedAt);

            CREATE NONCLUSTERED INDEX IX_vw_ContractSummary_Supplier
            ON vw_ContractSummary (SupplierName);";

        public const string CreatePriceAnalyticsView = @"
            -- Create view for price analytics with calculated fields
            CREATE VIEW vw_PriceAnalytics
            WITH SCHEMABINDING
            AS
            SELECT 
                pe.ProductId,
                p.Name as ProductName,
                CAST(pe.EventDate AS date) as Date,
                MIN(pe.Price_Amount) as OpenPrice,
                MAX(pe.Price_Amount) as HighPrice,
                MIN(pe.Price_Amount) as LowPrice,
                MAX(pe.Price_Amount) as ClosePrice,
                SUM(ISNULL(pe.Volume_Value, 0)) as Volume,
                AVG(pe.Price_Amount) OVER (
                    PARTITION BY pe.ProductId 
                    ORDER BY CAST(pe.EventDate AS date) 
                    ROWS BETWEEN 6 PRECEDING AND CURRENT ROW
                ) as MovingAverage7,
                AVG(pe.Price_Amount) OVER (
                    PARTITION BY pe.ProductId 
                    ORDER BY CAST(pe.EventDate AS date) 
                    ROWS BETWEEN 29 PRECEDING AND CURRENT ROW
                ) as MovingAverage30,
                STDEV(pe.Price_Amount) OVER (
                    PARTITION BY pe.ProductId 
                    ORDER BY CAST(pe.EventDate AS date) 
                    ROWS BETWEEN 29 PRECEDING AND CURRENT ROW
                ) as Volatility
            FROM dbo.PricingEvents pe
            INNER JOIN dbo.Products p ON pe.ProductId = p.Id
            WHERE p.IsActive = 1
            GROUP BY pe.ProductId, p.Name, CAST(pe.EventDate AS date), pe.Price_Amount, pe.Volume_Value, pe.EventDate;

            -- Create clustered index
            CREATE UNIQUE CLUSTERED INDEX IX_vw_PriceAnalytics_Product_Date
            ON vw_PriceAnalytics (ProductId, Date);";

        public const string CreateIndexOptimizationScript = @"
            -- Rebuild and reorganize indexes for optimal performance
            DECLARE @SQL NVARCHAR(MAX) = '';
            
            -- Generate index maintenance commands
            SELECT @SQL = @SQL + 
                CASE 
                    WHEN avg_fragmentation_in_percent > 30 THEN
                        'ALTER INDEX ' + i.name + ' ON ' + SCHEMA_NAME(t.schema_id) + '.' + t.name + ' REBUILD;' + CHAR(13)
                    WHEN avg_fragmentation_in_percent > 10 THEN
                        'ALTER INDEX ' + i.name + ' ON ' + SCHEMA_NAME(t.schema_id) + '.' + t.name + ' REORGANIZE;' + CHAR(13)
                    ELSE ''
                END
            FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
            INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
            INNER JOIN sys.tables t ON i.object_id = t.object_id
            WHERE avg_fragmentation_in_percent > 10
            AND i.name IS NOT NULL;
            
            -- Execute the maintenance commands
            EXEC sp_executesql @SQL;
            
            -- Update statistics
            EXEC sp_updatestats;";
    }
}

/// <summary>
/// View entities for materialized views
/// </summary>
public class ContractSummaryView
{
    public Guid ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public int Status { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string TraderName { get; set; } = string.Empty;
    public decimal ContractValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LaycanStart { get; set; }
    public DateTime LaycanEnd { get; set; }
}

public class PriceAnalyticsView
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal Volume { get; set; }
    public decimal MovingAverage7 { get; set; }
    public decimal MovingAverage30 { get; set; }
    public decimal Volatility { get; set; }
}

public class RiskSummaryView
{
    public DateTime Date { get; set; }
    public decimal PortfolioValue { get; set; }
    public decimal VaR95 { get; set; }
    public decimal VaR99 { get; set; }
    public decimal ExpectedShortfall95 { get; set; }
    public decimal ExpectedShortfall99 { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal Volatility { get; set; }
}

public class TradingPerformanceView
{
    public Guid TraderId { get; set; }
    public string TraderName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public int TotalContracts { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal TotalValue { get; set; }
    public decimal ProfitLoss { get; set; }
    public decimal SuccessRate { get; set; }
}