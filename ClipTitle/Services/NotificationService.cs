using System;
using System.Threading.Tasks;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Extensions.Logging;

namespace ClipTitle.Services;

public interface INotificationService
{
    Task ShowClipboardNotificationAsync(string title, string content);
    Task ShowSuccessNotificationAsync(string message);
    Task ShowErrorNotificationAsync(string message);
}

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly AppNotificationManager _notificationManager;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
        _notificationManager = AppNotificationManager.Default;
    }

    public async Task ShowClipboardNotificationAsync(string title, string content)
    {
        try
        {
            // Create preview text (first 100 chars)
            var preview = content.Length > 100 
                ? content.Substring(0, 100) + "..." 
                : content;
            
            // Remove markdown formatting for preview
            preview = System.Text.RegularExpressions.Regex.Replace(preview, @"[#*`\[\]()]", "");
            preview = preview.Replace("\n", " ").Trim();

            var notification = new AppNotificationBuilder()
                .AddText($"📋 {title}")
                .AddText(preview)
                .AddButton(new AppNotificationButton("Append")
                    .AddArgument("action", "append")
                    .AddArgument("data", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content)))
                    .AddArgument("title", title))
                .AddButton(new AppNotificationButton("New File")
                    .AddArgument("action", "new")
                    .AddArgument("data", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content)))
                    .AddArgument("title", title))
                .SetDuration(AppNotificationDuration.Default)
                .BuildNotification();

            _notificationManager.Show(notification);
            _logger.LogInformation($"Showed clipboard notification with title: {title}");
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show clipboard notification");
        }
    }

    public async Task ShowSuccessNotificationAsync(string message)
    {
        try
        {
            var notification = new AppNotificationBuilder()
                .AddText("✅ ClipTitle")
                .AddText(message)
                .SetDuration(AppNotificationDuration.Default)
                .BuildNotification();

            _notificationManager.Show(notification);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show success notification");
        }
    }

    public async Task ShowErrorNotificationAsync(string message)
    {
        try
        {
            var notification = new AppNotificationBuilder()
                .AddText("❌ ClipTitle Error")
                .AddText(message)
                .AddButton(new AppNotificationButton("Open Settings")
                    .AddArgument("action", "settings"))
                .SetDuration(AppNotificationDuration.Long)
                .BuildNotification();

            _notificationManager.Show(notification);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show error notification");
        }
    }
}