using Microsoft.JSInterop;
using HidaSushi.Shared.Models;

namespace HidaSushi.Client.Services;

public class StripeService : IStripeService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ApiService _apiService;
    private readonly ILogger<StripeService> _logger;
    private readonly string _publishableKey = "pk_test_51RN6FuPqDNuhspIcrTxPu9zczvcl9Lbe13pERuAwiLeUY2i0OVJdsvniXxCtQ2e7GjmYT25btMTSwTaO2poyoAqS00f11Bpayl";

    public StripeService(IJSRuntime jsRuntime, ApiService apiService, ILogger<StripeService> logger)
    {
        _jsRuntime = jsRuntime;
        _apiService = apiService;
        _logger = logger;
    }

    public async Task<string> CreateCheckoutSessionAsync(List<CartItem> cartItems, decimal totalAmount, string customerEmail)
    {
        try
        {
            var request = new StripePaymentIntentRequest
            {
                OrderId = 0, // Will be set after order creation
                Amount = totalAmount,
                Currency = "eur",
                CustomerEmail = customerEmail
            };

            var result = await _apiService.CreateStripePaymentIntentAsync(request);
            
            if (result?.Success == true && !string.IsNullOrEmpty(result.PaymentIntentId))
            {
                return result.PaymentIntentId;
            }

            throw new Exception(result?.ErrorMessage ?? "Failed to create checkout session");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe checkout session");
            throw;
        }
    }

    public async Task<bool> RedirectToCheckoutAsync(string sessionId)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("stripeRedirectToCheckout", _publishableKey, sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error redirecting to Stripe checkout");
            return false;
        }
    }

    public Task<PaymentResult> VerifyPaymentAsync(string sessionId)
    {
        try
        {
            // This would typically call your backend to verify the session
            // For now, we'll return a success result
            return Task.FromResult(new PaymentResult
            {
                IsSuccess = true,
                PaymentIntentId = sessionId,
                Amount = 0, // Would be retrieved from session
                Currency = "EUR"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Stripe payment");
            return Task.FromResult(new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            });
        }
    }
} 