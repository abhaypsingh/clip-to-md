using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.Extensions.Logging;

namespace ClipTitle.Services;

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
    private SUBCLASSPROC? _subclassProc;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, uint uIdSubclass, IntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool RemoveWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, uint uIdSubclass);

    [DllImport("comctl32.dll")]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    private delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, uint uIdSubclass, IntPtr dwRefData);

    private const int WM_CLIPBOARDUPDATE = 0x031D;

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
        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        _hwnd = windowHandle;
        
        if (!AddClipboardFormatListener(_hwnd))
        {
            _logger.LogError("Failed to add clipboard format listener");
            return;
        }

        // Hook into the window's message processing using subclassing
        _subclassProc = new SUBCLASSPROC(SubclassWndProc);
        if (!SetWindowSubclass(_hwnd, _subclassProc, 1, IntPtr.Zero))
        {
            _logger.LogError("Failed to subclass window");
            return;
        }
        
        _logger.LogInformation("Clipboard monitoring started");
    }

    public void Stop()
    {
        if (_hwnd != IntPtr.Zero)
        {
            RemoveClipboardFormatListener(_hwnd);
            if (_subclassProc != null)
            {
                RemoveWindowSubclass(_hwnd, _subclassProc, 1);
            }
            _hwnd = IntPtr.Zero;
        }
        _logger.LogInformation("Clipboard monitoring stopped");
    }

    public void TogglePause()
    {
        _isPaused = !_isPaused;
        _logger.LogInformation($"Clipboard monitoring {(_isPaused ? "paused" : "resumed")}");
    }

    private IntPtr SubclassWndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, uint uIdSubclass, IntPtr dwRefData)
    {
        if (uMsg == WM_CLIPBOARDUPDATE && !_isPaused)
        {
            Task.Run(async () => await ProcessClipboardChangeAsync());
        }
        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private async Task ProcessClipboardChangeAsync()
    {
        try
        {
            var dataPackageView = Clipboard.GetContent();
            if (dataPackageView == null || !dataPackageView.Contains(StandardDataFormats.Text))
            {
                return;
            }

            string? markdownContent = null;
            string? plainText = null;

            // Try to get HTML content first for better formatting preservation
            if (dataPackageView.Contains(StandardDataFormats.Html))
            {
                var html = await dataPackageView.GetHtmlFormatAsync();
                if (!string.IsNullOrEmpty(html))
                {
                    markdownContent = _markdownConverter.ConvertHtmlToMarkdown(html);
                }
            }

            // Fall back to plain text if no HTML or conversion failed
            if (string.IsNullOrEmpty(markdownContent))
            {
                plainText = await dataPackageView.GetTextAsync();
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
            if (markdownContent.Length < settings.MinimumLength)
            {
                return;
            }

            // Generate title
            var title = await _ollamaService.GenerateTitleAsync(markdownContent);

            // Process based on settings
            if (settings.AutoAppend && !settings.AskEveryTime)
            {
                // Auto-append mode - no notification
                await _fileService.AppendToFileAsync(markdownContent, title);
            }
            else
            {
                // Show notification with action buttons
                await _notificationService.ShowClipboardNotificationAsync(title, markdownContent);
            }
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