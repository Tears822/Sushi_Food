using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using HidaSushi.Client;
using HidaSushi.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HTTP client for backend API
var backendUrl = builder.Configuration["BackendUrl"] ?? "https://localhost:5000";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(backendUrl) });

// Add a named HTTP client for API calls
builder.Services.AddHttpClient("ServerAPI", client =>
{
    client.BaseAddress = new Uri(backendUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Configure logging for development
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft.AspNetCore.Components.WebAssembly", LogLevel.Warning);

// Register services
builder.Services.AddScoped<IFlowbiteService, FlowbiteService>();
builder.Services.AddScoped<IApiService, ApiService>();
// builder.Services.AddScoped<IStripeService, StripeService>(); // TODO: Implement StripeService
builder.Services.AddSingleton<ICartService, CartService>();
builder.Services.AddSingleton<IToastService, ToastService>();

// Add configuration
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

// Global exception handler for unhandled exceptions
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    var logger = app.Services.GetService<ILogger<Program>>();
    logger?.LogCritical(e.ExceptionObject as Exception, "Unhandled exception occurred");
};

await app.RunAsync();
