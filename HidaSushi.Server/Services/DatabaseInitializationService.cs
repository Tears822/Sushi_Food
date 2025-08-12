using Microsoft.Data.SqlClient;
using System.Text;

namespace HidaSushi.Server.Services;

public interface IDatabaseInitializationService
{
    Task InitializeDatabaseAsync();
    Task<bool> DatabaseExistsAsync();
}

public class DatabaseInitializationService : IDatabaseInitializationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializationService> _logger;
    private readonly string _connectionString;

    public DatabaseInitializationService(
        IConfiguration configuration,
        ILogger<DatabaseInitializationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _connectionString = _configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<bool> DatabaseExistsAsync()
    {
        try
        {
            // Use master database to check if HidaSushiDb exists
            var masterConnectionString = _connectionString.Replace("Database=HidaSushiDb", "Database=master");
            
            using var connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand(
                "SELECT COUNT(*) FROM sys.databases WHERE name = 'HidaSushiDb'", 
                connection);
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if database exists");
            return false;
        }
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Starting database initialization...");

            var databaseExists = await DatabaseExistsAsync();
            if (databaseExists)
            {
                _logger.LogWarning("Database exists but forcing recreation due to schema updates...");
                await DropDatabaseAsync();
            }

            _logger.LogInformation("Creating new database...");

            // First, create the database using master connection
            var masterConnectionString = _connectionString.Replace("Database=HidaSushiDb", "Database=master");
            await CreateDatabaseAsync(masterConnectionString);

            _logger.LogInformation("Executing database schema script...");

            // Read the SQL schema file
            var schemaFilePath = Path.Combine(
                Directory.GetCurrentDirectory(), 
                "Database", 
                "HidaSushi_Complete_database_Schema.sql");

            if (!File.Exists(schemaFilePath))
            {
                _logger.LogError("Database schema file not found at: {SchemaFilePath}", schemaFilePath);
                throw new FileNotFoundException($"Database schema file not found at: {schemaFilePath}");
            }

            var sqlScript = await File.ReadAllTextAsync(schemaFilePath, Encoding.UTF8);
            
            // Now execute the schema against the new database
            await ExecuteSqlScriptAsync(_connectionString, sqlScript);

            _logger.LogInformation("Database initialization completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database initialization");
            throw;
        }
    }

    private async Task DropDatabaseAsync()
    {
        try
        {
            var masterConnectionString = _connectionString.Replace("Database=HidaSushiDb", "Database=master");
            
            using var connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync();
            
            // Kill all connections to the database first
            var killConnectionsSql = @"
                ALTER DATABASE [HidaSushiDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [HidaSushiDb];";
            
            using var command = new SqlCommand(killConnectionsSql, connection);
            command.CommandTimeout = 300; // 5 minutes timeout
            
            await command.ExecuteNonQueryAsync();
            
            _logger.LogInformation("Successfully dropped existing database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dropping database");
            throw;
        }
    }

    private async Task CreateDatabaseAsync(string masterConnectionString)
    {
        try
        {
            using var connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync();
            
            var createDatabaseSql = "CREATE DATABASE [HidaSushiDb];";
            
            using var command = new SqlCommand(createDatabaseSql, connection);
            command.CommandTimeout = 300; // 5 minutes timeout
            
            await command.ExecuteNonQueryAsync();
            
            _logger.LogInformation("Successfully created new database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating database");
            throw;
        }
    }

    private async Task ExecuteSqlScriptAsync(string connectionString, string sqlScript)
    {
        // Split the script by GO statements (handle different line endings)
        var batches = System.Text.RegularExpressions.Regex.Split(
            sqlScript, 
            @"^\s*GO\s*$", 
            System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        _logger.LogInformation("Found {BatchCount} SQL batches to execute", batches.Length);

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        int executedCount = 0;
        int skippedCount = 0;

        foreach (var batch in batches)
        {
            var trimmedBatch = batch.Trim();
            
            // Skip empty batches
            if (string.IsNullOrWhiteSpace(trimmedBatch))
            {
                _logger.LogInformation("Skipping empty batch");
                skippedCount++;
                continue;
            }
            
            // Skip only header comments and database operations that we handle separately
            if (trimmedBatch.StartsWith("-- =====================================================================================\r\n-- HIDA SUS", StringComparison.OrdinalIgnoreCase) ||
                trimmedBatch.StartsWith("-- =====================================================================================\n-- HIDA SUS", StringComparison.OrdinalIgnoreCase) ||
                trimmedBatch.StartsWith("/*", StringComparison.OrdinalIgnoreCase) ||
                trimmedBatch.StartsWith("CREATE DATABASE", StringComparison.OrdinalIgnoreCase) ||
                trimmedBatch.StartsWith("USE [master]", StringComparison.OrdinalIgnoreCase) ||
                trimmedBatch.StartsWith("USE master", StringComparison.OrdinalIgnoreCase) ||
                trimmedBatch.StartsWith("USE [HidaSushiDb]", StringComparison.OrdinalIgnoreCase) ||
                trimmedBatch.StartsWith("-- Drop and recreate database", StringComparison.OrdinalIgnoreCase) ||
                trimmedBatch.StartsWith("IF EXISTS (SELECT name FROM sys.databases", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Skipping batch: {BatchPreview}", trimmedBatch.Substring(0, Math.Min(100, trimmedBatch.Length)));
                skippedCount++;
                continue;
            }

            try
            {
                _logger.LogInformation("Executing batch with {Length} characters. Preview: {BatchPreview}", 
                    trimmedBatch.Length, trimmedBatch.Substring(0, Math.Min(150, trimmedBatch.Length)));

                using var command = new SqlCommand(trimmedBatch, connection);
                command.CommandTimeout = 600; // 10 minutes timeout for large operations
                
                var result = await command.ExecuteNonQueryAsync();
                _logger.LogInformation("Batch executed successfully. Rows affected: {RowsAffected}", result);
                executedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL batch. Batch length: {Length}. Preview: {Batch}", 
                    trimmedBatch.Length, trimmedBatch.Substring(0, Math.Min(500, trimmedBatch.Length)));
                throw;
            }
        }

        _logger.LogInformation("SQL script execution completed. Executed: {ExecutedCount}, Skipped: {SkippedCount}", 
            executedCount, skippedCount);
    }
} 