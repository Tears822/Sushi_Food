using Microsoft.EntityFrameworkCore;
using HidaSushi.Shared.Models;
using System.Reflection;
using BCrypt.Net;

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

        // Configure relationships with proper foreign key names
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
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.TotalSpent).HasColumnType("decimal(12,2)").HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.EmailVerified).HasDefaultValue(false);
            entity.Property(e => e.PhoneVerified).HasDefaultValue(false);
            entity.Property(e => e.PreferencesJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Unique constraints
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("IX_Customers_Email_Unique");
            entity.HasIndex(e => e.Phone).IsUnique().HasDatabaseName("IX_Customers_Phone_Unique");
        });
    }

    private void ConfigureAdminUserEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20).HasDefaultValue("Admin");
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
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
            entity.Property(e => e.Protein).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Carbs).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Fat).HasColumnType("decimal(5,2)");
            entity.Property(e => e.IngredientsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.AllergensJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.IsSignatureRoll).HasDefaultValue(false);
            entity.Property(e => e.IsVegetarian).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Index for filtering
            entity.HasIndex(e => new { e.IsAvailable, e.IsSignatureRoll, e.IsVegetarian })
                  .HasDatabaseName("IX_SushiRolls_Availability_Type");
        });
    }

    private void ConfigureIngredientEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(300).IsRequired(false);
            entity.Property(e => e.ImageUrl).HasMaxLength(500).IsRequired(false);
            entity.Property(e => e.AllergensJson).IsRequired(false);
            entity.Property(e => e.Price).HasColumnType("decimal(8,2)").HasDefaultValue(0);
            entity.Property(e => e.AdditionalPrice).HasColumnType("decimal(8,2)").HasDefaultValue(0);
            entity.Property(e => e.Protein).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Carbs).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Fat).HasColumnType("decimal(5,2)");
            entity.Property(e => e.IsVegetarian).HasDefaultValue(true);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Unique constraint
            entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("IX_Ingredients_Name_Unique");
        });
    }

    private void ConfigureOrderEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.OrderNumber).IsRequired().HasMaxLength(50);
            entity.Property(o => o.CustomerName).HasMaxLength(100);
            entity.Property(o => o.CustomerEmail).HasMaxLength(100);
            entity.Property(o => o.CustomerPhone).HasMaxLength(20);
            entity.Property(o => o.DeliveryAddress).HasMaxLength(500);
            entity.Property(o => o.Notes).HasMaxLength(1000);
            entity.Property(o => o.PaymentIntentId).HasMaxLength(100);
            entity.Property(o => o.PaymentReference).HasMaxLength(100);
            entity.Property(o => o.Location).HasMaxLength(100);
            
            // Configure decimal properties
            entity.Property(o => o.SubtotalAmount).HasPrecision(10, 2);
            entity.Property(o => o.DeliveryFee).HasPrecision(10, 2);
            entity.Property(o => o.TaxAmount).HasPrecision(10, 2);
            entity.Property(o => o.TotalAmount).HasPrecision(10, 2);
            
            // Disable triggers for Entity Framework compatibility
            entity.ToTable(tb => tb.HasTrigger("TR_Orders_UpdateCustomerStats"));
            
            // Configure relationships
            entity.HasOne(o => o.Customer)
                  .WithMany(c => c.Orders)
                  .HasForeignKey(o => o.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);
                  
            entity.HasOne(o => o.AcceptedByUser)
                  .WithMany()
                  .HasForeignKey(o => o.AcceptedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void ConfigureOrderItemEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500).IsRequired(false);
            entity.Property(e => e.SpecialInstructions).HasMaxLength(500).IsRequired(false);
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
            entity.Property(e => e.SelectedIngredientsJson).HasColumnType("nvarchar(max)").IsRequired(false);
            entity.Property(e => e.AllergensJson).HasColumnType("nvarchar(max)").IsRequired(false);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500).IsRequired(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });
    }

    private void ConfigureOrderStatusHistoryEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
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
            entity.Property(e => e.CustomerRetentionRate).HasColumnType("decimal(5,2)").HasDefaultValue(0);
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
            .HasConstraintName("FK_CustomerAddresses_CustomerId")
            .OnDelete(DeleteBehavior.Cascade);

        // Customer -> Order (One-to-Many, nullable for guest orders)
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .HasConstraintName("FK_Orders_CustomerId")
            .OnDelete(DeleteBehavior.SetNull);

        // AdminUser -> Order (One-to-Many, nullable)
        modelBuilder.Entity<Order>()
            .HasOne(o => o.AcceptedByUser)
            .WithMany()
            .HasForeignKey(o => o.AcceptedBy)
            .HasConstraintName("FK_Orders_AcceptedBy")
            .OnDelete(DeleteBehavior.SetNull);
            
        // Order -> OrderItem (One-to-Many)
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .HasConstraintName("FK_OrderItems_OrderId")
            .OnDelete(DeleteBehavior.Cascade);

        // SushiRoll -> OrderItem (One-to-Many, nullable)
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.SushiRoll)
            .WithMany()
            .HasForeignKey(oi => oi.SushiRollId)
            .HasConstraintName("FK_OrderItems_SushiRollId")
            .OnDelete(DeleteBehavior.SetNull);

        // CustomRoll -> OrderItem (One-to-Many, nullable)
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.CustomRoll)
            .WithMany()
            .HasForeignKey(oi => oi.CustomRollId)
            .HasConstraintName("FK_OrderItems_CustomRollId")
            .OnDelete(DeleteBehavior.SetNull);

        // Order -> OrderStatusHistory (One-to-Many)
        modelBuilder.Entity<OrderStatusHistory>()
            .HasOne(osh => osh.Order)
            .WithMany(o => o.StatusHistory)
            .HasForeignKey(osh => osh.OrderId)
            .HasConstraintName("FK_OrderStatusHistory_OrderId")
            .OnDelete(DeleteBehavior.Cascade);

        // AdminUser -> OrderStatusHistory (One-to-Many, nullable)
        modelBuilder.Entity<OrderStatusHistory>()
            .HasOne(osh => osh.UpdatedByUser)
            .WithMany()
            .HasForeignKey(osh => osh.UpdatedBy)
            .HasConstraintName("FK_OrderStatusHistory_UpdatedBy")
            .OnDelete(DeleteBehavior.SetNull);
    }

    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Performance indexes for common queries
        
        // Orders - filtering by status, payment status, type
        modelBuilder.Entity<Order>()
            .HasIndex(e => e.Status)
            .HasDatabaseName("IX_Orders_Status");
            
        modelBuilder.Entity<Order>()
            .HasIndex(e => e.PaymentStatus)
            .HasDatabaseName("IX_Orders_PaymentStatus");
            
        modelBuilder.Entity<Order>()
            .HasIndex(e => e.Type)
            .HasDatabaseName("IX_Orders_Type");
            
        modelBuilder.Entity<Order>()
            .HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_Orders_CreatedAt");

        // OrderItems - performance for order retrieval
        modelBuilder.Entity<OrderItem>()
            .HasIndex(e => e.OrderId)
            .HasDatabaseName("IX_OrderItems_OrderId");

        // OrderStatusHistory - tracking order status changes
        modelBuilder.Entity<OrderStatusHistory>()
            .HasIndex(e => e.OrderId)
            .HasDatabaseName("IX_OrderStatusHistory_OrderId");
            
        modelBuilder.Entity<OrderStatusHistory>()
            .HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_OrderStatusHistory_CreatedAt");

        // Customer addresses - fast lookup
        modelBuilder.Entity<CustomerAddress>()
            .HasIndex(e => e.CustomerId)
            .HasDatabaseName("IX_CustomerAddresses_CustomerId");
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
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "SuperAdmin",
                FullName = "System Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        // Seed sample ingredients
        var ingredients = new[]
        {
            new Ingredient { Id = 1, Name = "Salmon", Description = "Fresh Atlantic salmon", Price = 3.50m, IsVegetarian = false, Category = IngredientCategory.Protein, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 2, Name = "Tuna", Description = "Fresh yellowfin tuna", Price = 4.00m, IsVegetarian = false, Category = IngredientCategory.Protein, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 3, Name = "Avocado", Description = "Fresh avocado", Price = 2.00m, IsVegetarian = true, Category = IngredientCategory.Vegetable, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 4, Name = "Cucumber", Description = "Fresh cucumber", Price = 1.00m, IsVegetarian = true, Category = IngredientCategory.Vegetable, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 5, Name = "Nori", Description = "Seaweed sheets", Price = 0.50m, IsVegetarian = true, Category = IngredientCategory.Wrapper, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 6, Name = "Sushi Rice", Description = "Seasoned sushi rice", Price = 1.50m, IsVegetarian = true, Category = IngredientCategory.Base, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 7, Name = "Tempura Shrimp", Description = "Crispy tempura shrimp", Price = 4.50m, IsVegetarian = false, Category = IngredientCategory.Protein, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 8, Name = "Cream Cheese", Description = "Philadelphia cream cheese", Price = 1.50m, IsVegetarian = true, Category = IngredientCategory.Extra, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 9, Name = "Crab", Description = "Fresh crab meat", Price = 5.00m, IsVegetarian = false, Category = IngredientCategory.Protein, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 10, Name = "Eel", Description = "Grilled freshwater eel", Price = 6.00m, IsVegetarian = false, Category = IngredientCategory.Protein, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 11, Name = "Yellowtail", Description = "Fresh yellowtail sashimi", Price = 4.50m, IsVegetarian = false, Category = IngredientCategory.Protein, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 12, Name = "Spicy Mayo", Description = "Sriracha mayonnaise", Price = 0.75m, IsVegetarian = true, Category = IngredientCategory.Sauce, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 13, Name = "Eel Sauce", Description = "Sweet eel glaze", Price = 0.75m, IsVegetarian = true, Category = IngredientCategory.Sauce, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 14, Name = "Tempura Flakes", Description = "Crispy tempura bits", Price = 1.00m, IsVegetarian = true, Category = IngredientCategory.Topping, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 15, Name = "Smoked Salmon", Description = "Cold smoked salmon", Price = 4.00m, IsVegetarian = false, Category = IngredientCategory.Protein, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 16, Name = "Carrot", Description = "Fresh julienned carrot", Price = 0.75m, IsVegetarian = true, Category = IngredientCategory.Vegetable, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 17, Name = "Lettuce", Description = "Fresh lettuce leaves", Price = 0.50m, IsVegetarian = true, Category = IngredientCategory.Vegetable, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 18, Name = "Bell Pepper", Description = "Fresh bell pepper strips", Price = 1.25m, IsVegetarian = true, Category = IngredientCategory.Vegetable, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 19, Name = "Sriracha", Description = "Spicy chili sauce", Price = 0.50m, IsVegetarian = true, Category = IngredientCategory.Sauce, CreatedAt = DateTime.UtcNow },
            new Ingredient { Id = 20, Name = "Sesame Seeds", Description = "Toasted sesame seeds", Price = 0.25m, IsVegetarian = true, Category = IngredientCategory.Topping, CreatedAt = DateTime.UtcNow }
        };
        modelBuilder.Entity<Ingredient>().HasData(ingredients);

        // Seed sample sushi rolls
        var sushiRolls = new[]
        {
            new SushiRoll
            {
                Id = 1,
                Name = "California Roll",
                Description = "Classic roll with crab, avocado, and cucumber",
                Price = 12.99m,
                IngredientsJson = "[\"Crab\", \"Avocado\", \"Cucumber\", \"Nori\", \"Sushi Rice\"]",
                AllergensJson = "[\"Shellfish\"]",
                IsAvailable = true,
                IsSignatureRoll = false,
                IsVegetarian = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SushiRoll
            {
                Id = 2,
                Name = "Salmon Avocado Roll",
                Description = "Fresh salmon with creamy avocado",
                Price = 14.99m,
                IngredientsJson = "[\"Salmon\", \"Avocado\", \"Nori\", \"Sushi Rice\"]",
                AllergensJson = "[\"Fish\"]",
                IsAvailable = true,
                IsSignatureRoll = true,
                IsVegetarian = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SushiRoll
            {
                Id = 3,
                Name = "Vegetarian Roll",
                Description = "Fresh vegetables wrapped in nori",
                Price = 10.99m,
                IngredientsJson = "[\"Avocado\", \"Cucumber\", \"Carrot\", \"Nori\", \"Sushi Rice\"]",
                AllergensJson = "[]",
                IsAvailable = true,
                IsSignatureRoll = false,
                IsVegetarian = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SushiRoll
            {
                Id = 4,
                Name = "Dragon Roll",
                Description = "Eel and cucumber topped with avocado and eel sauce",
                Price = 18.99m,
                IngredientsJson = "[\"Eel\", \"Cucumber\", \"Avocado\", \"Nori\", \"Sushi Rice\", \"Eel Sauce\"]",
                AllergensJson = "[\"Fish\"]",
                IsAvailable = true,
                IsSignatureRoll = true,
                IsVegetarian = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SushiRoll
            {
                Id = 5,
                Name = "Spicy Tuna Roll",
                Description = "Fresh tuna with spicy mayo and tempura flakes",
                Price = 15.99m,
                IngredientsJson = "[\"Tuna\", \"Spicy Mayo\", \"Tempura Flakes\", \"Nori\", \"Sushi Rice\"]",
                AllergensJson = "[\"Fish\"]",
                IsAvailable = true,
                IsSignatureRoll = true,
                IsVegetarian = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SushiRoll
            {
                Id = 6,
                Name = "Rainbow Roll",
                Description = "California roll topped with assorted fresh sashimi",
                Price = 22.99m,
                IngredientsJson = "[\"Crab\", \"Avocado\", \"Cucumber\", \"Salmon\", \"Tuna\", \"Yellowtail\", \"Nori\", \"Sushi Rice\"]",
                AllergensJson = "[\"Fish\", \"Shellfish\"]",
                IsAvailable = true,
                IsSignatureRoll = true,
                IsVegetarian = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SushiRoll
            {
                Id = 7,
                Name = "Philadelphia Roll",
                Description = "Smoked salmon, cream cheese, and cucumber",
                Price = 13.99m,
                IngredientsJson = "[\"Smoked Salmon\", \"Cream Cheese\", \"Cucumber\", \"Nori\", \"Sushi Rice\"]",
                AllergensJson = "[\"Fish\", \"Dairy\"]",
                IsAvailable = true,
                IsSignatureRoll = false,
                IsVegetarian = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SushiRoll
            {
                Id = 8,
                Name = "Tempura Shrimp Roll",
                Description = "Crispy tempura shrimp with avocado and cucumber",
                Price = 16.99m,
                IngredientsJson = "[\"Tempura Shrimp\", \"Avocado\", \"Cucumber\", \"Nori\", \"Sushi Rice\"]",
                AllergensJson = "[\"Shellfish\"]",
                IsAvailable = true,
                IsSignatureRoll = true,
                IsVegetarian = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SushiRoll
            {
                Id = 9,
                Name = "Garden Fresh Roll",
                Description = "Assorted fresh vegetables with sesame dressing",
                Price = 9.99m,
                IngredientsJson = "[\"Avocado\", \"Cucumber\", \"Carrot\", \"Lettuce\", \"Bell Pepper\", \"Nori\", \"Sushi Rice\"]",
                AllergensJson = "[\"Sesame\"]",
                IsAvailable = true,
                IsSignatureRoll = false,
                IsVegetarian = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SushiRoll
            {
                Id = 10,
                Name = "Volcano Roll",
                Description = "Spicy tuna roll topped with spicy mayo and sriracha",
                Price = 17.99m,
                IngredientsJson = "[\"Tuna\", \"Spicy Mayo\", \"Sriracha\", \"Tempura Flakes\", \"Nori\", \"Sushi Rice\"]",
                AllergensJson = "[\"Fish\"]",
                IsAvailable = true,
                IsSignatureRoll = true,
                IsVegetarian = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
        modelBuilder.Entity<SushiRoll>().HasData(sushiRolls);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("OUTPUT clause") == true)
        {
            // Handle trigger conflicts by using raw SQL for updates on Orders table
            var entries = ChangeTracker.Entries<Order>()
                .Where(e => e.State == EntityState.Modified)
                .ToList();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Modified)
                {
                    var order = entry.Entity;
                    
                    // Use raw SQL to update the order without OUTPUT clause
                    await Database.ExecuteSqlInterpolatedAsync($@"
                        UPDATE Orders 
                        SET PaymentStatus = {order.PaymentStatus},
                            PaymentReference = {order.PaymentReference},
                            UpdatedAt = {order.UpdatedAt}
                        WHERE Id = {order.Id}");
                    
                    // Mark as unchanged to prevent EF from trying to save it again
                    entry.State = EntityState.Unchanged;
                }
            }
            
            // Save any remaining changes
            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}

// Base entity for automatic timestamp management
public abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
} 