using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;

namespace ClipTitle.Services
{
    public class ClipboardMonitor
    {
        private readonly ILogger<ClipboardMonitor> _logger;
        private readonly IMarkdownConverter _markdownConverter;
        private readonly IOllamaService _ollamaService;
        private readonly INotificationService _notificationService;
        private readonly IFileService _fileService;
        private readonly ISettingsService _settingsService;
        
        private IntPtr _hwnd;
        private bool _isPaused;
        private string? _lastClipboardHash;
        private Window? _window;
        private HwndSource? _source;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private const int WM_CLIPBOARDUPDATE = 0x031D;

        public bool IsPaused => _isPaused;

        public ClipboardMonitor(
            ILogger<ClipboardMonitor> logger,
            IMarkdownConverter markdownConverter,
            IOllamaService ollamaService,
            INotificationService notificationService,
            IFileService fileService,
            ISettingsService settingsService)
        {
            _logger = logger;
            _markdownConverter = markdownConverter;
            _ollamaService = ollamaService;
            _notificationService = notificationService;
            _fileService = fileService;
            _settingsService = settingsService;
        }

        public void Start(Window window)
        {
            _window = window;
            var helper = new WindowInteropHelper(window);
            _hwnd = helper.Handle;
            
            if (_hwnd == IntPtr.Zero)
            {
                // Window not yet created, wait for it
                window.SourceInitialized += Window_SourceInitialized;
            }
            else
            {
                StartMonitoring();
            }
        }

        private void Window_SourceInitialized(object? sender, EventArgs e)
        {
            if (_window != null)
            {
                var helper = new WindowInteropHelper(_window);
                _hwnd = helper.Handle;
                StartMonitoring();
            }
        }

        private void StartMonitoring()
        {
            if (_hwnd == IntPtr.Zero)
            {
                Console.WriteLine("[ClipboardMonitor] ERROR: Window handle is zero!");
                return;
            }
            
            Console.WriteLine($"[ClipboardMonitor] Starting monitoring with handle: {_hwnd}");
            
            if (!AddClipboardFormatListener(_hwnd))
            {
                _logger.LogError("Failed to add clipboard format listener");
                Console.WriteLine("[ClipboardMonitor] ERROR: Failed to add clipboard format listener!");
                return;
            }

            _source = HwndSource.FromHwnd(_hwnd);
            _source?.AddHook(WndProc);
            
            _logger.LogInformation("Clipboard monitoring started");
            Console.WriteLine("[ClipboardMonitor] ✅ Clipboard monitoring started successfully!");
        }

        public void Stop()
        {
            if (_hwnd != IntPtr.Zero)
            {
                RemoveClipboardFormatListener(_hwnd);
                _source?.RemoveHook(WndProc);
                _hwnd = IntPtr.Zero;
            }
            _logger.LogInformation("Clipboard monitoring stopped");
        }

        public void TogglePause()
        {
            _isPaused = !_isPaused;
            _logger.LogInformation($"Clipboard monitoring {(_isPaused ? "paused" : "resumed")}");
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE && !_isPaused)
            {
                Task.Run(async () => await ProcessClipboardChangeAsync());
                handled = true;
            }
            return IntPtr.Zero;
        }

        private async Task ProcessClipboardChangeAsync()
        {
            try
            {
                Console.WriteLine("[ClipboardMonitor] Clipboard change detected!");
                
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (!Clipboard.ContainsText())
                    {
                        Console.WriteLine("[ClipboardMonitor] No text in clipboard, ignoring.");
                        return;
                    }
                    
                    Console.WriteLine("[ClipboardMonitor] Text found in clipboard.");

                    string? markdownContent = null;
                    string? plainText = null;

                    // Try to get HTML content first for better formatting preservation
                    if (Clipboard.ContainsText(TextDataFormat.Html))
                    {
                        var html = Clipboard.GetText(TextDataFormat.Html);
                        if (!string.IsNullOrEmpty(html))
                        {
                            markdownContent = _markdownConverter.ConvertHtmlToMarkdown(html);
                        }
                    }

                    // Fall back to plain text if no HTML or conversion failed
                    if (string.IsNullOrEmpty(markdownContent))
                    {
                        plainText = Clipboard.GetText();
                        if (string.IsNullOrEmpty(plainText))
                        {
                            return;
                        }
                        markdownContent = _markdownConverter.ConvertPlainTextToMarkdown(plainText);
                    }

                    // Check for duplicate
                    var contentHash = ComputeHash(markdownContent);
                    if (contentHash == _lastClipboardHash)
                    {
                        _logger.LogDebug("Duplicate clipboard content ignored");
                        return;
                    }
                    _lastClipboardHash = contentHash;

                    // Check minimum length
                    var settings = await _settingsService.GetSettingsAsync();
                    Console.WriteLine($"[ClipboardMonitor] Settings loaded - SaveDir: {settings.SaveDirectory}, AutoAppend: {settings.AutoAppend}, AskEveryTime: {settings.AskEveryTime}");
                    
                    if (markdownContent.Length < settings.MinimumLength)
                    {
                        Console.WriteLine($"[ClipboardMonitor] Content too short ({markdownContent.Length} < {settings.MinimumLength}), ignoring.");
                        return;
                    }

                    // Check ignore patterns
                    foreach (var pattern in settings.IgnorePatterns)
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(markdownContent, pattern))
                        {
                            _logger.LogDebug($"Content matches ignore pattern: {pattern}");
                            return;
                        }
                    }

                    // Generate title
                    string title = "Untitled";
                    try 
                    {
                        title = await _ollamaService.GenerateTitleAsync(markdownContent);
                        _logger.LogInformation($"Generated title: {title}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate title, using default");
                    }

                    // Process based on settings
                    _logger.LogInformation($"Processing with settings - AutoAppend: {settings.AutoAppend}, AskEveryTime: {settings.AskEveryTime}");
                    
                    if (settings.AutoAppend && !settings.AskEveryTime)
                    {
                        // Auto-append mode - no notification
                        _logger.LogInformation("Auto-appending to file...");
                        Console.WriteLine("[ClipboardMonitor] Auto-append mode - saving file...");
                        
                        try
                        {
                            var filePath = await _fileService.AppendToFileAsync(markdownContent, title);
                            Console.WriteLine($"[ClipboardMonitor] ✅ File saved to: {filePath}");
                            await _notificationService.ShowSuccessNotificationAsync($"Saved: {title}");
                        }
                        catch (Exception saveEx)
                        {
                            Console.WriteLine($"[ClipboardMonitor] ❌ Save failed: {saveEx.Message}");
                            _logger.LogError(saveEx, "Failed to save file");
                        }
                    }
                    else
                    {
                        // Show notification with action buttons
                        _logger.LogInformation("Showing notification window...");
                        await _notificationService.ShowClipboardNotificationAsync(title, markdownContent);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing clipboard change");
            }
        }

        private string ComputeHash(string content)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}