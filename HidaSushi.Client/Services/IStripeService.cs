using HidaSushi.Shared.Models;

namespace HidaSushi.Client.Services;

public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(List<CartItem> cartItems, decimal totalAmount, string customerEmail);
    Task<bool> RedirectToCheckoutAsync(string sessionId);
    Task<PaymentResult> VerifyPaymentAsync(string sessionId);
}

public class PaymentResult
{
    public bool IsSuccess { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
} 