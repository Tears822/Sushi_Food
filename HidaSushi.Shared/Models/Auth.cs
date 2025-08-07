using System.ComponentModel.DataAnnotations;

namespace HidaSushi.Shared.Models;

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = "";
    
    [Required]
    public string Password { get; set; } = "";
}

public class LoginResponse
{
    public bool Success { get; set; }
    public string Token { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}

public class AdminUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Admin";
    public string FullName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string? PermissionsJson { get; set; } // JSON for granular permissions
    public DateTime? LastLogin { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
} 