using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;
using HidaSushi.Client.Resources;

namespace HidaSushi.Client.Services;

public interface ILocalizationService
{
    event Action? OnLanguageChanged;
    
    string CurrentLanguage { get; }
    List<SupportedLanguage> SupportedLanguages { get; }
    
    Task InitializeAsync();
    Task SetLanguage(string languageCode);
    string GetString(string key);
    string GetString(string key, params object[] arguments);
    
    // Specific methods for common use cases
    string GetCurrencyDisplay(decimal amount, string currencyCode = "EUR");
    string GetOrderStatus(string status);
    string GetPaymentMethod(string paymentMethod);
    string GetIngredientName(string ingredientName);
    string GetSushiRollName(string rollName);
    string GetSushiRollDescription(string description);
}

public class LocalizationService : ILocalizationService
{
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jsRuntime;
    private string _currentLanguage;

    public LocalizationService(NavigationManager navigationManager, IJSRuntime jsRuntime)
    {
        _navigationManager = navigationManager;
        _jsRuntime = jsRuntime;
        // Get current culture from the global culture info
        _currentLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    }

    public event Action? OnLanguageChanged;

    public string CurrentLanguage => _currentLanguage;

    public List<SupportedLanguage> SupportedLanguages { get; } = new()
    {
        new SupportedLanguage { Code = "en", Name = "English", Flag = "🇬🇧" },
        new SupportedLanguage { Code = "fr", Name = "Français", Flag = "🇫🇷" }
    };

    public async Task InitializeAsync()
    {
        // Get the stored culture from JavaScript
        var storedCulture = await _jsRuntime.InvokeAsync<string>("cultureInfo.get");
        if (!string.IsNullOrEmpty(storedCulture))
        {
            _currentLanguage = storedCulture;
            // Ensure the Resource.Culture is set to match the stored culture
            var culture = GetFullCultureInfo(storedCulture);
            Resource.Culture = culture;
        }
    }

    public async Task SetLanguage(string languageCode)
    {
        if (_currentLanguage != languageCode && SupportedLanguages.Any(l => l.Code == languageCode))
        {
            _currentLanguage = languageCode;
            
            // Store the selected culture in JavaScript
            await _jsRuntime.InvokeVoidAsync("cultureInfo.set", languageCode);
            
            // Update the global culture for the current thread
            var culture = GetFullCultureInfo(languageCode);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            
            // Set the culture for the Resource class - this is crucial for proper localization
            Resource.Culture = culture;
            
            // Add a small delay to ensure culture is properly set before notifying components
            await Task.Delay(10);
            
            // Notify subscribers that language has changed
            OnLanguageChanged?.Invoke();
            
            // Force a small additional delay to ensure all components have processed the change
            await Task.Delay(50);
        }
    }

    public string GetString(string key)
    {
        try
        {
            // Use the Resource class with the current culture that was set
            var resourceManager = Resource.ResourceManager;
            var localized = resourceManager.GetString(key, Resource.Culture);
            return localized ?? key;
        }
        catch (Exception)
        {
            return key;
        }
    }

    public string GetString(string key, params object[] arguments)
    {
        try
        {
            // Use the Resource class with the current culture that was set
            var resourceManager = Resource.ResourceManager;
            var localized = resourceManager.GetString(key, Resource.Culture);
            if (localized != null && arguments.Length > 0)
            {
                return string.Format(localized, arguments);
            }
            return localized ?? key;
        }
        catch
        {
            return key;
        }
    }

    public string GetCurrencyDisplay(decimal amount, string currencyCode = "EUR")
    {
        var culture = new CultureInfo(_currentLanguage);
        return amount.ToString("C", culture).Replace(culture.NumberFormat.CurrencySymbol, GetCurrencySymbol(currencyCode));
    }

    public string GetOrderStatus(string status)
    {
        return GetString($"OrderStatus.{status}");
    }

    public string GetPaymentMethod(string paymentMethod)
    {
        return GetString($"PaymentMethod.{paymentMethod}");
    }

    public string GetIngredientName(string ingredientName)
    {
        var key = $"Ingredient.{ingredientName.Replace(" ", "")}";
        var localized = GetString(key);
        return localized != key ? localized : ingredientName; // Fallback to original if not found
    }

    public string GetSushiRollName(string rollName)
    {
        var key = $"SushiRoll.{rollName.Replace(" ", "")}";
        var localized = GetString(key);
        return localized != key ? localized : rollName; // Fallback to original if not found
    }

    public string GetSushiRollDescription(string description)
    {
        var key = $"Description.{description.Replace(" ", "").Take(20)}"; // Use first 20 chars as key
        var localized = GetString(key);
        return localized != key ? localized : description; // Fallback to original if not found
    }

    private string GetCurrencySymbol(string currencyCode)
    {
        return currencyCode switch
        {
            "EUR" => "€",
            "USD" => "$",
            "GBP" => "£",
            "GDC" => "🪙", // GodCoin symbol
            _ => currencyCode
        };
    }

    private CultureInfo GetFullCultureInfo(string languageCode)
    {
        return languageCode switch
        {
            "en" => new CultureInfo("en"),
            "fr" => new CultureInfo("fr"),
            _ => new CultureInfo(languageCode)
        };
    }
}

public class SupportedLanguage
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
} 