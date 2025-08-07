using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HidaSushi.Server.Data;
using HidaSushi.Shared.Models;

namespace HidaSushi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngredientsController : ControllerBase
{
    private readonly HidaSushiDbContext _context;
    private readonly ILogger<IngredientsController> _logger;

    public IngredientsController(HidaSushiDbContext context, ILogger<IngredientsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Ingredients
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Ingredient>>> GetIngredients()
    {
        try
        {
            return await _context.Ingredients
                .Where(i => i.IsAvailable)
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ingredients");
            return StatusCode(500, "Internal server error");
        }
    }

    // GET: api/Ingredients/category/Base
    [HttpGet("category/{category}")]
    public async Task<ActionResult<IEnumerable<Ingredient>>> GetIngredientsByCategory(IngredientCategory category)
    {
        try
        {
            return await _context.Ingredients
                .Where(i => i.Category == category && i.IsAvailable)
                .OrderBy(i => i.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ingredients by category {Category}", category);
            return StatusCode(500, "Internal server error");
        }
    }

    // GET: api/Ingredients/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Ingredient>> GetIngredient(int id)
    {
        try
        {
            var ingredient = await _context.Ingredients.FindAsync(id);

            if (ingredient == null)
            {
                return NotFound();
            }

            return ingredient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ingredient with id {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    // POST: api/Ingredients/calculate-price
    [HttpPost("calculate-price")]
    public async Task<ActionResult<decimal>> CalculateCustomRollPrice([FromBody] List<int> ingredientIds)
    {
        try
        {
            var ingredients = await _context.Ingredients
                .Where(i => ingredientIds.Contains(i.Id))
                .ToListAsync();

            // Base price for custom roll
            decimal basePrice = 15m; // Prime number base price

            // Add additional costs for premium ingredients
            decimal additionalCost = ingredients.Sum(i => i.AdditionalPrice);

            var totalPrice = basePrice + additionalCost;

            return Ok(totalPrice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating custom roll price");
            return StatusCode(500, "Internal server error");
        }
    }

    // POST: api/Ingredients/validate-combination
    [HttpPost("validate-combination")]
    public async Task<ActionResult<object>> ValidateCombination([FromBody] List<int> ingredientIds)
    {
        try
        {
            var ingredients = await _context.Ingredients
                .Where(i => ingredientIds.Contains(i.Id))
                .ToListAsync();

            var validation = new
            {
                IsValid = true,
                Warnings = new List<string>(),
                Allergens = ingredients.SelectMany(i => i.Allergens).Distinct().ToList(),
                TotalIngredients = ingredients.Count,
                Categories = ingredients.GroupBy(i => i.Category)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count())
            };

            // Add warnings for unusual combinations
            if (ingredients.Count(i => i.Category == IngredientCategory.Protein) > 3)
            {
                validation.Warnings.Add("This roll has many proteins. Consider reducing for better balance.");
            }

            if (!ingredients.Any(i => i.Category == IngredientCategory.Base))
            {
                validation.Warnings.Add("Consider adding a base (rice) for a traditional roll.");
            }

            return Ok(validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating ingredient combination");
            return StatusCode(500, "Internal server error");
        }
    }

    // PUT: api/Ingredients/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutIngredient(int id, Ingredient ingredient)
    {
        if (id != ingredient.Id)
        {
            return BadRequest();
        }

        _context.Entry(ingredient).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!IngredientExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ingredient with id {Id}", id);
            return StatusCode(500, "Internal server error");
        }

        return NoContent();
    }

    // POST: api/Ingredients
    [HttpPost]
    public async Task<ActionResult<Ingredient>> PostIngredient(Ingredient ingredient)
    {
        try
        {
            _context.Ingredients.Add(ingredient);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetIngredient", new { id = ingredient.Id }, ingredient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ingredient");
            return StatusCode(500, "Internal server error");
        }
    }

    // PUT: api/Ingredients/5/availability
    [HttpPut("{id}/availability")]
    public async Task<IActionResult> ToggleAvailability(int id)
    {
        try
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                return NotFound();
            }

            ingredient.IsAvailable = !ingredient.IsAvailable;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling availability for ingredient {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    private bool IngredientExists(int id)
    {
        return _context.Ingredients.Any(e => e.Id == id);
    }
} 