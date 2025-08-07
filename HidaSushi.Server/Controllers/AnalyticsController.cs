using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HidaSushi.Shared.Models;
using HidaSushi.Server.Data;

namespace HidaSushi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Admin-only endpoints
public class AnalyticsController : ControllerBase
{
    private readonly HidaSushiDbContext _context;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(HidaSushiDbContext context, ILogger<AnalyticsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Analytics/daily/{date}
    [HttpGet("daily/{date}")]
    public async Task<ActionResult> GetDailyAnalytics(string date)
    {
        try
        {
            DateTime.TryParse(date, out var targetDate);
            if (targetDate == default)
                targetDate = DateTime.Today;

            var orders = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.SushiRoll)
                .Include(o => o.Items)
                .ThenInclude(i => i.CustomRoll)
                .ThenInclude(cr => cr.SelectedIngredients)
                .Where(o => o.CreatedAt.Date == targetDate.Date)
                .ToListAsync();

            var totalOrders = orders.Count;
            var completedOrders = orders.Count(o => o.Status == OrderStatus.Completed);
            var cancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled);
            var deliveryOrders = orders.Count(o => o.Type == OrderType.Delivery);
            var pickupOrders = orders.Count(o => o.Type == OrderType.Pickup);
            var totalRevenue = orders.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.TotalAmount);
            var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            // Calculate average prep time (mock for now)
            var averagePrepTime = TimeSpan.FromMinutes(18);

            // Popular rolls
            var popularRolls = orders
                .Where(o => o.Status == OrderStatus.Completed)
                .SelectMany(o => o.Items)
                .Where(i => i.SushiRoll != null)
                .GroupBy(i => i.SushiRoll!.Name)
                .Select(g => new {
                    Name = g.Key,
                    Count = g.Sum(x => x.Quantity),
                    Percentage = 0.0 // Will calculate below
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            var totalRollSales = popularRolls.Sum(r => r.Count);
            var popularRollsWithPercentage = popularRolls.Select(r => new {
                r.Name,
                r.Count,
                Percentage = totalRollSales > 0 ? (double)r.Count / totalRollSales * 100 : 0
            }).ToList();

            // Popular ingredients from custom rolls
            var popularIngredients = orders
                .Where(o => o.Status == OrderStatus.Completed)
                .SelectMany(o => o.Items)
                .Where(i => i.CustomRoll != null && i.CustomRoll.SelectedIngredients != null)
                .SelectMany(i => i.CustomRoll!.SelectedIngredients)
                .GroupBy(ing => ing.Name)
                .Select(g => new {
                    Name = g.Key,
                    Count = g.Count(),
                    Percentage = 0.0
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            var totalIngredientUsage = popularIngredients.Sum(i => i.Count);
            var popularIngredientsWithPercentage = popularIngredients.Select(i => new {
                i.Name,
                i.Count,
                Percentage = totalIngredientUsage > 0 ? (double)i.Count / totalIngredientUsage * 100 : 0
            }).ToList();

            // Hourly order counts
            var hourlyOrderCounts = orders
                .GroupBy(o => o.CreatedAt.Hour)
                .ToDictionary(g => g.Key, g => g.Count());

            var analytics = new
            {
                Date = targetDate,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                AverageOrderValue = averageOrderValue,
                DeliveryOrders = deliveryOrders,
                PickupOrders = pickupOrders,
                CompletedOrders = completedOrders,
                CancelledOrders = cancelledOrders,
                AveragePrepTime = averagePrepTime,
                PopularRolls = popularRollsWithPercentage,
                PopularIngredients = popularIngredientsWithPercentage,
                HourlyOrderCounts = hourlyOrderCounts
            };

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching daily analytics for date {Date}", date);
            return StatusCode(500, new { message = "Error fetching daily analytics" });
        }
    }

    // GET: api/Analytics/dashboard
    [HttpGet("dashboard")]
    public async Task<ActionResult> GetDashboardAnalytics()
    {
        try
        {
            var today = DateTime.Today;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            // Daily stats
            var todayOrders = await _context.Orders
                .Where(o => o.CreatedAt.Date == today)
                .CountAsync();

            var todayRevenue = await _context.Orders
                .Where(o => o.CreatedAt.Date == today && o.Status == OrderStatus.Completed)
                .SumAsync(o => (double?)o.TotalAmount) ?? 0;

            // Weekly stats
            var weeklyOrders = await _context.Orders
                .Where(o => o.CreatedAt >= thisWeek)
                .CountAsync();

            var weeklyRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= thisWeek && o.Status == OrderStatus.Completed)
                .SumAsync(o => (double?)o.TotalAmount) ?? 0;

            // Monthly stats
            var monthlyOrders = await _context.Orders
                .Where(o => o.CreatedAt >= thisMonth)
                .CountAsync();

            var monthlyRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= thisMonth && o.Status == OrderStatus.Completed)
                .SumAsync(o => (double?)o.TotalAmount) ?? 0;

            var analytics = new
            {
                Today = new
                {
                    Orders = todayOrders,
                    Revenue = Math.Round(todayRevenue, 2),
                    AverageOrderValue = todayOrders > 0 ? Math.Round(todayRevenue / todayOrders, 2) : 0
                },
                Week = new
                {
                    Orders = weeklyOrders,
                    Revenue = Math.Round(weeklyRevenue, 2),
                    AverageOrderValue = weeklyOrders > 0 ? Math.Round(weeklyRevenue / weeklyOrders, 2) : 0
                },
                Month = new
                {
                    Orders = monthlyOrders,
                    Revenue = Math.Round(monthlyRevenue, 2),
                    AverageOrderValue = monthlyOrders > 0 ? Math.Round(monthlyRevenue / monthlyOrders, 2) : 0
                }
            };

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard analytics");
            return StatusCode(500, new { message = "Error fetching dashboard analytics" });
        }
    }

    // GET: api/Analytics/popular-ingredients
    [HttpGet("popular-ingredients")]
    public async Task<ActionResult> GetPopularIngredients([FromQuery] int days = 7)
    {
        try
        {
            var startDate = DateTime.Today.AddDays(-days);

            var popularIngredients = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.Status == OrderStatus.Completed)
                .SelectMany(o => o.Items)
                .Where(i => i.CustomRoll != null)
                .SelectMany(i => i.CustomRoll!.SelectedIngredients)
                .GroupBy(ing => ing.Name)
                .Select(g => new { 
                    Name = g.Key, 
                    Count = g.Count(),
                    Category = g.First().Category.ToString()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            var totalUsage = popularIngredients.Sum(i => i.Count);
            var result = popularIngredients.Select(i => new {
                i.Name,
                i.Count,
                Percentage = totalUsage > 0 ? (double)i.Count / totalUsage * 100 : 0,
                i.Category
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching popular ingredients");
            return StatusCode(500, new { message = "Error fetching popular ingredients" });
        }
    }

    // GET: api/Analytics/popular-rolls
    [HttpGet("popular-rolls")]
    public async Task<ActionResult> GetPopularRolls([FromQuery] int days = 7)
    {
        try
        {
            var startDate = DateTime.Today.AddDays(-days);

            var popularRolls = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.Status == OrderStatus.Completed)
                .SelectMany(o => o.Items)
                .Where(i => i.SushiRoll != null)
                .GroupBy(i => i.SushiRoll!.Name)
                .Select(g => new { 
                    Name = g.Key, 
                    Count = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            var totalSales = popularRolls.Sum(r => r.Count);
            var result = popularRolls.Select(r => new {
                r.Name,
                r.Count,
                Percentage = totalSales > 0 ? (double)r.Count / totalSales * 100 : 0
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching popular rolls");
            return StatusCode(500, new { message = "Error fetching popular rolls" });
        }
    }

    // GET: api/Analytics/sales-trend
    [HttpGet("sales-trend")]
    public async Task<ActionResult> GetSalesTrend([FromQuery] int days = 30)
    {
        try
        {
            var startDate = DateTime.Today.AddDays(-days);

            var salesTrend = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.Status == OrderStatus.Completed)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { 
                    Date = g.Key,
                    Orders = g.Count(),
                    Revenue = g.Sum(o => (double)o.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return Ok(salesTrend);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sales trend");
            return StatusCode(500, new { message = "Error fetching sales trend" });
        }
    }

    // GET: api/Analytics/order-types
    [HttpGet("order-types")]
    public async Task<ActionResult> GetOrderTypeDistribution([FromQuery] int days = 7)
    {
        try
        {
            var startDate = DateTime.Today.AddDays(-days);

            var orderTypes = await _context.Orders
                .Where(o => o.CreatedAt >= startDate)
                .GroupBy(o => o.Type)
                .Select(g => new { 
                    Type = g.Key.ToString(),
                    Count = g.Count(),
                    Revenue = g.Where(o => o.Status == OrderStatus.Completed).Sum(o => (double)o.TotalAmount)
                })
                .ToListAsync();

            return Ok(orderTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order type distribution");
            return StatusCode(500, new { message = "Error fetching order type distribution" });
        }
    }

    // GET: api/Analytics/peak-hours
    [HttpGet("peak-hours")]
    public async Task<ActionResult> GetPeakHours([FromQuery] int days = 7)
    {
        try
        {
            var startDate = DateTime.Today.AddDays(-days);

            var peakHours = await _context.Orders
                .Where(o => o.CreatedAt >= startDate)
                .GroupBy(o => o.CreatedAt.Hour)
                .Select(g => new { 
                    Hour = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Where(o => o.Status == OrderStatus.Completed).Sum(o => (double)o.TotalAmount)
                })
                .OrderBy(x => x.Hour)
                .ToListAsync();

            return Ok(peakHours);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching peak hours");
            return StatusCode(500, new { message = "Error fetching peak hours" });
        }
    }
} 