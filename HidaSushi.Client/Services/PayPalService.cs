using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using System.Text;

namespace HidaSushi.Client.Services;

public interface IPayPalService
{
    Task<PayPalOrderResponse> CreateOrderAsync(decimal amount, string customerEmail, int orderId);
    Task<PayPalCaptureResponse> CapturePaymentAsync(string orderId);
    Task InitializePayPalButtonsAsync(ElementReference buttonContainer, decimal amount, int orderId, DotNetObjectReference<object> componentRef);
}

public class PayPalService : IPayPalService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<PayPalService> _logger;

    public PayPalService(HttpClient httpClient, IJSRuntime jsRuntime, ILogger<PayPalService> logger)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<PayPalOrderResponse> CreateOrderAsync(decimal amount, string customerEmail, int orderId)
    {
        try
        {
            var request = new PayPalOrderRequest
            {
                OrderId = orderId,
                Amount = amount,
                CustomerEmail = customerEmail
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/Payment/paypal/create-order", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PayPalOrderResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (result != null)
                {
                    _logger.LogInformation("PayPal order created: {OrderId}", result.Id);
                    return result;
                }
            }

            throw new Exception("Failed to create PayPal order");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayPal order for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<PayPalCaptureResponse> CapturePaymentAsync(string orderId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/Payment/paypal/capture-payment/{orderId}", new StringContent("", Encoding.UTF8, "application/json"));
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PayPalCaptureResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (result != null)
                {
                    _logger.LogInformation("PayPal payment captured: {OrderId}", orderId);
                    return result;
                }
            }

            throw new Exception("Failed to capture PayPal payment");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing PayPal payment for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task InitializePayPalButtonsAsync(ElementReference buttonContainer, decimal amount, int orderId, DotNetObjectReference<object> componentRef)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("initializePayPalButtons", buttonContainer, amount, orderId, componentRef);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing PayPal buttons for order {OrderId}", orderId);
            throw;
        }
    }
}

// PayPal Models for Client
public class PayPalOrderRequest
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string CustomerEmail { get; set; } = "";
}

public class PayPalOrderResponse
{
    public string Id { get; set; } = "";
    public string Status { get; set; } = "";
    public List<PayPalLink> Links { get; set; } = new();
}

public class PayPalCaptureResponse
{
    public string Id { get; set; } = "";
    public string Status { get; set; } = "";
    public List<PayPalLink> Links { get; set; } = new();
}

public class PayPalLink
{
    public string Href { get; set; } = "";
    public string Rel { get; set; } = "";
    public string Method { get; set; } = "";
} 