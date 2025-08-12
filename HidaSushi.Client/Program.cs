using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using HidaSushi.Client;
using HidaSushi.Client.Services;
using HidaSushi.Client.Resources;
using Microsoft.JSInterop;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HTTP client for backend API
var backendUrl = builder.Configuration["BackendUrl"] ?? "https://apimailbroker.ddns.net";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(backendUrl) });

// Add a named HTTP client for API calls
builder.Services.AddHttpClient("ServerAPI", client =>
{
    client.BaseAddress = new Uri(backendUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Register application services
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<IFlowbiteService, FlowbiteService>();
builder.Services.AddScoped<ILocalizationService, LocalizationService>();
builder.Services.AddScoped<IGodCoinService, GodCoinService>();
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddScoped<IPayPalService, PayPalService>();
builder.Services.AddScoped<IApiService, ApiService>();

// Add configuration
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

// Set culture of the application at startup
var jsInterop = app.Services.GetRequiredService<IJSRuntime>();
var result = await jsInterop.InvokeAsync<string>("cultureInfo.get");
CultureInfo culture;
if (result != null && !string.IsNullOrEmpty(result))
{
    // Use the same culture mapping logic as the LocalizationService
    culture = result switch
    {
        "en" => new CultureInfo("en"),
        "fr" => new CultureInfo("fr"),
        _ => new CultureInfo(result)
    };
}
else
{
    culture = new CultureInfo("en");
    await jsInterop.InvokeVoidAsync("cultureInfo.set", "en");
}

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// Set the Resource.Culture to match the startup culture - this ensures proper localization from the start
Resource.Culture = culture;

// Configure logging for development
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft.AspNetCore.Components.WebAssembly", LogLevel.Warning);

// Global exception handler for unhandled exceptions
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    var logger = app.Services.GetService<ILogger<Program>>();
    logger?.LogCritical(e.ExceptionObject as Exception, "Unhandled exception occurred");
};

await app.RunAsync();
