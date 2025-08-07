using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HidaSushi.Shared.Models;

public class SushiRoll
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public decimal Price { get; set; }
    
    public string ImageUrl { get; set; } = string.Empty;
    
    [NotMapped]
    public List<string> Ingredients { get; set; } = new();
    
    [NotMapped]
    public List<string> Allergens { get; set; } = new();
    
    // JSON properties for database storage
    public string? IngredientsJson { get; set; }
    public string? AllergensJson { get; set; }
    
    public bool IsSignatureRoll { get; set; } = true;
    
    public bool IsVegetarian { get; set; } = false;
    
    public bool IsVegan { get; set; } = false;
    
    public bool IsGlutenFree { get; set; } = false;
    
    public bool IsAvailable { get; set; } = true;
    
    // Analytics and performance properties
    public int PreparationTimeMinutes { get; set; } = 15;
    public int? Calories { get; set; }
    public int PopularityScore { get; set; } = 0;
    public int TimesOrdered { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public static class SignatureRolls
{
    public static readonly List<SushiRoll> DefaultRolls = new()
    {
        new SushiRoll
        {
            Id = 1,
            Name = "Taylor Swift – Tortured Poets Dragon Roll",
            Description = "🐉 Avocado top, double tuna, spicy crunch, seaweed pearls, tempura shrimp",
            Price = 13,
            ImageUrl = "/images/taylor-swift-roll.jpg",
            Ingredients = new() { "Avocado", "Tuna", "Tempura Shrimp", "Spicy Mayo", "Sesame Seeds" },
            Allergens = new() { "Fish", "Shellfish", "Sesame" }
        },
        new SushiRoll
        {
            Id = 2,
            Name = "Blackbird Rainbow Roll",
            Description = "🐦 5-piece nigiri symphony: salmon, tuna, scallop, eel, yellowtail",
            Price = 17,
            ImageUrl = "/images/blackbird-roll.jpg",
            Ingredients = new() { "Salmon", "Tuna", "Scallop", "Eel", "Yellowtail", "Sushi Rice" },
            Allergens = new() { "Fish", "Shellfish" }
        },
        new SushiRoll
        {
            Id = 3,
            Name = "M&M \"Beautiful\" Roll",
            Description = "👑 Big, bold, meaty — salmon + wagyu + wasabi aioli",
            Price = 19,
            ImageUrl = "/images/mm-beautiful-roll.jpg",
            Ingredients = new() { "Salmon", "Wagyu Beef", "Wasabi Aioli", "Cucumber", "Avocado" },
            Allergens = new() { "Fish", "Beef" }
        },
        new SushiRoll
        {
            Id = 4,
            Name = "Joker Laughing Volcano Roll",
            Description = "🃏 Spicy tuna, cream cheese, topped with flamed jalapeño mayo",
            Price = 23,
            ImageUrl = "/images/joker-volcano-roll.jpg",
            Ingredients = new() { "Spicy Tuna", "Cream Cheese", "Jalapeño", "Spicy Mayo", "Nori" },
            Allergens = new() { "Fish", "Dairy" }
        },
        new SushiRoll
        {
            Id = 5,
            Name = "Garden of Eden Veggie Roll",
            Description = "🌱 Fresh vegetables, avocado, cucumber, perfect for plant-based lovers",
            Price = 7,
            ImageUrl = "/images/garden-eden-roll.jpg",
            Ingredients = new() { "Avocado", "Cucumber", "Carrot", "Lettuce", "Tofu", "Sesame Seeds" },
            Allergens = new() { "Sesame", "Soy" },
            IsVegetarian = true
        }
    };
} 