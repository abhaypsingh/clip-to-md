using System;
using System.IO;
using System.Linq;
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
    private readonly IContentAnalyzer _contentAnalyzer;

    public FileService(
        ILogger<FileService> logger,
        ISettingsService settingsService,
        IContentAnalyzer contentAnalyzer)
    {
        _logger = logger;
        _settingsService = settingsService;
        _contentAnalyzer = contentAnalyzer;
    }

    public async Task<string> CreateNewFileAsync(string content, string? title = null)
    {
        try
        {
            Console.WriteLine($"[FileService] Creating new file with title: {title}");
            
            var settings = await _settingsService.GetSettingsAsync();
            Console.WriteLine($"[FileService] Save directory: {settings.SaveDirectory}");
            
            // Ensure save directory exists
            if (!Directory.Exists(settings.SaveDirectory))
            {
                Console.WriteLine($"[FileService] Creating directory: {settings.SaveDirectory}");
                Directory.CreateDirectory(settings.SaveDirectory);
            }

            // Generate filename
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var slugifiedTitle = SlugifyTitle(title ?? "untitled");
            var fileName = $"{timestamp}-{slugifiedTitle}.md";
            var filePath = Path.Combine(settings.SaveDirectory, fileName);

            // Analyze content for metadata
            var analysis = _contentAnalyzer.AnalyzeContent(content);
            
            // Prepare content with YAML front matter including analysis metadata
            var fileContent = new StringBuilder();
            fileContent.AppendLine("---");
            fileContent.AppendLine($"title: \"{EscapeYamlString(title ?? "Untitled")}\"");
            fileContent.AppendLine("source: \"clipboard\"");
            fileContent.AppendLine($"created: \"{DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}\"");
            fileContent.AppendLine($"content_type: \"{analysis.ContentType}\"");
            
            if (!string.IsNullOrEmpty(analysis.Domain))
                fileContent.AppendLine($"domain: \"{analysis.Domain}\"");
            
            if (!string.IsNullOrEmpty(analysis.Language))
                fileContent.AppendLine($"language: \"{analysis.Language}\"");
            
            if (analysis.Keywords.Any())
                fileContent.AppendLine($"keywords: [{string.Join(", ", analysis.Keywords.Take(5).Select(k => $"\"{k}\""))}]");
            
            fileContent.AppendLine($"word_count: {analysis.WordCount}");
            fileContent.AppendLine($"line_count: {analysis.LineCount}");
            fileContent.AppendLine("---");
            fileContent.AppendLine();
            fileContent.AppendLine(content);

            // Atomic write
            await WriteFileAtomicallyAsync(filePath, fileContent.ToString());
            
            Console.WriteLine($"[FileService] File written to: {filePath}");

            // Update last file path
            settings.LastFilePath = filePath;
            await _settingsService.SaveSettingsAsync(settings);

            _logger.LogInformation($"Created new file: {filePath}");
            _logger.LogInformation($"Saved: {fileName}");
            Console.WriteLine($"[FileService] ✅ Successfully saved: {fileName}");

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create new file");
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
                _logger.LogInformation($"Appended to: {Path.GetFileName(filePath)}");
            }

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append to file");
            _logger.LogError(ex, "Failed to append clip to file");
            throw;
        }
    }

    private async Task WriteFileAtomicallyAsync(string filePath, string content)
    {
        // For better compatibility, use a simpler approach with retry logic
        const int maxRetries = 3;
        const int retryDelayMs = 100;
        
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                // Use simple write with UTF-8 encoding (no BOM)
                await File.WriteAllTextAsync(filePath, content, new UTF8Encoding(false));
                return; // Success
            }
            catch (IOException ex) when (attempt < maxRetries - 1)
            {
                _logger.LogWarning($"File write attempt {attempt + 1} failed: {ex.Message}. Retrying...");
                await Task.Delay(retryDelayMs);
            }
        }
        
        // If all retries failed, throw the last exception
        throw new IOException($"Failed to write file after {maxRetries} attempts: {filePath}");
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