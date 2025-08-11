using System.ComponentModel.DataAnnotations;

namespace HidaSushi.Shared.Models;

public class Customer
{
    public int Id { get; set; }
    
    [Required]
    public string FullName { get; set; } = "";
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";
    
    public string Phone { get; set; } = "";
    
    public string PasswordHash { get; set; } = "";
    
    public bool IsActive { get; set; } = true;
    
    public bool EmailVerified { get; set; } = false;
    
    public string? PreferencesJson { get; set; } // JSON for dietary preferences
    
    public int TotalOrders { get; set; } = 0;
    
    public decimal TotalSpent { get; set; } = 0;
    
    public int LoyaltyPoints { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public List<CustomerAddress> Addresses { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
}

// Customer Registration Models
public class CustomerRegistrationRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
}

public class CustomerRegistrationResult
{
    public bool Success { get; set; }
    public int CustomerId { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CustomerUpdateRequest
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class GuestOrderRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class GuestOrderResult
{
    public bool Success { get; set; }
    public int CustomerId { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CustomerAddress
{
    public int Id { get; set; }
    
    public int CustomerId { get; set; }
    
    [Required]
    public string Label { get; set; } = ""; // Home, Work, etc.
    
    [Required]
    public string AddressLine1 { get; set; } = "";
    
    public string AddressLine2 { get; set; } = "";
    
    [Required]
    public string City { get; set; } = "";
    
    [Required]
    public string PostalCode { get; set; } = "";
    
    public string Country { get; set; } = "Belgium";
    
    public bool IsDefault { get; set; } = false;
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation property
    public Customer Customer { get; set; } = null!;
}

public class CustomerPreferences
{
    public List<string> DietaryRestrictions { get; set; } = new();
    public List<string> Allergens { get; set; } = new();
    public List<int> FavoriteRollIds { get; set; } = new();
    public List<int> FavoriteIngredientIds { get; set; } = new();
    public bool ReceivePromotions { get; set; } = true;
    public bool ReceiveOrderUpdates { get; set; } = true;
} 