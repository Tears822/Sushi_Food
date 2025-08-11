using HidaSushi.Shared.Models;
using System.Net.Http.Json;

namespace HidaSushi.Client.Services;

public interface IGodCoinService
{
    Task<GodCoinBalance?> GetBalanceAsync(string userId);
    Task<GodCoinPaymentResult> ProcessPaymentAsync(GodCoinPaymentRequest request);
    Task<decimal> GetExchangeRateAsync(string fromCurrency = "EUR", string toCurrency = "GDC");
    Task<List<GodCoinTransaction>> GetTransactionHistoryAsync(string userId, int limit = 50);
    decimal ConvertToGodCoin(decimal eurAmount, decimal exchangeRate);
    decimal ConvertFromGodCoin(decimal godCoinAmount, decimal exchangeRate);
}

public class GodCoinService : IGodCoinService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GodCoinService> _logger;

    public GodCoinService(HttpClient httpClient, ILogger<GodCoinService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GodCoinBalance?> GetBalanceAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/godcoin/balance/{userId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<GodCoinBalance>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching GodCoin balance for user {UserId}", userId);
            return null;
        }
    }

    public async Task<GodCoinPaymentResult> ProcessPaymentAsync(GodCoinPaymentRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/godcoin/payment", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<GodCoinPaymentResult>();
                return result ?? new GodCoinPaymentResult 
                { 
                    Success = false, 
                    ErrorMessage = "Failed to parse payment response" 
                };
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return new GodCoinPaymentResult 
            { 
                Success = false, 
                ErrorMessage = $"Payment failed: {errorContent}" 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GodCoin payment");
            return new GodCoinPaymentResult 
            { 
                Success = false, 
                ErrorMessage = "Payment processing error occurred" 
            };
        }
    }

    public async Task<decimal> GetExchangeRateAsync(string fromCurrency = "EUR", string toCurrency = "GDC")
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/godcoin/exchange-rate?from={fromCurrency}&to={toCurrency}");
            if (response.IsSuccessStatusCode)
            {
                var rateResponse = await response.Content.ReadFromJsonAsync<ExchangeRateResponse>();
                return rateResponse?.Rate ?? 1000m; // Default: 1 EUR = 1000 GDC
            }
            return 1000m; // Fallback exchange rate
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching exchange rate");
            return 1000m; // Fallback exchange rate
        }
    }

    public async Task<List<GodCoinTransaction>> GetTransactionHistoryAsync(string userId, int limit = 50)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/godcoin/transactions/{userId}?limit={limit}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<GodCoinTransaction>>() ?? new();
            }
            return new List<GodCoinTransaction>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching transaction history for user {UserId}", userId);
            return new List<GodCoinTransaction>();
        }
    }

    public decimal ConvertToGodCoin(decimal eurAmount, decimal exchangeRate)
    {
        return Math.Round(eurAmount * exchangeRate, 0); // GodCoin doesn't have decimal places
    }

    public decimal ConvertFromGodCoin(decimal godCoinAmount, decimal exchangeRate)
    {
        return Math.Round(godCoinAmount / exchangeRate, 2); // EUR has 2 decimal places
    }
}

// GodCoin Models
public class GodCoinBalance
{
    public string UserId { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal LockedBalance { get; set; } // For pending transactions
    public decimal AvailableBalance => Balance - LockedBalance;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class GodCoinPaymentRequest
{
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; } // Amount in GodCoin
    public decimal EurEquivalent { get; set; } // EUR equivalent for reference
    public string OrderId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;
}

public class GodCoinPaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public decimal AmountCharged { get; set; }
    public decimal RemainingBalance { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public class GodCoinTransaction
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? OrderReference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public TransactionStatus Status { get; set; }
}

public class ExchangeRateResponse
{
    public decimal Rate { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

public enum TransactionType
{
    Payment,
    Refund,
    Deposit,
    Withdrawal,
    Bonus,
    Loyalty
}

public enum TransactionStatus
{
    Pending,
    Completed,
    Failed,
    Cancelled
} 