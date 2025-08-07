using Microsoft.EntityFrameworkCore;
using HidaSushi.Shared.Models;

namespace HidaSushi.Server.Data;

public class HidaSushiDbContext : DbContext
{
    public HidaSushiDbContext(DbContextOptions<HidaSushiDbContext> options) : base(options)
    {
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Customer
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.TotalSpent).HasColumnType("decimal(10,2)");
            entity.Property(e => e.PreferencesJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure CustomerAddress
        modelBuilder.Entity<CustomerAddress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Label).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AddressLine1).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AddressLine2).HasMaxLength(200);
            entity.Property(e => e.City).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PostalCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Country).HasMaxLength(50);
            
            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Addresses)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AdminUser
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PermissionsJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(e => e.Username).IsUnique();
        });

        // Configure SushiRoll
        modelBuilder.Entity<SushiRoll>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IngredientsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.AllergensJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.PreparationTimeMinutes).HasDefaultValue(15);
            entity.Property(e => e.PopularityScore).HasDefaultValue(0);
            entity.Property(e => e.TimesOrdered).HasDefaultValue(0);
        });

        // Configure Ingredient
        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.AdditionalPrice).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Category).HasConversion<string>();
            entity.Property(e => e.AllergensJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.MaxAllowed).HasDefaultValue(1);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Calories).HasDefaultValue(0);
            entity.Property(e => e.Protein).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Carbs).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Fat).HasColumnType("decimal(5,2)");
            entity.Property(e => e.StockQuantity);
            entity.Property(e => e.MinStockLevel).HasDefaultValue(10);
            entity.Property(e => e.PopularityScore).HasDefaultValue(0);
            entity.Property(e => e.TimesUsed).HasDefaultValue(0);
        });

        // Configure Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CustomerEmail).HasMaxLength(200);
            entity.Property(e => e.CustomerPhone).HasMaxLength(20);
            entity.Property(e => e.SubtotalAmount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.DeliveryFee).HasColumnType("decimal(10,2)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.DeliveryAddress).HasMaxLength(500);
            entity.Property(e => e.DeliveryInstructions).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.PaymentIntentId).HasMaxLength(200);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.PaymentMethod).HasConversion<string>();
            entity.Property(e => e.PaymentStatus).HasConversion<string>();
            entity.HasIndex(e => e.OrderNumber).IsUnique();

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.AcceptedByUser)
                .WithMany()
                .HasForeignKey(e => e.AcceptedBy)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Items)
                .WithOne(oi => oi.Order)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.StatusHistory)
                .WithOne(h => h.Order)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure OrderItem
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
            entity.Property(e => e.SpecialInstructions).HasMaxLength(500);

            entity.HasOne(e => e.SushiRoll)
                .WithMany()
                .HasForeignKey(e => e.SushiRollId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CustomRoll)
                .WithMany()
                .HasForeignKey(e => e.CustomRollId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure CustomRoll
        modelBuilder.Entity<CustomRoll>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.RollType).HasConversion<string>();
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(10,2)");
            entity.Property(e => e.SelectedIngredientsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.AllergensJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Notes).HasMaxLength(500);
        });

        // Configure OrderStatusHistory
        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PreviousStatus).HasMaxLength(30);
            entity.Property(e => e.NewStatus).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.ChangedByUser)
                .WithMany()
                .HasForeignKey(e => e.ChangedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure DailyAnalytics
        modelBuilder.Entity<DailyAnalytics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.TotalOrders).IsRequired();
            entity.Property(e => e.TotalRevenue).HasColumnType("decimal(10,2)");
            entity.Property(e => e.AverageOrderValue).HasColumnType("decimal(10,2)");
            entity.Property(e => e.AveragePrepTime).HasColumnType("time");
            entity.Property(e => e.PopularRollsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.PopularIngredientsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.HourlyOrderCountsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CustomerRetentionRate).HasColumnType("decimal(5,2)");
            entity.HasIndex(e => e.Date).IsUnique();
            
            // Ignore the Dictionary property as it's handled via JSON
            entity.Ignore(e => e.HourlyOrderCounts);
            entity.Ignore(e => e.PopularRolls);
            entity.Ignore(e => e.PopularIngredients);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Admin Users
        modelBuilder.Entity<AdminUser>().HasData(
            new AdminUser
            {
                Id = 1,
                Username = "admin",
                Email = "admin@hidasushi.net",
                PasswordHash = "$2a$11$dummy.hash.for.HidaSushi2024!",
                Role = "Admin",
                FullName = "System Administrator",
                PermissionsJson = "[\"all\"]",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new AdminUser
            {
                Id = 2,
                Username = "jonathan",
                Email = "jonathan@hidasushi.net",
                PasswordHash = "$2a$11$dummy.hash.for.ChefJonathan123!",
                Role = "Chef",
                FullName = "Chef Jonathan",
                PermissionsJson = "[\"orders\", \"menu\", \"analytics\", \"ingredients\"]",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new AdminUser
            {
                Id = 3,
                Username = "kitchen",
                Email = "kitchen@hidasushi.net",
                PasswordHash = "$2a$11$dummy.hash.for.Kitchen2024!",
                Role = "Kitchen",
                FullName = "Kitchen Staff",
                PermissionsJson = "[\"orders\", \"order_status\"]",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        // Seed Demo Customer
        modelBuilder.Entity<Customer>().HasData(
            new Customer
            {
                Id = 1,
                FullName = "Demo Customer",
                Email = "demo@customer.com",
                Phone = "+32 470 12 34 56",
                PasswordHash = "$2a$11$dummy.hash.for.demo123!",
                IsActive = true,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        // Seed Signature Rolls
        modelBuilder.Entity<SushiRoll>().HasData(SignatureRolls.DefaultRolls);

        // Seed Ingredients
        modelBuilder.Entity<Ingredient>().HasData(DefaultIngredients.All);
    }

    // Public method for runtime seeding
    public void SeedRuntimeData()
    {
        // Only add if empty
        if (!SushiRolls.Any())
        {
            SushiRolls.AddRange(SignatureRolls.DefaultRolls);
        }

        if (!Ingredients.Any())
        {
            Ingredients.AddRange(DefaultIngredients.All);
        }

        if (!Customers.Any())
        {
            Customers.Add(new Customer
            {
                FullName = "Demo Customer",
                Email = "demo@customer.com",
                Phone = "+32 470 12 34 56",
                PasswordHash = "$2a$11$dummy.hash.for.demo123!",
                IsActive = true,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }
} 