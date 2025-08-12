using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HidaSushi.Shared.Models;

public class Ingredient
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; } = string.Empty;
    
    public IngredientCategory Category { get; set; }
    
    public decimal AdditionalPrice { get; set; } = 0;
    
    // Alias for backward compatibility with existing code
    public decimal Price { get; set; } = 0;
    
    public bool IsAvailable { get; set; } = true;
    
    public bool IsVegetarian { get; set; } = true;
    
    [NotMapped]
    public List<string> Allergens { get; set; } = new();
    
    // JSON property for database storage
    public string? AllergensJson { get; set; }
    
    public int MaxAllowed { get; set; } = 1;
    
    public string? ImageUrl { get; set; } = string.Empty;
    
    // Additional properties for nutrition info (optional)
    public int Calories { get; set; } = 0;
    public bool IsVegan { get; set; } = false;
    public bool IsGlutenFree { get; set; } = false;
    
    // Nutrition details
    public decimal? Protein { get; set; } // grams
    public decimal? Carbs { get; set; } // grams
    public decimal? Fat { get; set; } // grams
    
    // Inventory management
    public int? StockQuantity { get; set; }
    public int MinStockLevel { get; set; } = 10;
    
    // Analytics properties
    public int PopularityScore { get; set; } = 0;
    public int TimesUsed { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum IngredientCategory
{
    Base,
    Protein,
    Vegetable,  // Changed from Vegetables to match database
    Extra,      // Changed from Extras to match database
    Topping,    // Changed from Toppings to match database
    Sauce,      // Changed from Sauces to match database
    Wrapper     // Added for nori, etc.
}

public static class DefaultIngredients
{
    public static readonly List<Ingredient> All = new()
    {
        // Base
        new() { Id = 1, Name = "Sushi Rice", Category = IngredientCategory.Base, Description = "Traditional seasoned sushi rice" },
        new() { Id = 2, Name = "Brown Rice", Category = IngredientCategory.Base, Description = "Healthy brown rice option", AdditionalPrice = 1 },
        
        // Proteins
        new() { Id = 10, Name = "Fresh Tuna", Category = IngredientCategory.Protein, Description = "Premium grade tuna", Allergens = new() { "Fish" } },
        new() { Id = 11, Name = "Norwegian Salmon", Category = IngredientCategory.Protein, Description = "Fresh Atlantic salmon", Allergens = new() { "Fish" } },
        new() { Id = 12, Name = "Tempura Shrimp", Category = IngredientCategory.Protein, Description = "Crispy fried shrimp", Allergens = new() { "Shellfish", "Gluten" } },
        new() { Id = 13, Name = "Imitation Crab", Category = IngredientCategory.Protein, Description = "Sweet crab stick", Allergens = new() { "Shellfish" } },
        new() { Id = 14, Name = "Organic Tofu", Category = IngredientCategory.Protein, Description = "Fresh organic tofu", Allergens = new() { "Soy" } },
        new() { Id = 15, Name = "Tamago (Egg)", Category = IngredientCategory.Protein, Description = "Sweet Japanese omelette", Allergens = new() { "Eggs" } },
        
        // Vegetables
        new() { Id = 20, Name = "Avocado", Category = IngredientCategory.Vegetable, Description = "Creamy fresh avocado" },
        new() { Id = 21, Name = "Cucumber", Category = IngredientCategory.Vegetable, Description = "Crisp cucumber" },
        new() { Id = 22, Name = "Carrot", Category = IngredientCategory.Vegetable, Description = "Fresh julienned carrot" },
        new() { Id = 23, Name = "Lettuce", Category = IngredientCategory.Vegetable, Description = "Fresh green lettuce" },
        
        // Extras
        new() { Id = 30, Name = "Cream Cheese", Category = IngredientCategory.Extra, Description = "Rich cream cheese", Allergens = new() { "Dairy" } },
        new() { Id = 31, Name = "Avocado", Category = IngredientCategory.Extra, Description = "Extra avocado" },
        
        // Toppings
        new() { Id = 40, Name = "Sesame Seeds", Category = IngredientCategory.Topping, Description = "Toasted sesame seeds", Allergens = new() { "Sesame" } },
        new() { Id = 41, Name = "Tempura Flakes", Category = IngredientCategory.Topping, Description = "Crispy tempura bits", Allergens = new() { "Gluten" } },
        
        // Sauces
        new() { Id = 50, Name = "Spicy Mayo", Category = IngredientCategory.Sauce, Description = "Creamy spicy sauce", Allergens = new() { "Eggs" } },
        new() { Id = 51, Name = "Eel Sauce", Category = IngredientCategory.Sauce, Description = "Sweet eel sauce", Allergens = new() { "Soy" } }
    };
} 