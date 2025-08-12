using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using HidaSushi.Shared.Models;

namespace HidaSushi.Admin.Services;

public class AuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly CustomAuthenticationStateProvider _authStateProvider;
    private readonly IAdminApiService _adminApiService;

    public AuthenticationService(IHttpClientFactory httpClientFactory, CustomAuthenticationStateProvider authStateProvider, IAdminApiService adminApiService)
    {
        _httpClient = httpClientFactory.CreateClient("AuthClient");
        _authStateProvider = authStateProvider;
        _adminApiService = adminApiService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            Console.WriteLine($"=== AuthenticationService.LoginAsync called ===");
            Console.WriteLine($"Username: {request.Username}");
            Console.WriteLine($"API Base URL: {_httpClient.BaseAddress}");
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"Sending POST to: {_httpClient.BaseAddress}api/auth/login");
            var response = await _httpClient.PostAsync("api/auth/login", content);
            
            Console.WriteLine($"API Response Status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response Content: {responseJson}");
                
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loginResponse?.Success == true && !string.IsNullOrEmpty(loginResponse.Token))
                {
                    Console.WriteLine($"Login successful, setting token: {loginResponse.Token.Substring(0, Math.Min(10, loginResponse.Token.Length))}...");
                    
                    // Set the token for future API calls on both HTTP clients
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.Token);
                    
                    // IMPORTANT: Set the token on AdminApiService as well
                    _adminApiService.SetAuthToken(loginResponse.Token);
                    Console.WriteLine("Token set on AdminApiService during login");

                    // Update authentication state
                    await _authStateProvider.LoginAsync(loginResponse.Token, request.Username);
                    Console.WriteLine("Authentication state updated");

                    return loginResponse;
                }
                else
                {
                    Console.WriteLine($"Login failed - Success: {loginResponse?.Success}, Token empty: {string.IsNullOrEmpty(loginResponse?.Token)}");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Error Response: {errorContent}");
            }

            return new LoginResponse
            {
                Success = false,
                Message = "Invalid credentials"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return new LoginResponse
            {
                Success = false,
                Message = "Login failed. Please try again."
            };
        }
    }

    public async Task LogoutAsync()
    {
        // Clear the authorization headers on both HTTP clients
        _httpClient.DefaultRequestHeaders.Authorization = null;
        _adminApiService.SetAuthToken(string.Empty);

        // Update authentication state
        await _authStateProvider.LogoutAsync();
    }
} 