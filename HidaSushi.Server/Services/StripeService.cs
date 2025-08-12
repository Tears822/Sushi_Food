using Stripe;
using Stripe.Checkout;
using HidaSushi.Shared.Models;

namespace HidaSushi.Server.Services;

public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(decimal amount, string customerEmail, int orderId);
    Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, string customerEmail);
    Task<bool> VerifyWebhookSignatureAsync(string payload, string signature);
    Task<Event> ConstructEventAsync(string payload, string signature);
}

public class StripeService : IStripeService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeService> _logger;
    private readonly string _webhookSecret;

    public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _webhookSecret = _configuration["Stripe:WebhookSecret"] ?? "";
        
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    public async Task<string> CreateCheckoutSessionAsync(decimal amount, string customerEmail, int orderId)
    {
        try
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(amount * 100), // Convert to cents
                            Currency = "eur",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"HidaSushi Order #{orderId}",
                                Description = "Sushi order payment"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = $"{_configuration["Domain:FrontendUrl"]}/payment/success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{_configuration["Domain:FrontendUrl"]}/payment/cancel",
                CustomerEmail = customerEmail,
                Metadata = new Dictionary<string, string>
                {
                    { "order_id", orderId.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);
            
            _logger.LogInformation("Stripe checkout session created: {SessionId} for order {OrderId}", session.Id, orderId);
            
            return session.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Stripe checkout session for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, string customerEmail)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Convert to cents
                Currency = "eur",
                Metadata = new Dictionary<string, string>
                {
                    { "customer_email", customerEmail }
                }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);
            
            _logger.LogInformation("Stripe payment intent created: {PaymentIntentId}", paymentIntent.Id);
            
            return paymentIntent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Stripe payment intent for customer {CustomerEmail}", customerEmail);
            throw;
        }
    }

    public Task<bool> VerifyWebhookSignatureAsync(string payload, string signature)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, _webhookSecret);
            return Task.FromResult(stripeEvent != null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature verification failed");
            return Task.FromResult(false);
        }
    }

    public Task<Event> ConstructEventAsync(string payload, string signature)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, _webhookSecret);
            _logger.LogInformation("Stripe webhook event constructed: {EventType}", stripeEvent.Type);
            return Task.FromResult(stripeEvent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to construct Stripe webhook event");
            throw;
        }
    }
} 