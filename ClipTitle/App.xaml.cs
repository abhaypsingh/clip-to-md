using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Linq;
using ClipTitle.Services;
using ClipTitle.ViewModels;
using ClipTitle.Views;
using Microsoft.Windows.AppNotifications;
using System.Threading.Tasks;
using H.NotifyIcon;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ClipTitle;

public partial class App : Application
{
    private Window? m_window;
    private IServiceProvider? _serviceProvider;
    private ClipboardMonitor? _clipboardMonitor;
    private TaskbarIcon? _trayIcon;

    public App()
    {
        this.InitializeComponent();
        ConfigureServices();
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var manager = AppNotificationManager.Default;
        manager.NotificationInvoked += OnNotificationInvoked;
        manager.Register();

        await InitializeServicesAsync();

        m_window = new MainWindow();
        
        // Start minimized to tray
        InitializeTrayIcon();
        
        // Start clipboard monitoring
        _clipboardMonitor = _serviceProvider?.GetRequiredService<ClipboardMonitor>();
        _clipboardMonitor?.Start(m_window);
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IOllamaService, OllamaService>();
        services.AddSingleton<IMarkdownConverter, MarkdownConverter>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ClipboardMonitor>();
        
        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddDebug();
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    private async Task InitializeServicesAsync()
    {
        if (_serviceProvider != null)
        {
            var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
            await settingsService.LoadSettingsAsync();
        }
    }

    private void InitializeTrayIcon()
    {
        _trayIcon = new TaskbarIcon();
        // _trayIcon.IconSource = new BitmapImage(new Uri("ms-appx:///Assets/ClipTitle.ico"));
        _trayIcon.ToolTipText = "ClipTitle";
        
        // Create context menu
        var contextMenu = new MenuFlyout();
        
        var toggleAutoAppendItem = new MenuFlyoutItem { Text = "Toggle Auto-append" };
        toggleAutoAppendItem.Click += async (s, e) => await ToggleAutoAppend();
        contextMenu.Items.Add(toggleAutoAppendItem);
        
        var openFolderItem = new MenuFlyoutItem { Text = "Open Save Folder" };
        openFolderItem.Click += async (s, e) => await OpenSaveFolder();
        contextMenu.Items.Add(openFolderItem);
        
        var openLastFileItem = new MenuFlyoutItem { Text = "Open Last File" };
        openLastFileItem.Click += async (s, e) => await OpenLastFile();
        contextMenu.Items.Add(openLastFileItem);
        
        contextMenu.Items.Add(new MenuFlyoutSeparator());
        
        var pauseItem = new MenuFlyoutItem { Text = "Pause Capture" };
        pauseItem.Click += (s, e) => TogglePause();
        contextMenu.Items.Add(pauseItem);
        
        var settingsItem = new MenuFlyoutItem { Text = "Settings" };
        settingsItem.Click += (s, e) => ShowSettings();
        contextMenu.Items.Add(settingsItem);
        
        contextMenu.Items.Add(new MenuFlyoutSeparator());
        
        var exitItem = new MenuFlyoutItem { Text = "Exit" };
        exitItem.Click += (s, e) => Exit();
        contextMenu.Items.Add(exitItem);
        
        _trayIcon.ContextFlyout = contextMenu;
        _trayIcon.LeftClickCommand = new RelayCommand(() => ShowSettings());
    }

    private async Task ToggleAutoAppend()
    {
        var settingsService = _serviceProvider?.GetRequiredService<ISettingsService>();
        if (settingsService != null)
        {
            var settings = await settingsService.GetSettingsAsync();
            settings.AutoAppend = !settings.AutoAppend;
            settings.AskEveryTime = !settings.AutoAppend;
            await settingsService.SaveSettingsAsync(settings);
        }
    }

    private async Task OpenSaveFolder()
    {
        var settingsService = _serviceProvider?.GetRequiredService<ISettingsService>();
        if (settingsService != null)
        {
            var settings = await settingsService.GetSettingsAsync();
            await Windows.System.Launcher.LaunchFolderPathAsync(settings.SaveDirectory);
        }
    }

    private async Task OpenLastFile()
    {
        var settingsService = _serviceProvider?.GetRequiredService<ISettingsService>();
        if (settingsService != null)
        {
            var settings = await settingsService.GetSettingsAsync();
            if (!string.IsNullOrEmpty(settings.LastFilePath) && System.IO.File.Exists(settings.LastFilePath))
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(settings.LastFilePath));
            }
        }
    }

    private void TogglePause()
    {
        _clipboardMonitor?.TogglePause();
    }

    private void ShowSettings()
    {
        if (m_window != null)
        {
            m_window.Activate();
            if (m_window is MainWindow mainWindow)
            {
                mainWindow.NavigateToSettings();
            }
        }
    }

    private new void Exit()
    {
        _trayIcon?.Dispose();
        _clipboardMonitor?.Stop();
        m_window?.Close();
        Environment.Exit(0);
    }

    private async void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        var arguments = args.Arguments;
        var userInput = args.UserInput;

        if (arguments.ContainsKey("action"))
        {
            var action = arguments["action"];
            var clipboardData = arguments.ContainsKey("data") ? arguments["data"] : "";
            
            var fileService = _serviceProvider?.GetRequiredService<IFileService>();
            if (fileService != null)
            {
                if (action == "append")
                {
                    await fileService.AppendToFileAsync(clipboardData);
                }
                else if (action == "new")
                {
                    await fileService.CreateNewFileAsync(clipboardData);
                }
            }
        }
    }

    public static IServiceProvider? ServiceProvider => (Current as App)?._serviceProvider;
}