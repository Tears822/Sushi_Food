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
    private readonly IConfiguration _configuration;

    public PaymentController(ILogger<PaymentController> logger, OrderService orderService, IConfiguration configuration)
    {
        _logger = logger;
        _orderService = orderService;
        _configuration = configuration;
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

    // POST: api/Payment/stripe/webhook
    [HttpPost("stripe/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        try
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            
            // TODO: Verify Stripe webhook signature
            // TODO: Handle Stripe events (payment_intent.succeeded, etc.)
            
            _logger.LogInformation("Stripe webhook received");
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return BadRequest();
        }
    }

    private async Task<PaymentResult> ProcessCashPayment(PaymentRequest request)
    {
        // Cash on delivery - no immediate processing needed
        _logger.LogInformation("Cash on delivery payment registered for order {OrderId}", request.OrderId);
        
        return new PaymentResult
        {
            Success = true,
            TransactionId = $"CASH_{DateTime.UtcNow:yyyyMMddHHmmss}_{request.OrderId}",
            PaymentMethod = PaymentMethod.CashOnDelivery,
            Message = "Cash on delivery payment registered"
        };
    }

    private async Task<PaymentResult> ProcessStripePayment(PaymentRequest request)
    {
        try
        {
            // TODO: Integrate with Stripe API
            // var stripe = new StripeClient(_configuration["Stripe:SecretKey"]);
            // var paymentIntentService = new PaymentIntentService(stripe);
            
            _logger.LogInformation("Processing Stripe payment for order {OrderId}", request.OrderId);
            
            // Mock implementation - replace with real Stripe integration
            if (string.IsNullOrEmpty(request.PaymentToken))
            {
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Payment token is required for Stripe payments"
                };
            }

            // Simulate successful payment
            return new PaymentResult
            {
                Success = true,
                TransactionId = $"pi_mock_{DateTime.UtcNow:yyyyMMddHHmmss}",
                PaymentMethod = PaymentMethod.Stripe,
                Message = "Payment processed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe payment failed for order {OrderId}", request.OrderId);
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = "Stripe payment processing failed"
            };
        }
    }

    private async Task<PaymentResult> ProcessGodPayment(PaymentRequest request)
    {
        try
        {
            // TODO: Integrate with GodPay API
            _logger.LogInformation("Processing GodPay payment for order {OrderId}", request.OrderId);
            
            // Mock implementation - replace with real GodPay integration
            return new PaymentResult
            {
                Success = true,
                TransactionId = $"god_{DateTime.UtcNow:yyyyMMddHHmmss}",
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
} 