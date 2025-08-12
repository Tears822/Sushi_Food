using HidaSushi.Shared.Models;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace HidaSushi.Admin.Services;

public interface IAdminApiService
{
    Task<DashboardStats> GetDashboardStatsAsync();
    Task<List<Order>> GetOrdersAsync();
    Task<List<Ingredient>> GetIngredientsAsync();
    Task<List<SushiRoll>> GetMenuItemsAsync();
    Task<bool> SaveMenuItemAsync(SushiRoll roll);
    Task<bool> DeleteMenuItemAsync(int id);
    Task<bool> SaveIngredientAsync(Ingredient ingredient);
    Task<bool> DeleteIngredientAsync(int id);
    Task<DailyAnalytics?> GetDailyAnalyticsAsync(DateTime date);
    Task<Order?> GetOrderByIdAsync(int id);
    Task<bool> ToggleSushiRollAvailabilityAsync(int id);
    Task<bool> ToggleIngredientAvailabilityAsync(int id);
    Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus);
    
    // File upload methods
    Task<FileUploadResult?> UploadMenuImageAsync(Stream fileStream, string fileName, string menuItemName, int? menuItemId = null);
    Task<FileUploadResult?> UploadIngredientImageAsync(Stream fileStream, string fileName, string ingredientName, int? ingredientId = null);
    Task<bool> DeleteImageAsync(string imageUrl);
    
    // Legacy methods for compatibility
    Task<List<SushiRoll>> GetSushiRollsAsync();
    Task<bool> SaveSushiRollAsync(SushiRoll roll);
    Task<bool> DeleteSushiRollAsync(int id);
    
    void SetAuthToken(string token);
}

public class AdminApiService : IAdminApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminApiService> _logger;
    private string? _authToken;

    public AdminApiService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<AdminApiService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("AdminApi");
        _configuration = configuration;
        _logger = logger;
    }

    public void SetAuthToken(string token)
    {
        _authToken = token;
        if (!string.IsNullOrEmpty(_authToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
        }
    }

    private async Task<T?> MakeApiCallAsync<T>(string endpoint, HttpMethod? method = null, object? data = null)
    {
        try
        {
            var request = new HttpRequestMessage(method ?? HttpMethod.Get, endpoint);
            
            if (data != null && method != HttpMethod.Get)
            {
                if (data is MultipartFormDataContent multipartContent)
                {
                    request.Content = multipartContent;
                    _logger.LogInformation("Making {Method} request to {Endpoint} with multipart form data", method, endpoint);
                }
                else
                {
                    var json = JsonSerializer.Serialize(data);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    _logger.LogInformation("Making {Method} request to {Endpoint} with data: {Data}", method, endpoint, json);
                }
            }
            else
            {
                _logger.LogInformation("Making {Method} request to {Endpoint}", method ?? HttpMethod.Get, endpoint);
            }

            var response = await _httpClient.SendAsync(request);
            
            _logger.LogInformation("API Response: {StatusCode} for {Endpoint}", response.StatusCode, endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("API Response Content: {Content}", content);
                
                if (string.IsNullOrEmpty(content))
                {
                    return default(T);
                }
                
                return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("API call failed: {Endpoint} - Status: {StatusCode}, Content: {ErrorContent}", endpoint, response.StatusCode, errorContent);
                return default;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making API call to {Endpoint}", endpoint);
            return default;
        }
    }

    // Add a separate method for operations that return success/failure
    private async Task<bool> MakeApiCallForSuccessAsync(string endpoint, HttpMethod? method = null, object? data = null)
    {
        try
        {
            var request = new HttpRequestMessage(method ?? HttpMethod.Get, endpoint);
            
            if (data != null && method != HttpMethod.Get)
            {
                var json = JsonSerializer.Serialize(data);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                _logger.LogInformation("Making {Method} request to {Endpoint} with data: {Data}", method, endpoint, json);
            }
            else
            {
                _logger.LogInformation("Making {Method} request to {Endpoint}", method ?? HttpMethod.Get, endpoint);
            }

            var response = await _httpClient.SendAsync(request);
            
            _logger.LogInformation("API Response: {StatusCode} for {Endpoint}", response.StatusCode, endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("API Response Content: {Content}", content);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("API call failed: {Endpoint} - Status: {StatusCode}, Content: {ErrorContent}", endpoint, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making API call to {Endpoint}", endpoint);
            return false;
        }
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        return await MakeApiCallAsync<DashboardStats>("api/analytics/dashboard") ?? new DashboardStats();
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        var orders = await MakeApiCallAsync<List<Order>>("api/orders");
        return orders ?? new List<Order>();
    }

    public async Task<List<Ingredient>> GetIngredientsAsync()
    {
        var ingredients = await MakeApiCallAsync<List<Ingredient>>("api/ingredients");
        return ingredients ?? new List<Ingredient>();
    }

    public async Task<List<SushiRoll>> GetMenuItemsAsync()
    {
        var menuItems = await MakeApiCallAsync<List<SushiRoll>>("api/sushis");
        return menuItems ?? new List<SushiRoll>();
    }

    public async Task<bool> SaveMenuItemAsync(SushiRoll roll)
    {
        try
        {
            var endpoint = roll.Id > 0 ? $"api/sushis/{roll.Id}" : "api/sushis";
            var method = roll.Id > 0 ? HttpMethod.Put : HttpMethod.Post;
            
            _logger.LogInformation("Saving menu item: {RollName} using {Method} to {Endpoint}", roll.Name, method, endpoint);
            
            var result = await MakeApiCallForSuccessAsync(endpoint, method, roll);
            
            _logger.LogInformation("Save menu item result: {Success}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving menu item");
            return false;
        }
    }

    public async Task<bool> DeleteMenuItemAsync(int id)
    {
        try
        {
            _logger.LogInformation("Deleting menu item with ID: {Id}", id);
            
            var result = await MakeApiCallForSuccessAsync($"api/sushis/{id}", HttpMethod.Delete);
            
            _logger.LogInformation("Delete menu item result: {Success}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting menu item");
            return false;
        }
    }

    public async Task<bool> SaveIngredientAsync(Ingredient ingredient)
    {
        try
        {
            var endpoint = ingredient.Id > 0 ? $"api/ingredients/{ingredient.Id}" : "api/ingredients";
            var method = ingredient.Id > 0 ? HttpMethod.Put : HttpMethod.Post;
            
            var result = await MakeApiCallForSuccessAsync(endpoint, method, ingredient);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving ingredient");
            return false;
        }
    }

    public async Task<bool> DeleteIngredientAsync(int id)
    {
        try
        {
            var result = await MakeApiCallForSuccessAsync($"api/ingredients/{id}", HttpMethod.Delete);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ingredient");
            return false;
        }
    }

    public async Task<DailyAnalytics?> GetDailyAnalyticsAsync(DateTime date)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        return await MakeApiCallAsync<DailyAnalytics>($"api/analytics/daily/{dateStr}");
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await MakeApiCallAsync<Order>($"api/orders/{id}");
    }

    public async Task<bool> ToggleSushiRollAvailabilityAsync(int id)
    {
        try
        {
            _logger.LogInformation("Toggling sushi roll availability for ID: {Id}", id);
            
            var result = await MakeApiCallForSuccessAsync($"api/sushis/{id}/toggle-availability", HttpMethod.Put);
            
            _logger.LogInformation("Toggle availability result: {Success}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling sushi roll availability");
            return false;
        }
    }

    public async Task<bool> ToggleIngredientAvailabilityAsync(int id)
    {
        try
        {
            var result = await MakeApiCallForSuccessAsync($"api/ingredients/{id}/toggle-availability", HttpMethod.Put);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling ingredient availability");
            return false;
        }
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
    {
        try
        {
            var result = await MakeApiCallForSuccessAsync($"api/orders/{orderId}/status", HttpMethod.Put, new { newStatus });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status");
            return false;
        }
    }

    // File upload methods
    public async Task<FileUploadResult?> UploadMenuImageAsync(Stream fileStream, string fileName, string menuItemName, int? menuItemId = null)
    {
        try
        {
            var endpoint = "api/FileUpload/menu";
            using var formDataContent = new MultipartFormDataContent();
            formDataContent.Add(new StreamContent(fileStream), "File", fileName);
            formDataContent.Add(new StringContent(menuItemName), "MenuItemName");
            
            if (menuItemId.HasValue)
            {
                formDataContent.Add(new StringContent(menuItemId.Value.ToString()), "MenuItemId");
            }

            _logger.LogInformation("Uploading menu image for menu item: {MenuItemName} (ID: {MenuItemId})", menuItemName, menuItemId);
            var result = await MakeApiCallAsync<FileUploadResult>(endpoint, HttpMethod.Post, formDataContent);
            _logger.LogInformation("Upload menu image result: {Success}", result?.Success);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading menu image");
            return null;
        }
    }

    public async Task<FileUploadResult?> UploadIngredientImageAsync(Stream fileStream, string fileName, string ingredientName, int? ingredientId = null)
    {
        try
        {
            var endpoint = "api/FileUpload/ingredient";
            using var formDataContent = new MultipartFormDataContent();
            formDataContent.Add(new StreamContent(fileStream), "File", fileName);
            formDataContent.Add(new StringContent(ingredientName), "IngredientName");
            
            if (ingredientId.HasValue)
            {
                formDataContent.Add(new StringContent(ingredientId.Value.ToString()), "IngredientId");
            }

            _logger.LogInformation("Uploading ingredient image for ingredient: {IngredientName} (ID: {IngredientId})", ingredientName, ingredientId);
            var result = await MakeApiCallAsync<FileUploadResult>(endpoint, HttpMethod.Post, formDataContent);
            _logger.LogInformation("Upload ingredient image result: {Success}", result?.Success);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading ingredient image");
            return null;
        }
    }

    public async Task<bool> DeleteImageAsync(string imageUrl)
    {
        try
        {
            var endpoint = $"api/FileUpload?imageUrl={Uri.EscapeDataString(imageUrl)}";

            _logger.LogInformation("Deleting image: {ImageUrl}", imageUrl);
            var result = await MakeApiCallForSuccessAsync(endpoint, HttpMethod.Delete);
            _logger.LogInformation("Delete image result: {Success}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image");
            return false;
        }
    }

    // Legacy methods for compatibility
    public async Task<List<SushiRoll>> GetSushiRollsAsync()
    {
        return await GetMenuItemsAsync();
    }

    public async Task<bool> SaveSushiRollAsync(SushiRoll roll)
    {
        return await SaveMenuItemAsync(roll);
    }

    public async Task<bool> DeleteSushiRollAsync(int id)
    {
        return await DeleteMenuItemAsync(id);
    }
} 