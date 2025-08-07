using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HidaSushi.Shared.Models;
using HidaSushi.Server.Data;

namespace HidaSushi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Admin-only endpoints
public class MenuController : ControllerBase
{
    private readonly HidaSushiDbContext _context;
    private readonly ILogger<MenuController> _logger;

    public MenuController(HidaSushiDbContext context, ILogger<MenuController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Menu/signature-rolls
    [HttpGet("signature-rolls")]
    public async Task<ActionResult<IEnumerable<SushiRoll>>> GetSignatureRolls()
    {
        try
        {
            var rolls = await _context.SushiRolls.ToListAsync();
            return Ok(rolls);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching signature rolls");
            return StatusCode(500, new { message = "Error fetching signature rolls" });
        }
    }

    // POST: api/Menu/signature-rolls
    [HttpPost("signature-rolls")]
    public async Task<ActionResult<SushiRoll>> CreateSignatureRoll([FromBody] SushiRoll roll)
    {
        try
        {
            if (roll == null)
            {
                return BadRequest(new { message = "Roll data is required" });
            }

            // Validate required fields
            if (string.IsNullOrEmpty(roll.Name))
            {
                return BadRequest(new { message = "Roll name is required" });
            }

            if (roll.Price <= 0)
            {
                return BadRequest(new { message = "Valid price is required" });
            }

            roll.CreatedAt = DateTime.UtcNow;
            _context.SushiRolls.Add(roll);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new signature roll: {RollName} - €{Price}", roll.Name, roll.Price);
            return CreatedAtAction(nameof(GetSignatureRoll), new { id = roll.Id }, roll);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating signature roll");
            return StatusCode(500, new { message = "Error creating signature roll" });
        }
    }

    // GET: api/Menu/signature-rolls/{id}
    [HttpGet("signature-rolls/{id}")]
    public async Task<ActionResult<SushiRoll>> GetSignatureRoll(int id)
    {
        try
        {
            var roll = await _context.SushiRolls.FindAsync(id);
            if (roll == null)
            {
                return NotFound(new { message = "Signature roll not found" });
            }
            return Ok(roll);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching signature roll {RollId}", id);
            return StatusCode(500, new { message = "Error fetching signature roll" });
        }
    }

    // PUT: api/Menu/signature-rolls/{id}
    [HttpPut("signature-rolls/{id}")]
    public async Task<ActionResult<SushiRoll>> UpdateSignatureRoll(int id, [FromBody] SushiRoll roll)
    {
        try
        {
            if (id != roll.Id)
            {
                return BadRequest(new { message = "Roll ID mismatch" });
            }

            var existingRoll = await _context.SushiRolls.FindAsync(id);
            if (existingRoll == null)
            {
                return NotFound(new { message = "Signature roll not found" });
            }

            // Update properties
            existingRoll.Name = roll.Name;
            existingRoll.Description = roll.Description;
            existingRoll.Price = roll.Price;
            existingRoll.IsVegetarian = roll.IsVegetarian;
            existingRoll.Ingredients = roll.Ingredients;
            existingRoll.Allergens = roll.Allergens;
            existingRoll.IsAvailable = roll.IsAvailable;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated signature roll: {RollName} - €{Price}", roll.Name, roll.Price);
            return Ok(existingRoll);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating signature roll {RollId}", id);
            return StatusCode(500, new { message = "Error updating signature roll" });
        }
    }

    // DELETE: api/Menu/signature-rolls/{id}
    [HttpDelete("signature-rolls/{id}")]
    public async Task<ActionResult> DeleteSignatureRoll(int id)
    {
        try
        {
            var roll = await _context.SushiRolls.FindAsync(id);
            if (roll == null)
            {
                return NotFound(new { message = "Signature roll not found" });
            }

            _context.SushiRolls.Remove(roll);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted signature roll: {RollName}", roll.Name);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting signature roll {RollId}", id);
            return StatusCode(500, new { message = "Error deleting signature roll" });
        }
    }

    // PUT: api/Menu/signature-rolls/{id}/availability
    [HttpPut("signature-rolls/{id}/availability")]
    public async Task<ActionResult> UpdateRollAvailability(int id, [FromBody] bool isAvailable)
    {
        try
        {
            var roll = await _context.SushiRolls.FindAsync(id);
            if (roll == null)
            {
                return NotFound(new { message = "Signature roll not found" });
            }

            roll.IsAvailable = isAvailable;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated availability for roll {RollName}: {IsAvailable}", roll.Name, isAvailable);
            return Ok(new { message = $"Roll availability updated to {isAvailable}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating roll availability {RollId}", id);
            return StatusCode(500, new { message = "Error updating roll availability" });
        }
    }

    // GET: api/Menu/ingredients
    [HttpGet("ingredients")]
    public async Task<ActionResult<IEnumerable<Ingredient>>> GetIngredients()
    {
        try
        {
            var ingredients = await _context.Ingredients.ToListAsync();
            if (!ingredients.Any())
            {
                // Return default ingredients if none in database
                return Ok(DefaultIngredients.All);
            }
            return Ok(ingredients);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ingredients");
            return StatusCode(500, new { message = "Error fetching ingredients" });
        }
    }

    // PUT: api/Menu/ingredients/{id}/availability
    [HttpPut("ingredients/{id}/availability")]
    public async Task<ActionResult> UpdateIngredientAvailability(int id, [FromBody] bool isAvailable)
    {
        try
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                return NotFound(new { message = "Ingredient not found" });
            }

            ingredient.IsAvailable = isAvailable;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated availability for ingredient {IngredientName}: {IsAvailable}", ingredient.Name, isAvailable);
            return Ok(new { message = $"Ingredient availability updated to {isAvailable}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ingredient availability {IngredientId}", id);
            return StatusCode(500, new { message = "Error updating ingredient availability" });
        }
    }

    // PUT: api/Menu/ingredients/{id}/price
    [HttpPut("ingredients/{id}/price")]
    public async Task<ActionResult> UpdateIngredientPrice(int id, [FromBody] decimal newPrice)
    {
        try
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                return NotFound(new { message = "Ingredient not found" });
            }

            if (newPrice < 0)
            {
                return BadRequest(new { message = "Price cannot be negative" });
            }

            ingredient.AdditionalPrice = newPrice;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated price for ingredient {IngredientName}: €{NewPrice}", ingredient.Name, newPrice);
            return Ok(new { message = $"Ingredient price updated to €{newPrice:F2}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ingredient price {IngredientId}", id);
            return StatusCode(500, new { message = "Error updating ingredient price" });
        }
    }

    // GET: api/Menu/analytics
    [HttpGet("analytics")]
    public async Task<ActionResult> GetMenuAnalytics()
    {
        try
        {
            var totalRolls = await _context.SushiRolls.CountAsync();
            var availableRolls = await _context.SushiRolls.CountAsync(r => r.IsAvailable);
            var vegetarianRolls = await _context.SushiRolls.CountAsync(r => r.IsVegetarian);
            var averagePrice = await _context.SushiRolls.AverageAsync(r => (double)r.Price);

            // Get most popular rolls from orders (if order data exists)
            var popularRolls = await _context.Orders
                .Where(o => o.Status == OrderStatus.Completed)
                .SelectMany(o => o.Items)
                .Where(i => i.SushiRoll != null)
                .GroupBy(i => i.SushiRoll!.Name)
                .Select(g => new { RollName = g.Key, OrderCount = g.Count() })
                .OrderByDescending(x => x.OrderCount)
                .Take(5)
                .ToListAsync();

            var analytics = new
            {
                TotalRolls = totalRolls,
                AvailableRolls = availableRolls,
                VegetarianRolls = vegetarianRolls,
                AveragePrice = Math.Round(averagePrice, 2),
                PopularRolls = popularRolls
            };

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching menu analytics");
            return StatusCode(500, new { message = "Error fetching menu analytics" });
        }
    }
} 