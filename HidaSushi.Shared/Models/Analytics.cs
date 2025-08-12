using System.ComponentModel.DataAnnotations.Schema;

namespace HidaSushi.Shared.Models;

public class DashboardStats
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public int PendingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalMenuItems { get; set; }
    public int TotalIngredients { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<PopularItem> TopSellingItems { get; set; } = new();
    public List<PopularItem> TopIngredients { get; set; } = new();
    public Dictionary<string, int> OrdersByStatus { get; set; } = new();
    public Dictionary<string, decimal> RevenueByDay { get; set; } = new();
}

public class DailyAnalytics
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int DeliveryOrders { get; set; }
    public int PickupOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public TimeSpan AveragePrepTime { get; set; }
    
    [NotMapped]
    public List<PopularItem> PopularRolls { get; set; } = new();
    
    [NotMapped]
    public List<PopularItem> PopularIngredients { get; set; } = new();
    
    [NotMapped]
    public Dictionary<int, int> HourlyOrderCounts { get; set; } = new();
    
    public decimal CustomerRetentionRate { get; set; }
    public int NewCustomers { get; set; }
    public int ReturningCustomers { get; set; }
    
    // JSON properties for database storage
    public string? PopularRollsJson { get; set; }
    public string? PopularIngredientsJson { get; set; }
    public string? HourlyOrderCountsJson { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PopularItem
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public double Percentage { get; set; }
    public decimal Revenue { get; set; } = 0;
}

public class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string PreviousStatus { get; set; } = "";
    public string NewStatus { get; set; } = "";
    public int? UpdatedBy { get; set; } // AdminUser ID
    public string Notes { get; set; } = "";
    public DateTime? EstimatedCompletionTime { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Order Order { get; set; } = null!;
    public AdminUser? UpdatedByUser { get; set; }
}

public class WeeklyReport
{
    public DateTime WeekStarting { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int NewCustomers { get; set; }
    public List<PopularItem> TopSellingRolls { get; set; } = new();
    public List<PopularItem> TopIngredients { get; set; } = new();
    public Dictionary<DayOfWeek, int> OrdersByDay { get; set; } = new();
    public TimeSpan AveragePreparationTime { get; set; }
    public decimal CustomerSatisfactionScore { get; set; }
}

public class MonthlyReport
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal GrowthPercentage { get; set; }
    public int ActiveCustomers { get; set; }
    public decimal CustomerLifetimeValue { get; set; }
    public List<DailyAnalytics> DailyBreakdown { get; set; } = new();
    public List<PopularItem> MonthlyBestSellers { get; set; } = new();
} 