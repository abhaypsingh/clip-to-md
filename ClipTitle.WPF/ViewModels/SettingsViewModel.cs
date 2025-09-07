using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using ClipTitle.Services;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace ClipTitle.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IOllamaService _ollamaService;
    private AppSettings? _settings;

    [ObservableProperty]
    private string _saveDirectory = string.Empty;

    [ObservableProperty]
    private bool _askEveryTime = true;

    [ObservableProperty]
    private bool _autoAppend = false;

    [ObservableProperty]
    private int _minimumLength = 5;

    [ObservableProperty]
    private bool _ollamaEnabled = false;

    [ObservableProperty]
    private string _ollamaBaseUrl = "http://localhost:11434";

    [ObservableProperty]
    private string _ollamaModel = "llama3:8b";

    [ObservableProperty]
    private int _ollamaTimeout = 5000;

    [ObservableProperty]
    private string _testConnectionStatus = string.Empty;

    [ObservableProperty]
    private bool _isTestingConnection = false;

    public SettingsViewModel(ISettingsService settingsService, IOllamaService ollamaService)
    {
        _settingsService = settingsService;
        _ollamaService = ollamaService;
        _ = LoadSettingsAsync();
    }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        _settings = await _settingsService.GetSettingsAsync();
        
        SaveDirectory = _settings.SaveDirectory;
        AskEveryTime = _settings.AskEveryTime;
        AutoAppend = _settings.AutoAppend;
        MinimumLength = _settings.MinimumLength;
        OllamaEnabled = _settings.OllamaSettings.Enabled;
        OllamaBaseUrl = _settings.OllamaSettings.BaseUrl;
        OllamaModel = _settings.OllamaSettings.Model;
        OllamaTimeout = _settings.OllamaSettings.TimeoutMs;
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (_settings == null)
        {
            _settings = new AppSettings();
        }

        _settings.SaveDirectory = SaveDirectory;
        _settings.AskEveryTime = AskEveryTime;
        _settings.AutoAppend = AutoAppend;
        _settings.MinimumLength = MinimumLength;
        _settings.OllamaSettings.Enabled = OllamaEnabled;
        _settings.OllamaSettings.BaseUrl = OllamaBaseUrl;
        _settings.OllamaSettings.Model = OllamaModel;
        _settings.OllamaSettings.TimeoutMs = OllamaTimeout;

        await _settingsService.SaveSettingsAsync(_settings);
        TestConnectionStatus = "Settings saved successfully!";
    }

    [RelayCommand]
    private async Task BrowseFolderAsync()
    {
        // Use Windows Forms FolderBrowserDialog for WPF
        using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
        {
            dialog.Description = "Select a folder to save markdown files";
            dialog.ShowNewFolderButton = true;
            dialog.SelectedPath = SaveDirectory;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SaveDirectory = dialog.SelectedPath;
            }
        }
        
        await Task.CompletedTask; // Keep async signature
    }

    [RelayCommand]
    private async Task TestOllamaConnectionAsync()
    {
        IsTestingConnection = true;
        TestConnectionStatus = "Testing connection...";

        try
        {
            var isConnected = await _ollamaService.TestConnectionAsync();
            TestConnectionStatus = isConnected 
                ? "✅ Connection successful!" 
                : "❌ Connection failed. Make sure Ollama is running.";
        }
        catch (Exception ex)
        {
            TestConnectionStatus = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    partial void OnAutoAppendChanged(bool value)
    {
        if (value)
        {
            AskEveryTime = false;
        }
    }

    partial void OnAskEveryTimeChanged(bool value)
    {
        if (value)
        {
            AutoAppend = false;
        }
    }
}