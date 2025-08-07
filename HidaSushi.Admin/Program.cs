using HidaSushi.Admin.Components;
using HidaSushi.Admin.Services;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure HTTP client for backend API (following Contoso.Pizza pattern)
var apiUrl = builder.Configuration["Api:Url"]!;

// Simple HttpClient configuration like Contoso.Pizza
builder.Services.AddHttpClient("AdminApi", client =>
{
    client.BaseAddress = new Uri(apiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "HidaSushi-Admin/1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
});

// Configure HTTP client for authentication using the same pattern
builder.Services.AddHttpClient("AuthClient", client =>
{
    client.BaseAddress = new Uri(apiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "HidaSushi-Admin/1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
});

// Add authentication services
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationService>();

// Register the custom authentication state provider as the default one
builder.Services.AddScoped<AuthenticationStateProvider>(provider => 
    provider.GetRequiredService<CustomAuthenticationStateProvider>());

// Add authorization
builder.Services.AddAuthorization();

// Configure logging for development
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Warning);

// Register admin services
builder.Services.AddScoped<AdminApiService>();

// Add configuration for the admin
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Global exception handler for unhandled exceptions
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    var logger = app.Services.GetService<ILogger<Program>>();
    logger?.LogCritical(e.ExceptionObject as Exception, "Unhandled exception occurred in Admin Dashboard");
};

Console.WriteLine("🍣 HIDA SUSHI Admin Dashboard Starting...");
Console.WriteLine($"🔗 Backend API: {apiUrl}");
Console.WriteLine("🔐 Authentication: JWT-based with state management");
Console.WriteLine("📊 Features: Live Orders, Analytics, Menu Management, Ingredient Control");

app.Run();
