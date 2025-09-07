using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ClipTitle.Services;

public interface IFileService
{
    Task<string> CreateNewFileAsync(string content, string? title = null);
    Task<string> AppendToFileAsync(string content, string? title = null);
}

public class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;
    private readonly ISettingsService _settingsService;
    private readonly INotificationService _notificationService;

    public FileService(
        ILogger<FileService> logger,
        ISettingsService settingsService,
        INotificationService notificationService)
    {
        _logger = logger;
        _settingsService = settingsService;
        _notificationService = notificationService;
    }

    public async Task<string> CreateNewFileAsync(string content, string? title = null)
    {
        try
        {
            var settings = await _settingsService.GetSettingsAsync();
            
            // Ensure save directory exists
            if (!Directory.Exists(settings.SaveDirectory))
            {
                Directory.CreateDirectory(settings.SaveDirectory);
            }

            // Generate filename
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var slugifiedTitle = SlugifyTitle(title ?? "untitled");
            var fileName = $"{timestamp}-{slugifiedTitle}.md";
            var filePath = Path.Combine(settings.SaveDirectory, fileName);

            // Prepare content with YAML front matter
            var fileContent = new StringBuilder();
            fileContent.AppendLine("---");
            fileContent.AppendLine($"title: \"{EscapeYamlString(title ?? "Untitled")}\"");
            fileContent.AppendLine("source: \"clipboard\"");
            fileContent.AppendLine($"created: \"{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}\"");
            fileContent.AppendLine("---");
            fileContent.AppendLine();
            fileContent.AppendLine(content);

            // Atomic write
            await WriteFileAtomicallyAsync(filePath, fileContent.ToString());

            // Update last file path
            settings.LastFilePath = filePath;
            await _settingsService.SaveSettingsAsync(settings);

            _logger.LogInformation($"Created new file: {filePath}");
            await _notificationService.ShowSuccessNotificationAsync($"Saved: {fileName}");

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create new file");
            await _notificationService.ShowErrorNotificationAsync("Failed to save clip to file");
            throw;
        }
    }

    public async Task<string> AppendToFileAsync(string content, string? title = null)
    {
        try
        {
            var settings = await _settingsService.GetSettingsAsync();
            
            // Get or create last file
            string filePath;
            if (string.IsNullOrEmpty(settings.LastFilePath) || !File.Exists(settings.LastFilePath))
            {
                // Create first file
                return await CreateNewFileAsync(content, title);
            }
            else
            {
                filePath = settings.LastFilePath;
            }

            // Read existing content
            var existingContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);

            // Prepare append content
            var appendContent = new StringBuilder();
            appendContent.AppendLine();
            appendContent.AppendLine("---");
            appendContent.AppendLine();
            appendContent.AppendLine($"## {title ?? "Untitled"}");
            appendContent.AppendLine();
            appendContent.AppendLine($"*Clipped:* {DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}");
            appendContent.AppendLine();
            appendContent.AppendLine(content);

            // Atomic write
            await WriteFileAtomicallyAsync(filePath, existingContent + appendContent.ToString());

            _logger.LogInformation($"Appended to file: {filePath}");
            
            if (settings.AutoAppend && !settings.AskEveryTime)
            {
                // Silent success for auto-append mode
                _logger.LogDebug("Auto-appended silently");
            }
            else
            {
                await _notificationService.ShowSuccessNotificationAsync($"Appended to: {Path.GetFileName(filePath)}");
            }

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append to file");
            await _notificationService.ShowErrorNotificationAsync("Failed to append clip to file");
            throw;
        }
    }

    private async Task WriteFileAtomicallyAsync(string filePath, string content)
    {
        // Write to temp file first
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, content, new UTF8Encoding(false)); // UTF-8 without BOM
            
            // Move temp file to target (atomic on same drive)
            if (File.Exists(filePath))
            {
                File.Replace(tempFile, filePath, null);
            }
            else
            {
                File.Move(tempFile, filePath);
            }
        }
        finally
        {
            // Clean up temp file if it still exists
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { }
            }
        }
    }

    private string SlugifyTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "untitled";
        }

        // Convert to lowercase
        var slug = title.ToLowerInvariant();

        // Replace spaces with hyphens
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");

        // Remove invalid characters
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");

        // Remove multiple consecutive hyphens
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\-{2,}", "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        // Limit length
        if (slug.Length > 80)
        {
            slug = slug.Substring(0, 80);
            slug = slug.TrimEnd('-');
        }

        return string.IsNullOrEmpty(slug) ? "untitled" : slug;
    }

    private string EscapeYamlString(string value)
    {
        // Escape quotes for YAML
        return value.Replace("\"", "\\\"");
    }
}