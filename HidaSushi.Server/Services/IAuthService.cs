namespace HidaSushi.Server.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password);
    Task<bool> ValidateTokenAsync(string token);
    string GenerateJwtToken(string username, string role);
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Message { get; set; }
    public string? Username { get; set; }
    public string? Role { get; set; }
} 