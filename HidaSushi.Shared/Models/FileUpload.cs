namespace HidaSushi.Shared.Models;

public class FileUploadResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public long FileSize { get; set; }
    public string? ContentType { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ImageUploadRequest
{
    public string Category { get; set; } = string.Empty;
    public string? Name { get; set; }
}

public class MenuImageUploadRequest
{
    public string MenuItemName { get; set; } = string.Empty;
    public int? MenuItemId { get; set; }
}

public class IngredientImageUploadRequest
{
    public string IngredientName { get; set; } = string.Empty;
    public int? IngredientId { get; set; }
} 