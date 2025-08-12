using Microsoft.EntityFrameworkCore;
using HidaSushi.Server.Data;
using HidaSushi.Shared.Models;

namespace HidaSushi.Server.Services;

public class AnalyticsService
{
    private readonly HidaSushiDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(HidaSushiDbContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DailyAnalytics> GetDailyAnalyticsAsync(DateTime date)
    {
        var analytics = await _context.DailyAnalytics
            .FirstOrDefaultAsync(da => da.Date == date.Date);

        if (analytics == null)
        {
            // Calculate analytics for the date
            analytics = await CalculateDailyAnalyticsAsync(date);
        }

        return analytics;
    }

    public async Task<WeeklyReport> GetWeeklyReportAsync(DateTime weekStarting)
    {
        var weekEnding = weekStarting.AddDays(7);
        
        var orders = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.CreatedAt >= weekStarting && o.CreatedAt < weekEnding)
            .ToListAsync();

        var report = new WeeklyReport
        {
            WeekStarting = weekStarting,
            TotalOrders = orders.Count,
            TotalRevenue = orders.Sum(o => o.TotalAmount),
            NewCustomers = await GetNewCustomersCountAsync(weekStarting, weekEnding),
            AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0,
            TopSellingRolls = await GetTopSellingRollsAsync(weekStarting, weekEnding),
            TopIngredients = await GetTopIngredientsAsync(weekStarting, weekEnding),
            OrdersByDay = GetOrdersByDay(orders),
            AveragePreparationTime = CalculateAveragePreparationTime(orders),
            CustomerSatisfactionScore = 4.5m // Placeholder - would need customer feedback system
        };

        return report;
    }

    public async Task<MonthlyReport> GetMonthlyReportAsync(int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var orders = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt < endDate)
            .ToListAsync();

        var previousMonth = startDate.AddMonths(-1);
        var previousMonthOrders = await _context.Orders
            .Where(o => o.CreatedAt >= previousMonth && o.CreatedAt < startDate)
            .ToListAsync();

        var report = new MonthlyReport
        {
            Year = year,
            Month = month,
            TotalOrders = orders.Count,
            TotalRevenue = orders.Sum(o => o.TotalAmount),
            ActiveCustomers = await GetActiveCustomersCountAsync(startDate, endDate),
            CustomerLifetimeValue = await CalculateCustomerLifetimeValueAsync(),
            GrowthPercentage = CalculateGrowthPercentage(orders, previousMonthOrders),
            DailyBreakdown = await GetDailyBreakdownAsync(startDate, endDate),
            MonthlyBestSellers = await GetTopSellingRollsAsync(startDate, endDate)
        };

        return report;
    }

    public async Task<List<PopularItem>> GetPopularRollsAsync(DateTime fromDate, DateTime toDate)
    {
        return await _context.OrderItems
            .Include(oi => oi.SushiRoll)
            .Where(oi => oi.SushiRollId != null && 
                        oi.Order != null &&
                        oi.Order.CreatedAt >= fromDate && 
                        oi.Order.CreatedAt < toDate)
            .GroupBy(oi => oi.SushiRoll)
            .Select(g => new PopularItem
            {
                Name = g.Key!.Name,
                Count = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.Price),
                Percentage = 0 // Will be calculated
            })
            .OrderByDescending(pi => pi.Count)
            .Take(10)
            .ToListAsync();
    }

    public async Task<List<PopularItem>> GetPopularIngredientsAsync(DateTime fromDate, DateTime toDate)
    {
        // This would require a more complex query to track ingredient usage
        // For now, return based on popularity score
        return await _context.Ingredients
            .Where(i => i.PopularityScore > 0)
            .OrderByDescending(i => i.PopularityScore)
            .Take(10)
            .Select(i => new PopularItem
            {
                Name = i.Name,
                Count = i.TimesUsed,
                Revenue = 0, // Would need to calculate from orders
                Percentage = 0
            })
            .ToListAsync();
    }

    private async Task<DailyAnalytics> CalculateDailyAnalyticsAsync(DateTime date)
    {
        var startDate = date.Date;
        var endDate = startDate.AddDays(1);

        var orders = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt < endDate)
            .ToListAsync();

        var analytics = new DailyAnalytics
        {
            Date = date.Date,
            TotalOrders = orders.Count,
            TotalRevenue = orders.Sum(o => o.TotalAmount),
            DeliveryOrders = orders.Count(o => o.Type == OrderType.Delivery),
            PickupOrders = orders.Count(o => o.Type == OrderType.Pickup),
            CompletedOrders = orders.Count(o => o.Status == OrderStatus.Completed),
            CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled),
            AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0,
            AveragePrepTime = CalculateAveragePreparationTime(orders),
            PopularRolls = await GetPopularRollsAsync(startDate, endDate),
            PopularIngredients = await GetPopularIngredientsAsync(startDate, endDate),
            HourlyOrderCounts = GetHourlyOrderCounts(orders),
            CustomerRetentionRate = await CalculateCustomerRetentionRateAsync(startDate, endDate),
            NewCustomers = await GetNewCustomersCountAsync(startDate, endDate),
            ReturningCustomers = await GetReturningCustomersCountAsync(startDate, endDate),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Save to database
        _context.DailyAnalytics.Add(analytics);
        await _context.SaveChangesAsync();

        return analytics;
    }

    private async Task<List<PopularItem>> GetTopSellingRollsAsync(DateTime fromDate, DateTime toDate)
    {
        return await _context.OrderItems
            .Include(oi => oi.SushiRoll)
            .Where(oi => oi.SushiRollId != null && 
                        oi.Order != null &&
                        oi.Order.CreatedAt >= fromDate && 
                        oi.Order.CreatedAt < toDate)
            .GroupBy(oi => oi.SushiRoll)
            .Select(g => new PopularItem
            {
                Name = g.Key!.Name,
                Count = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.Price),
                Percentage = 0
            })
            .OrderByDescending(pi => pi.Count)
            .Take(5)
            .ToListAsync();
    }

    private async Task<List<PopularItem>> GetTopIngredientsAsync(DateTime fromDate, DateTime toDate)
    {
        return await _context.Ingredients
            .Where(i => i.PopularityScore > 0)
            .OrderByDescending(i => i.PopularityScore)
            .Take(5)
            .Select(i => new PopularItem
            {
                Name = i.Name,
                Count = i.TimesUsed,
                Revenue = 0,
                Percentage = 0
            })
            .ToListAsync();
    }

    private Dictionary<DayOfWeek, int> GetOrdersByDay(List<Order> orders)
    {
        return orders
            .GroupBy(o => o.CreatedAt.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private TimeSpan CalculateAveragePreparationTime(List<Order> orders)
    {
        var completedOrders = orders.Where(o => 
            o.PreparationStartedAt.HasValue && 
            o.PreparationCompletedAt.HasValue).ToList();

        if (!completedOrders.Any())
            return TimeSpan.Zero;

        var totalTime = completedOrders.Sum(o => 
            (o.PreparationCompletedAt!.Value - o.PreparationStartedAt!.Value).Ticks);

        return TimeSpan.FromTicks(totalTime / completedOrders.Count);
    }

    private async Task<int> GetNewCustomersCountAsync(DateTime fromDate, DateTime toDate)
    {
        return await _context.Customers
            .CountAsync(c => c.CreatedAt >= fromDate && c.CreatedAt < toDate);
    }

    private async Task<int> GetReturningCustomersCountAsync(DateTime fromDate, DateTime toDate)
    {
        var customersWithOrders = await _context.Orders
            .Where(o => o.CreatedAt >= fromDate && o.CreatedAt < toDate && o.CustomerId.HasValue)
            .Select(o => o.CustomerId!.Value)
            .Distinct()
            .ToListAsync();

        return customersWithOrders.Count;
    }

    private async Task<int> GetActiveCustomersCountAsync(DateTime fromDate, DateTime toDate)
    {
        return await _context.Customers
            .CountAsync(c => c.LastLoginAt >= fromDate && c.LastLoginAt < toDate);
    }

    private async Task<decimal> CalculateCustomerLifetimeValueAsync()
    {
        var customers = await _context.Customers
            .Where(c => c.TotalOrders > 0)
            .ToListAsync();

        if (!customers.Any())
            return 0;

        return customers.Average(c => c.TotalSpent);
    }

    private decimal CalculateGrowthPercentage(List<Order> currentOrders, List<Order> previousOrders)
    {
        var currentRevenue = currentOrders.Sum(o => o.TotalAmount);
        var previousRevenue = previousOrders.Sum(o => o.TotalAmount);

        if (previousRevenue == 0)
            return currentRevenue > 0 ? 100 : 0;

        return ((currentRevenue - previousRevenue) / previousRevenue) * 100;
    }

    private async Task<decimal> CalculateCustomerRetentionRateAsync(DateTime fromDate, DateTime toDate)
    {
        // This is a simplified calculation
        // In a real system, you'd track customer return rates more precisely
        var totalCustomers = await _context.Customers.CountAsync();
        var returningCustomers = await GetReturningCustomersCountAsync(fromDate, toDate);

        if (totalCustomers == 0)
            return 0;

        return (decimal)returningCustomers / totalCustomers * 100;
    }

    private async Task<List<DailyAnalytics>> GetDailyBreakdownAsync(DateTime fromDate, DateTime toDate)
    {
        return await _context.DailyAnalytics
            .Where(da => da.Date >= fromDate && da.Date < toDate)
            .OrderBy(da => da.Date)
            .ToListAsync();
    }

    private Dictionary<int, int> GetHourlyOrderCounts(List<Order> orders)
    {
        return orders
            .GroupBy(o => o.CreatedAt.Hour)
            .ToDictionary(g => g.Key, g => g.Count());
    }
} 