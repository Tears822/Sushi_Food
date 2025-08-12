-- Drop HidaSushi Database Script
USE master;
GO

-- Close all connections to the database
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'HidaSushiDb')
BEGIN
    ALTER DATABASE [HidaSushiDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [HidaSushiDb];
    PRINT 'HidaSushiDb database dropped successfully.';
END
ELSE
BEGIN
    PRINT 'HidaSushiDb database does not exist.';
END
GO 