using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HidaSushi.Server.Data;
using HidaSushi.Server.Services;
using HidaSushi.Server.Hubs;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Database with optimizations
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

        // Performance optimizations
        sqlOptions.CommandTimeout(builder.Configuration.GetValue<int>("Database:CommandTimeout", 30));
        
        // Migration assembly
        sqlOptions.MigrationsAssembly("HidaSushi.Server");
    });

    // Configure pooling for high performance
    options.EnableSensitiveDataLogging(builder.Configuration.GetValue<bool>("Database:EnableSensitiveDataLogging", false));
    options.EnableDetailedErrors(builder.Configuration.GetValue<bool>("Database:EnableDetailedErrors", false));
    
    // Query splitting for better performance with includes (only available for SQL Server)
    var querySplittingBehavior = builder.Configuration.GetValue<string>("Database:QuerySplittingBehavior", "SplitQuery");
    if (querySplittingBehavior == "SplitQuery")
    {
        options.UseSqlServer(connectionString, sqlServerOptions =>
        {
            sqlServerOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });
    }
});

// Memory Caching
builder.Services.AddMemoryCache(options =>
{
    var maxSize = builder.Configuration.GetValue<string>("Caching:InMemory:MaxSize", "100MB");
    if (maxSize.EndsWith("MB"))
    {
        var sizeInMB = int.Parse(maxSize.Replace("MB", ""));
        options.SizeLimit = sizeInMB * 1024 * 1024; // Convert to bytes
    }
});

// Redis Caching (if enabled)
if (builder.Configuration.GetValue<bool>("Caching:Redis:Enabled", false))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = "HidaSushi";
    });
}

// Register Cache Service
builder.Services.AddSingleton<ICacheService, CacheService>();

// Health Checks
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

// Database initialization
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<HidaSushiDbContext>();
        
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        // Log database status
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database connection established successfully");
        
        // Optional: Run migrations
        if (app.Environment.IsDevelopment())
        {
            // Use EnsureCreated for development to avoid migration issues
            // await context.Database.MigrateAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database");
        throw;
    }
}

app.Run(); 