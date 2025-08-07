using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HidaSushi.Server.Data;
using HidaSushi.Shared.Models;

namespace HidaSushi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SushiRollsController : ControllerBase
{
    private readonly HidaSushiDbContext _context;
    private readonly ILogger<SushiRollsController> _logger;

    public SushiRollsController(HidaSushiDbContext context, ILogger<SushiRollsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/SushiRolls
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SushiRoll>>> GetSushiRolls()
    {
        try
        {
            return await _context.SushiRolls
                .Where(r => r.IsAvailable)
                .OrderBy(r => r.Price)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sushi rolls");
            return StatusCode(500, "Internal server error");
        }
    }

    // GET: api/SushiRolls/5
    [HttpGet("{id}")]
    public async Task<ActionResult<SushiRoll>> GetSushiRoll(int id)
    {
        try
        {
            var sushiRoll = await _context.SushiRolls.FindAsync(id);

            if (sushiRoll == null)
            {
                return NotFound();
            }

            return sushiRoll;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sushi roll with id {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    // GET: api/SushiRolls/signature
    [HttpGet("signature")]
    public async Task<ActionResult<IEnumerable<SushiRoll>>> GetSignatureRolls()
    {
        try
        {
            return await _context.SushiRolls
                .Where(r => r.IsSignatureRoll && r.IsAvailable)
                .OrderBy(r => r.Price)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching signature rolls");
            return StatusCode(500, "Internal server error");
        }
    }

    // GET: api/SushiRolls/vegetarian
    [HttpGet("vegetarian")]
    public async Task<ActionResult<IEnumerable<SushiRoll>>> GetVegetarianRolls()
    {
        try
        {
            return await _context.SushiRolls
                .Where(r => r.IsAvailable && 
                           (r.Name.Contains("Garden") || 
                            !r.Allergens.Any(a => a == "Fish" || a == "Shellfish" || a == "Beef")))
                .OrderBy(r => r.Price)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching vegetarian rolls");
            return StatusCode(500, "Internal server error");
        }
    }

    // PUT: api/SushiRolls/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutSushiRoll(int id, SushiRoll sushiRoll)
    {
        if (id != sushiRoll.Id)
        {
            return BadRequest();
        }

        _context.Entry(sushiRoll).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SushiRollExists(id))
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
            _logger.LogError(ex, "Error updating sushi roll with id {Id}", id);
            return StatusCode(500, "Internal server error");
        }

        return NoContent();
    }

    // POST: api/SushiRolls
    [HttpPost]
    public async Task<ActionResult<SushiRoll>> PostSushiRoll(SushiRoll sushiRoll)
    {
        try
        {
            _context.SushiRolls.Add(sushiRoll);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSushiRoll", new { id = sushiRoll.Id }, sushiRoll);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sushi roll");
            return StatusCode(500, "Internal server error");
        }
    }

    // DELETE: api/SushiRolls/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSushiRoll(int id)
    {
        try
        {
            var sushiRoll = await _context.SushiRolls.FindAsync(id);
            if (sushiRoll == null)
            {
                return NotFound();
            }

            // Soft delete by marking as unavailable
            sushiRoll.IsAvailable = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sushi roll with id {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    private bool SushiRollExists(int id)
    {
        return _context.SushiRolls.Any(e => e.Id == id);
    }
} 