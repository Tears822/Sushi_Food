using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HidaSushi.Shared.Models;
using Microsoft.Extensions.Configuration;

namespace HidaSushi.Client.Services;

public interface IApiService
{
    Task<Order?> TrackOrderAsync(string orderNumber);
    Task<Order?> CreateOrderAsync(Order order);
    Task<Order?> GetOrderByIdAsync(int orderId);
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
                
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error tracking order {OrderNumber}", orderNumber);
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking order {OrderNumber}", orderNumber);
            return null;
        }
    }

    public async Task<Order?> CreateOrderAsync(Order order)
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
                
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error creating order");
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return null; // Changed from throw to return null to be consistent
        }
    }

    public async Task<Order?> GetOrderByIdAsync(int orderId)
    {
        try
        {
            _logger.LogInformation("Fetching order by ID: {OrderId}", orderId);
            
            var response = await _httpClient.GetAsync($"/api/Orders/{orderId}");
            
            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<Order>();
                _logger.LogInformation("Successfully fetched order {OrderId}", orderId);
                return order;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Order with ID {OrderId} not found", orderId);
                return null;
            }
            else
            {
                _logger.LogError("Failed to fetch order {OrderId}. Status: {StatusCode}", orderId, response.StatusCode);
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error fetching order {OrderId}", orderId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order {OrderId}", orderId);
            return null;
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
            _logger.LogInformation("Fetching all sushi rolls from backend");
            
            var response = await _httpClient.GetAsync("/api/SushiRolls");
            
            if (response.IsSuccessStatusCode)
            {
                var rolls = await response.Content.ReadFromJsonAsync<List<SushiRoll>>();
                if (rolls != null)
                {
                    _logger.LogInformation("Successfully fetched {Count} sushi rolls from backend", rolls.Count);
                    return rolls;
                }
                else
                {
                    _logger.LogWarning("Backend returned null sushi rolls list");
                    return new List<SushiRoll>();
                }
            }
            else
            {
                _logger.LogError("Failed to fetch sushi rolls. Status: {StatusCode}", response.StatusCode);
                return new List<SushiRoll>();
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error fetching sushi rolls - backend may be unavailable");
            return new List<SushiRoll>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sushi rolls");
            return new List<SushiRoll>();
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
                if (rolls != null)
                {
                    _logger.LogInformation("Successfully fetched {Count} signature rolls from backend", rolls.Count);
                    return rolls;
                }
                else
                {
                    _logger.LogWarning("Backend returned null signature rolls list");
                    return new List<SushiRoll>();
                }
            }
            else
            {
                _logger.LogError("Failed to fetch signature rolls. Status: {StatusCode}", response.StatusCode);
                return new List<SushiRoll>();
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error fetching signature rolls - backend may be unavailable");
            return new List<SushiRoll>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching signature rolls");
            return new List<SushiRoll>();
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
                if (rolls != null)
                {
                    _logger.LogInformation("Successfully fetched {Count} vegetarian rolls from backend", rolls.Count);
                    return rolls;
                }
                else
                {
                    _logger.LogWarning("Backend returned null vegetarian rolls list");
                    return new List<SushiRoll>();
                }
            }
            else
            {
                _logger.LogError("Failed to fetch vegetarian rolls. Status: {StatusCode}", response.StatusCode);
                return new List<SushiRoll>();
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error fetching vegetarian rolls - backend may be unavailable");
            return new List<SushiRoll>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching vegetarian rolls");
            return new List<SushiRoll>();
        }
    }

    public async Task<List<Ingredient>> GetIngredientsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching ingredients from backend");
            
            var response = await _httpClient.GetAsync("/api/Ingredients");
            
            if (response.IsSuccessStatusCode)
            {
                var ingredients = await response.Content.ReadFromJsonAsync<List<Ingredient>>();
                if (ingredients != null && ingredients.Any())
                {
                    _logger.LogInformation("Successfully fetched {Count} ingredients from backend", ingredients.Count);
                    return ingredients;
                }
                else
                {
                    _logger.LogWarning("Backend returned empty ingredients list");
                    return new List<Ingredient>();
                }
            }
            else
            {
                _logger.LogError("Failed to fetch ingredients. Status: {StatusCode}", response.StatusCode);
                return new List<Ingredient>();
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error fetching ingredients - backend may be unavailable");
            return new List<Ingredient>();
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
            
            // Create customer registration request that matches server model
            var customerRegistrationRequest = new CustomerRegistrationRequest
            {
                FullName = request.FullName,
                Email = request.Email,
                Password = request.Password,
                Phone = request.Phone
            };
            
            var json = JsonSerializer.Serialize(customerRegistrationRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/Customer/register", content);
            
            if (response.IsSuccessStatusCode)
            {
                var registrationResult = await response.Content.ReadFromJsonAsync<CustomerRegistrationResult>();
                
                if (registrationResult?.Success == true)
                {
                    _logger.LogInformation("Customer registration successful: {Email}", request.Email);
                    return new LoginResponse 
                    { 
                        Success = true, 
                        Message = "Registration successful! Please log in with your credentials.",
                        Token = "" // Registration doesn't provide token, user needs to login
                    };
                }
                else
                {
                    return new LoginResponse 
                    { 
                        Success = false, 
                        Message = registrationResult?.ErrorMessage ?? "Registration failed"
                    };
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Registration failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                
                return new LoginResponse 
                { 
                    Success = false, 
                    Message = response.StatusCode == System.Net.HttpStatusCode.Conflict 
                        ? "Email already registered" 
                        : "Registration failed"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return new LoginResponse { Success = false, Message = "Connection error. Please try again." };
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
            
            var response = await _httpClient.PostAsync("/api/Payment/stripe/create-checkout-session", content);
            
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
}

// Additional models for registration
public class RegisterRequest
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Phone { get; set; } = "";
} 