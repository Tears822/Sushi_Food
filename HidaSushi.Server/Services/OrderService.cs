using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using HidaSushi.Server.Data;
using HidaSushi.Server.Hubs;
using HidaSushi.Shared.Models;

namespace HidaSushi.Server.Services;

public class OrderService
{
    private readonly HidaSushiDbContext _context;
    private readonly IHubContext<OrderHub> _orderHub;
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        HidaSushiDbContext context,
        IHubContext<OrderHub> orderHub,
        IHubContext<NotificationHub> notificationHub,
        ILogger<OrderService> logger)
    {
        _context = context;
        _orderHub = orderHub;
        _notificationHub = notificationHub;
        _logger = logger;
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        _logger.LogInformation("Creating new order for customer: {CustomerName}", order.CustomerName);
        
        // Generate order number if not provided
        if (string.IsNullOrEmpty(order.OrderNumber))
        {
            order.OrderNumber = GenerateOrderNumber();
        }
        
        order.CreatedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;
        order.Status = OrderStatus.Received;

        // Set estimated delivery time
        order.EstimatedDeliveryTime = DateTime.UtcNow.AddMinutes(
            order.Type == OrderType.Pickup ? 30 : 60);

        // Clear any circular references in OrderItems
        foreach (var item in order.Items)
        {
            item.Order = null; // Clear to avoid circular reference during validation
            item.OrderId = 0; // Will be set by EF Core
        }

        // Calculate totals
        CalculateOrderTotals(order);

        _logger.LogInformation("Adding order to database with order number: {OrderNumber}", order.OrderNumber);
        
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Order saved successfully with ID: {OrderId} and order number: {OrderNumber}", order.Id, order.OrderNumber);

        // Add initial status history
        await AddStatusHistoryAsync(order.Id, null, OrderStatus.Received.ToString(), "Order received");

        // Send real-time updates
        await _orderHub.Clients.Group("Admins").SendAsync("NewOrder", order);
        if (order.CustomerId.HasValue)
        {
            await _orderHub.Clients.Group($"Customer_{order.CustomerId}").SendAsync("OrderCreated", order);
        }

        _logger.LogInformation("New order created: {OrderNumber} for {CustomerName} with ID: {OrderId}", order.OrderNumber, order.CustomerName, order.Id);
        return order;
    }

    public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
    {
        return await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.SushiRoll)
            .Include(o => o.Items)
                .ThenInclude(oi => oi.CustomRoll)
            .Include(o => o.StatusHistory.OrderByDescending(h => h.CreatedAt))
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<List<Order>> GetOrdersAsync(OrderStatus? status = null, DateTime? fromDate = null)
    {
        var query = _context.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.SushiRoll)
            .Include(o => o.Items)
                .ThenInclude(oi => oi.CustomRoll)
            .Include(o => o.Customer)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= fromDate.Value);
        }

        return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, int? adminUserId = null, string notes = "")
    {
        var order = await _context.Orders
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return false;
        }

        var previousStatus = order.Status;
        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        // Update timestamps based on status
        switch (newStatus)
        {
            case OrderStatus.Accepted:
                order.AcceptedAt = DateTime.UtcNow;
                order.AcceptedBy = adminUserId;
                break;
            case OrderStatus.InPreparation:
                order.PreparationStartedAt = DateTime.UtcNow;
                break;
            case OrderStatus.Ready:
                order.PreparationCompletedAt = DateTime.UtcNow;
                break;
            case OrderStatus.Completed:
                order.ActualDeliveryTime = DateTime.UtcNow;
                break;
        }

        // Add status history
        await AddStatusHistoryAsync(orderId, previousStatus.ToString(), newStatus.ToString(), notes, adminUserId);

        await _context.SaveChangesAsync();

        // Send real-time updates
        await _orderHub.Clients.Group("Admins").SendAsync("OrderStatusUpdated", order);
        await _orderHub.Clients.Group($"Order_{order.OrderNumber}").SendAsync("OrderStatusUpdated", order);
        
        if (order.CustomerId.HasValue)
        {
            await _orderHub.Clients.Group($"Customer_{order.CustomerId}").SendAsync("OrderStatusUpdated", order);
        }

        _logger.LogInformation("Order {OrderNumber} status updated from {PreviousStatus} to {NewStatus}", 
            order.OrderNumber, previousStatus, newStatus);

        return true;
    }

    public async Task<List<OrderStatusHistory>> GetOrderStatusHistoryAsync(int orderId)
    {
        return await _context.OrderStatusHistory
            .Include(h => h.UpdatedByUser)
            .Where(h => h.OrderId == orderId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderByIdAsync(int orderId)
    {
        return await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.SushiRoll)
            .Include(o => o.Items)
                .ThenInclude(oi => oi.CustomRoll)
            .Include(o => o.StatusHistory.OrderByDescending(h => h.CreatedAt))
            .Include(o => o.Customer)
            .Include(o => o.AcceptedByUser)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    private string GenerateOrderNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"HS{timestamp}{random}";
    }

    private void CalculateOrderTotals(Order order)
    {
        order.SubtotalAmount = order.Items.Sum(item => item.Price);
        
        // Add delivery fee for delivery orders
        if (order.Type == OrderType.Delivery)
        {
            order.DeliveryFee = 3.50m; // Fixed delivery fee
        }
        else
        {
            order.DeliveryFee = 0;
        }

        // Calculate tax (6% VAT for Belgium)
        order.TaxAmount = (order.SubtotalAmount + order.DeliveryFee) * 0.06m;
        
        order.TotalAmount = order.SubtotalAmount + order.DeliveryFee + order.TaxAmount;
    }

    private async Task AddStatusHistoryAsync(int orderId, string? previousStatus, string newStatus, string notes = "", int? changedBy = null)
    {
        var history = new OrderStatusHistory
        {
            OrderId = orderId,
            PreviousStatus = previousStatus ?? "",
            NewStatus = newStatus,
            UpdatedBy = changedBy,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.OrderStatusHistory.Add(history);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdatePaymentStatusAsync(int orderId, PaymentStatus status, string? transactionId = null)
    {
        try
        {
            _logger.LogInformation("Attempting to update payment status for order ID: {OrderId} to status: {Status}", orderId, status);
            
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for payment status update", orderId);
                
                // Let's also check if there are any orders in the database at all
                var orderCount = await _context.Orders.CountAsync();
                _logger.LogWarning("Total orders in database: {OrderCount}", orderCount);
                
                // Check if there are any recent orders
                var recentOrders = await _context.Orders
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .Select(o => new { o.Id, o.OrderNumber, o.CreatedAt })
                    .ToListAsync();
                    
                _logger.LogWarning("Recent orders: {@RecentOrders}", recentOrders);
                
                return false;
            }

            _logger.LogInformation("Found order {OrderId} with number {OrderNumber}, updating payment status", orderId, order.OrderNumber);
            
            order.PaymentStatus = status;
            if (!string.IsNullOrEmpty(transactionId))
            {
                order.PaymentReference = transactionId;
            }
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} payment status updated to {PaymentStatus}, transaction: {TransactionId}",
                orderId, status, transactionId);

            // Notify admin via SignalR
            await _orderHub.Clients.Group("Admins").SendAsync("PaymentStatusUpdated", order);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment status for order {OrderId}", orderId);
            return false;
        }
    }
} 