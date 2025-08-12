-- =====================================================================================
-- UPDATE INGREDIENTS TABLE SCHEMA
-- =====================================================================================
-- This script adds missing columns to the Ingredients table to match the Entity Framework model
-- Missing columns: IsVegetarian, Price
-- =====================================================================================

USE [HidaSushiDb];
GO

-- Check if the columns exist before adding them
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Ingredients]') AND name = 'IsVegetarian')
BEGIN
    ALTER TABLE [dbo].[Ingredients] 
    ADD [IsVegetarian] BIT NOT NULL DEFAULT 1;
    PRINT 'Added IsVegetarian column to Ingredients table';
END
ELSE
BEGIN
    PRINT 'IsVegetarian column already exists in Ingredients table';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Ingredients]') AND name = 'Price')
BEGIN
    ALTER TABLE [dbo].[Ingredients] 
    ADD [Price] DECIMAL(8,2) NOT NULL DEFAULT 0.00;
    PRINT 'Added Price column to Ingredients table';
END
ELSE
BEGIN
    PRINT 'Price column already exists in Ingredients table';
END

-- Update existing records to set reasonable default values
UPDATE [dbo].[Ingredients] 
SET [IsVegetarian] = 1, [Price] = [AdditionalPrice]
WHERE [IsVegetarian] IS NULL OR [Price] IS NULL OR [Price] = 0;

PRINT 'Updated existing Ingredients records with default values';
PRINT 'Ingredients table schema update completed successfully';
GO 