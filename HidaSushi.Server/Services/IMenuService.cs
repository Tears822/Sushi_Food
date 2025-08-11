using HidaSushi.Shared.Models;

namespace HidaSushi.Server.Services;

public interface IMenuService
{
    // Read operations
    Task<List<SushiRoll>> GetSushiRollsAsync();
    Task<List<SushiRoll>> GetSignatureRollsAsync();
    Task<List<SushiRoll>> GetVegetarianRollsAsync();
    Task<List<Ingredient>> GetIngredientsAsync();
    Task<List<Ingredient>> GetIngredientsByCategoryAsync(string category);
    Task<SushiRoll?> GetSushiRollByIdAsync(int id);
    Task<Ingredient?> GetIngredientByIdAsync(int id);
    
    // CRUD operations for SushiRolls
    Task<SushiRoll> CreateSushiRollAsync(SushiRoll sushiRoll);
    Task<SushiRoll> UpdateSushiRollAsync(SushiRoll sushiRoll);
    Task<bool> DeleteSushiRollAsync(int id);
    Task<bool> ToggleSushiRollAvailabilityAsync(int id);
    
    // CRUD operations for Ingredients
    Task<Ingredient> CreateIngredientAsync(Ingredient ingredient);
    Task<Ingredient> UpdateIngredientAsync(Ingredient ingredient);
    Task<bool> DeleteIngredientAsync(int id);
    Task<bool> ToggleIngredientAvailabilityAsync(int id);
} 