using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HidaSushi.Server.Data;
using HidaSushi.Server.Services;
using HidaSushi.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Entity Framework with SQL Server
builder.Services.AddDbContext<HidaSushiDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add SignalR for real-time updates
builder.Services.AddSignalR();

// Configure JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "HidaSushi-Super-Secret-Key-For-Admin-Auth-2024!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "HidaSushi";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Register custom services
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<EmailService>();

// Configure CORS to allow requests from Blazor client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient",
        policy => policy
            .AllowAnyOrigin() // Allow any origin in development
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed(origin => true)); // Allow any origin in development
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Add CORS debugging in development
    app.Use(async (context, next) =>
    {
        Console.WriteLine($"🌐 Request from: {context.Request.Headers["Origin"]}");
        Console.WriteLine($"🔗 Request to: {context.Request.Method} {context.Request.Path}");
        await next();
    });
}

app.UseHttpsRedirection();

// Apply CORS before authentication
app.UseCors("AllowBlazorClient");

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs
app.MapHub<OrderHub>("/orderHub");
app.MapHub<NotificationHub>("/notificationHub");

// Database initialization
try 
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<HidaSushiDbContext>();
        
        Console.WriteLine("🗄️ Initializing SQL Server database...");
        
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        // Seed data if database is empty
        if (!context.SushiRolls.Any())
        {
            Console.WriteLine("🌱 Seeding database with initial data...");
            context.SeedRuntimeData();
            await context.SaveChangesAsync();
        }
        
        Console.WriteLine("✅ Database initialized successfully!");
        Console.WriteLine($"📊 Database contains: {context.SushiRolls.Count()} rolls, {context.Ingredients.Count()} ingredients");
    }
}
catch (Exception ex) 
{
    Console.WriteLine($"⚠️ Database initialization error: {ex.Message}");
    Console.WriteLine("📝 Please ensure SQL Server LocalDB is installed and running.");
}

Console.WriteLine("🔐 Admin Authentication Enabled!");
Console.WriteLine("📋 Test credentials available at: /api/Auth/credentials");
Console.WriteLine("🌐 Server is running on: https://localhost:5000");
Console.WriteLine("📡 SignalR hubs available for real-time updates");

app.Run();
