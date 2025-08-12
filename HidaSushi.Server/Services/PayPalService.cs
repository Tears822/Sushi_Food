using System.Text.Json;
using System.Text;
using RestSharp;

namespace HidaSushi.Server.Services;

public interface IPayPalService
{
    Task<CreateOrderResponse> CreateOrderAsync(decimal amount, string customerEmail, int orderId);
    Task<CapturePaymentResponse> CapturePaymentAsync(string orderId);
    Task<string> GetAccessTokenAsync();
}

public class PayPalService : IPayPalService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PayPalService> _logger;
    private readonly string _baseUrl;
    private readonly string _clientId;
    private readonly string _appSecret;

    public PayPalService(HttpClient httpClient, IConfiguration configuration, ILogger<PayPalService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _baseUrl = _configuration["PayPal:BaseUrl"] ?? "https://api-m.sandbox.paypal.com";
        _clientId = _configuration["PayPal:ClientId"] ?? "";
        _appSecret = _configuration["PayPal:AppSecret"] ?? "";
    }

    public async Task<CreateOrderResponse> CreateOrderAsync(decimal amount, string customerEmail, int orderId)
    {
        try
        {
            var orderRequest = new CreateOrderRequest
            {
                Intent = "CAPTURE",
                PurchaseUnits = new[]
                {
                    new PurchaseUnit
                    {
                        Amount = new Amount
                        {
                            Value = amount.ToString("F2"),
                            CurrencyCode = "EUR"
                        },
                        Description = $"HidaSushi Order #{orderId}",
                        Reference_Id = orderId.ToString()
                    }
                },
                ApplicationContext = new ApplicationContext
                {
                    ReturnUrl = $"{_configuration["Domain:FrontendUrl"]}/payment/paypal/success",
                    CancelUrl = $"{_configuration["Domain:FrontendUrl"]}/payment/paypal/cancel"
                }
            };

            var client = new RestClient(_baseUrl);
            var request = new RestRequest("v2/checkout/orders", Method.Post);
            request.AddHeader("Authorization", $"Bearer {await GetAccessTokenAsync()}");
            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(orderRequest);

            var response = await client.PostAsync(request);
            if (response.IsSuccessful && response.Content != null)
            {
                var result = JsonSerializer.Deserialize<CreateOrderResponse>(response.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _logger.LogInformation("PayPal order created: {OrderId} for HidaSushi order {HidaOrderId}", result?.Id, orderId);
                return result ?? throw new Exception("Failed to deserialize PayPal response");
            }

            _logger.LogError("PayPal order creation failed: {StatusCode} - {Content}", response.StatusCode, response.Content);
            throw new Exception($"PayPal order creation failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayPal order for HidaSushi order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<CapturePaymentResponse> CapturePaymentAsync(string orderId)
    {
        try
        {
            var client = new RestClient(_baseUrl);
            var request = new RestRequest($"v2/checkout/orders/{orderId}/capture", Method.Post);
            request.AddHeader("Authorization", $"Bearer {await GetAccessTokenAsync()}");
            request.AddHeader("Content-Type", "application/json");

            var response = await client.PostAsync(request);
            if (response.IsSuccessful && response.Content != null)
            {
                var result = JsonSerializer.Deserialize<CapturePaymentResponse>(response.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _logger.LogInformation("PayPal payment captured: {OrderId}", orderId);
                return result ?? throw new Exception("Failed to deserialize PayPal capture response");
            }

            _logger.LogError("PayPal payment capture failed: {StatusCode} - {Content}", response.StatusCode, response.Content);
            throw new Exception($"PayPal payment capture failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing PayPal payment for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<string> GetAccessTokenAsync()
    {
        try
        {
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_appSecret}"));

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{_baseUrl}/v1/oauth2/token"),
                Headers = { { "Authorization", $"Basic {auth}" } },
                Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            var body = await response.Content.ReadFromJsonAsync<PayPalOAuthResponse>();
            return body?.AccessToken ?? throw new Exception("Failed to get PayPal access token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PayPal access token");
            throw;
        }
    }
}

// PayPal Models
public class CreateOrderRequest
{
    public string Intent { get; set; } = "";
    public PurchaseUnit[] PurchaseUnits { get; set; } = Array.Empty<PurchaseUnit>();
    public ApplicationContext? ApplicationContext { get; set; }
}

public class PurchaseUnit
{
    public Amount Amount { get; set; } = new();
    public string Description { get; set; } = "";
    public string Reference_Id { get; set; } = "";
}

public class Amount
{
    public string Value { get; set; } = "";
    public string CurrencyCode { get; set; } = "";
}

public class ApplicationContext
{
    public string ReturnUrl { get; set; } = "";
    public string CancelUrl { get; set; } = "";
}

public class CreateOrderResponse
{
    public string Id { get; set; } = "";
    public string Status { get; set; } = "";
    public List<Link> Links { get; set; } = new();
}

public class CapturePaymentResponse
{
    public string Id { get; set; } = "";
    public string Status { get; set; } = "";
    public List<Link> Links { get; set; } = new();
}

public class Link
{
    public string Href { get; set; } = "";
    public string Rel { get; set; } = "";
    public string Method { get; set; } = "";
}

public class PayPalOAuthResponse
{
    public string AccessToken { get; set; } = "";
    public string TokenType { get; set; } = "";
    public int ExpiresIn { get; set; }
} 