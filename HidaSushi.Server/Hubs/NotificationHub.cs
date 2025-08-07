using Microsoft.AspNetCore.SignalR;

namespace HidaSushi.Server.Hubs;

public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to NotificationHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from NotificationHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // Join admin notifications group
    public async Task JoinAdminNotifications()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "AdminNotifications");
        _logger.LogInformation("Admin joined notifications group: {ConnectionId}", Context.ConnectionId);
    }

    // Join customer notifications group
    public async Task JoinCustomerNotifications(int customerId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"CustomerNotifications_{customerId}");
        _logger.LogInformation("Customer {CustomerId} joined notifications group: {ConnectionId}", customerId, Context.ConnectionId);
    }
} 