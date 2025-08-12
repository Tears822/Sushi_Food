using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HidaSushi.Server.Services;
using HidaSushi.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace HidaSushi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for file uploads
public class FileUploadController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;
    private readonly HidaSushiDbContext _context;
    private readonly ILogger<FileUploadController> _logger;

    public FileUploadController(
        IFileUploadService fileUploadService, 
        HidaSushiDbContext context,
        ILogger<FileUploadController> logger)
    {
        _fileUploadService = fileUploadService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Upload an image for a menu item
    /// </summary>
    [HttpPost("menu")]
    public async Task<ActionResult<FileUploadResult>> UploadMenuImage([FromForm] MenuImageUploadRequest request)
    {
        try
        {
            if (request.File == null)
            {
                return BadRequest(new FileUploadResult 
                { 
                    Success = false, 
                    ErrorMessage = "No file provided" 
                });
            }

            if (string.IsNullOrEmpty(request.MenuItemName))
            {
                return BadRequest(new FileUploadResult 
                { 
                    Success = false, 
                    ErrorMessage = "Menu item name is required" 
                });
            }

            // Upload the image
            var result = await _fileUploadService.UploadMenuImageAsync(request.File, request.MenuItemName);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            // If MenuItemId is provided, update the database
            if (request.MenuItemId.HasValue)
            {
                var menuItem = await _context.SushiRolls.FindAsync(request.MenuItemId.Value);
                if (menuItem != null)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(menuItem.ImageUrl))
                    {
                        await _fileUploadService.DeleteImageAsync(menuItem.ImageUrl);
                    }

                    menuItem.ImageUrl = "/" + result.FilePath;
                    menuItem.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Updated menu item {MenuItemId} with new image: {ImageUrl}", 
                        request.MenuItemId, menuItem.ImageUrl);
                }
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading menu image for: {MenuItemName}", request.MenuItemName);
            return StatusCode(500, new FileUploadResult 
            { 
                Success = false, 
                ErrorMessage = "An error occurred while uploading the image" 
            });
        }
    }

    /// <summary>
    /// Upload an image for an ingredient
    /// </summary>
    [HttpPost("ingredient")]
    public async Task<ActionResult<FileUploadResult>> UploadIngredientImage([FromForm] IngredientImageUploadRequest request)
    {
        try
        {
            if (request.File == null)
            {
                return BadRequest(new FileUploadResult 
                { 
                    Success = false, 
                    ErrorMessage = "No file provided" 
                });
            }

            if (string.IsNullOrEmpty(request.IngredientName))
            {
                return BadRequest(new FileUploadResult 
                { 
                    Success = false, 
                    ErrorMessage = "Ingredient name is required" 
                });
            }

            // Upload the image
            var result = await _fileUploadService.UploadIngredientImageAsync(request.File, request.IngredientName);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            // If IngredientId is provided, update the database
            if (request.IngredientId.HasValue)
            {
                var ingredient = await _context.Ingredients.FindAsync(request.IngredientId.Value);
                if (ingredient != null)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(ingredient.ImageUrl))
                    {
                        await _fileUploadService.DeleteImageAsync(ingredient.ImageUrl);
                    }

                    ingredient.ImageUrl = "/" + result.FilePath;
                    ingredient.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Updated ingredient {IngredientId} with new image: {ImageUrl}", 
                        request.IngredientId, ingredient.ImageUrl);
                }
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading ingredient image for: {IngredientName}", request.IngredientName);
            return StatusCode(500, new FileUploadResult 
            { 
                Success = false, 
                ErrorMessage = "An error occurred while uploading the image" 
            });
        }
    }

    /// <summary>
    /// Upload a generic image
    /// </summary>
    [HttpPost("image")]
    public async Task<ActionResult<FileUploadResult>> UploadImage([FromForm] ImageUploadRequest request)
    {
        try
        {
            if (request.File == null)
            {
                return BadRequest(new FileUploadResult 
                { 
                    Success = false, 
                    ErrorMessage = "No file provided" 
                });
            }

            if (string.IsNullOrEmpty(request.Category))
            {
                request.Category = "general";
            }

            var result = await _fileUploadService.UploadImageAsync(request.File, request.Category, request.Name);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image");
            return StatusCode(500, new FileUploadResult 
            { 
                Success = false, 
                ErrorMessage = "An error occurred while uploading the image" 
            });
        }
    }

    /// <summary>
    /// Delete an image
    /// </summary>
    [HttpDelete]
    public async Task<ActionResult<bool>> DeleteImage([FromQuery] string imageUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return BadRequest("Image URL is required");
            }

            var result = await _fileUploadService.DeleteImageAsync(imageUrl);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image: {ImageUrl}", imageUrl);
            return StatusCode(500, false);
        }
    }

    /// <summary>
    /// Get upload guidelines
    /// </summary>
    [HttpGet("guidelines")]
    [AllowAnonymous]
    public ActionResult<object> GetUploadGuidelines()
    {
        return Ok(new
        {
            AllowedExtensions = new[] { "jpg", "jpeg", "png", "webp", "gif" },
            MaxFileSize = "5MB",
            MaxFileSizeBytes = 5 * 1024 * 1024,
            RecommendedDimensions = new
            {
                Menu = "800x600 pixels",
                Ingredients = "400x400 pixels"
            },
            AllowedCategories = new[] { "menu", "ingredients", "general" }
        });
    }
} 