using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HidaSushi.Shared.Models;
using HidaSushi.Server.Services;

namespace HidaSushi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly ILogger<PaymentController> _logger;
    private readonly OrderService _orderService;
    private readonly IStripeService _stripeService;
    private readonly IPayPalService _paypalService;

    public PaymentController(
        ILogger<PaymentController> logger, 
        OrderService orderService,
        IStripeService stripeService,
        IPayPalService paypalService)
    {
        _logger = logger;
        _orderService = orderService;
        _stripeService = stripeService;
        _paypalService = paypalService;
    }

    // POST: api/Payment/process
    [HttpPost("process")]
    [AllowAnonymous]
    public async Task<ActionResult<PaymentResult>> ProcessPayment([FromBody] PaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Processing payment for order {OrderId}, method: {PaymentMethod}", 
                request.OrderId, request.PaymentMethod);

            var result = request.PaymentMethod switch
            {
                PaymentMethod.CashOnDelivery => await ProcessCashPayment(request),
                PaymentMethod.Stripe => await ProcessStripePayment(request),
                PaymentMethod.PayPal => await ProcessPayPalPayment(request),
                PaymentMethod.GodPay => await ProcessGodPayment(request),
                _ => new PaymentResult { Success = false, ErrorMessage = "Unsupported payment method" }
            };

            if (result.Success)
            {
                // Update order payment status
                await _orderService.UpdatePaymentStatusAsync(request.OrderId, PaymentStatus.Paid, result.TransactionId);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for order {OrderId}", request.OrderId);
            return StatusCode(500, new PaymentResult 
            { 
                Success = false, 
                ErrorMessage = "Payment processing failed" 
            });
        }
    }

    // POST: api/Payment/stripe/create-checkout-session
    [HttpPost("stripe/create-checkout-session")]
    [AllowAnonymous]
    public async Task<ActionResult<StripePaymentIntentResult>> CreateStripeCheckoutSession([FromBody] StripePaymentIntentRequest request)
    {
        try
        {
            var sessionId = await _stripeService.CreateCheckoutSessionAsync(request.Amount, request.CustomerEmail ?? "", request.OrderId);
            
            return Ok(new StripePaymentIntentResult
            {
                Success = true,
                PaymentIntentId = sessionId,
                ClientSecret = sessionId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe checkout session for order {OrderId}", request.OrderId);
            return StatusCode(500, new StripePaymentIntentResult 
            { 
                Success = false, 
                ErrorMessage = "Failed to create checkout session" 
            });
        }
    }

    // POST: api/Payment/paypal/create-order
    [HttpPost("paypal/create-order")]
    [AllowAnonymous]
    public async Task<ActionResult> CreatePayPalOrder([FromBody] PayPalOrderRequest request)
    {
        try
        {
            var response = await _paypalService.CreateOrderAsync(request.Amount, request.CustomerEmail, request.OrderId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayPal order for order {OrderId}", request.OrderId);
            return StatusCode(500, new { error = "Failed to create PayPal order" });
        }
    }

    // POST: api/Payment/paypal/capture-payment
    [HttpPost("paypal/capture-payment/{orderId}")]
    [AllowAnonymous]
    public async Task<ActionResult> CapturePayPalPayment(string orderId)
    {
        try
        {
            var response = await _paypalService.CapturePaymentAsync(orderId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing PayPal payment for order {OrderId}", orderId);
            return StatusCode(500, new { error = "Failed to capture PayPal payment" });
        }
    }

    // POST: api/Payment/stripe/webhook
    [HttpPost("stripe/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        try
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Stripe webhook received without signature");
                return BadRequest("Missing signature");
            }

            var isValid = await _stripeService.VerifyWebhookSignatureAsync(json, signature);
            if (!isValid)
            {
                _logger.LogWarning("Stripe webhook signature verification failed");
                return BadRequest("Invalid signature");
            }

            var stripeEvent = await _stripeService.ConstructEventAsync(json, signature);
            
            // Handle different event types
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleStripeCheckoutCompleted(stripeEvent);
                    break;
                case "payment_intent.succeeded":
                    await HandleStripePaymentSucceeded(stripeEvent);
                    break;
                default:
                    _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return BadRequest();
        }
    }

    private Task<PaymentResult> ProcessCashPayment(PaymentRequest request)
    {
        // Cash on delivery - no immediate processing needed
        _logger.LogInformation("Cash on delivery payment registered for order {OrderId}", request.OrderId);
        
        return Task.FromResult(new PaymentResult
        {
            Success = true,
            TransactionId = $"CASH_{DateTime.UtcNow:yyyyMMddHHmmss}_{request.OrderId}",
            PaymentMethod = PaymentMethod.CashOnDelivery,
            Message = "Cash on delivery payment registered"
        });
    }

    private Task<PaymentResult> ProcessStripePayment(PaymentRequest request)
    {
        try
        {
            // For Stripe, we expect the payment to be processed via checkout session
            // This is more for webhook confirmation
            _logger.LogInformation("Processing Stripe payment confirmation for order {OrderId}", request.OrderId);
            
            return Task.FromResult(new PaymentResult
            {
                Success = true,
                TransactionId = request.PaymentToken ?? $"stripe_{DateTime.UtcNow:yyyyMMddHHmmss}",
                PaymentMethod = PaymentMethod.Stripe,
                Message = "Stripe payment processed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe payment failed for order {OrderId}", request.OrderId);
            return Task.FromResult(new PaymentResult
            {
                Success = false,
                ErrorMessage = "Stripe payment processing failed"
            });
        }
    }

    private Task<PaymentResult> ProcessPayPalPayment(PaymentRequest request)
    {
        try
        {
            // PayPal payment simulation - in real implementation, integrate with PayPal SDK
            _logger.LogInformation("Processing PayPal payment for order {OrderId}", request.OrderId);
            
            return Task.FromResult(new PaymentResult
            {
                Success = true,
                TransactionId = $"pp_{DateTime.UtcNow:yyyyMMddHHmmss}",
                PaymentMethod = PaymentMethod.PayPal,
                Message = "PayPal payment processed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal payment failed for order {OrderId}", request.OrderId);
            return Task.FromResult(new PaymentResult
            {
                Success = false,
                ErrorMessage = "PayPal payment processing failed"
            });
        }
    }

    private async Task<PaymentResult> ProcessGodPayment(PaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Processing GodPay payment for order {OrderId}", request.OrderId);
            
            // Mock GodPay implementation
            await Task.Delay(100); // Simulate API call
            
            return new PaymentResult
            {
                Success = true,
                TransactionId = $"GOD_{DateTime.UtcNow:yyyyMMddHHmmss}_{request.OrderId}",
                PaymentMethod = PaymentMethod.GodPay,
                Message = "GodPay payment processed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GodPay payment failed for order {OrderId}", request.OrderId);
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = "GodPay payment processing failed"
            };
        }
    }

    private Task HandleStripeCheckoutCompleted(Stripe.Event stripeEvent)
    {
        // Handle successful checkout session
        _logger.LogInformation("Stripe checkout session completed: {EventId}", stripeEvent.Id);
        // TODO: Update order status based on session metadata
        return Task.CompletedTask;
    }

    private async Task HandleStripePaymentSucceeded(Stripe.Event stripeEvent)
    {
        try
        {
            _logger.LogInformation("Stripe payment intent succeeded: {EventId}", stripeEvent.Id);
            
            var paymentIntent = stripeEvent.Data.Object as Stripe.PaymentIntent;
            if (paymentIntent?.Metadata?.ContainsKey("order_id") == true)
            {
                if (int.TryParse(paymentIntent.Metadata["order_id"], out int orderId))
                {
                    _logger.LogInformation("Processing Stripe payment success for order ID: {OrderId}", orderId);
                    
                    var success = await _orderService.UpdatePaymentStatusAsync(orderId, PaymentStatus.Paid, paymentIntent.Id);
                    if (success)
                    {
                        _logger.LogInformation("Successfully updated payment status for order {OrderId}", orderId);
                    }
                    else
                    {
                        _logger.LogError("Failed to update payment status for order {OrderId}", orderId);
                    }
                }
                else
                {
                    _logger.LogError("Could not parse order_id from Stripe metadata: {OrderId}", paymentIntent.Metadata["order_id"]);
                }
            }
            else
            {
                _logger.LogWarning("No order_id found in Stripe payment intent metadata for event {EventId}", stripeEvent.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Stripe payment success for event {EventId}", stripeEvent.Id);
        }
    }
}

// Additional models for PayPal
public class PayPalOrderRequest
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string CustomerEmail { get; set; } = "";
} 