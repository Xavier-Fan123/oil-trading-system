-- =====================================
-- 油品贸易系统数据库优化脚本
-- 专为高性能查询和大数据量处理设计
-- =====================================

-- 1. 创建高性能索引策略
-- =====================================

-- 合同查询优化索引
CREATE NONCLUSTERED INDEX IX_PurchaseContracts_Performance_Optimized
ON PurchaseContracts (Status, CreatedAt DESC)
INCLUDE (TradingPartnerId, ProductId, TraderId, LaycanStart, LaycanEnd, Quantity_Value)
WHERE Status IN (1, 2, 3); -- Active, Draft, PendingApproval

-- 供应商和客户查询优化
CREATE NONCLUSTERED INDEX IX_PurchaseContracts_Supplier_Product_Status
ON PurchaseContracts (TradingPartnerId, ProductId, Status)
INCLUDE (CreatedAt, Quantity_Value, LaycanStart);

-- 交易员绩效查询优化
CREATE NONCLUSTERED INDEX IX_PurchaseContracts_Trader_Performance
ON PurchaseContracts (TraderId, CreatedAt DESC, Status)
INCLUDE (TradingPartnerId, ProductId, Quantity_Value);

-- 价格事件时序数据优化
CREATE NONCLUSTERED INDEX IX_PricingEvents_TimeSeries_Optimized
ON PricingEvents (ProductId, EventDate DESC)
INCLUDE (EventType, Price_Amount, Volume_Value, ContractId)
WHERE EventDate >= DATEADD(year, -2, GETDATE());

-- 近期价格查询优化（热数据）
CREATE NONCLUSTERED INDEX IX_PricingEvents_Recent_Hot_Data
ON PricingEvents (EventDate DESC, ProductId)
INCLUDE (Price_Amount, EventType)
WHERE EventDate >= DATEADD(month, -3, GETDATE());

-- 库存管理查询优化
CREATE NONCLUSTERED INDEX IX_InventoryPositions_Location_Product
ON InventoryPositions (LocationId, ProductId, LastUpdated DESC)
INCLUDE (AvailableQuantity_Value, ReservedQuantity_Value, TotalQuantity_Value);

-- 库存预留查询优化
CREATE NONCLUSTERED INDEX IX_InventoryReservations_Active
ON InventoryReservations (ProductId, LocationId, Status, ExpiresAt)
WHERE Status = 1 AND ExpiresAt > GETDATE();

-- 审计日志时序优化
CREATE NONCLUSTERED INDEX IX_OperationAuditLogs_TimeSeries
ON OperationAuditLogs (Timestamp DESC, EntityType, OperationType)
INCLUDE (EntityId, UserId, Details);

-- 2. 数据分区策略（按时间分区）
-- =====================================

-- 创建价格事件分区函数（按月分区）
CREATE PARTITION FUNCTION PF_PricingEvents_Monthly (datetime2)
AS RANGE RIGHT FOR VALUES (
    '2024-01-01', '2024-02-01', '2024-03-01', '2024-04-01', '2024-05-01', '2024-06-01',
    '2024-07-01', '2024-08-01', '2024-09-01', '2024-10-01', '2024-11-01', '2024-12-01',
    '2025-01-01', '2025-02-01', '2025-03-01', '2025-04-01', '2025-05-01', '2025-06-01',
    '2025-07-01', '2025-08-01', '2025-09-01', '2025-10-01', '2025-11-01', '2025-12-01',
    '2026-01-01', '2026-02-01', '2026-03-01', '2026-04-01', '2026-05-01', '2026-06-01'
);

-- 创建价格事件分区架构
CREATE PARTITION SCHEME PS_PricingEvents_Monthly
AS PARTITION PF_PricingEvents_Monthly
ALL TO ([PRIMARY]);

-- 创建审计日志分区函数（按季度分区）
CREATE PARTITION FUNCTION PF_AuditLogs_Quarterly (datetime2)
AS RANGE RIGHT FOR VALUES (
    '2024-01-01', '2024-04-01', '2024-07-01', '2024-10-01',
    '2025-01-01', '2025-04-01', '2025-07-01', '2025-10-01',
    '2026-01-01', '2026-04-01', '2026-07-01', '2026-10-01'
);

-- 创建审计日志分区架构
CREATE PARTITION SCHEME PS_AuditLogs_Quarterly
AS PARTITION PF_AuditLogs_Quarterly
ALL TO ([PRIMARY]);

-- 3. 高性能查询优化视图
-- =====================================

-- 合同汇总视图（物化视图）
CREATE VIEW vw_ContractSummary_Optimized
WITH SCHEMABINDING
AS
SELECT 
    pc.Id as ContractId,
    pc.ContractNumber_Value as ContractNumber,
    'Purchase' as ContractType,
    pc.Status,
    tp.Name as CounterpartyName,
    p.Name as ProductName,
    p.Type as ProductType,
    u.FirstName + ' ' + u.LastName as TraderName,
    pc.Quantity_Value as Quantity,
    pc.Quantity_Unit as QuantityUnit,
    ISNULL(pc.PriceFormula_FixedPrice, 0) * pc.Quantity_Value as EstimatedValue,
    pc.CreatedAt,
    pc.LaycanStart,
    pc.LaycanEnd,
    DATEDIFF(day, pc.LaycanStart, pc.LaycanEnd) as LaycanDays,
    CASE 
        WHEN pc.LaycanEnd < GETDATE() THEN 'Expired'
        WHEN pc.LaycanStart <= GETDATE() AND pc.LaycanEnd >= GETDATE() THEN 'Active'
        ELSE 'Future'
    END as LaycanStatus
FROM dbo.PurchaseContracts pc
INNER JOIN dbo.TradingPartners tp ON pc.TradingPartnerId = tp.Id
INNER JOIN dbo.Products p ON pc.ProductId = p.Id
INNER JOIN dbo.Users u ON pc.TraderId = u.Id
WHERE pc.IsDeleted = 0 AND tp.IsActive = 1 AND p.IsActive = 1;

-- 为合同汇总视图创建聚集索引
CREATE UNIQUE CLUSTERED INDEX IX_vw_ContractSummary_Optimized_Clustered
ON vw_ContractSummary_Optimized (ContractId);

-- 为合同汇总视图创建优化索引
CREATE NONCLUSTERED INDEX IX_vw_ContractSummary_Status_Date
ON vw_ContractSummary_Optimized (Status, CreatedAt DESC)
INCLUDE (CounterpartyName, ProductName, TraderName, Quantity, EstimatedValue);

CREATE NONCLUSTERED INDEX IX_vw_ContractSummary_Counterparty
ON vw_ContractSummary_Optimized (CounterpartyName, Status)
INCLUDE (ProductName, Quantity, EstimatedValue, CreatedAt);

CREATE NONCLUSTERED INDEX IX_vw_ContractSummary_Product
ON vw_ContractSummary_Optimized (ProductType, ProductName, Status)
INCLUDE (CounterpartyName, Quantity, EstimatedValue, LaycanStart);

-- 价格分析视图（用于风险计算）
CREATE VIEW vw_PriceAnalytics_Optimized
WITH SCHEMABINDING
AS
SELECT 
    pe.ProductId,
    p.Name as ProductName,
    p.Type as ProductType,
    CAST(pe.EventDate AS date) as Date,
    AVG(pe.Price_Amount) as AvgPrice,
    MIN(pe.Price_Amount) as MinPrice,
    MAX(pe.Price_Amount) as MaxPrice,
    COUNT(*) as PriceCount,
    SUM(ISNULL(pe.Volume_Value, 0)) as TotalVolume,
    STDEV(pe.Price_Amount) as PriceVolatility
FROM dbo.PricingEvents pe
INNER JOIN dbo.Products p ON pe.ProductId = p.Id
WHERE p.IsActive = 1 AND pe.EventDate >= DATEADD(year, -2, GETDATE())
GROUP BY pe.ProductId, p.Name, p.Type, CAST(pe.EventDate AS date);

-- 为价格分析视图创建聚集索引
CREATE UNIQUE CLUSTERED INDEX IX_vw_PriceAnalytics_Optimized_Clustered
ON vw_PriceAnalytics_Optimized (ProductId, Date DESC);

-- 4. 查询性能优化存储过程
-- =====================================

-- 高性能合同查询存储过程
CREATE OR ALTER PROCEDURE sp_GetContractsSummary_Optimized
    @Status INT = NULL,
    @TradingPartnerId UNIQUEIDENTIFIER = NULL,
    @ProductId UNIQUEIDENTIFIER = NULL,
    @TraderId UNIQUEIDENTIFIER = NULL,
    @DateFrom DATETIME2 = NULL,
    @DateTo DATETIME2 = NULL,
    @PageSize INT = 50,
    @PageNumber INT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    -- 使用参数化查询避免计划缓存污染
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;
    
    -- 优化查询计划
    OPTION (RECOMPILE);
    
    WITH ContractData AS (
        SELECT 
            ContractId,
            ContractNumber,
            Status,
            CounterpartyName,
            ProductName,
            TraderName,
            Quantity,
            EstimatedValue,
            CreatedAt,
            LaycanStart,
            LaycanEnd,
            LaycanStatus,
            ROW_NUMBER() OVER (ORDER BY CreatedAt DESC) as RowNum
        FROM vw_ContractSummary_Optimized
        WHERE (@Status IS NULL OR Status = @Status)
          AND (@TradingPartnerId IS NULL OR CounterpartyName = (SELECT Name FROM TradingPartners WHERE Id = @TradingPartnerId))
          AND (@ProductId IS NULL OR ProductName = (SELECT Name FROM Products WHERE Id = @ProductId))
          AND (@TraderId IS NULL OR TraderName = (SELECT FirstName + ' ' + LastName FROM Users WHERE Id = @TraderId))
          AND (@DateFrom IS NULL OR CreatedAt >= @DateFrom)
          AND (@DateTo IS NULL OR CreatedAt <= @DateTo)
    )
    SELECT 
        ContractId,
        ContractNumber,
        Status,
        CounterpartyName,
        ProductName,
        TraderName,
        Quantity,
        EstimatedValue,
        CreatedAt,
        LaycanStart,
        LaycanEnd,
        LaycanStatus
    FROM ContractData
    WHERE RowNum BETWEEN @Offset + 1 AND @Offset + @PageSize
    ORDER BY RowNum;
    
    -- 返回总记录数
    SELECT COUNT(*) as TotalRecords
    FROM vw_ContractSummary_Optimized
    WHERE (@Status IS NULL OR Status = @Status)
      AND (@TradingPartnerId IS NULL OR CounterpartyName = (SELECT Name FROM TradingPartners WHERE Id = @TradingPartnerId))
      AND (@ProductId IS NULL OR ProductName = (SELECT Name FROM Products WHERE Id = @ProductId))
      AND (@TraderId IS NULL OR TraderName = (SELECT FirstName + ' ' + LastName FROM Users WHERE Id = @TraderId))
      AND (@DateFrom IS NULL OR CreatedAt >= @DateFrom)
      AND (@DateTo IS NULL OR CreatedAt <= @DateTo);
END;

-- 高性能价格查询存储过程
CREATE OR ALTER PROCEDURE sp_GetPriceHistory_Optimized
    @ProductId UNIQUEIDENTIFIER,
    @DateFrom DATETIME2 = NULL,
    @DateTo DATETIME2 = NULL,
    @Granularity VARCHAR(10) = 'DAILY' -- DAILY, WEEKLY, MONTHLY
AS
BEGIN
    SET NOCOUNT ON;
    
    -- 设置默认日期范围
    IF @DateFrom IS NULL SET @DateFrom = DATEADD(month, -12, GETDATE());
    IF @DateTo IS NULL SET @DateTo = GETDATE();
    
    -- 根据粒度聚合数据
    IF @Granularity = 'DAILY'
    BEGIN
        SELECT 
            Date,
            ProductName,
            AvgPrice,
            MinPrice,
            MaxPrice,
            TotalVolume,
            PriceVolatility
        FROM vw_PriceAnalytics_Optimized
        WHERE ProductId = @ProductId
          AND Date BETWEEN @DateFrom AND @DateTo
        ORDER BY Date DESC;
    END
    ELSE IF @Granularity = 'WEEKLY'
    BEGIN
        SELECT 
            DATEADD(week, DATEDIFF(week, 0, Date), 0) as WeekStart,
            ProductName,
            AVG(AvgPrice) as AvgPrice,
            MIN(MinPrice) as MinPrice,
            MAX(MaxPrice) as MaxPrice,
            SUM(TotalVolume) as TotalVolume,
            AVG(PriceVolatility) as PriceVolatility
        FROM vw_PriceAnalytics_Optimized
        WHERE ProductId = @ProductId
          AND Date BETWEEN @DateFrom AND @DateTo
        GROUP BY DATEADD(week, DATEDIFF(week, 0, Date), 0), ProductName
        ORDER BY WeekStart DESC;
    END
    ELSE IF @Granularity = 'MONTHLY'
    BEGIN
        SELECT 
            DATEADD(month, DATEDIFF(month, 0, Date), 0) as MonthStart,
            ProductName,
            AVG(AvgPrice) as AvgPrice,
            MIN(MinPrice) as MinPrice,
            MAX(MaxPrice) as MaxPrice,
            SUM(TotalVolume) as TotalVolume,
            AVG(PriceVolatility) as PriceVolatility
        FROM vw_PriceAnalytics_Optimized
        WHERE ProductId = @ProductId
          AND Date BETWEEN @DateFrom AND @DateTo
        GROUP BY DATEADD(month, DATEDIFF(month, 0, Date), 0), ProductName
        ORDER BY MonthStart DESC;
    END
END;

-- 5. 数据库维护和优化作业
-- =====================================

-- 索引维护存储过程
CREATE OR ALTER PROCEDURE sp_OptimizeIndexes
    @FragmentationThreshold FLOAT = 10.0,
    @RebuildThreshold FLOAT = 30.0
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SQL NVARCHAR(MAX) = '';
    DECLARE @IndexName NVARCHAR(255);
    DECLARE @TableName NVARCHAR(255);
    DECLARE @SchemaName NVARCHAR(255);
    DECLARE @Fragmentation FLOAT;
    
    -- 获取需要维护的索引
    DECLARE index_cursor CURSOR FOR
    SELECT 
        SCHEMA_NAME(t.schema_id) as SchemaName,
        t.name as TableName,
        i.name as IndexName,
        ips.avg_fragmentation_in_percent
    FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
    INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
    INNER JOIN sys.tables t ON i.object_id = t.object_id
    WHERE ips.avg_fragmentation_in_percent > @FragmentationThreshold
      AND i.name IS NOT NULL
      AND ips.page_count > 1000; -- 只处理大表
    
    OPEN index_cursor;
    FETCH NEXT FROM index_cursor INTO @SchemaName, @TableName, @IndexName, @Fragmentation;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @Fragmentation > @RebuildThreshold
        BEGIN
            SET @SQL = 'ALTER INDEX [' + @IndexName + '] ON [' + @SchemaName + '].[' + @TableName + '] REBUILD ONLINE = ON;';
            PRINT 'Rebuilding index: ' + @IndexName + ' (Fragmentation: ' + CAST(@Fragmentation AS VARCHAR(10)) + '%)';
        END
        ELSE
        BEGIN
            SET @SQL = 'ALTER INDEX [' + @IndexName + '] ON [' + @SchemaName + '].[' + @TableName + '] REORGANIZE;';
            PRINT 'Reorganizing index: ' + @IndexName + ' (Fragmentation: ' + CAST(@Fragmentation AS VARCHAR(10)) + '%)';
        END
        
        EXEC sp_executesql @SQL;
        
        FETCH NEXT FROM index_cursor INTO @SchemaName, @TableName, @IndexName, @Fragmentation;
    END
    
    CLOSE index_cursor;
    DEALLOCATE index_cursor;
    
    -- 更新统计信息
    EXEC sp_updatestats;
    
    PRINT 'Index optimization completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);
END;

-- 数据清理存储过程
CREATE OR ALTER PROCEDURE sp_CleanupOldData
    @DaysToKeepPricingEvents INT = 730,  -- 2 years
    @DaysToKeepAuditLogs INT = 2555,     -- 7 years
    @BatchSize INT = 10000
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @PricingEventsCutoff DATETIME2 = DATEADD(day, -@DaysToKeepPricingEvents, GETDATE());
    DECLARE @AuditLogsCutoff DATETIME2 = DATEADD(day, -@DaysToKeepAuditLogs, GETDATE());
    DECLARE @DeletedCount INT;
    
    -- 清理旧的价格事件（保留与活动合同相关的）
    WHILE 1 = 1
    BEGIN
        DELETE TOP (@BatchSize) pe
        FROM PricingEvents pe
        WHERE pe.EventDate < @PricingEventsCutoff
          AND NOT EXISTS (
              SELECT 1 FROM PurchaseContracts pc 
              WHERE pc.ProductId = pe.ProductId 
                AND pc.Status IN (1, 2) -- Active, Draft
          );
        
        SET @DeletedCount = @@ROWCOUNT;
        IF @DeletedCount = 0 BREAK;
        
        PRINT 'Deleted ' + CAST(@DeletedCount AS VARCHAR) + ' old pricing events';
        WAITFOR DELAY '00:00:01'; -- 避免阻塞
    END
    
    -- 清理旧的审计日志
    WHILE 1 = 1
    BEGIN
        DELETE TOP (@BatchSize)
        FROM OperationAuditLogs
        WHERE Timestamp < @AuditLogsCutoff;
        
        SET @DeletedCount = @@ROWCOUNT;
        IF @DeletedCount = 0 BREAK;
        
        PRINT 'Deleted ' + CAST(@DeletedCount AS VARCHAR) + ' old audit logs';
        WAITFOR DELAY '00:00:01';
    END
    
    PRINT 'Data cleanup completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);
END;

-- 6. 查询性能监控视图
-- =====================================

-- 查询性能监控视图
CREATE VIEW vw_QueryPerformanceMonitor
AS
SELECT TOP 50
    qs.execution_count,
    qs.total_elapsed_time / 1000000.0 as total_elapsed_time_sec,
    qs.total_elapsed_time / qs.execution_count / 1000000.0 as avg_elapsed_time_sec,
    qs.total_logical_reads,
    qs.total_logical_reads / qs.execution_count as avg_logical_reads,
    qs.total_physical_reads,
    qs.total_worker_time / 1000000.0 as total_cpu_time_sec,
    SUBSTRING(qt.text, (qs.statement_start_offset/2)+1, 
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(qt.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset)/2) + 1) as query_text,
    qs.creation_time,
    qs.last_execution_time
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE qt.text NOT LIKE '%sys.%'
  AND qt.text NOT LIKE '%INFORMATION_SCHEMA%'
ORDER BY qs.total_elapsed_time DESC;

-- 索引使用统计视图
CREATE VIEW vw_IndexUsageStats
AS
SELECT 
    SCHEMA_NAME(o.schema_id) as SchemaName,
    OBJECT_NAME(i.object_id) as TableName,
    i.name as IndexName,
    i.type_desc as IndexType,
    s.user_seeks,
    s.user_scans,
    s.user_lookups,
    s.user_updates,
    s.user_seeks + s.user_scans + s.user_lookups as total_reads,
    CASE 
        WHEN s.user_updates > 0 
        THEN (s.user_seeks + s.user_scans + s.user_lookups) / CAST(s.user_updates AS FLOAT)
        ELSE s.user_seeks + s.user_scans + s.user_lookups
    END as read_to_write_ratio
FROM sys.indexes i
LEFT JOIN sys.dm_db_index_usage_stats s 
    ON i.object_id = s.object_id AND i.index_id = s.index_id
INNER JOIN sys.objects o ON i.object_id = o.object_id
WHERE o.type = 'U' -- 只显示用户表
  AND i.name IS NOT NULL;

PRINT '=====================================';
PRINT '数据库优化脚本执行完成!';
PRINT '包含以下优化:';
PRINT '1. 高性能索引策略';
PRINT '2. 数据分区配置';
PRINT '3. 物化视图优化';
PRINT '4. 性能优化存储过程';
PRINT '5. 自动维护作业';
PRINT '6. 性能监控视图';
PRINT '=====================================';