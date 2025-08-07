using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HidaSushi.Shared.Models;
using Microsoft.Extensions.Configuration;

namespace HidaSushi.Client.Services;

public interface IApiService
{
    Task<Order?> TrackOrderAsync(string orderNumber);
    Task<Order> CreateOrderAsync(Order order);
    Task<List<SushiRoll>> GetMenuAsync();
    Task<List<SushiRoll>> GetSignatureRollsAsync();
    Task<List<SushiRoll>> GetVegetarianRollsAsync();
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task<bool> ValidateTokenAsync(string token);
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;
    private readonly IConfiguration _configuration;

    public ApiService(HttpClient httpClient, ILogger<ApiService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<Order?> TrackOrderAsync(string orderNumber)
    {
        try
        {
            _logger.LogInformation("Tracking order: {OrderNumber}", orderNumber);
            
            var response = await _httpClient.GetAsync($"/api/Orders/track/{orderNumber}");
            
            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<Order>();
                return order;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Order not found: {OrderNumber}", orderNumber);
                return null;
            }
            else
            {
                _logger.LogError("Failed to track order {OrderNumber}. Status: {StatusCode}", orderNumber, response.StatusCode);
                
                // Fallback to mock data for development
                return await GetMockOrder(orderNumber);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error tracking order {OrderNumber}", orderNumber);
            
            // Fallback to mock data when backend is not available
            return await GetMockOrder(orderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking order {OrderNumber}", orderNumber);
            return null;
        }
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        try
        {
            _logger.LogInformation("Creating order for customer: {CustomerName}", order.CustomerName);
            
            var json = JsonSerializer.Serialize(order);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/Orders", content);
            
            if (response.IsSuccessStatusCode)
            {
                var createdOrder = await response.Content.ReadFromJsonAsync<Order>();
                return createdOrder ?? order;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create order. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
                
                // Fallback to mock order creation
                return await GetMockOrderCreation(order);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error creating order");
            
            // Fallback to mock data when backend is not available
            return await GetMockOrderCreation(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            throw;
        }
    }

    public async Task<List<SushiRoll>> GetMenuAsync()
    {
        try
        {
            _logger.LogInformation("Fetching menu items from backend");
            
            var response = await _httpClient.GetAsync("/api/SushiRolls");
            
            if (response.IsSuccessStatusCode)
            {
                var rolls = await response.Content.ReadFromJsonAsync<List<SushiRoll>>();
                return rolls ?? new List<SushiRoll>();
            }
            else
            {
                _logger.LogError("Failed to fetch menu. Status: {StatusCode}", response.StatusCode);
                
                // Fallback to default rolls
                return SignatureRolls.DefaultRolls.ToList();
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error fetching menu");
            
            // Fallback to default rolls when backend is not available
            return SignatureRolls.DefaultRolls.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching menu");
            return SignatureRolls.DefaultRolls.ToList();
        }
    }

    public async Task<List<SushiRoll>> GetSignatureRollsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching signature rolls from backend");
            
            var response = await _httpClient.GetAsync("/api/SushiRolls/signature");
            
            if (response.IsSuccessStatusCode)
            {
                var rolls = await response.Content.ReadFromJsonAsync<List<SushiRoll>>();
                return rolls ?? new List<SushiRoll>();
            }
            else
            {
                // Fallback to filtered default rolls
                return SignatureRolls.DefaultRolls.Where(r => !r.IsVegetarian).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching signature rolls");
            return SignatureRolls.DefaultRolls.Where(r => !r.IsVegetarian).ToList();
        }
    }

    public async Task<List<SushiRoll>> GetVegetarianRollsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching vegetarian rolls from backend");
            
            var response = await _httpClient.GetAsync("/api/SushiRolls/vegetarian");
            
            if (response.IsSuccessStatusCode)
            {
                var rolls = await response.Content.ReadFromJsonAsync<List<SushiRoll>>();
                return rolls ?? new List<SushiRoll>();
            }
            else
            {
                // Fallback to filtered default rolls
                return SignatureRolls.DefaultRolls.Where(r => r.IsVegetarian).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching vegetarian rolls");
            return SignatureRolls.DefaultRolls.Where(r => r.IsVegetarian).ToList();
        }
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Attempting login for: {Username}", request.Username);
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/Auth/login", content);
            
            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                return loginResponse ?? new LoginResponse { Success = false, Message = "Invalid response from server" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new LoginResponse { Success = false, Message = "Invalid credentials" };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return new LoginResponse { Success = false, Message = "Connection error" };
        }
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            _logger.LogInformation("Attempting registration for: {Email}", request.Email);
            
            // For now, simulate registration since we don't have a register endpoint yet
            await Task.Delay(1000);
            
            // TODO: Implement actual registration endpoint in backend
            return new LoginResponse 
            { 
                Success = true, 
                Message = "Registration successful",
                Token = "mock_token"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return new LoginResponse { Success = false, Message = "Registration failed" };
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.GetAsync("/api/Auth/validate");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return false;
        }
    }

    // Fallback methods for development when backend is not available
    private async Task<Order> GetMockOrder(string orderNumber)
    {
        await Task.Delay(500);
        
        return new Order
        {
            Id = 1,
            OrderNumber = orderNumber,
            CustomerName = "Demo Customer",
            CustomerEmail = "demo@example.com",
            CustomerPhone = "+32 470 42 82 90",
            TotalAmount = 45.50m,
            Type = OrderType.Delivery,
            Status = OrderStatus.InPreparation,
            PaymentMethod = PaymentMethod.Stripe,
            PaymentStatus = PaymentStatus.Paid,
            DeliveryAddress = "Brussels, Belgium",
            Notes = "Extra wasabi please",
            EstimatedDeliveryTime = DateTime.Now.AddMinutes(30),
            CreatedAt = DateTime.Now.AddMinutes(-15),
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = 1,
                    SushiRoll = SignatureRolls.DefaultRolls.First(),
                    Quantity = 2,
                    Price = SignatureRolls.DefaultRolls.First().Price * 2
                }
            }
        };
    }

    private async Task<Order> GetMockOrderCreation(Order order)
    {
        await Task.Delay(1000);
        
        order.Id = Random.Shared.Next(1000, 9999);
        order.OrderNumber = $"HS{DateTime.Now:yyyyMMdd}{order.Id}";
        order.Status = OrderStatus.Received;
        order.PaymentStatus = PaymentStatus.Pending;
        order.CreatedAt = DateTime.Now;
        order.EstimatedDeliveryTime = DateTime.Now.AddMinutes(order.Type == OrderType.Pickup ? 20 : 45);
        
        return order;
    }
}

// Additional models for registration
public class RegisterRequest
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Phone { get; set; } = "";
} 