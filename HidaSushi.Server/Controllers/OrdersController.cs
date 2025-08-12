using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HidaSushi.Server.Services;
using HidaSushi.Shared.Models;
using System.Text.Json;

namespace HidaSushi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    // GET: api/Orders
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    {
        var orders = await _orderService.GetOrdersAsync();
        return Ok(orders);
    }

    // GET: api/Orders/pending
    [HttpGet("pending")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Order>>> GetPendingOrders()
    {
        var orders = await _orderService.GetOrdersAsync(status: OrderStatus.Received);
        return Ok(orders);
    }

    // GET: api/Orders/{id}
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }
        return Ok(order);
    }

    // GET: api/Orders/track/{orderNumber}
    [HttpGet("track/{orderNumber}")]
    [AllowAnonymous]
    public async Task<ActionResult<Order>> TrackOrder(string orderNumber)
    {
        var order = await _orderService.GetOrderByNumberAsync(orderNumber);
            if (order == null)
            {
            return NotFound();
            }
        return Ok(order);
    }

    // POST: api/Orders
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] Order order)
    {
        var created = await _orderService.CreateOrderAsync(order);
        return CreatedAtAction(nameof(GetOrder), new { id = created.Id }, created);
    }

    // PUT: api/Orders/{id}/status
    [HttpPut("{id}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] JsonElement update)
    {
        OrderStatus statusEnum;
        
        // Handle both formats: { Status: enum } and { newStatus: string }
        if (update.TryGetProperty("newStatus", out var newStatusProp))
        {
            var statusString = newStatusProp.GetString();
            if (!Enum.TryParse<OrderStatus>(statusString, out statusEnum))
            {
                return BadRequest("Invalid status value");
            }
        }
        else if (update.TryGetProperty("Status", out var statusProp))
        {
            if (statusProp.ValueKind == JsonValueKind.String)
            {
                var statusString = statusProp.GetString();
                if (!Enum.TryParse<OrderStatus>(statusString, out statusEnum))
                {
                    return BadRequest("Invalid status value");
                }
            }
            else if (statusProp.ValueKind == JsonValueKind.Number)
            {
                var statusValue = statusProp.GetInt32();
                if (!Enum.IsDefined(typeof(OrderStatus), statusValue))
                {
                    return BadRequest("Invalid status value");
                }
                statusEnum = (OrderStatus)statusValue;
            }
            else
            {
                return BadRequest("Invalid status value format");
            }
        }
        else
        {
            return BadRequest("Missing status field");
        }

        var success = await _orderService.UpdateOrderStatusAsync(id, statusEnum);
        if (!success)
        {
            return NotFound();
        }
        
        var order = await _orderService.GetOrderByIdAsync(id);
        return Ok(order);
    }
}

public class OrderStatusUpdate
{
    public OrderStatus Status { get; set; }
} 