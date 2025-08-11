using Microsoft.EntityFrameworkCore;
using HidaSushi.Server.Data;
using HidaSushi.Shared.Models;

namespace HidaSushi.Server.Services;

public class MenuService : IMenuService
{
    private readonly HidaSushiDbContext _context;
    private readonly ICacheService _cache;
    private readonly ILogger<MenuService> _logger;

    public MenuService(HidaSushiDbContext context, ICacheService cache, ILogger<MenuService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<SushiRoll>> GetSushiRollsAsync()
    {
        return await _cache.GetOrSetAsync(
            CacheKeys.MENU_SUSHI_ROLLS,
            async () =>
            {
                var rolls = await _context.SushiRolls
                    .AsNoTracking()
                    .Where(sr => sr.IsAvailable)
                    .OrderByDescending(sr => sr.PopularityScore)
                    .ThenBy(sr => sr.Name)
                    .ToListAsync();
                
                _logger.LogInformation("Loaded {Count} sushi rolls from database", rolls.Count);
                return rolls;
            },
            TimeSpan.FromMinutes(15)
        );
    }

    public async Task<List<SushiRoll>> GetSignatureRollsAsync()
    {
        return await _cache.GetOrSetAsync(
            CacheKeys.MENU_SIGNATURE_ROLLS,
            async () =>
            {
                var rolls = await _context.SushiRolls
                    .AsNoTracking()
                    .Where(sr => sr.IsSignatureRoll && sr.IsAvailable)
                    .OrderByDescending(sr => sr.PopularityScore)
                    .ThenBy(sr => sr.Name)
                    .ToListAsync();
                
                _logger.LogInformation("Loaded {Count} signature rolls from database", rolls.Count);
                return rolls;
            },
            TimeSpan.FromMinutes(30) // Cache signature rolls longer
        );
    }

    public async Task<List<SushiRoll>> GetVegetarianRollsAsync()
    {
        return await _cache.GetOrSetAsync(
            "menu:vegetarian_rolls",
            async () =>
            {
                var rolls = await _context.SushiRolls
                    .AsNoTracking()
                    .Where(sr => sr.IsVegetarian && sr.IsAvailable)
                    .OrderByDescending(sr => sr.PopularityScore)
                    .ThenBy(sr => sr.Name)
                    .ToListAsync();
                
                _logger.LogInformation("Loaded {Count} vegetarian rolls from database", rolls.Count);
                return rolls;
            },
            TimeSpan.FromMinutes(20)
        );
    }

    public async Task<List<Ingredient>> GetIngredientsAsync()
    {
        return await _cache.GetOrSetAsync(
            CacheKeys.MENU_INGREDIENTS,
            async () =>
            {
                var ingredients = await _context.Ingredients
                    .AsNoTracking()
                    .Where(i => i.IsAvailable && i.StockQuantity > 0)
                    .OrderBy(i => i.Category)
                    .ThenBy(i => i.Name)
                    .ToListAsync();
                
                _logger.LogInformation("Loaded {Count} ingredients from database", ingredients.Count);
                return ingredients;
            },
            TimeSpan.FromMinutes(10)
        );
    }

    public async Task<List<Ingredient>> GetIngredientsByCategoryAsync(string category)
    {
        var cacheKey = CacheKeys.FormatKey(CacheKeys.MENU_INGREDIENTS_BY_CATEGORY, category);
        
        return await _cache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                if (!Enum.TryParse<IngredientCategory>(category, true, out var parsedCategory))
                {
                    _logger.LogWarning("Invalid ingredient category: {Category}", category);
                    return new List<Ingredient>();
                }

                var ingredients = await _context.Ingredients
                    .AsNoTracking()
                    .Where(i => i.Category == parsedCategory && i.IsAvailable && i.StockQuantity > 0)
                    .OrderBy(i => i.Name)
                    .ToListAsync();
                
                _logger.LogInformation("Loaded {Count} ingredients for category {Category}", ingredients.Count, category);
                return ingredients;
            },
            TimeSpan.FromMinutes(15)
        );
    }

    public async Task<SushiRoll?> GetSushiRollByIdAsync(int id)
    {
        // Don't cache individual items as much, they change more frequently
        var roll = await _context.SushiRolls
            .AsNoTracking()
            .FirstOrDefaultAsync(sr => sr.Id == id);
            
        if (roll != null)
        {
            _logger.LogDebug("Retrieved sushi roll: {Name} (ID: {Id})", roll.Name, id);
        }
        
        return roll;
    }

    public async Task<Ingredient?> GetIngredientByIdAsync(int id)
    {
        var ingredient = await _context.Ingredients
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
            
        if (ingredient != null)
        {
            _logger.LogDebug("Retrieved ingredient: {Name} (ID: {Id})", ingredient.Name, id);
        }
        
        return ingredient;
    }

    public async Task<SushiRoll> CreateSushiRollAsync(SushiRoll sushiRoll)
    {
        _context.SushiRolls.Add(sushiRoll);
        await _context.SaveChangesAsync();
        
        // Invalidate relevant caches
        await InvalidateMenuCaches();
        
        _logger.LogInformation("Created new sushi roll: {Name} (ID: {Id})", sushiRoll.Name, sushiRoll.Id);
        return sushiRoll;
    }

    public async Task<SushiRoll> UpdateSushiRollAsync(SushiRoll sushiRoll)
    {
        _context.SushiRolls.Update(sushiRoll);
        await _context.SaveChangesAsync();
        
        // Invalidate relevant caches
        await InvalidateMenuCaches();
        
        _logger.LogInformation("Updated sushi roll: {Name} (ID: {Id})", sushiRoll.Name, sushiRoll.Id);
        return sushiRoll;
    }

    public async Task<bool> DeleteSushiRollAsync(int id)
    {
        var sushiRoll = await _context.SushiRolls.FindAsync(id);
        if (sushiRoll == null)
        {
            _logger.LogWarning("Attempted to delete non-existent sushi roll with ID: {Id}", id);
            return false;
        }

        _context.SushiRolls.Remove(sushiRoll);
        await _context.SaveChangesAsync();
        
        // Invalidate relevant caches
        await InvalidateMenuCaches();
        
        _logger.LogInformation("Deleted sushi roll: {Name} (ID: {Id})", sushiRoll.Name, id);
        return true;
    }

    public async Task<Ingredient> CreateIngredientAsync(Ingredient ingredient)
    {
        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();
        
        // Invalidate relevant caches
        await InvalidateIngredientCaches();
        
        _logger.LogInformation("Created new ingredient: {Name} (ID: {Id})", ingredient.Name, ingredient.Id);
        return ingredient;
    }

    public async Task<Ingredient> UpdateIngredientAsync(Ingredient ingredient)
    {
        _context.Ingredients.Update(ingredient);
        await _context.SaveChangesAsync();
        
        // Invalidate relevant caches
        await InvalidateIngredientCaches();
        
        _logger.LogInformation("Updated ingredient: {Name} (ID: {Id})", ingredient.Name, ingredient.Id);
        return ingredient;
    }

    public async Task<bool> DeleteIngredientAsync(int id)
    {
        var ingredient = await _context.Ingredients.FindAsync(id);
        if (ingredient == null)
        {
            _logger.LogWarning("Attempted to delete non-existent ingredient with ID: {Id}", id);
            return false;
        }

        _context.Ingredients.Remove(ingredient);
        await _context.SaveChangesAsync();
        
        // Invalidate relevant caches
        await InvalidateIngredientCaches();
        
        _logger.LogInformation("Deleted ingredient: {Name} (ID: {Id})", ingredient.Name, id);
        return true;
    }

    public async Task<bool> ToggleSushiRollAvailabilityAsync(int id)
    {
        var sushiRoll = await _context.SushiRolls.FindAsync(id);
        if (sushiRoll == null)
        {
            _logger.LogWarning("Attempted to toggle availability of non-existent sushi roll with ID: {Id}", id);
            return false;
        }

        sushiRoll.IsAvailable = !sushiRoll.IsAvailable;
        await _context.SaveChangesAsync();
        
        // Invalidate relevant caches
        await InvalidateMenuCaches();
        
        _logger.LogInformation("Toggled availability for sushi roll: {Name} (ID: {Id}) to {IsAvailable}", 
            sushiRoll.Name, id, sushiRoll.IsAvailable);
        return true;
    }

    public async Task<bool> ToggleIngredientAvailabilityAsync(int id)
    {
        var ingredient = await _context.Ingredients.FindAsync(id);
        if (ingredient == null)
        {
            _logger.LogWarning("Attempted to toggle availability of non-existent ingredient with ID: {Id}", id);
            return false;
        }

        ingredient.IsAvailable = !ingredient.IsAvailable;
        await _context.SaveChangesAsync();
        
        // Invalidate relevant caches
        await InvalidateIngredientCaches();
        
        _logger.LogInformation("Toggled availability for ingredient: {Name} (ID: {Id}) to {IsAvailable}", 
            ingredient.Name, id, ingredient.IsAvailable);
        return true;
    }

    private async Task InvalidateMenuCaches()
    {
        await Task.WhenAll(
            _cache.RemoveAsync(CacheKeys.MENU_SUSHI_ROLLS),
            _cache.RemoveAsync(CacheKeys.MENU_SIGNATURE_ROLLS),
            _cache.RemoveAsync("menu:vegetarian_rolls")
        );
        
        _logger.LogDebug("Invalidated sushi roll caches");
    }

    private async Task InvalidateIngredientCaches()
    {
        await Task.WhenAll(
            _cache.RemoveAsync(CacheKeys.MENU_INGREDIENTS),
            _cache.RemoveByPatternAsync("menu:ingredients:category:*")
        );
        
        _logger.LogDebug("Invalidated ingredient caches");
    }
} 