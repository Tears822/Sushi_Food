using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HidaSushi.Shared.Models;
using HidaSushi.Server.Services;

namespace HidaSushi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(JwtService jwtService, ILogger<AuthController> logger)
    {
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("login")]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("🔐 Login request received for username: {Username}", request?.Username ?? "null");
        
        try
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new LoginResponse 
                { 
                    Success = false, 
                    Message = "Username and password are required" 
                });
            }

            if (_jwtService.ValidateCredentials(request.Username, request.Password))
            {
                var response = _jwtService.GenerateToken(request.Username);
                _logger.LogInformation("Admin user {Username} logged in successfully", request.Username);
                return Ok(response);
            }

            _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
            return Unauthorized(new LoginResponse 
            { 
                Success = false, 
                Message = "Invalid username or password" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login attempt for username: {Username}", request.Username);
            return StatusCode(500, new LoginResponse 
            { 
                Success = false, 
                Message = "An error occurred during login" 
            });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public ActionResult Logout()
    {
        // For JWT, logout is typically handled client-side by removing the token
        // But we can log it for audit purposes
        var username = User.Identity?.Name ?? "Unknown";
        _logger.LogInformation("Admin user {Username} logged out", username);
        
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("validate")]
    [Authorize]
    public ActionResult<AdminUser> ValidateToken()
    {
        // This endpoint can be used to validate if a token is still valid
        var username = User.Identity?.Name ?? "Unknown";
        return Ok(new AdminUser { Username = username, Role = "Admin" });
    }

    [HttpGet("health")]
    public ActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            server = "HidaSushi Backend",
            version = "1.0.0"
        });
    }

    [HttpGet("credentials")]
    public ActionResult GetTestCredentials()
    {
        // For development/testing - remove in production
        return Ok(new
        {
            message = "Test credentials for HIDA SUSHI Admin",
            credentials = new[]
            {
                new { username = "admin", password = "HidaSushi2024!", role = "Main Admin" },
                new { username = "jonathan", password = "ChefJonathan123!", role = "Chef" },
                new { username = "kitchen", password = "Kitchen2024!", role = "Kitchen Staff" }
            },
            note = "Use any of these credentials to access the admin dashboard"
        });
    }
} 