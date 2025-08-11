using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HidaSushi.Server.Data;
using HidaSushi.Shared.Models;

namespace HidaSushi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngredientsController : ControllerBase
{
    private readonly HidaSushiDbContext _context;

    public IngredientsController(HidaSushiDbContext context)
    {
        _context = context;
    }

    // GET: api/Ingredients
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Ingredient>>> GetIngredients()
    {
        var list = await _context.Ingredients
            .AsNoTracking()
                .Where(i => i.IsAvailable)
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Name)
                .ToListAsync();
        return Ok(list);
    }

    // GET: api/Ingredients/all - For admin management
    [HttpGet("all")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Ingredient>>> GetAllIngredients()
    {
        var list = await _context.Ingredients
            .AsNoTracking()
            .OrderBy(i => i.Category)
            .ThenBy(i => i.Name)
            .ToListAsync();
        return Ok(list);
    }

    // GET: api/Ingredients/category/{category}
    [HttpGet("category/{category}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Ingredient>>> GetIngredientsByCategory(string category)
    {
        if (!Enum.TryParse<IngredientCategory>(category, true, out var parsed))
        {
            return BadRequest(new { message = "Invalid category" });
        }

        var list = await _context.Ingredients
            .AsNoTracking()
            .Where(i => i.Category == parsed && i.IsAvailable)
                .OrderBy(i => i.Name)
                .ToListAsync();
        return Ok(list);
    }

    // GET: api/Ingredients/5
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Ingredient>> GetIngredient(int id)
    {
        var ingredient = await _context.Ingredients.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
            if (ingredient == null)
            {
                return NotFound();
            }
        return Ok(ingredient);
    }

    // POST: api/Ingredients
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Ingredient>> CreateIngredient([FromBody] Ingredient ingredient)
    {
        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetIngredient), new { id = ingredient.Id }, ingredient);
    }

    // PUT: api/Ingredients/5
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateIngredient(int id, [FromBody] Ingredient ingredient)
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
            var exists = await _context.Ingredients.AnyAsync(e => e.Id == id);
            if (!exists)
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/Ingredients/5
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteIngredient(int id)
    {
        var ingredient = await _context.Ingredients.FindAsync(id);
        if (ingredient == null)
        {
            return NotFound();
        }

        _context.Ingredients.Remove(ingredient);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PATCH: api/Ingredients/5/availability
    [HttpPatch("{id}/availability")]
    [Authorize]
    public async Task<IActionResult> ToggleAvailability(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                return NotFound();
            }

            ingredient.IsAvailable = !ingredient.IsAvailable;
            await _context.SaveChangesAsync();

        return Ok(new { IsAvailable = ingredient.IsAvailable });
        }

    // POST: api/Ingredients/calculate-price
    [HttpPost("calculate-price")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> CalculatePrice([FromBody] List<int> ingredientIds)
    {
        var ingredients = await _context.Ingredients
            .AsNoTracking()
            .Where(i => ingredientIds.Contains(i.Id))
            .ToListAsync();

        var basePrice = 8.00m; // Base price for custom roll
        var additionalPrice = ingredients.Sum(i => i.AdditionalPrice);

        return Ok(new
        {
            BasePrice = basePrice,
            AdditionalPrice = additionalPrice,
            TotalPrice = basePrice + additionalPrice,
            Ingredients = ingredients
        });
    }
} 