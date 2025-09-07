using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using ClipTitle.Services;
using Microsoft.UI.Xaml;
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
        _settings.OllamaSettings.BaseUrl = OllamaBaseUrl;
        _settings.OllamaSettings.Model = OllamaModel;
        _settings.OllamaSettings.TimeoutMs = OllamaTimeout;

        await _settingsService.SaveSettingsAsync(_settings);
        TestConnectionStatus = "Settings saved successfully!";
    }

    [RelayCommand]
    private async Task BrowseFolderAsync()
    {
        var picker = new FolderPicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add("*");

        // Get the window handle
        if (App.ServiceProvider?.GetRequiredService<Window>() is Window window)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        }

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            SaveDirectory = folder.Path;
        }
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