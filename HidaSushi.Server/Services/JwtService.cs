using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HidaSushi.Shared.Models;

namespace HidaSushi.Server.Services;

public class JwtService
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
        _secretKey = _configuration["Jwt:SecretKey"] ?? "HidaSushi-Super-Secret-Key-For-Admin-Auth-2024!";
        _issuer = _configuration["Jwt:Issuer"] ?? "HidaSushi";
        _audience = _configuration["Jwt:Audience"] ?? "HidaSushiUsers";
        _expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "480"); // 8 hours default
    }

    public LoginResponse GenerateToken(string username)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);
        var expiresAt = DateTime.UtcNow.AddMinutes(_expirationMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("Username", username)
            }),
            Expires = expiresAt,
            Issuer = _issuer,
            Audience = _audience,  // FIXED: Use correct audience from configuration
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return new LoginResponse
        {
            Success = true,
            Token = tokenString,
            ExpiresAt = expiresAt,
            Message = "Login successful"
        };
    }

    public bool ValidateCredentials(string username, string password)
    {
        // Simple hardcoded credentials for MVP
        // In production, this would check against a database
        var validCredentials = new Dictionary<string, string>
        {
            { "admin", "HidaSushi2024!" },
            { "jonathan", "ChefJonathan123!" },
            { "kitchen", "Kitchen2024!" }
        };

        return validCredentials.ContainsKey(username.ToLower()) && 
               validCredentials[username.ToLower()] == password;
    }

    public LoginResponse GenerateCustomerToken(string email, int customerId, string fullName)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);
        var expiresAt = DateTime.UtcNow.AddMinutes(_expirationMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Role, "Customer"),
                new Claim(ClaimTypes.Email, email),
                new Claim("CustomerId", customerId.ToString()),
                new Claim("FullName", fullName),
                new Claim("UserType", "Customer")
            }),
            Expires = expiresAt,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return new LoginResponse
        {
            Success = true,
            Token = tokenString,
            ExpiresAt = expiresAt,
            Message = "Customer login successful"
        };
    }
} 