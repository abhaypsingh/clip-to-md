using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ClipTitle.Services;

public interface ISettingsService
{
    Task<AppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
    Task LoadSettingsAsync();
}

public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsPath;
    private AppSettings? _cachedSettings;
    private readonly JsonSerializerOptions _jsonOptions;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDataPath = Path.Combine(localAppData, "ClipTitle");
        
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }
        
        _settingsPath = Path.Combine(appDataPath, "settings.json");
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        if (_cachedSettings == null)
        {
            await LoadSettingsAsync();
        }
        return _cachedSettings ?? GetDefaultSettings();
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(_settingsPath, json);
            _cachedSettings = settings;
            _logger.LogInformation("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            throw;
        }
    }

    public async Task LoadSettingsAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                _logger.LogInformation("Settings loaded successfully");
            }
            else
            {
                _cachedSettings = GetDefaultSettings();
                await SaveSettingsAsync(_cachedSettings);
                _logger.LogInformation("Created default settings");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings, using defaults");
            _cachedSettings = GetDefaultSettings();
        }
    }

    private AppSettings GetDefaultSettings()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var defaultSaveDir = Path.Combine(documentsPath, "Clips");
        
        return new AppSettings
        {
            SaveDirectory = defaultSaveDir,
            AskEveryTime = true,
            AutoAppend = false,
            LastFilePath = null,
            MinimumLength = 5,
            IgnorePatterns = new[] { @"^\d{6}$" }, // Ignore 6-digit OTP codes
            OllamaSettings = new OllamaSettings
            {
                Enabled = false, // Disabled by default - user must opt-in
                BaseUrl = "http://localhost:11434",
                Model = "llama3:8b",
                TimeoutMs = 5000,
                TitlePromptTemplate = null // Will use default in OllamaService
            }
        };
    }
}

public class AppSettings
{
    public string SaveDirectory { get; set; } = string.Empty;
    public bool AskEveryTime { get; set; } = true;
    public bool AutoAppend { get; set; } = false;
    public string? LastFilePath { get; set; }
    public int MinimumLength { get; set; } = 5;
    public string[] IgnorePatterns { get; set; } = Array.Empty<string>();
    public OllamaSettings OllamaSettings { get; set; } = new();
}

public class OllamaSettings
{
    public bool Enabled { get; set; } = false; // Disabled by default
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3:8b";
    public int TimeoutMs { get; set; } = 5000;
    public string? TitlePromptTemplate { get; set; }
}