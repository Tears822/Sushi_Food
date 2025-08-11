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
    Task<List<SushiRoll>> GetSushiRollsAsync();
    Task<List<SushiRoll>> GetSignatureRollsAsync();
    Task<List<SushiRoll>> GetVegetarianRollsAsync();
    Task<List<Ingredient>> GetIngredientsAsync();
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task<bool> ValidateTokenAsync(string token);
    
    // Payment methods
    Task<HidaSushi.Shared.Models.PaymentResult?> ProcessPaymentAsync(PaymentRequest request);
    Task<StripePaymentIntentResult?> CreateStripePaymentIntentAsync(StripePaymentIntentRequest request);
    
    // Customer methods
    Task<CustomerRegistrationResult?> RegisterCustomerAsync(CustomerRegistrationRequest request);
    Task<Customer?> GetCustomerAsync(string email);
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
        return await GetSushiRollsAsync();
    }

    public async Task<List<SushiRoll>> GetSushiRollsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all sushi rolls");
            
            var response = await _httpClient.GetAsync("/api/SushiRolls");
            response.EnsureSuccessStatusCode();
            
                var rolls = await response.Content.ReadFromJsonAsync<List<SushiRoll>>();
                return rolls ?? new List<SushiRoll>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sushi rolls");
            return GetFallbackRolls();
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

    public async Task<List<Ingredient>> GetIngredientsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching ingredients");
            
            var response = await _httpClient.GetAsync("/api/Ingredients");
            response.EnsureSuccessStatusCode();
            
            var ingredients = await response.Content.ReadFromJsonAsync<List<Ingredient>>();
            return ingredients ?? new List<Ingredient>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ingredients");
            return new List<Ingredient>();
        }
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Attempting customer login for: {Username}", request.Username);
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/Customer/login", content);
            
            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                return loginResponse ?? new LoginResponse { Success = false, Message = "Invalid response from server" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Customer login failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return new LoginResponse { Success = false, Message = "Invalid credentials" };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during customer login");
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

    public async Task<HidaSushi.Shared.Models.PaymentResult?> ProcessPaymentAsync(PaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Processing payment for order {OrderId}", request.OrderId);
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/Payment/process", content);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<HidaSushi.Shared.Models.PaymentResult>();
                _logger.LogInformation("Payment processed successfully for order {OrderId}", request.OrderId);
                return result;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Payment failed for order {OrderId}. Status: {StatusCode}, Error: {Error}", 
                    request.OrderId, response.StatusCode, errorContent);
                
                return new HidaSushi.Shared.Models.PaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Payment failed: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for order {OrderId}", request.OrderId);
            return new HidaSushi.Shared.Models.PaymentResult
            {
                Success = false,
                ErrorMessage = "An error occurred while processing payment"
            };
        }
    }

    public async Task<StripePaymentIntentResult?> CreateStripePaymentIntentAsync(StripePaymentIntentRequest request)
    {
        try
        {
            _logger.LogInformation("Creating Stripe payment intent for order {OrderId}", request.OrderId);
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/Payment/create-intent", content);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<StripePaymentIntentResult>();
                _logger.LogInformation("Stripe payment intent created successfully for order {OrderId}", request.OrderId);
                return result;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create Stripe payment intent for order {OrderId}. Status: {StatusCode}, Error: {Error}", 
                    request.OrderId, response.StatusCode, errorContent);
                
                return new StripePaymentIntentResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to create payment intent: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe payment intent for order {OrderId}", request.OrderId);
            return new StripePaymentIntentResult
            {
                Success = false,
                ErrorMessage = "An error occurred while creating payment intent"
            };
        }
    }

    public async Task<CustomerRegistrationResult?> RegisterCustomerAsync(CustomerRegistrationRequest request)
    {
        try
        {
            _logger.LogInformation("Registering customer: {Email}", request.Email);
            
            // Log detailed request for debugging
            _logger.LogInformation("Registration request details - FullName: {FullName}, Email: {Email}, Password: {PasswordPresent}, Phone: {Phone}",
                request.FullName ?? "NULL",
                request.Email ?? "NULL", 
                !string.IsNullOrEmpty(request.Password) ? "YES" : "NO",
                request.Phone ?? "NULL");
            
            var json = JsonSerializer.Serialize(request);
            _logger.LogInformation("Serialized registration request: {Json}", json);
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/Customer/register", content);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CustomerRegistrationResult>();
                _logger.LogInformation("Customer registered successfully: {Email}", request.Email);
                return result;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Customer registration failed for {Email}. Status: {StatusCode}, Error: {Error}", 
                    request.Email, response.StatusCode, errorContent);
                
                return new CustomerRegistrationResult
                {
                    Success = false,
                    ErrorMessage = $"Registration failed: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering customer: {Email}", request.Email);
            return new CustomerRegistrationResult
            {
                Success = false,
                ErrorMessage = "An error occurred during registration"
            };
        }
    }

    public async Task<Customer?> GetCustomerAsync(string email)
    {
        try
        {
            _logger.LogInformation("Fetching customer: {Email}", email);
            
            var response = await _httpClient.GetAsync($"/api/Customer/{Uri.EscapeDataString(email)}");
            
            if (response.IsSuccessStatusCode)
            {
                var customer = await response.Content.ReadFromJsonAsync<Customer>();
                return customer;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Customer not found: {Email}", email);
                return null;
            }
            else
            {
                _logger.LogError("Error fetching customer {Email}. Status: {StatusCode}", email, response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer: {Email}", email);
            return null;
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

    private List<SushiRoll> GetFallbackRolls()
    {
        _logger.LogWarning("Falling back to default rolls due to backend error.");
        return SignatureRolls.DefaultRolls.ToList();
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