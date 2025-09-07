using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ClipTitle.Services;

public interface IOllamaService
{
    Task<string> GenerateTitleAsync(string content);
    Task<bool> TestConnectionAsync();
}

public class OllamaService : IOllamaService
{
    private readonly ILogger<OllamaService> _logger;
    private readonly ISettingsService _settingsService;
    private readonly IContentAnalyzer _contentAnalyzer;
    private readonly HttpClient _httpClient;

    public OllamaService(ILogger<OllamaService> logger, ISettingsService settingsService, IContentAnalyzer contentAnalyzer)
    {
        _logger = logger;
        _settingsService = settingsService;
        _contentAnalyzer = contentAnalyzer;
        _httpClient = new HttpClient();
    }

    public async Task<string> GenerateTitleAsync(string content)
    {
        try
        {
            var settings = await _settingsService.GetSettingsAsync();
            var ollamaSettings = settings.OllamaSettings;
            
            // Skip Ollama if disabled, use smart content analysis instead
            if (!ollamaSettings.Enabled)
            {
                _logger.LogInformation("Ollama is disabled, using content analysis for title generation");
                var analysis = _contentAnalyzer.AnalyzeContent(content);
                var smartTitle = _contentAnalyzer.GenerateSmartTitle(analysis);
                _logger.LogInformation($"Generated smart title: {smartTitle}");
                return smartTitle;
            }
            
            _httpClient.BaseAddress = new Uri(ollamaSettings.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromMilliseconds(ollamaSettings.TimeoutMs);

            var prompt = BuildPrompt(content, ollamaSettings.TitlePromptTemplate);
            
            var request = new
            {
                model = ollamaSettings.Model,
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.3,
                    top_p = 0.9
                }
            };

            var json = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Try with retry
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(ollamaSettings.TimeoutMs));
                    var response = await _httpClient.PostAsync("/api/generate", httpContent, cts.Token);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = await response.Content.ReadAsStringAsync();
                        var responseObj = JsonSerializer.Deserialize<OllamaResponse>(responseJson);
                        
                        if (!string.IsNullOrWhiteSpace(responseObj?.Response))
                        {
                            return CleanTitle(responseObj.Response);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning($"Ollama request timed out (attempt {attempt + 1})");
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning($"Ollama request failed (attempt {attempt + 1}): {ex.Message}");
                }
                
                if (attempt == 0)
                {
                    await Task.Delay(500); // Brief delay before retry
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate title with Ollama");
        }

        // Fallback to smart content analysis
        var fallbackAnalysis = _contentAnalyzer.AnalyzeContent(content);
        return _contentAnalyzer.GenerateSmartTitle(fallbackAnalysis);
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var settings = await _settingsService.GetSettingsAsync();
            var ollamaSettings = settings.OllamaSettings;
            
            _httpClient.BaseAddress = new Uri(ollamaSettings.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(5);

            var response = await _httpClient.GetAsync("/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test Ollama connection");
            return false;
        }
    }

    private string BuildPrompt(string content, string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            template = @"Generate a concise title for the following content.
Requirements:
- 3-8 words maximum
- Title Case
- No quotes or trailing punctuation
- Be specific and descriptive
- If it's code, mention the language or technology

Content:
{content}

Title:";
        }

        // Truncate content if too long
        var truncatedContent = content.Length > 500 
            ? content.Substring(0, 500) + "..." 
            : content;

        return template.Replace("{content}", truncatedContent);
    }

    private string CleanTitle(string title)
    {
        // Remove quotes
        title = title.Trim('"', '\'', ' ', '\n', '\r');
        
        // Remove trailing punctuation
        title = System.Text.RegularExpressions.Regex.Replace(title, @"[.,;:!?]+$", "");
        
        // Convert to Title Case if not already
        if (!System.Text.RegularExpressions.Regex.IsMatch(title, @"^[A-Z]"))
        {
            title = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title.ToLower());
        }
        
        // Limit to 8 words
        var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 8)
        {
            title = string.Join(" ", words.Take(8));
        }
        
        return title;
    }

    private string GenerateFallbackTitle(string content)
    {
        // Get first non-empty line
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return "Untitled Clip";
        }

        var firstLine = lines[0].Trim();
        
        // Remove markdown formatting
        firstLine = System.Text.RegularExpressions.Regex.Replace(firstLine, @"^#+\s*", "");
        firstLine = System.Text.RegularExpressions.Regex.Replace(firstLine, @"\*{1,2}([^*]+)\*{1,2}", "$1");
        firstLine = System.Text.RegularExpressions.Regex.Replace(firstLine, @"\[([^\]]+)\]\([^)]+\)", "$1");
        
        // Limit to 8 words
        var words = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 8)
        {
            firstLine = string.Join(" ", words.Take(8));
        }
        
        return string.IsNullOrWhiteSpace(firstLine) ? "Untitled Clip" : firstLine;
    }

    private class OllamaResponse
    {
        public string? Response { get; set; }
        public string? Model { get; set; }
        public bool Done { get; set; }
    }
}