using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Microsoft.JSInterop;
using HidaSushi.Shared.Models;

namespace HidaSushi.Admin.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomAuthenticationStateProvider> _logger;

    public CustomAuthenticationStateProvider(
        IJSRuntime jsRuntime, 
        HttpClient httpClient,
        ILogger<CustomAuthenticationStateProvider> logger)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Try to access localStorage, if it fails we're in prerendering
            string? token = null;
            string? username = null;

            try
            {
                token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "adminToken");
                username = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "adminUsername");
            }
            catch (InvalidOperationException)
            {
                // We're in prerendering, return unauthenticated state
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(username))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // Set the authorization header for API calls
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Validate token with server
            try
            {
                var response = await _httpClient.GetAsync("api/Auth/validate");
                if (!response.IsSuccessStatusCode)
                {
                    // Token is invalid, clear it
                    try
                    {
                        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "adminToken");
                        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "adminUsername");
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore if we're in prerendering
                    }
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
            }
            catch
            {
                // Server error, clear token
                try
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "adminToken");
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "adminUsername");
                }
                catch (InvalidOperationException)
                {
                    // Ignore if we're in prerendering
                }
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // Create claims for the authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("Username", username)
            };

            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);

            return new AuthenticationState(principal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication state");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public async Task LoginAsync(string token, string username)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "adminToken", token);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "adminUsername", username);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("Username", username)
            };

            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "adminToken");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "adminUsername");

            var identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }

    public void NotifyUserAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
} 