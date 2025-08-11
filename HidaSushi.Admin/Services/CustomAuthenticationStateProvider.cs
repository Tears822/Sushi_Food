using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace HidaSushi.Admin.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public CustomAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // For server-side Blazor, check HttpContext.User first
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                Console.WriteLine($"User is authenticated in HttpContext: {httpContext.User.Identity.Name}");
                
                // Check if token claim exists
                var tokenClaim = httpContext.User.FindFirst("token");
                if (tokenClaim != null)
                {
                    Console.WriteLine($"Token found in HttpContext claims: {tokenClaim.Value.Substring(0, Math.Min(10, tokenClaim.Value.Length))}...");
                }
                else
                {
                    Console.WriteLine("No token claim found in HttpContext.User");
                }
                
                _currentUser = httpContext.User;
                return Task.FromResult(new AuthenticationState(_currentUser));
            }
            else
            {
                Console.WriteLine("User is not authenticated in HttpContext");
            }

            // Return unauthenticated state
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            return Task.FromResult(new AuthenticationState(anonymous));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting authentication state: {ex.Message}");
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            return Task.FromResult(new AuthenticationState(anonymous));
        }
    }

    public async Task LoginAsync(string token, string username)
    {
        try
        {
            Console.WriteLine($"CustomAuthenticationStateProvider.LoginAsync called with token: {token.Substring(0, Math.Min(10, token.Length))}...");
            
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, username),
                new(ClaimTypes.Email, username),
                new("token", token)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // For server-side Blazor, we need to be careful about when we call SignInAsync
            // The error "Headers are read-only, response has already started" means we're trying
            // to set cookies after the response has started, which happens in some Blazor scenarios
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                try
                {
                    // Only try to sign in if the response hasn't started
                    if (!httpContext.Response.HasStarted)
                    {
                        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                        Console.WriteLine("Cookie authentication completed successfully");
                    }
                    else
                    {
                        Console.WriteLine("Response has already started, skipping cookie sign-in");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during cookie sign-in: {ex.Message}");
                    // Continue with setting the principal even if cookie sign-in fails
                }
            }

            // Always update the current user state, even if cookie sign-in fails
            _currentUser = principal;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            Console.WriteLine("Authentication state change notified");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during login: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            // Sign out from cookie authentication
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during logout: {ex.Message}");
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            var authState = await GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                var token = authState.User.FindFirst("token")?.Value;
                if (!string.IsNullOrEmpty(token))
                {
                    Console.WriteLine($"Token retrieved from claims: {token.Substring(0, Math.Min(10, token.Length))}...");
                    return token;
                }
                else
                {
                    Console.WriteLine("No token claim found in authenticated user");
                }
            }
            else
            {
                Console.WriteLine("User is not authenticated");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting token: {ex.Message}");
        }
        return null;
    }
} 