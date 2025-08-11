-- HIDA SUSHI Database Scaling and Performance Optimizations
-- This script applies production-ready optimizations for high-volume operations

USE HidaSushiDb;
GO

-- =====================================================
-- 1. TABLE PARTITIONING FOR LARGE TABLES
-- =====================================================

-- Create partition function for Orders by month
CREATE PARTITION FUNCTION OrderDatePartition (DATETIME2)
AS RANGE RIGHT FOR VALUES (
    '2024-01-01', '2024-02-01', '2024-03-01', '2024-04-01',
    '2024-05-01', '2024-06-01', '2024-07-01', '2024-08-01',
    '2024-09-01', '2024-10-01', '2024-11-01', '2024-12-01',
    '2025-01-01'
);
GO

-- Create partition scheme
CREATE PARTITION SCHEME OrderDateScheme
AS PARTITION OrderDatePartition
ALL TO ([PRIMARY]);
GO

-- =====================================================
-- 2. ADVANCED INDEXING STRATEGY
-- =====================================================

-- Orders table - Performance critical indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_Status_CreatedAt_Covering')
CREATE NONCLUSTERED INDEX IX_Orders_Status_CreatedAt_Covering
ON Orders (Status, CreatedAt)
INCLUDE (Id, OrderNumber, CustomerName, TotalAmount, Type, PaymentStatus)
WITH (FILLFACTOR = 90, PAD_INDEX = ON);
GO

-- Composite index for order filtering and sorting
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_Complex_Lookup')
CREATE NONCLUSTERED INDEX IX_Orders_Complex_Lookup
ON Orders (PaymentStatus, Status, Type, CreatedAt DESC)
INCLUDE (Id, OrderNumber, CustomerName, CustomerEmail, TotalAmount)
WITH (FILLFACTOR = 85, PAD_INDEX = ON);
GO

-- SushiRolls - Menu optimization indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SushiRolls_Menu_Optimization')
CREATE NONCLUSTERED INDEX IX_SushiRolls_Menu_Optimization
ON SushiRolls (IsAvailable, IsSignatureRoll, PopularityScore DESC)
INCLUDE (Id, Name, Description, Price, ImageUrl, IsVegetarian, IsVegan, PreparationTimeMinutes)
WITH (FILLFACTOR = 95, PAD_INDEX = ON);
GO

-- Ingredients - Category and availability
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Ingredients_Category_Stock_Covering')
CREATE NONCLUSTERED INDEX IX_Ingredients_Category_Stock_Covering
ON Ingredients (Category, IsAvailable, StockQuantity)
INCLUDE (Id, Name, Description, AdditionalPrice, ImageUrl, IsVegan, IsGlutenFree)
WITH (FILLFACTOR = 90, PAD_INDEX = ON);
GO

-- OrderItems - Fast order detail retrieval
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrderItems_Order_Details')
CREATE NONCLUSTERED INDEX IX_OrderItems_Order_Details
ON OrderItems (OrderId)
INCLUDE (Id, SushiRollId, CustomRollId, Quantity, UnitPrice, Price, Notes)
WITH (FILLFACTOR = 80, PAD_INDEX = ON);
GO

-- Customer lookup optimization
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Customers_Lookup_Optimization')
CREATE NONCLUSTERED INDEX IX_Customers_Lookup_Optimization
ON Customers (Email, IsActive)
INCLUDE (Id, FullName, Phone, TotalOrders, TotalSpent, CreatedAt)
WITH (FILLFACTOR = 95, PAD_INDEX = ON);
GO

-- Analytics daily performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DailyAnalytics_Date_Performance')
CREATE NONCLUSTERED INDEX IX_DailyAnalytics_Date_Performance
ON DailyAnalytics (Date DESC)
INCLUDE (TotalOrders, TotalRevenue, AverageOrderValue, HourlyOrderCountsJson)
WITH (FILLFACTOR = 100, PAD_INDEX = ON);
GO

-- =====================================================
-- 3. PERFORMANCE STATISTICS AND MAINTENANCE
-- =====================================================

-- Update statistics for all tables
UPDATE STATISTICS Orders WITH FULLSCAN;
UPDATE STATISTICS OrderItems WITH FULLSCAN;
UPDATE STATISTICS SushiRolls WITH FULLSCAN;
UPDATE STATISTICS Ingredients WITH FULLSCAN;
UPDATE STATISTICS Customers WITH FULLSCAN;
UPDATE STATISTICS DailyAnalytics WITH FULLSCAN;
GO

-- =====================================================
-- 4. STORED PROCEDURES FOR HIGH-PERFORMANCE OPERATIONS
-- =====================================================

-- Fast order retrieval with caching-friendly structure
CREATE OR ALTER PROCEDURE sp_GetOrdersOptimized
    @Status NVARCHAR(30) = NULL,
    @FromDate DATETIME2 = NULL,
    @ToDate DATETIME2 = NULL,
    @PageSize INT = 20,
    @PageNumber INT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;
    
    WITH OrdersCTE AS (
        SELECT 
            o.Id, o.OrderNumber, o.CustomerName, o.CustomerEmail, o.CustomerPhone,
            o.TotalAmount, o.Type, o.Status, o.PaymentStatus, o.PaymentMethod,
            o.DeliveryAddress, o.CreatedAt,
            COUNT(*) OVER() as TotalCount,
            ROW_NUMBER() OVER (ORDER BY o.CreatedAt DESC) as RowNum
        FROM Orders o WITH (NOLOCK)
        WHERE 
            (@Status IS NULL OR o.Status = @Status)
            AND (@FromDate IS NULL OR o.CreatedAt >= @FromDate)
            AND (@ToDate IS NULL OR o.CreatedAt <= @ToDate)
    )
    SELECT *
    FROM OrdersCTE
    WHERE RowNum > @Offset AND RowNum <= (@Offset + @PageSize)
    ORDER BY CreatedAt DESC;
END;
GO

-- High-performance menu retrieval
CREATE OR ALTER PROCEDURE sp_GetMenuOptimized
    @CategoryFilter NVARCHAR(50) = NULL,
    @IsSignatureOnly BIT = 0,
    @IsVegetarianOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Sushi Rolls
    SELECT 
        Id, Name, Description, Price, ImageUrl,
        IsSignatureRoll, IsVegetarian, IsVegan, IsGlutenFree,
        PreparationTimeMinutes, Calories, PopularityScore
    FROM SushiRolls WITH (NOLOCK)
    WHERE 
        IsAvailable = 1
        AND (@IsSignatureOnly = 0 OR IsSignatureRoll = 1)
        AND (@IsVegetarianOnly = 0 OR IsVegetarian = 1)
    ORDER BY PopularityScore DESC, Name;
    
    -- Available Ingredients by Category
    SELECT 
        Id, Name, Description, Category, AdditionalPrice, ImageUrl,
        IsVegan, IsGlutenFree, Calories, StockQuantity
    FROM Ingredients WITH (NOLOCK)
    WHERE 
        IsAvailable = 1
        AND StockQuantity > 0
        AND (@CategoryFilter IS NULL OR Category = @CategoryFilter)
    ORDER BY Category, Name;
END;
GO

-- Analytics data aggregation
CREATE OR ALTER PROCEDURE sp_GetAnalyticsOptimized
    @FromDate DATE,
    @ToDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Daily summary
    SELECT 
        CAST(CreatedAt AS DATE) as Date,
        COUNT(*) as TotalOrders,
        SUM(TotalAmount) as Revenue,
        AVG(TotalAmount) as AvgOrderValue,
        COUNT(CASE WHEN Type = 'Delivery' THEN 1 END) as DeliveryOrders,
        COUNT(CASE WHEN Type = 'Pickup' THEN 1 END) as PickupOrders
    FROM Orders WITH (NOLOCK)
    WHERE 
        CreatedAt >= @FromDate 
        AND CreatedAt < DATEADD(day, 1, @ToDate)
        AND Status != 'Cancelled'
    GROUP BY CAST(CreatedAt AS DATE)
    ORDER BY Date DESC;
    
    -- Popular items
    SELECT TOP 10
        sr.Name,
        SUM(oi.Quantity) as TimesOrdered,
        SUM(oi.Price) as TotalRevenue
    FROM OrderItems oi WITH (NOLOCK)
    INNER JOIN Orders o WITH (NOLOCK) ON oi.OrderId = o.Id
    INNER JOIN SushiRolls sr WITH (NOLOCK) ON oi.SushiRollId = sr.Id
    WHERE 
        o.CreatedAt >= @FromDate 
        AND o.CreatedAt < DATEADD(day, 1, @ToDate)
        AND o.Status != 'Cancelled'
    GROUP BY sr.Id, sr.Name
    ORDER BY TimesOrdered DESC;
END;
GO

-- =====================================================
-- 5. QUERY OPTIMIZATION HINTS AND SETTINGS
-- =====================================================

-- Enable optimizations for the database
ALTER DATABASE HidaSushiDb SET AUTO_UPDATE_STATISTICS ON;
ALTER DATABASE HidaSushiDb SET AUTO_UPDATE_STATISTICS_ASYNC ON;
ALTER DATABASE HidaSushiDb SET AUTO_CREATE_STATISTICS ON;
ALTER DATABASE HidaSushiDb SET PARAMETERIZATION FORCED;
GO

-- =====================================================
-- 6. MAINTENANCE JOBS (Templates for scheduling)
-- =====================================================

-- Index maintenance procedure
CREATE OR ALTER PROCEDURE sp_MaintenanceIndexRebuild
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @TableName NVARCHAR(128);
    DECLARE @IndexName NVARCHAR(128);
    DECLARE @SQL NVARCHAR(MAX);
    
    DECLARE index_cursor CURSOR FOR
    SELECT 
        t.name as TableName,
        i.name as IndexName
    FROM sys.indexes i
    INNER JOIN sys.tables t ON i.object_id = t.object_id
    WHERE 
        i.index_id > 0
        AND i.is_disabled = 0
        AND t.name IN ('Orders', 'OrderItems', 'SushiRolls', 'Ingredients', 'Customers');
    
    OPEN index_cursor;
    FETCH NEXT FROM index_cursor INTO @TableName, @IndexName;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @SQL = 'ALTER INDEX ' + QUOTENAME(@IndexName) + ' ON ' + QUOTENAME(@TableName) + ' REBUILD WITH (FILLFACTOR = 90, SORT_IN_TEMPDB = ON)';
        
        BEGIN TRY
            EXEC sp_executesql @SQL;
            PRINT 'Rebuilt index: ' + @IndexName + ' on table: ' + @TableName;
        END TRY
        BEGIN CATCH
            PRINT 'Failed to rebuild index: ' + @IndexName + ' on table: ' + @TableName + ' - ' + ERROR_MESSAGE();
        END CATCH
        
        FETCH NEXT FROM index_cursor INTO @TableName, @IndexName;
    END;
    
    CLOSE index_cursor;
    DEALLOCATE index_cursor;
END;
GO

-- Statistics update procedure
CREATE OR ALTER PROCEDURE sp_MaintenanceUpdateStatistics
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @TableName NVARCHAR(128);
    DECLARE @SQL NVARCHAR(MAX);
    
    DECLARE table_cursor CURSOR FOR
    SELECT name 
    FROM sys.tables 
    WHERE name IN ('Orders', 'OrderItems', 'SushiRolls', 'Ingredients', 'Customers', 'DailyAnalytics');
    
    OPEN table_cursor;
    FETCH NEXT FROM table_cursor INTO @TableName;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @SQL = 'UPDATE STATISTICS ' + QUOTENAME(@TableName) + ' WITH FULLSCAN';
        
        BEGIN TRY
            EXEC sp_executesql @SQL;
            PRINT 'Updated statistics for table: ' + @TableName;
        END TRY
        BEGIN CATCH
            PRINT 'Failed to update statistics for table: ' + @TableName + ' - ' + ERROR_MESSAGE();
        END CATCH
        
        FETCH NEXT FROM table_cursor INTO @TableName;
    END;
    
    CLOSE table_cursor;
    DEALLOCATE table_cursor;
END;
GO

-- =====================================================
-- 7. MONITORING AND ALERTING VIEWS
-- =====================================================

-- Performance monitoring view
CREATE OR ALTER VIEW vw_PerformanceMetrics
AS
SELECT 
    'Database Size' as Metric,
    CAST(SUM(size) * 8.0 / 1024 / 1024 AS DECIMAL(10,2)) as Value,
    'GB' as Unit
FROM sys.master_files 
WHERE database_id = DB_ID()

UNION ALL

SELECT 
    'Active Connections' as Metric,
    COUNT(*) as Value,
    'Count' as Unit
FROM sys.dm_exec_sessions 
WHERE database_id = DB_ID() AND is_user_process = 1

UNION ALL

SELECT 
    'Total Orders Today' as Metric,
    COUNT(*) as Value,
    'Count' as Unit
FROM Orders 
WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)

UNION ALL

SELECT 
    'Revenue Today' as Metric,
    ISNULL(SUM(TotalAmount), 0) as Value,
    'EUR' as Unit
FROM Orders 
WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE) AND Status != 'Cancelled';
GO

-- Query performance monitoring
CREATE OR ALTER VIEW vw_SlowQueries
AS
SELECT TOP 10
    qs.total_elapsed_time / qs.execution_count / 1000 as avg_duration_ms,
    qs.execution_count,
    qs.total_elapsed_time / 1000 as total_duration_ms,
    SUBSTRING(st.text, (qs.statement_start_offset/2)+1,
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(st.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset)/2) + 1) AS query_text,
    qs.creation_time,
    qs.last_execution_time
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
WHERE st.text LIKE '%Orders%' OR st.text LIKE '%SushiRolls%' OR st.text LIKE '%Ingredients%'
ORDER BY avg_duration_ms DESC;
GO

PRINT '✅ Database scaling optimizations applied successfully!';
PRINT '📊 Performance indexes created';
PRINT '🚀 Stored procedures for high-performance operations ready';
PRINT '📈 Monitoring views available: vw_PerformanceMetrics, vw_SlowQueries';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Schedule sp_MaintenanceIndexRebuild to run weekly';
PRINT '2. Schedule sp_MaintenanceUpdateStatistics to run daily';
PRINT '3. Monitor query performance using vw_SlowQueries';
PRINT '4. Consider implementing Redis caching for frequently accessed data';
GO 