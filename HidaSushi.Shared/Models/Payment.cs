namespace HidaSushi.Shared.Models;

public class PaymentRequest
{
    public int OrderId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentToken { get; set; } // For Stripe/card payments
    public string? CustomerEmail { get; set; }
    public string? BillingAddress { get; set; }
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public class StripePaymentIntentRequest
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "eur";
    public string? CustomerEmail { get; set; }
}

public class StripePaymentIntentResult
{
    public bool Success { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? ClientSecret { get; set; }
    public string? ErrorMessage { get; set; }
} 