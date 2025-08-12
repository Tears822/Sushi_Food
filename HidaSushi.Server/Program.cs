using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using HidaSushi.Server.Data;
using HidaSushi.Server.Services;
using HidaSushi.Server.Hubs;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container with JSON configuration for circular references
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Database with enhanced performance settings
builder.Services.AddDbContext<HidaSushiDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Connection resilience
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: builder.Configuration.GetValue<int>("Database:MaxRetryCount", 3),
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);

        // Query performance
        sqlOptions.CommandTimeout(builder.Configuration.GetValue<int>("Database:CommandTimeout", 30));
        
        // Query splitting for better performance with includes
        var querySplittingBehavior = builder.Configuration.GetValue<string>("Database:QuerySplittingBehavior", "SplitQuery");
        if (querySplittingBehavior == "SplitQuery")
        {
            sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        }
    });

    // Configure pooling for high performance
    options.EnableSensitiveDataLogging(builder.Configuration.GetValue<bool>("Database:EnableSensitiveDataLogging", false));
    options.EnableDetailedErrors(builder.Configuration.GetValue<bool>("Database:EnableDetailedErrors", false));
    
    // Configure to work with database triggers
    options.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuted));
    
    // Use SQL Server specific features
    options.EnableServiceProviderCaching();
    options.EnableSensitiveDataLogging(false); // Security best practice
});

// Caching services
builder.Services.AddMemoryCache();

// Add distributed cache based on configuration
if (builder.Configuration.GetValue<bool>("Caching:Redis:Enabled", false))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = "HidaSushi";
    });
}
else
{
    // Use in-memory distributed cache as fallback
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddSingleton<ICacheService, CacheService>();

// Email service
builder.Services.AddSingleton<EmailService>();

// Authentication service  
builder.Services.AddScoped<IAuthService, AuthService>();

// Analytics service
builder.Services.AddScoped<AnalyticsService>();

// Health checks for monitoring
if (builder.Configuration.GetValue<bool>("Monitoring:EnableHealthChecks", true))
{
    var healthChecksBuilder = builder.Services.AddHealthChecks()
        .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!, 
                     name: "database",
                     timeout: TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("Monitoring:DatabaseHealthCheckTimeout", 10)))
        .AddPrivateMemoryHealthCheck(500_000_000, "memory") // 500MB
        .AddCheck("api", () => HealthCheckResult.Healthy("API is running"));

    // Add Redis health check if enabled
    if (builder.Configuration.GetValue<bool>("Caching:Redis:Enabled", false))
    {
        healthChecksBuilder.AddRedis(builder.Configuration.GetConnectionString("Redis")!);
    }
}

// JWT helper
builder.Services.AddSingleton<JwtService>();

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? ""))
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"message\":\"Unauthorized\"}");
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"message\":\"Forbidden\"}");
            }
        };
    });

builder.Services.AddAuthorization();

// Register application services
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<CustomerService>();

// Register database initialization service
builder.Services.AddScoped<IDatabaseInitializationService, DatabaseInitializationService>();

// Register HttpClient for payment services
builder.Services.AddHttpClient();

// Register payment services
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddScoped<IPayPalService, PayPalService>();

// Register file upload service
builder.Services.AddScoped<IFileUploadService, FileUploadService>();

// Configure multipart request limits for file uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
    options.MultipartHeadersLengthLimit = int.MaxValue;
    options.MemoryBufferThreshold = int.MaxValue;
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// SignalR for real-time features
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable static files for serving uploaded images
app.UseStaticFiles();

// Configure static files for images with proper headers
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Set cache headers for images
        if (ctx.File.Name.ToLowerInvariant().EndsWith(".jpg") ||
            ctx.File.Name.ToLowerInvariant().EndsWith(".jpeg") ||
            ctx.File.Name.ToLowerInvariant().EndsWith(".png") ||
            ctx.File.Name.ToLowerInvariant().EndsWith(".gif") ||
            ctx.File.Name.ToLowerInvariant().EndsWith(".webp"))
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000");
            ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
            ctx.Context.Response.Headers.Append("Access-Control-Allow-Methods", "GET");
            ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "*");
        }
    }
});

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Health checks endpoint
if (builder.Configuration.GetValue<bool>("Monitoring:EnableHealthChecks", true))
{
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });
}

app.MapControllers();

// SignalR Hub
app.MapHub<OrderHub>("/orderhub");

// Custom signin endpoint for admin authentication
app.MapGet("/signin", (string name, string token) =>
{
    return Results.Ok(new { message = "Signin endpoint", name, token });
});

// Test endpoint for image serving
app.MapGet("/test-image/{*imagePath}", (string imagePath) =>
{
    var fullPath = Path.Combine(builder.Environment.WebRootPath, "images", imagePath);
    if (File.Exists(fullPath))
    {
        return Results.Ok(new { 
            exists = true, 
            path = fullPath,
            size = new FileInfo(fullPath).Length,
            webRootPath = builder.Environment.WebRootPath,
            requestedPath = imagePath
        });
    }
    return Results.NotFound(new { 
        exists = false, 
        path = fullPath,
        webRootPath = builder.Environment.WebRootPath,
        requestedPath = imagePath
    });
});

// Database initialization with comprehensive schema
using (var scope = app.Services.CreateScope())
{
    try
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting application database initialization...");
        
        // Use our comprehensive database initialization service
        var databaseInitializationService = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();
        await databaseInitializationService.InitializeDatabaseAsync();
        
        // Test the connection to ensure everything is working
        var context = scope.ServiceProvider.GetRequiredService<HidaSushiDbContext>();
        var canConnect = await context.Database.CanConnectAsync();
        
        if (canConnect)
        {
            logger.LogInformation("✅ Database connection established successfully!");
            logger.LogInformation("✅ HidaSushi comprehensive database schema is ready!");
        }
        else
        {
            logger.LogWarning("⚠️ Database connection test failed");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ An error occurred while initializing the database");
        throw;
    }
}

app.Run(); 