using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace HidaSushi.Server.Services;

public interface IFileUploadService
{
    Task<FileUploadResult> UploadImageAsync(IFormFile file, string category, string? filename = null);
    Task<bool> DeleteImageAsync(string imageUrl);
    Task<FileUploadResult> UploadMenuImageAsync(IFormFile file, string menuItemName);
    Task<FileUploadResult> UploadIngredientImageAsync(IFormFile file, string ingredientName);
    bool IsValidImageFile(IFormFile file);
    string GetImageUrl(string relativePath);
}

public class FileUploadService : IFileUploadService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileUploadService> _logger;
    private readonly IWebHostEnvironment _environment;
    
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private readonly string[] _allowedMimeTypes = { 
        "image/jpeg", "image/jpg", "image/pjpeg", // Various JPEG MIME types
        "image/png", 
        "image/webp", 
        "image/gif" 
    };
    private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB
    
    public FileUploadService(
        IConfiguration configuration, 
        ILogger<FileUploadService> logger,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
    }

    public async Task<FileUploadResult> UploadImageAsync(IFormFile file, string category, string? filename = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = "No file provided"
                };
            }

            if (!IsValidImageFile(file))
            {
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = "Invalid file type. Only JPG, PNG, WebP, and GIF files are allowed."
                };
            }

            if (file.Length > _maxFileSize)
            {
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = $"File size exceeds the maximum limit of {_maxFileSize / 1024 / 1024}MB"
                };
            }

            // Create upload directory if it doesn't exist
            var uploadsDirectory = Path.Combine(_environment.WebRootPath, "images", category);
            Directory.CreateDirectory(uploadsDirectory);

            // Generate unique filename if not provided
            if (string.IsNullOrEmpty(filename))
            {
                filename = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
            }
            else
            {
                filename = SanitizeFilename(filename);
                if (!filename.Contains('.'))
                {
                    filename += Path.GetExtension(file.FileName).ToLowerInvariant();
                }
            }

            var filePath = Path.Combine(uploadsDirectory, filename);
            var relativePath = "/" + Path.Combine("images", category, filename).Replace("\\", "/");

            // Save file to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("File uploaded successfully: {FilePath}", relativePath);

            return new FileUploadResult
            {
                Success = true,
                FilePath = relativePath,
                FileName = filename,
                FileSize = file.Length,
                ContentType = file.ContentType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
            return new FileUploadResult
            {
                Success = false,
                ErrorMessage = "An error occurred while uploading the file"
            };
        }
    }

    public async Task<FileUploadResult> UploadMenuImageAsync(IFormFile file, string menuItemName)
    {
        var sanitizedName = SanitizeFilename(menuItemName);
        var filename = $"menu-{sanitizedName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        return await UploadImageAsync(file, "menu", filename);
    }

    public async Task<FileUploadResult> UploadIngredientImageAsync(IFormFile file, string ingredientName)
    {
        var sanitizedName = SanitizeFilename(ingredientName);
        var filename = $"ingredient-{sanitizedName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        return await UploadImageAsync(file, "ingredients", filename);
    }

    public Task<bool> DeleteImageAsync(string imageUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(imageUrl))
                return Task.FromResult(false);

            // Convert URL to file path
            var relativePath = imageUrl.TrimStart('/');
            var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Image deleted successfully: {ImageUrl}", imageUrl);
                return Task.FromResult(true);
            }

            _logger.LogWarning("Image file not found for deletion: {ImageUrl}", imageUrl);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image: {ImageUrl}", imageUrl);
            return Task.FromResult(false);
        }
    }

    public bool IsValidImageFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("File validation failed: file is null or empty");
            return false;
        }

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        var contentType = file.ContentType?.ToLowerInvariant();
        
        _logger.LogInformation("File validation - FileName: {FileName}, Extension: {Extension}, ContentType: {ContentType}, Size: {Size}", 
            file.FileName, extension, contentType, file.Length);
        
        if (string.IsNullOrEmpty(extension) || !_allowedExtensions.Contains(extension))
        {
            _logger.LogWarning("File validation failed: invalid extension '{Extension}'. Allowed: {AllowedExtensions}", 
                extension, string.Join(", ", _allowedExtensions));
            return false;
        }

        // If MIME type is missing or empty, validate based on file extension only
        // This is common with some upload scenarios
        if (string.IsNullOrEmpty(contentType))
        {
            _logger.LogInformation("MIME type is missing, validating based on file extension only for {FileName}", file.FileName);
            return true; // Extension validation already passed
        }

        if (!_allowedMimeTypes.Contains(contentType))
        {
            _logger.LogWarning("File validation failed: invalid MIME type '{ContentType}'. Allowed: {AllowedMimeTypes}", 
                contentType, string.Join(", ", _allowedMimeTypes));
            return false;
        }

        _logger.LogInformation("File validation successful for {FileName}", file.FileName);
        return true;
    }

    public string GetImageUrl(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return "/images/placeholder.jpg";

        var baseUrl = _configuration.GetValue<string>("Domain:ApiUrl", "https://apimailbroker.ddns.net");
        return $"{baseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
    }

    private string SanitizeFilename(string filename)
    {
        // Remove invalid characters and replace spaces with hyphens
        var sanitized = Regex.Replace(filename, @"[^a-zA-Z0-9\-_.]", "-");
        sanitized = Regex.Replace(sanitized, @"-+", "-"); // Replace multiple hyphens with single
        return sanitized.Trim('-').ToLowerInvariant();
    }
}

public class FileUploadResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public long FileSize { get; set; }
    public string? ContentType { get; set; }
    public string? ErrorMessage { get; set; }
}

// Upload request models
public class ImageUploadRequest
{
    public IFormFile File { get; set; } = null!;
    public string Category { get; set; } = string.Empty;
    public string? Name { get; set; }
}

public class MenuImageUploadRequest
{
    public IFormFile File { get; set; } = null!;
    public string MenuItemName { get; set; } = string.Empty;
    public int? MenuItemId { get; set; }
}

public class IngredientImageUploadRequest
{
    public IFormFile File { get; set; } = null!;
    public string IngredientName { get; set; } = string.Empty;
    public int? IngredientId { get; set; }
} 