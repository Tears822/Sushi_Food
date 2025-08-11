using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using HidaSushi.Admin.Services;
using HidaSushi.Admin.Components;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(o => o.DetailedErrors = true);

// Add HttpContextAccessor for server-side auth
builder.Services.AddHttpContextAccessor();

// Add HttpClient for API calls
builder.Services.AddHttpClient("AuthClient", client =>
{
    client.BaseAddress = new Uri("https://apimailbroker.ddns.net/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient("AdminApi", client =>
{
    client.BaseAddress = new Uri("https://apimailbroker.ddns.net/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Add Authentication/Authorization for Blazor Server
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie(options =>
{
    options.Cookie.Name = ".HidaSushi.AdminAuth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
});

builder.Services.AddAuthorization();

// Register custom services
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => 
    provider.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddScoped<HidaSushi.Admin.Services.AuthenticationService>();
builder.Services.AddScoped<AdminApiService>();

// Add forwarded headers for reverse proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseForwardedHeaders();

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// Sign-in endpoint to set the auth cookie, then redirect
app.MapGet("/signin", async (HttpContext context, string? name, string? token, string? returnUrl) =>
{
    // Log all received parameters
    Console.WriteLine($"=== /signin endpoint called ===");
    Console.WriteLine($"Query string: {context.Request.QueryString}");
    Console.WriteLine($"name parameter: '{name}'");
    Console.WriteLine($"token parameter: '{(string.IsNullOrEmpty(token) ? "NULL/EMPTY" : token.Substring(0, Math.Min(10, token.Length)) + "...")}'");
    Console.WriteLine($"returnUrl parameter: '{returnUrl}'");
    
    // Validate required parameters
    if (string.IsNullOrEmpty(name))
    {
        Console.WriteLine("ERROR: Missing name parameter");
        return Results.BadRequest("Missing required parameter: name");
    }
    
    if (string.IsNullOrEmpty(token))
    {
        Console.WriteLine("ERROR: Missing token parameter");
        return Results.BadRequest("Missing required parameter: token");
    }

    Console.WriteLine("All parameters valid, creating claims...");
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, name),
        new Claim(ClaimTypes.Email, name),
        new Claim("token", token)  // IMPORTANT: Include the JWT token in claims
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    var props = new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
    };

    Console.WriteLine("Signing in user...");
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
    var target = string.IsNullOrWhiteSpace(returnUrl) ? "/admin" : returnUrl;
    Console.WriteLine($"Redirecting to: {target}");
    return Results.Redirect(target);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

Console.WriteLine($"🌐 Admin Panel is running on: https://adminmailbroker.ddns.net");
app.Run();