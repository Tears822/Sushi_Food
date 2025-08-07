@echo off
echo 🍣 Setting up HIDA SUSHI SQL Server Database...
echo.

echo 📊 Running database creation script...
sqlcmd -S localhost\SQLEXPRESS -E -i "Scripts\CreateDatabase.sql"

if %ERRORLEVEL% EQU 0 (
    echo ✅ Database setup completed successfully!
    echo.
    echo 🔐 Admin credentials:
    echo    - admin / HidaSushi2024!
    echo    - jonathan / ChefJonathan123!
    echo    - kitchen / Kitchen2024!
    echo.
    echo 🚀 You can now run the server with: dotnet run
) else (
    echo ❌ Database setup failed!
    echo.
    echo 📝 Please ensure:
    echo    1. SQL Server Express is installed and running
    echo    2. You have permissions to create databases
    echo    3. SQL Server Express is accessible on localhost\SQLEXPRESS
    echo.
    echo 🔧 To install SQL Server Express:
    echo    - Download from: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
    echo    - Choose "Express" edition
    echo    - Install with default settings
)

pause 