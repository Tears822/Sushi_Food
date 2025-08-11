using Microsoft.EntityFrameworkCore;
using HidaSushi.Shared.Models;
using System.Reflection;

namespace HidaSushi.Server.Data;

public class HidaSushiDbContext : DbContext
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<HidaSushiDbContext> _logger;

    public HidaSushiDbContext(DbContextOptions<HidaSushiDbContext> options, 
                              IConfiguration configuration,
                              ILogger<HidaSushiDbContext> logger) : base(options)
    {
        _configuration = configuration;
        _logger = logger;
    }

    // Core entities
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerAddress> CustomerAddresses { get; set; }
    public DbSet<AdminUser> AdminUsers { get; set; }
    public DbSet<SushiRoll> SushiRolls { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<CustomRoll> CustomRolls { get; set; }
    public DbSet<OrderStatusHistory> OrderStatusHistory { get; set; }
    public DbSet<DailyAnalytics> DailyAnalytics { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
            {
                // Connection resilience
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: _configuration.GetValue<int>("Database:MaxRetryCount", 3),
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);

                // Query performance
                sqlOptions.CommandTimeout(_configuration.GetValue<int>("Database:CommandTimeout", 30));
            });

            // Performance optimizations
            if (_configuration.GetValue<bool>("Database:EnableSensitiveDataLogging", false))
            {
                optionsBuilder.EnableSensitiveDataLogging();
            }

            if (_configuration.GetValue<bool>("Database:EnableDetailedErrors", false))
            {
                optionsBuilder.EnableDetailedErrors();
            }

            // Logging configuration
            if (_configuration.GetValue<bool>("Performance:EnableQueryLogging", false))
            {
                optionsBuilder.LogTo(message => _logger.LogInformation(message));
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure Customer entity
        ConfigureCustomerEntity(modelBuilder);

        // Configure AdminUser entity
        ConfigureAdminUserEntity(modelBuilder);

        // Configure SushiRoll entity
        ConfigureSushiRollEntity(modelBuilder);

        // Configure Ingredient entity
        ConfigureIngredientEntity(modelBuilder);

        // Configure Order entity
        ConfigureOrderEntity(modelBuilder);

        // Configure OrderItem entity
        ConfigureOrderItemEntity(modelBuilder);

        // Configure CustomRoll entity
        ConfigureCustomRollEntity(modelBuilder);

        // Configure OrderStatusHistory entity
        ConfigureOrderStatusHistoryEntity(modelBuilder);

        // Configure DailyAnalytics entity
        ConfigureDailyAnalyticsEntity(modelBuilder);

        // Configure CustomerAddress entity
        ConfigureCustomerAddressEntity(modelBuilder);

        // Configure relationships
        ConfigureRelationships(modelBuilder);

        // Configure indexes for performance
        ConfigureIndexes(modelBuilder);

        // Configure data seeding
        SeedData(modelBuilder);
    }

    private void ConfigureCustomerEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.PreferencesJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.TotalSpent).HasColumnType("decimal(10,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Unique constraint
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("IX_Customers_Email_Unique");
        });
    }

    private void ConfigureAdminUserEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20).HasDefaultValue("Admin");
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PermissionsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Unique constraints
            entity.HasIndex(e => e.Username).IsUnique().HasDatabaseName("IX_AdminUsers_Username_Unique");
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("IX_AdminUsers_Email_Unique");
        });
    }

    private void ConfigureSushiRollEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SushiRoll>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.IngredientsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.AllergensJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.PreparationTimeMinutes).HasDefaultValue(15);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });
    }

    private void ConfigureIngredientEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AdditionalPrice).HasColumnType("decimal(10,2)").HasDefaultValue(0);
            entity.Property(e => e.AllergensJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Protein).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Carbs).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Fat).HasColumnType("decimal(5,2)");
            entity.Property(e => e.MinStockLevel).HasDefaultValue(10);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });
    }

    private void ConfigureOrderEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CustomerEmail).HasMaxLength(200);
            entity.Property(e => e.CustomerPhone).HasMaxLength(20);
            entity.Property(e => e.SubtotalAmount).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.DeliveryFee).HasColumnType("decimal(10,2)").HasDefaultValue(0);
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(10,2)").HasDefaultValue(0);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.Type).HasMaxLength(20).HasDefaultValue(OrderType.Pickup);
            entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValue(OrderStatus.Received);
            entity.Property(e => e.PaymentMethod).HasMaxLength(20).HasDefaultValue(PaymentMethod.CashOnDelivery);
            entity.Property(e => e.PaymentStatus).HasMaxLength(20).HasDefaultValue(PaymentStatus.Pending);
            entity.Property(e => e.PaymentIntentId).HasMaxLength(200);
            entity.Property(e => e.PaymentReference).HasMaxLength(200);
            entity.Property(e => e.DeliveryAddress).HasMaxLength(500);
            entity.Property(e => e.DeliveryInstructions).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(100).HasDefaultValue("Brussels");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Unique constraint
            entity.HasIndex(e => e.OrderNumber).IsUnique().HasDatabaseName("IX_Orders_OrderNumber_Unique");
        });
    }

    private void ConfigureOrderItemEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.SpecialInstructions).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });
    }

    private void ConfigureCustomRollEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomRoll>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).HasDefaultValue("Custom Roll");
            entity.Property(e => e.RollType).HasMaxLength(20).HasDefaultValue(RollType.Normal);
            entity.Property(e => e.SelectedIngredientsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.AllergensJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });
    }

    private void ConfigureOrderStatusHistoryEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderId).IsRequired();
            entity.Property(e => e.NewStatus).IsRequired().HasMaxLength(30);
            entity.Property(e => e.PreviousStatus).HasMaxLength(30);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });
    }

    private void ConfigureDailyAnalyticsEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailyAnalytics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.TotalRevenue).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.AverageOrderValue).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.HourlyOrderCountsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.PopularRollsJson).HasColumnType("nvarchar(max)");

            // Unique constraint on date
            entity.HasIndex(e => e.Date).IsUnique().HasDatabaseName("IX_DailyAnalytics_Date_Unique");
        });
    }

    private void ConfigureCustomerAddressEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerAddress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Label).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AddressLine1).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AddressLine2).HasMaxLength(200);
            entity.Property(e => e.City).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PostalCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Country).HasMaxLength(100).HasDefaultValue("Belgium");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });
    }

    private void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        // Customer -> CustomerAddress (One-to-Many)
        modelBuilder.Entity<CustomerAddress>()
            .HasOne(ca => ca.Customer)
            .WithMany(c => c.Addresses)
            .HasForeignKey(ca => ca.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Customer -> Order (One-to-Many, nullable)
        modelBuilder.Entity<Order>()
            .HasOne<Customer>()
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        // AdminUser -> Order (One-to-Many, nullable)
        modelBuilder.Entity<Order>()
            .HasOne(o => o.AcceptedByUser)
            .WithMany()
            .HasForeignKey(o => o.AcceptedBy)
            .OnDelete(DeleteBehavior.SetNull);
            
        // Order -> OrderItem (One-to-Many)
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // SushiRoll -> OrderItem (One-to-Many, nullable)
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.SushiRoll)
            .WithMany()
            .HasForeignKey(oi => oi.SushiRollId)
            .OnDelete(DeleteBehavior.SetNull);

        // CustomRoll -> OrderItem (One-to-Many, nullable)
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.CustomRoll)
            .WithMany()
            .HasForeignKey(oi => oi.CustomRollId)
            .OnDelete(DeleteBehavior.SetNull);

        // Order -> OrderStatusHistory (One-to-Many)
        modelBuilder.Entity<OrderStatusHistory>()
            .HasOne<Order>()
            .WithMany(o => o.StatusHistory)
            .HasForeignKey(osh => osh.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // AdminUser -> OrderStatusHistory (One-to-Many, nullable)
        modelBuilder.Entity<OrderStatusHistory>()
            .HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(osh => osh.ChangedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // High-performance indexes for frequent queries

        // Orders - Performance critical indexes
        modelBuilder.Entity<Order>()
            .HasIndex(o => new { o.Status, o.CreatedAt })
            .HasDatabaseName("IX_Orders_Status_CreatedAt");

        modelBuilder.Entity<Order>()
            .HasIndex(o => new { o.CustomerId, o.CreatedAt })
            .HasDatabaseName("IX_Orders_CustomerId_CreatedAt");

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.PaymentStatus)
            .HasDatabaseName("IX_Orders_PaymentStatus");

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.Type)
            .HasDatabaseName("IX_Orders_Type");

        // SushiRolls - Menu and popularity indexes
        modelBuilder.Entity<SushiRoll>()
            .HasIndex(sr => new { sr.IsAvailable, sr.PopularityScore })
            .HasDatabaseName("IX_SushiRolls_Available_Popularity");

        modelBuilder.Entity<SushiRoll>()
            .HasIndex(sr => new { sr.IsSignatureRoll, sr.IsAvailable })
            .HasDatabaseName("IX_SushiRolls_Signature_Available");

        modelBuilder.Entity<SushiRoll>()
            .HasIndex(sr => new { sr.IsVegetarian, sr.IsAvailable })
            .HasDatabaseName("IX_SushiRolls_Vegetarian_Available");

        // Ingredients - Filtering and availability indexes
        modelBuilder.Entity<Ingredient>()
            .HasIndex(i => new { i.Category, i.IsAvailable })
            .HasDatabaseName("IX_Ingredients_Category_Available");

        modelBuilder.Entity<Ingredient>()
            .HasIndex(i => new { i.IsAvailable, i.StockQuantity })
            .HasDatabaseName("IX_Ingredients_Available_Stock");

        // Customers - Login and search indexes
        modelBuilder.Entity<Customer>()
            .HasIndex(c => new { c.IsActive, c.LastLoginAt })
            .HasDatabaseName("IX_Customers_Active_LastLogin");

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.TotalOrders)
            .HasDatabaseName("IX_Customers_TotalOrders");

        // AdminUsers - Authentication indexes
        modelBuilder.Entity<AdminUser>()
            .HasIndex(au => new { au.IsActive, au.Role })
            .HasDatabaseName("IX_AdminUsers_Active_Role");

        // OrderItems - Order details performance
        modelBuilder.Entity<OrderItem>()
            .HasIndex(oi => new { oi.OrderId, oi.SushiRollId })
            .HasDatabaseName("IX_OrderItems_Order_SushiRoll");

        // OrderStatusHistory - Tracking and audit
        modelBuilder.Entity<OrderStatusHistory>()
            .HasIndex(osh => new { osh.OrderId, osh.CreatedAt })
            .HasDatabaseName("IX_OrderStatusHistory_Order_CreatedAt");

        // DailyAnalytics - Reporting indexes
        modelBuilder.Entity<DailyAnalytics>()
            .HasIndex(da => da.Date)
            .HasDatabaseName("IX_DailyAnalytics_Date");

        // CustomerAddress - Customer lookup
        modelBuilder.Entity<CustomerAddress>()
            .HasIndex(ca => new { ca.CustomerId, ca.IsDefault })
            .HasDatabaseName("IX_CustomerAddress_Customer_Default");
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed default admin user
        modelBuilder.Entity<AdminUser>().HasData(
            new AdminUser
            {
                Id = 1,
                Username = "admin",
                Email = "admin@hidasushi.com",
                PasswordHash = "$2a$11$7hOB7LpLz7wkKgxF7yGCp.1JvGKOJtCQj3M2YPXBz9J3qBYGcb.HO", // "admin123"
                Role = "SuperAdmin",
                FullName = "System Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        // Seed sample sushi rolls for development
        modelBuilder.Entity<SushiRoll>().HasData(
            new SushiRoll
            {
                Id = 1,
                Name = "Dragon Roll",
                Description = "Grilled eel, cucumber, avocado with special dragon sauce",
                Price = 18.90m,
                IsSignatureRoll = true,
                IsAvailable = true,
                PreparationTimeMinutes = 20,
                Calories = 320,
                PopularityScore = 95,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SushiRoll
            {
                Id = 2,
                Name = "California Roll",
                Description = "Crab, avocado, cucumber wrapped in seaweed and sesame seeds",
                Price = 12.50m,
                IsSignatureRoll = true,
                IsAvailable = true,
                PreparationTimeMinutes = 15,
                Calories = 255,
                PopularityScore = 88,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SushiRoll
            {
                Id = 3,
                Name = "Vegetarian Garden Roll",
                Description = "Fresh avocado, cucumber, carrot, and lettuce with sesame dressing",
                Price = 11.90m,
                IsSignatureRoll = true,
                IsVegetarian = true,
                IsAvailable = true,
                PreparationTimeMinutes = 12,
                Calories = 180,
                PopularityScore = 75,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        // Seed sample ingredients
        modelBuilder.Entity<Ingredient>().HasData(
            new Ingredient
            {
                Id = 1,
                Name = "Fresh Salmon",
                Description = "Premium Norwegian salmon",
                Category = IngredientCategory.Protein,
                AdditionalPrice = 3.50m,
                IsAvailable = true,
                StockQuantity = 50,
                MinStockLevel = 10,
                Calories = 208,
                Protein = 25.4m,
                Fat = 12.4m,
                Carbs = 0m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                Id = 2,
                Name = "Avocado",
                Description = "Fresh ripe avocado",
                Category = IngredientCategory.Vegetable,
                AdditionalPrice = 1.50m,
                IsAvailable = true,
                IsVegan = true,
                StockQuantity = 30,
                MinStockLevel = 5,
                Calories = 160,
                Protein = 2m,
                Fat = 14.7m,
                Carbs = 8.5m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps automatically
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
    {
            var entity = (BaseEntity)entry.Entity;
            
            if (entry.State == EntityState.Added)
        {
                entity.CreatedAt = DateTime.UtcNow;
        }

            entity.UpdatedAt = DateTime.UtcNow;
        }

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes to database");
            throw;
        }
    }
}

// Base entity for automatic timestamp management
public abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
} 