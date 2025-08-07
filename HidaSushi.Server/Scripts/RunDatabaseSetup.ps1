Write-Host "🍣 Setting up HIDA SUSHI SQL Server Database..." -ForegroundColor Green
Write-Host ""

Write-Host "📊 Running database creation script..." -ForegroundColor Yellow

try {
    # Check if sqlcmd is available
    $sqlcmdPath = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if (-not $sqlcmdPath) {
        Write-Host "❌ sqlcmd not found. Please install SQL Server Command Line Utilities." -ForegroundColor Red
        Write-Host "📝 Download from: https://docs.microsoft.com/en-us/sql/tools/sqlcmd-utility" -ForegroundColor Yellow
        exit 1
    }

    # Run the database creation script
    $scriptPath = Join-Path $PSScriptRoot "CreateDatabase.sql"
    sqlcmd -S localhost\SQLEXPRESS -E -i $scriptPath

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Database setup completed successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "🔐 Admin credentials:" -ForegroundColor Cyan
        Write-Host "   - admin / HidaSushi2024!" -ForegroundColor White
        Write-Host "   - jonathan / ChefJonathan123!" -ForegroundColor White
        Write-Host "   - kitchen / Kitchen2024!" -ForegroundColor White
        Write-Host ""
        Write-Host "🚀 You can now run the server with: dotnet run" -ForegroundColor Green
    } else {
        Write-Host "❌ Database setup failed!" -ForegroundColor Red
        Write-Host ""
        Write-Host "📝 Please ensure:" -ForegroundColor Yellow
        Write-Host "   1. SQL Server Express is installed and running" -ForegroundColor White
        Write-Host "   2. You have permissions to create databases" -ForegroundColor White
        Write-Host "   3. SQL Server Express is accessible on localhost\SQLEXPRESS" -ForegroundColor White
        Write-Host ""
        Write-Host "🔧 To install SQL Server Express:" -ForegroundColor Yellow
        Write-Host "   - Download from: https://www.microsoft.com/en-us/sql-server/sql-server-downloads" -ForegroundColor White
        Write-Host "   - Choose 'Express' edition" -ForegroundColor White
        Write-Host "   - Install with default settings" -ForegroundColor White
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 