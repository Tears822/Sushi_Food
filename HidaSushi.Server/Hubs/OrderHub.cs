using Microsoft.AspNetCore.SignalR;
using HidaSushi.Shared.Models;

namespace HidaSushi.Server.Hubs;

public class OrderHub : Hub
{
    private readonly ILogger<OrderHub> _logger;

    public OrderHub(ILogger<OrderHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to OrderHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from OrderHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // Join admin group for order updates
    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        _logger.LogInformation("Admin joined order updates group: {ConnectionId}", Context.ConnectionId);
    }

    // Join customer group for their order updates
    public async Task JoinCustomerGroup(int customerId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Customer_{customerId}");
        _logger.LogInformation("Customer {CustomerId} joined order updates group: {ConnectionId}", customerId, Context.ConnectionId);
    }

    // Join order-specific group
    public async Task JoinOrderGroup(string orderNumber)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Order_{orderNumber}");
        _logger.LogInformation("Client joined order {OrderNumber} group: {ConnectionId}", orderNumber, Context.ConnectionId);
    }

    // Leave order group
    public async Task LeaveOrderGroup(string orderNumber)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Order_{orderNumber}");
        _logger.LogInformation("Client left order {OrderNumber} group: {ConnectionId}", orderNumber, Context.ConnectionId);
    }
} 