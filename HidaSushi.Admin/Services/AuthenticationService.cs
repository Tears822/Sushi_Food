using Microsoft.AspNetCore.Components.Authorization;
using HidaSushi.Shared.Models;
using System.Net.Http.Json;

namespace HidaSushi.Admin.Services;

public class AuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly CustomAuthenticationStateProvider _authStateProvider;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IHttpClientFactory httpClientFactory,
        CustomAuthenticationStateProvider authStateProvider,
        ILogger<AuthenticationService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("AuthClient");
        _authStateProvider = authStateProvider;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Attempting login for user: {Username}", request.Username);
            _logger.LogInformation("Sending request to: {BaseAddress}api/Auth/login", _httpClient.BaseAddress);
            
            // First, test if server is reachable
            try
            {
                var testResponse = await _httpClient.GetAsync("api/Auth/credentials");
                _logger.LogInformation("Server test response: {StatusCode}", testResponse.StatusCode);
            }
            catch (Exception testEx)
            {
                _logger.LogError(testEx, "Server connectivity test failed");
            }
            
            var response = await _httpClient.PostAsJsonAsync("api/Auth/login", request);
            
            _logger.LogInformation("Received response with status: {StatusCode}", response.StatusCode);
            
            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                _logger.LogInformation("Successfully deserialized response");

                if (loginResponse?.Success == true)
                {
                    // Update authentication state
                    await _authStateProvider.LoginAsync(loginResponse.Token, request.Username);
                    
                    // Set authorization header for future requests
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.Token);

                    _logger.LogInformation("Admin user {Username} logged in successfully", request.Username);
                }
                else
                {
                    _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
                }

                return loginResponse ?? new LoginResponse { Success = false, Message = "Login failed" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Server returned error status: {StatusCode}, Content: {Content}", response.StatusCode, errorContent);
                return new LoginResponse { Success = false, Message = $"Server error: {response.StatusCode}" };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", request.Username);
            return new LoginResponse { Success = false, Message = "Unable to connect to server" };
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            // Call logout endpoint
            await _httpClient.PostAsync("api/Auth/logout", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
        finally
        {
            // Clear authentication state
            await _authStateProvider.LogoutAsync();
            
            // Clear authorization header
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<bool> ValidateTokenAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/Auth/validate");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CheckServerHealthAsync()
    {
        try
        {
            _logger.LogInformation("🔍 Checking server health at: {BaseAddress}api/Auth/health", _httpClient.BaseAddress);
            
            var response = await _httpClient.GetAsync("api/Auth/health");
            
            _logger.LogInformation("📡 Health check response: {StatusCode}", response.StatusCode);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("✅ Server health check successful: {Content}", content);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ Server health check failed: {StatusCode}, {Content}", response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Exception during server health check");
            return false;
        }
    }
} 