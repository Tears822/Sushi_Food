using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HidaSushi.Server.Data;
using HidaSushi.Shared.Models;

namespace HidaSushi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SushiRollsController : ControllerBase
{
    private readonly HidaSushiDbContext _context;

    public SushiRollsController(HidaSushiDbContext context)
    {
        _context = context;
    }

    // GET: api/SushiRolls
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SushiRoll>>> GetSushiRolls()
    {
        var rolls = await _context.SushiRolls.AsNoTracking().ToListAsync();
        return Ok(rolls);
    }

    // GET: api/SushiRolls/signature
    [HttpGet("signature")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SushiRoll>>> GetSignatureRolls()
    {
        var rolls = await _context.SushiRolls
            .AsNoTracking()
                .Where(r => r.IsSignatureRoll && r.IsAvailable)
                .ToListAsync();
        return Ok(rolls);
    }

    // GET: api/SushiRolls/vegetarian
    [HttpGet("vegetarian")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SushiRoll>>> GetVegetarianRolls()
    {
        var rolls = await _context.SushiRolls
            .AsNoTracking()
            .Where(r => r.IsVegetarian && r.IsAvailable)
                .ToListAsync();
        return Ok(rolls);
    }

    // GET: api/SushiRolls/5
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<SushiRoll>> GetSushiRoll(int id)
        {
        var sushiRoll = await _context.SushiRolls.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        if (sushiRoll == null)
        {
            return NotFound();
        }
        return Ok(sushiRoll);
    }

    // POST: api/SushiRolls
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<SushiRoll>> CreateSushiRoll([FromBody] SushiRoll sushiRoll)
    {
        _context.SushiRolls.Add(sushiRoll);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSushiRoll), new { id = sushiRoll.Id }, sushiRoll);
    }

    // PUT: api/SushiRolls/5
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateSushiRoll(int id, [FromBody] SushiRoll sushiRoll)
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
            var exists = await _context.SushiRolls.AnyAsync(e => e.Id == id);
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

    // DELETE: api/SushiRolls/5
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteSushiRoll(int id)
        {
            var sushiRoll = await _context.SushiRolls.FindAsync(id);
            if (sushiRoll == null)
            {
                return NotFound();
            }

        _context.SushiRolls.Remove(sushiRoll);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    // PATCH: api/SushiRolls/5/availability
    [HttpPatch("{id}/availability")]
    [Authorize]
    public async Task<IActionResult> ToggleAvailability(int id)
    {
        var sushiRoll = await _context.SushiRolls.FindAsync(id);
        if (sushiRoll == null)
        {
            return NotFound();
        }

        sushiRoll.IsAvailable = !sushiRoll.IsAvailable;
        await _context.SaveChangesAsync();

        return Ok(new { IsAvailable = sushiRoll.IsAvailable });
    }
} 