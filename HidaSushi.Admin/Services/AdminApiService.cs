using System.Text;
using System.Text.Json;
using HidaSushi.Shared.Models;
using Microsoft.AspNetCore.Authentication;

namespace HidaSushi.Admin.Services;

public class AdminApiService
{
    private readonly HttpClient _httpClient;
    private readonly CustomAuthenticationStateProvider _authStateProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminApiService(IHttpClientFactory httpClientFactory, CustomAuthenticationStateProvider authStateProvider, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClientFactory.CreateClient("AdminApi");
        _authStateProvider = authStateProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    private async Task EnsureTokenRestoredAsync()
    {
        // Always check and restore token for each API call - remove the _tokenRestored flag
        try
        {
            string? token = null;
            
            // First try to get token from authentication state provider
            token = await _authStateProvider.GetTokenAsync();
            
            // If that fails, try to get it directly from HttpContext
            if (string.IsNullOrEmpty(token))
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                    var tokenClaim = httpContext.User.FindFirst("token");
                    if (tokenClaim != null)
                    {
                        token = tokenClaim.Value;
                        Console.WriteLine($"Token retrieved directly from HttpContext: {token.Substring(0, Math.Min(10, token.Length))}...");
                    }
                    else
                    {
                        Console.WriteLine("No token claim found in HttpContext.User");
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(token))
                {
                SetAuthToken(token);
                Console.WriteLine("Token restored successfully in AdminApiService");
        }
            else
            {
                Console.WriteLine("No token found in authentication state or HttpContext");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error restoring token: {ex.Message}");
        }
    }

    public void SetAuthToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            Console.WriteLine("Authorization header cleared");
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            Console.WriteLine($"Authorization header set with token: {token.Substring(0, Math.Min(10, token.Length))}...");
    }
    }

    // Dashboard methods
    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        await EnsureTokenRestoredAsync(); // Ensure token is restored before making API calls
        try
        {
            // Optional placeholder; server has daily analytics endpoint
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync($"api/analytics/daily/{date}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            // Map minimal fields if shape differs
            return JsonSerializer.Deserialize<DashboardStats>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new DashboardStats();
        }
        catch (Exception ex)
    {
            Console.WriteLine($"Error getting dashboard stats: {ex.Message}");
            return new DashboardStats();
    }
    }

    // Menu management methods
    public async Task<List<SushiRoll>> GetSushiRollsAsync()
    {
        await EnsureTokenRestoredAsync(); // Ensure token is restored before making API calls
        try
        {
            var response = await _httpClient.GetAsync("api/sushirolls");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<SushiRoll>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<SushiRoll>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting sushi rolls: {ex.Message}");
            return new List<SushiRoll>();
        }
    }

    public async Task<bool> ToggleSushiRollAvailabilityAsync(int id)
    {
        await EnsureTokenRestoredAsync(); // Ensure token is restored before making API calls
        try
        {
            var response = await _httpClient.PatchAsync($"api/sushirolls/{id}/availability", new StringContent("{}", Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error toggling roll availability: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteSushiRollAsync(int id)
    {
        await EnsureTokenRestoredAsync(); // Ensure token is restored before making API calls
        try
        {
            var response = await _httpClient.DeleteAsync($"api/sushirolls/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting roll: {ex.Message}");
            return false;
        }
    }

    public async Task<SushiRoll?> SaveSushiRollAsync(SushiRoll roll)
    {
        await EnsureTokenRestoredAsync(); // Ensure token is restored before making API calls
        try
        {
            HttpResponseMessage response;
            var payload = new StringContent(JsonSerializer.Serialize(roll), Encoding.UTF8, "application/json");
            if (roll.Id <= 0)
            {
                // POST for new items - should return the created item
                response = await _httpClient.PostAsync("api/sushirolls", payload);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<SushiRoll>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            else
            {
                // PUT for updates - returns 204 No Content, so just return the original roll if successful
                response = await _httpClient.PutAsync($"api/sushirolls/{roll.Id}", payload);
                response.EnsureSuccessStatusCode();
                
                // PUT returns 204 No Content, so return the updated roll object
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    Console.WriteLine("Roll updated successfully (204 No Content)");
                    return roll; // Return the roll that was sent, as the update was successful
                }
                
                // Fallback: try to parse response if it has content
                var json = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(json))
                {
                    return JsonSerializer.Deserialize<SushiRoll>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                
                return roll; // Return the updated roll
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving roll: {ex.Message}");
            return null;
        }
    }

    // Order methods
    public async Task<List<Order>> GetOrdersAsync()
    {
        await EnsureTokenRestoredAsync(); // Ensure token is restored before making API calls
        try
        {
            var response = await _httpClient.GetAsync("api/orders");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Order>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Order>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting orders: {ex.Message}");
            return new List<Order>();
        }
    }

    public async Task<List<Ingredient>> GetIngredientsAsync()
    {
        await EnsureTokenRestoredAsync(); // Ensure token is restored before making API calls
        try
        {
            // For admin management, we need ALL ingredients, not just available ones
            // Backend /api/ingredients only returns available, so we need to create admin endpoint
            var response = await _httpClient.GetAsync("api/ingredients/all");
            if (!response.IsSuccessStatusCode)
            {
                // Fallback to regular endpoint if /all doesn't exist
                response = await _httpClient.GetAsync("api/ingredients");
            }
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Ingredient>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Ingredient>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting ingredients: {ex.Message}");
            return new List<Ingredient>();
    }
    }

    // Order Management Methods
    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        await EnsureTokenRestoredAsync(); // Ensure token is restored before making API calls
        try
        {
            var response = await _httpClient.GetAsync($"api/orders/{id}");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Order>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting order: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status)
    {
        await EnsureTokenRestoredAsync(); // Ensure token is restored before making API calls
        try
        {
            var payload = new StringContent(JsonSerializer.Serialize(new { Status = status }), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/orders/{orderId}/status", payload);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating order status: {ex.Message}");
            return false;
        }
    }

    public async Task<List<Order>> GetPendingOrdersAsync()
    {
        await EnsureTokenRestoredAsync(); // Ensure token is restored before making API calls
        try
        {
            var response = await _httpClient.GetAsync("api/orders/pending");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Order>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Order>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting pending orders: {ex.Message}");
            return new List<Order>();
            }
        }

    // Analytics Methods
    public async Task<DailyAnalytics> GetDailyAnalyticsAsync(DateTime date)
            {
        await EnsureTokenRestoredAsync(); // Ensure token is restored before making API calls
        try
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync($"api/analytics/daily/{dateStr}");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DailyAnalytics>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new DailyAnalytics();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting daily analytics: {ex.Message}");
            return new DailyAnalytics();
        }
    }

    public async Task<bool> ToggleIngredientAvailabilityAsync(int id)
    {
        await EnsureTokenRestoredAsync(); // Ensure token is restored before making API calls
        try
        {
            var response = await _httpClient.PatchAsync($"api/ingredients/{id}/availability", new StringContent("{}", Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error toggling ingredient availability: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteIngredientAsync(int id)
    {
        await EnsureTokenRestoredAsync(); // Ensure token is restored before making API calls
        try
        {
            var response = await _httpClient.DeleteAsync($"api/ingredients/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting ingredient: {ex.Message}");
            return false;
        }
    }

    public async Task<Ingredient?> SaveIngredientAsync(Ingredient ingredient)
    {
        await EnsureTokenRestoredAsync(); // Ensure token is restored before making API calls
        try
        {
            HttpResponseMessage response;
            var payload = new StringContent(JsonSerializer.Serialize(ingredient), Encoding.UTF8, "application/json");
            if (ingredient.Id <= 0)
            {
                // POST for new items - should return the created item
                response = await _httpClient.PostAsync("api/ingredients", payload);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Ingredient>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
            else
            {
                // PUT for updates - returns 204 No Content, so just return the original ingredient if successful
                response = await _httpClient.PutAsync($"api/ingredients/{ingredient.Id}", payload);
                response.EnsureSuccessStatusCode();
                
                // PUT returns 204 No Content, so return the updated ingredient object
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    Console.WriteLine("Ingredient updated successfully (204 No Content)");
                    return ingredient; // Return the ingredient that was sent, as the update was successful
                }
                
                // Fallback: try to parse response if it has content
                var json = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(json))
    {
                    return JsonSerializer.Deserialize<Ingredient>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                
                return ingredient; // Return the updated ingredient
            }
            }
        catch (Exception ex)
    {
            Console.WriteLine($"Error saving ingredient: {ex.Message}");
            return null;
        }
    }
}

// Data models
public class DashboardStats
{
    public int TotalOrders { get; set; }
    public decimal TodayRevenue { get; set; }
    public int TotalMenuItems { get; set; }
    public int PendingOrders { get; set; }
} 