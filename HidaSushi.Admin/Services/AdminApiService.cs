using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HidaSushi.Shared.Models;
using Microsoft.Extensions.Configuration;

namespace HidaSushi.Admin.Services;

public class AdminApiService
{
    private readonly HttpClient _httpClient;
    private string? _authToken;

    public AdminApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("AdminApi");
    }

    public void SetAuthToken(string token)
    {
        _authToken = token;
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/Auth/login", content);
            
            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (loginResponse?.Success == true && !string.IsNullOrEmpty(loginResponse.Token))
                {
                    SetAuthToken(loginResponse.Token);
                }
                return loginResponse ?? new LoginResponse { Success = false, Message = "Invalid response" };
        }
            
            return new LoginResponse { Success = false, Message = "Login failed" };
        }
        catch (Exception)
        {
            // For development, return mock response
            return new LoginResponse 
            { 
                Success = true, 
                Token = "mock_admin_token",
                Message = "Mock login successful"
            };
        }
    }

    public async Task<bool> ValidateTokenAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/Auth/validate");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<Order>> GetLiveOrdersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/Orders/live");
            if (response.IsSuccessStatusCode)
            {
                var orders = await response.Content.ReadFromJsonAsync<List<Order>>();
                return orders ?? new List<Order>();
            }
        }
        catch
        {
            // Fallback to mock data
        }
        
        return GetMockOrders().Where(o => o.Status == OrderStatus.Received || o.Status == OrderStatus.InPreparation).ToList();
    }

    public async Task<List<Order>> GetPendingOrdersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/Orders?status=Received");
            if (response.IsSuccessStatusCode)
            {
                var orders = await response.Content.ReadFromJsonAsync<List<Order>>();
                return orders ?? new List<Order>();
            }
        }
        catch
        {
            // Fallback to mock data
        }
        
        return GetMockOrders().Where(o => o.Status == OrderStatus.Received).ToList();
    }

    public async Task<List<Order>> GetAllOrdersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/Orders");
            if (response.IsSuccessStatusCode)
            {
                var orders = await response.Content.ReadFromJsonAsync<List<Order>>();
                return orders ?? new List<Order>();
            }
        }
        catch
        {
            // Fallback to mock data
        }
        
        return GetMockOrders();
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status)
    {
        try
        {
            var response = await _httpClient.PutAsync($"/api/Orders/{orderId}/status", 
                new StringContent(JsonSerializer.Serialize(new { Status = status }), Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AcceptOrderAsync(int orderId)
    {
        return await UpdateOrderStatusAsync(orderId, OrderStatus.Accepted);
    }

    public async Task<bool> MarkOrderReadyAsync(int orderId)
    {
        return await UpdateOrderStatusAsync(orderId, OrderStatus.Ready);
    }

    public async Task<bool> MarkOrderOutForDeliveryAsync(int orderId)
    {
        return await UpdateOrderStatusAsync(orderId, OrderStatus.OutForDelivery);
    }

    public async Task<bool> CompleteOrderAsync(int orderId)
    {
        return await UpdateOrderStatusAsync(orderId, OrderStatus.Completed);
    }

    public async Task<bool> CancelOrderAsync(int orderId)
    {
        return await UpdateOrderStatusAsync(orderId, OrderStatus.Cancelled);
        }

    public async Task<List<SushiRoll>> GetMenuAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/SushiRolls");
            if (response.IsSuccessStatusCode)
            {
                var rolls = await response.Content.ReadFromJsonAsync<List<SushiRoll>>();
                return rolls ?? new List<SushiRoll>();
            }
        }
        catch
        {
            // Fallback to mock data
        }
        
        return GetMockSushiRolls();
    }

    public async Task<bool> CreateSushiRollAsync(SushiRoll roll)
    {
        try
        {
            var json = JsonSerializer.Serialize(roll);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/SushiRolls", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateSushiRollAsync(int rollId, SushiRoll roll)
    {
        try
        {
            var json = JsonSerializer.Serialize(roll);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api/SushiRolls/{rollId}", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteSushiRollAsync(int rollId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/SushiRolls/{rollId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ToggleRollAvailabilityAsync(int rollId, bool isAvailable)
    {
        try
        {
            var json = JsonSerializer.Serialize(new { IsAvailable = isAvailable });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PatchAsync($"/api/SushiRolls/{rollId}/availability", content);
                return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<Ingredient>> GetIngredientsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/Ingredients");
            if (response.IsSuccessStatusCode)
            {
                var ingredients = await response.Content.ReadFromJsonAsync<List<Ingredient>>();
                return ingredients ?? new List<Ingredient>();
            }
        }
        catch
        {
            // Fallback to mock data
        }
        
        return GetMockIngredients();
    }

    public async Task<bool> CreateIngredientAsync(Ingredient ingredient)
    {
        try
        {
            var json = JsonSerializer.Serialize(ingredient);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/Ingredients", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateIngredientAsync(int ingredientId, Ingredient ingredient)
    {
        try
        {
            var json = JsonSerializer.Serialize(ingredient);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api/Ingredients/{ingredientId}", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ToggleIngredientAvailabilityAsync(int ingredientId, bool isAvailable)
    {
        try
        {
            var json = JsonSerializer.Serialize(new { IsAvailable = isAvailable });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PatchAsync($"/api/Ingredients/{ingredientId}/availability", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<DailyAnalytics> GetDailyAnalyticsAsync(DateTime? date = null)
    {
        try
        {
            var dateParam = date?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync($"/api/Analytics/daily?date={dateParam}");
            if (response.IsSuccessStatusCode)
            {
                var analytics = await response.Content.ReadFromJsonAsync<DailyAnalytics>();
                return analytics ?? GetMockDailyAnalytics();
            }
        }
        catch
        {
            // Fallback to mock data
        }
        
        return GetMockDailyAnalytics();
    }

    public async Task<List<PopularItem>> GetPopularIngredientsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/Analytics/popular-ingredients");
            if (response.IsSuccessStatusCode)
            {
                var ingredients = await response.Content.ReadFromJsonAsync<List<PopularItem>>();
                return ingredients ?? new List<PopularItem>();
            }
        }
        catch
        {
            // Fallback to mock data
        }
        
        return GetMockPopularIngredients();
    }

    public async Task<List<PopularItem>> GetPopularRollsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/Analytics/popular-rolls");
            if (response.IsSuccessStatusCode)
            {
                var rolls = await response.Content.ReadFromJsonAsync<List<PopularItem>>();
                return rolls ?? new List<PopularItem>();
            }
        }
        catch
        {
            // Fallback to mock data
        }
        
        return GetMockPopularRolls();
    }

    public async Task<object> GetDailyStatsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/Analytics/stats");
            if (response.IsSuccessStatusCode)
            {
                var stats = await response.Content.ReadFromJsonAsync<object>();
                return stats ?? GetMockStats();
            }
        }
        catch
        {
            // Fallback to mock data
        }
        
        return GetMockStats();
    }

    public async Task<List<Order>> GetOrdersByDateAsync(DateTime date)
    {
        try
        {
            var dateParam = date.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync($"/api/Orders?date={dateParam}");
            if (response.IsSuccessStatusCode)
            {
                var orders = await response.Content.ReadFromJsonAsync<List<Order>>();
                return orders ?? new List<Order>();
            }
        }
        catch
        {
            // Fallback to mock data
        }
        
        return GetMockOrders().Where(o => o.CreatedAt.Date == date.Date).ToList();
    }

    public async Task<List<Order>> GetOrdersByCustomerAsync(string customerName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/Orders?customer={Uri.EscapeDataString(customerName)}");
            if (response.IsSuccessStatusCode)
            {
                var orders = await response.Content.ReadFromJsonAsync<List<Order>>();
                return orders ?? new List<Order>();
            }
        }
        catch
        {
            // Fallback to mock data
        }
        
        return GetMockOrders().Where(o => o.CustomerName.Contains(customerName, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<List<Order>> GetOrdersByTypeAsync(OrderType orderType)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/Orders?type={orderType}");
            if (response.IsSuccessStatusCode)
            {
                var orders = await response.Content.ReadFromJsonAsync<List<Order>>();
                return orders ?? new List<Order>();
            }
        }
        catch
        {
            // Fallback to mock data
        }
        
        return GetMockOrders().Where(o => o.Type == orderType).ToList();
    }

    // Mock data methods
    private List<Order> GetMockOrders()
    {
        return new List<Order>
        {
            new Order
            {
                Id = 1,
                OrderNumber = "HS202412011234567",
                CustomerName = "John Doe",
                CustomerEmail = "john@example.com",
                CustomerPhone = "+32 470 12 34 56",
                TotalAmount = 45.50m,
                Type = OrderType.Delivery,
                Status = OrderStatus.Received,
                PaymentMethod = PaymentMethod.Stripe,
                PaymentStatus = PaymentStatus.Paid,
                DeliveryAddress = "123 Main St, Brussels, Belgium",
                Notes = "Extra wasabi please",
                EstimatedDeliveryTime = DateTime.Now.AddMinutes(45),
                CreatedAt = DateTime.Now.AddMinutes(-15),
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Id = 1,
                        SushiRoll = new SushiRoll { Id = 1, Name = "Taylor Swift Dragon Roll", Price = 13.00m },
                        Quantity = 2,
                        Price = 26.00m
                    },
                    new OrderItem
                    {
                        Id = 2,
                        SushiRoll = new SushiRoll { Id = 2, Name = "Garden of Eden Veggie Roll", Price = 7.00m },
                        Quantity = 1,
                        Price = 7.00m
                    }
                }
            },
            new Order
            {
                Id = 2,
                OrderNumber = "HS202412011234568",
                CustomerName = "Jane Smith",
                CustomerEmail = "jane@example.com",
                CustomerPhone = "+32 470 98 76 54",
                TotalAmount = 32.00m,
                Type = OrderType.Pickup,
                Status = OrderStatus.InPreparation,
                PaymentMethod = PaymentMethod.CashOnDelivery,
                PaymentStatus = PaymentStatus.Pending,
                Notes = "No wasabi",
                EstimatedDeliveryTime = DateTime.Now.AddMinutes(30),
                CreatedAt = DateTime.Now.AddMinutes(-30),
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Id = 3,
                        SushiRoll = new SushiRoll { Id = 3, Name = "Blackbird Rainbow Roll", Price = 17.00m },
                        Quantity = 1,
                        Price = 17.00m
                    },
                    new OrderItem
                    {
                        Id = 4,
                        SushiRoll = new SushiRoll { Id = 4, Name = "M&M Beautiful Roll", Price = 19.00m },
                        Quantity = 1,
                        Price = 19.00m
                    }
                }
            },
            new Order
            {
                Id = 3,
                OrderNumber = "HS202412011234569",
                CustomerName = "Bob Wilson",
                CustomerEmail = "bob@example.com",
                CustomerPhone = "+32 470 11 22 33",
                TotalAmount = 28.50m,
                Type = OrderType.Delivery,
                Status = OrderStatus.Ready,
                PaymentMethod = PaymentMethod.Stripe,
                PaymentStatus = PaymentStatus.Paid,
                DeliveryAddress = "456 Oak Ave, Antwerp, Belgium",
                Notes = "Extra spicy",
                EstimatedDeliveryTime = DateTime.Now.AddMinutes(20),
                CreatedAt = DateTime.Now.AddMinutes(-60),
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Id = 5,
                        SushiRoll = new SushiRoll { Id = 5, Name = "Joker Laughing Volcano Roll", Price = 23.00m },
                        Quantity = 1,
                        Price = 23.00m
                    }
                }
            }
        };
    }

    private List<SushiRoll> GetMockSushiRolls()
    {
        return new List<SushiRoll>
        {
            new SushiRoll { Id = 1, Name = "Taylor Swift – Tortured Poets Dragon Roll", Price = 13.00m, IsAvailable = true },
            new SushiRoll { Id = 2, Name = "Blackbird Rainbow Roll", Price = 17.00m, IsAvailable = true },
            new SushiRoll { Id = 3, Name = "M&M \"Beautiful\" Roll", Price = 19.00m, IsAvailable = true },
            new SushiRoll { Id = 4, Name = "Joker Laughing Volcano Roll", Price = 23.00m, IsAvailable = true },
            new SushiRoll { Id = 5, Name = "Garden of Eden Veggie Roll", Price = 7.00m, IsAvailable = true }
        };
    }

    private List<Ingredient> GetMockIngredients()
    {
        return new List<Ingredient>
        {
            new Ingredient { Id = 1, Name = "Tuna", Category = IngredientCategory.Protein, AdditionalPrice = 3.5m, IsAvailable = true },
            new Ingredient { Id = 2, Name = "Salmon", Category = IngredientCategory.Protein, AdditionalPrice = 3.0m, IsAvailable = true },
            new Ingredient { Id = 3, Name = "Shrimp", Category = IngredientCategory.Protein, AdditionalPrice = 2.5m, IsAvailable = true },
            new Ingredient { Id = 4, Name = "Crab", Category = IngredientCategory.Protein, AdditionalPrice = 4.0m, IsAvailable = false },
            new Ingredient { Id = 5, Name = "Tofu", Category = IngredientCategory.Protein, AdditionalPrice = 1.5m, IsAvailable = true },
            new Ingredient { Id = 6, Name = "Avocado", Category = IngredientCategory.Vegetable, AdditionalPrice = 1.0m, IsAvailable = true },
            new Ingredient { Id = 7, Name = "Cucumber", Category = IngredientCategory.Vegetable, AdditionalPrice = 0.5m, IsAvailable = true },
            new Ingredient { Id = 8, Name = "Carrot", Category = IngredientCategory.Vegetable, AdditionalPrice = 0.5m, IsAvailable = true },
            new Ingredient { Id = 9, Name = "Goat Cheese", Category = IngredientCategory.Extra, AdditionalPrice = 1.0m, IsAvailable = true },
            new Ingredient { Id = 10, Name = "Mango", Category = IngredientCategory.Extra, AdditionalPrice = 1.0m, IsAvailable = true }
        };
    }

    private DailyAnalytics GetMockDailyAnalytics()
    {
        return new DailyAnalytics
        {
            Date = DateTime.Today,
            TotalOrders = 24,
            TotalRevenue = 567.50m,
            AverageOrderValue = 23.65m,
            DeliveryOrders = 18,
            PickupOrders = 6,
            CompletedOrders = 22,
            CancelledOrders = 2,
            AveragePrepTime = TimeSpan.FromMinutes(18),
            PopularRolls = GetMockPopularRolls(),
            PopularIngredients = GetMockPopularIngredients(),
            HourlyOrderCounts = new Dictionary<int, int>
            {
                { 11, 2 }, { 12, 5 }, { 13, 3 }, { 17, 4 }, { 18, 6 }, { 19, 4 }
            }
        };
    }

    private List<PopularItem> GetMockPopularRolls()
    {
        return new List<PopularItem>
        {
            new PopularItem { Name = "Taylor Swift Dragon Roll", Count = 15, Percentage = 35.7 },
            new PopularItem { Name = "Garden of Eden Veggie Roll", Count = 12, Percentage = 28.6 },
            new PopularItem { Name = "Blackbird Rainbow Roll", Count = 8, Percentage = 19.0 },
            new PopularItem { Name = "M&M Beautiful Roll", Count = 7, Percentage = 16.7 }
        };
    }

    private List<PopularItem> GetMockPopularIngredients()
    {
        return new List<PopularItem>
        {
            new PopularItem { Name = "Avocado", Count = 28, Percentage = 31.5 },
            new PopularItem { Name = "Tuna", Count = 22, Percentage = 24.7 },
            new PopularItem { Name = "Salmon", Count = 18, Percentage = 20.2 },
            new PopularItem { Name = "Cucumber", Count = 15, Percentage = 16.9 },
            new PopularItem { Name = "Spicy Mayo", Count = 6, Percentage = 6.7 }
        };
    }

    private object GetMockStats()
    {
        return new
        {
            TotalOrders = 24,
            PendingOrders = 3,
            CompletedOrders = 20,
            CancelledOrders = 1,
            TotalRevenue = 567.50m,
            AverageOrderValue = 23.65m
        };
    }
} 