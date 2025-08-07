using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HidaSushi.Server.Data;
using HidaSushi.Shared.Models;

namespace HidaSushi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly HidaSushiDbContext _context;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(HidaSushiDbContext context, ILogger<OrdersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Orders
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    {
        try
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.SushiRoll)
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.CustomRoll)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching orders");
            return StatusCode(500, "Internal server error");
        }
    }

    // GET: api/Orders/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.SushiRoll)
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.CustomRoll)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order with id {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    // GET: api/Orders/track/{orderNumber}
    [HttpGet("track/{orderNumber}")]
    public async Task<ActionResult<Order>> TrackOrder(string orderNumber)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.SushiRoll)
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.CustomRoll)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

            if (order == null)
            {
                return NotFound("Order not found");
            }

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking order {OrderNumber}", orderNumber);
            return StatusCode(500, "Internal server error");
        }
    }

    // GET: api/Orders/pending
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<Order>>> GetPendingOrders()
    {
        try
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.SushiRoll)
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.CustomRoll)
                .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending orders");
            return StatusCode(500, "Internal server error");
        }
    }

    // POST: api/Orders
    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder(Order order)
    {
        try
        {
            // Generate unique order number
            order.OrderNumber = GenerateOrderNumber();
            order.CreatedAt = DateTime.UtcNow;
            order.Status = OrderStatus.Received;

            // Calculate total amount
            order.TotalAmount = order.Items.Sum(item => item.Price * item.Quantity);

            // Set estimated delivery time
            order.EstimatedDeliveryTime = order.Type == OrderType.Delivery 
                ? DateTime.UtcNow.AddMinutes(45) 
                : DateTime.UtcNow.AddMinutes(20);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetOrder", new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, "Internal server error");
        }
    }

    // PUT: api/Orders/5/status
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatus status)
    {
        try
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            
            // Update estimated times based on status
            switch (status)
            {
                case OrderStatus.InPreparation:
                    order.EstimatedDeliveryTime = order.Type == OrderType.Delivery 
                        ? DateTime.UtcNow.AddMinutes(30) 
                        : DateTime.UtcNow.AddMinutes(15);
                    break;
                case OrderStatus.Ready:
                    order.EstimatedDeliveryTime = order.Type == OrderType.Delivery 
                        ? DateTime.UtcNow.AddMinutes(15) 
                        : DateTime.UtcNow;
                    break;
                case OrderStatus.OutForDelivery:
                    order.EstimatedDeliveryTime = DateTime.UtcNow.AddMinutes(15);
                    break;
                case OrderStatus.Completed:
                    order.EstimatedDeliveryTime = DateTime.UtcNow;
                    break;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for order {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    // GET: api/Orders/stats/daily
    [HttpGet("stats/daily")]
    public async Task<ActionResult<object>> GetDailyStats()
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var orders = await _context.Orders
                .Where(o => o.CreatedAt.Date == today)
                .ToListAsync();

            var stats = new
            {
                TotalOrders = orders.Count,
                TotalRevenue = orders.Sum(o => o.TotalAmount),
                CompletedOrders = orders.Count(o => o.Status == OrderStatus.Completed),
                PendingOrders = orders.Count(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled),
                DeliveryOrders = orders.Count(o => o.Type == OrderType.Delivery),
                PickupOrders = orders.Count(o => o.Type == OrderType.Pickup)
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching daily stats");
            return StatusCode(500, "Internal server error");
        }
    }

    // DELETE: api/Orders/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        try
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // Only allow cancellation if order is not in preparation or beyond
            if (order.Status == OrderStatus.InPreparation || 
                order.Status == OrderStatus.Ready || 
                order.Status == OrderStatus.OutForDelivery ||
                order.Status == OrderStatus.Completed)
            {
                return BadRequest("Cannot cancel order that is already in preparation or completed");
            }

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    private string GenerateOrderNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"HS{timestamp}{random}";
    }
} 