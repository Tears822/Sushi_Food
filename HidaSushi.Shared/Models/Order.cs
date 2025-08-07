using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HidaSushi.Shared.Models;

public class Order
{
    public int Id { get; set; }
    
    [Required]
    public string OrderNumber { get; set; } = string.Empty;
    
    // Customer relationship (nullable for guest orders)
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    public string CustomerName { get; set; } = string.Empty;
    
    public string CustomerEmail { get; set; } = string.Empty;
    
    public string CustomerPhone { get; set; } = string.Empty;
    
    public OrderType Type { get; set; }
    
    public string DeliveryAddress { get; set; } = string.Empty;
    
    public string DeliveryInstructions { get; set; } = string.Empty;
    
    // Enhanced pricing breakdown
    public decimal SubtotalAmount { get; set; }
    
    public decimal DeliveryFee { get; set; } = 0;
    
    public decimal TaxAmount { get; set; } = 0;
    
    public decimal TotalAmount { get; set; }
    
    public List<OrderItem> Items { get; set; } = new();
    
    public OrderStatus Status { get; set; } = OrderStatus.Received;
    
    // Enhanced payment fields
    public PaymentMethod PaymentMethod { get; set; }
    
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    
    public string? PaymentIntentId { get; set; } // For Stripe integration
    
    public string Notes { get; set; } = string.Empty;
    
    // Enhanced tracking timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? EstimatedDeliveryTime { get; set; }
    
    public DateTime? ActualDeliveryTime { get; set; }
    
    public DateTime? AcceptedAt { get; set; }
    
    public int? AcceptedBy { get; set; } // AdminUser ID
    
    public AdminUser? AcceptedByUser { get; set; }
    
    public DateTime? PreparationStartedAt { get; set; }
    
    public DateTime? PreparationCompletedAt { get; set; }
    
    public string Location { get; set; } = "Brussels"; // Support for multiple locations
    
    // Navigation properties
    public List<OrderStatusHistory> StatusHistory { get; set; } = new();
    
    // Calculated properties
    [NotMapped]
    public TimeSpan? PreparationTime => 
        PreparationStartedAt.HasValue && PreparationCompletedAt.HasValue 
            ? PreparationCompletedAt - PreparationStartedAt 
            : null;
    
    [NotMapped]
    public TimeSpan? TotalProcessingTime => 
        AcceptedAt.HasValue && ActualDeliveryTime.HasValue 
            ? ActualDeliveryTime - AcceptedAt 
            : null;
}

public class OrderItem
{
    public int Id { get; set; }
    
    public int OrderId { get; set; }
    
    public Order Order { get; set; } = null!;
    
    public int? SushiRollId { get; set; } // For signature rolls
    
    public SushiRoll? SushiRoll { get; set; }
    
    public int? CustomRollId { get; set; }
    
    public CustomRoll? CustomRoll { get; set; } // For build-your-own rolls
    
    public int Quantity { get; set; }
    
    public decimal UnitPrice { get; set; } // Price per item at time of order
    
    public decimal Price { get; set; } // Total price (UnitPrice * Quantity)
    
    public string Notes { get; set; } = string.Empty;
    
    public string SpecialInstructions { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CustomRoll
{
    public int Id { get; set; }
    
    public string Name { get; set; } = "Custom Roll";
    
    public RollType RollType { get; set; } = RollType.Normal;
    
    [NotMapped]
    public List<int> SelectedIngredientIds { get; set; } = new();
    
    [NotMapped]
    public List<Ingredient> SelectedIngredients { get; set; } = new();
    
    // JSON properties for database storage
    public string? SelectedIngredientsJson { get; set; }
    public string? AllergensJson { get; set; }
    
    public decimal TotalPrice { get; set; }
    
    public int? Calories { get; set; } // Calculated from ingredients
    
    [NotMapped]
    public List<string> Allergens { get; set; } = new(); // Calculated from ingredients
    
    public string Notes { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum OrderType
{
    Pickup,
    Delivery
}

public enum OrderStatus
{
    Received,
    Accepted,
    InPreparation,
    Ready,
    OutForDelivery,
    Completed,
    Cancelled
}

public enum PaymentMethod
{
    Stripe,
    CashOnDelivery,
    GodPay
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Failed,
    Refunded
}

public enum RollType
{
    Normal,
    InsideOut,
    CucumberWrap
}

public static class OrderExtensions
{
    public static string GetStatusDisplay(this OrderStatus status) => status switch
    {
        OrderStatus.Received => "✅ Received",
        OrderStatus.Accepted => "👨‍🍳 Accepted",
        OrderStatus.InPreparation => "🍣 In Preparation",
        OrderStatus.Ready => "📦 Ready for Pickup",
        OrderStatus.OutForDelivery => "🛵 Out for Delivery",
        OrderStatus.Completed => "✔️ Completed",
        OrderStatus.Cancelled => "❌ Cancelled",
        _ => status.ToString()
    };
    
    public static string GetStatusColor(this OrderStatus status) => status switch
    {
        OrderStatus.Received => "bg-blue-100 text-blue-800",
        OrderStatus.Accepted => "bg-yellow-100 text-yellow-800",
        OrderStatus.InPreparation => "bg-orange-100 text-orange-800",
        OrderStatus.Ready => "bg-green-100 text-green-800",
        OrderStatus.OutForDelivery => "bg-purple-100 text-purple-800",
        OrderStatus.Completed => "bg-gray-100 text-gray-800",
        OrderStatus.Cancelled => "bg-red-100 text-red-800",
        _ => "bg-gray-100 text-gray-800"
    };
    
    public static bool CanTransitionTo(this OrderStatus current, OrderStatus target) => current switch
    {
        OrderStatus.Received => target == OrderStatus.Accepted || target == OrderStatus.Cancelled,
        OrderStatus.Accepted => target == OrderStatus.InPreparation || target == OrderStatus.Cancelled,
        OrderStatus.InPreparation => target == OrderStatus.Ready || target == OrderStatus.Cancelled,
        OrderStatus.Ready => target == OrderStatus.OutForDelivery || target == OrderStatus.Completed || target == OrderStatus.Cancelled,
        OrderStatus.OutForDelivery => target == OrderStatus.Completed,
        _ => false
    };
} 