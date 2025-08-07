namespace HidaSushi.Client.Services;

public interface IToastService
{
    event Action<ToastMessage>? OnToastShow;
    void ShowSuccess(string title, string message, int durationMs = 4000);
    void ShowError(string title, string message, int durationMs = 5000);
    void ShowInfo(string title, string message, int durationMs = 3000);
    void ShowWarning(string title, string message, int durationMs = 4000);
}

public class ToastService : IToastService
{
    public event Action<ToastMessage>? OnToastShow;

    public void ShowSuccess(string title, string message, int durationMs = 4000)
    {
        var toast = new ToastMessage
        {
            Id = Guid.NewGuid().ToString(),
            Type = ToastType.Success,
            Title = title,
            Message = message,
            Duration = durationMs,
            Icon = "✅"
        };
        OnToastShow?.Invoke(toast);
    }

    public void ShowError(string title, string message, int durationMs = 5000)
    {
        var toast = new ToastMessage
        {
            Id = Guid.NewGuid().ToString(),
            Type = ToastType.Error,
            Title = title,
            Message = message,
            Duration = durationMs,
            Icon = "❌"
        };
        OnToastShow?.Invoke(toast);
    }

    public void ShowInfo(string title, string message, int durationMs = 3000)
    {
        var toast = new ToastMessage
        {
            Id = Guid.NewGuid().ToString(),
            Type = ToastType.Info,
            Title = title,
            Message = message,
            Duration = durationMs,
            Icon = "ℹ️"
        };
        OnToastShow?.Invoke(toast);
    }

    public void ShowWarning(string title, string message, int durationMs = 4000)
    {
        var toast = new ToastMessage
        {
            Id = Guid.NewGuid().ToString(),
            Type = ToastType.Warning,
            Title = title,
            Message = message,
            Duration = durationMs,
            Icon = "⚠️"
        };
        OnToastShow?.Invoke(toast);
    }
}

public class ToastMessage
{
    public string Id { get; set; } = string.Empty;
    public ToastType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int Duration { get; set; } = 4000;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
} 