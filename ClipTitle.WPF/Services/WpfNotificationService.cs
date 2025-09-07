using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Hardcodet.Wpf.TaskbarNotification;
using ClipTitle.Views;

namespace ClipTitle.Services
{
    public class WpfNotificationService : INotificationService
    {
        private readonly ILogger<WpfNotificationService> _logger;
        private readonly IFileService _fileService;
        private readonly TaskbarIcon? _trayIcon;

        public WpfNotificationService(ILogger<WpfNotificationService> logger, IFileService fileService)
        {
            _logger = logger;
            _fileService = fileService;
            
            // Get tray icon reference from App
            var app = System.Windows.Application.Current as App;
            _trayIcon = app?.GetType().GetField("_notifyIcon", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(app) as TaskbarIcon;
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

                // Show notification with custom balloon
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        var notification = new ClipboardNotificationWindow(title, preview, content);
                        notification.AppendClicked += async (s, e) => await OnAppendClicked(content, title);
                        notification.NewFileClicked += async (s, e) => await OnNewFileClicked(content, title);
                        notification.Show();
                    }
                    catch (Exception winEx)
                    {
                        _logger.LogError(winEx, "Failed to show notification window");
                        Console.WriteLine($"[NotificationService] ❌ Failed to show window: {winEx.Message}");
                        
                        // Fallback: Try to save directly without UI
                        Task.Run(async () => await OnAppendClicked(content, title));
                    }
                });

                _logger.LogInformation($"Showed clipboard notification with title: {title}");
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
                _trayIcon?.ShowBalloonTip("ClipTitle", message, BalloonIcon.Info);
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
                _trayIcon?.ShowBalloonTip("ClipTitle Error", message, BalloonIcon.Error);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show error notification");
            }
        }

        private async Task OnAppendClicked(string content, string title)
        {
            try
            {
                Console.WriteLine($"[NotificationService] Append clicked - Title: {title}");
                var filePath = await _fileService.AppendToFileAsync(content, title);
                Console.WriteLine($"[NotificationService] ✅ Appended to: {filePath}");
                await ShowSuccessNotificationAsync($"Appended to: {System.IO.Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to append to file");
                Console.WriteLine($"[NotificationService] ❌ Append failed: {ex.Message}");
                await ShowErrorNotificationAsync($"Failed to save: {ex.Message}");
            }
        }

        private async Task OnNewFileClicked(string content, string title)
        {
            try
            {
                Console.WriteLine($"[NotificationService] New file clicked - Title: {title}");
                var filePath = await _fileService.CreateNewFileAsync(content, title);
                Console.WriteLine($"[NotificationService] ✅ Created new file: {filePath}");
                await ShowSuccessNotificationAsync($"Created: {System.IO.Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create new file");
                Console.WriteLine($"[NotificationService] ❌ New file failed: {ex.Message}");
                await ShowErrorNotificationAsync($"Failed to create file: {ex.Message}");
            }
        }
    }
}