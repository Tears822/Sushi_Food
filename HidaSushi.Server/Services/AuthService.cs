using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HidaSushi.Server.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly JwtService _jwtService;

    public AuthService(IConfiguration configuration, JwtService jwtService)
    {
        _configuration = configuration;
        _jwtService = jwtService;
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        var validCredentials = new Dictionary<string, (string password, string role)>
        {
            { "admin", ("HidaSushi2024!", "Admin") },
            { "jonathan", ("ChefJonathan123!", "Chef") },
            { "kitchen", ("Kitchen2024!", "Kitchen") }
        };

        if (validCredentials.TryGetValue(username, out var credentials) && credentials.password == password)
        {
            var jwtResponse = _jwtService.GenerateToken(username);
            return new AuthResult
            {
                Success = true,
                Token = jwtResponse.Token,
                Username = username,
                Role = credentials.role,
                Message = jwtResponse.Message
            };
        }

        return new AuthResult
        {
            Success = false,
            Message = "Invalid username or password"
        };
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken _);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GenerateJwtToken(string username, string role)
    {
        var jwtResponse = _jwtService.GenerateToken(username);
        return jwtResponse.Token ?? string.Empty;
    }
} 