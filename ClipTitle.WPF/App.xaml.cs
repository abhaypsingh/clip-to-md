using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ClipTitle.Services;
using ClipTitle.Views;
using ClipTitle.Helpers;
using Hardcodet.Wpf.TaskbarNotification;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClipTitle
{
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;
        private TaskbarIcon? _notifyIcon;
        private ClipboardMonitor? _clipboardMonitor;
        private MainWindow? _mainWindow;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            Console.WriteLine("[App] Starting ClipTitle...");
            
            ConfigureServices();
            await InitializeServicesAsync();
            InitializeSystemTray();
            
            // Create main window first (needed for clipboard monitoring)
            _mainWindow = new MainWindow();
            _mainWindow.Hide();
            
            // Start clipboard monitoring after window is created
            StartClipboardMonitoring();
            
            Console.WriteLine("[App] ClipTitle startup complete!");
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Services
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IContentAnalyzer, ContentAnalyzer>();
            services.AddSingleton<IOllamaService, OllamaService>();
            services.AddSingleton<IMarkdownConverter, MarkdownConverter>();
            services.AddSingleton<INotificationService, WpfNotificationService>();
            services.AddSingleton<ClipboardMonitor>();
            
            // ViewModels - removed for simplicity

            // Logging
            services.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
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

        private void InitializeSystemTray()
        {
            _notifyIcon = new TaskbarIcon
            {
                Icon = IconHelper.CreateIconFromResource(),
                // IconSource = IconHelper.GetImageSource(), // Commented out - causing crash
                ToolTipText = "ClipTitle - Clipboard Monitor"
            };

            // Create context menu
            _notifyIcon.ContextMenu = new System.Windows.Controls.ContextMenu();
            
            var toggleAutoAppendItem = new System.Windows.Controls.MenuItem 
            { 
                Header = "Toggle Auto-append",
                // Icon = new System.Windows.Controls.Image
                // {
                //     Source = new System.Windows.Media.Imaging.BitmapImage(
                //         new Uri("pack://application:,,,/Assets/append.png"))
                // }
            };
            toggleAutoAppendItem.Click += async (s, e) => await ToggleAutoAppend();
            _notifyIcon.ContextMenu.Items.Add(toggleAutoAppendItem);
            
            var openFolderItem = new System.Windows.Controls.MenuItem { Header = "Open Save Folder" };
            openFolderItem.Click += async (s, e) => await OpenSaveFolder();
            _notifyIcon.ContextMenu.Items.Add(openFolderItem);
            
            var openLastFileItem = new System.Windows.Controls.MenuItem { Header = "Open Last File" };
            openLastFileItem.Click += async (s, e) => await OpenLastFile();
            _notifyIcon.ContextMenu.Items.Add(openLastFileItem);
            
            _notifyIcon.ContextMenu.Items.Add(new System.Windows.Controls.Separator());
            
            var pauseItem = new System.Windows.Controls.MenuItem { Header = "Pause Capture" };
            pauseItem.Click += (s, e) => TogglePause();
            _notifyIcon.ContextMenu.Items.Add(pauseItem);
            
            var settingsItem = new System.Windows.Controls.MenuItem { Header = "Settings" };
            settingsItem.Click += (s, e) => ShowSettings();
            _notifyIcon.ContextMenu.Items.Add(settingsItem);
            
            _notifyIcon.ContextMenu.Items.Add(new System.Windows.Controls.Separator());
            
            var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
            exitItem.Click += (s, e) => ExitApplication();
            _notifyIcon.ContextMenu.Items.Add(exitItem);
            
            // Double-click to show settings
            _notifyIcon.TrayMouseDoubleClick += (s, e) => ShowSettings();
        }

        private void StartClipboardMonitoring()
        {
            Console.WriteLine("[App] Initializing clipboard monitoring...");
            
            _clipboardMonitor = _serviceProvider?.GetRequiredService<ClipboardMonitor>();
            if (_clipboardMonitor != null && _mainWindow != null)
            {
                Console.WriteLine("[App] Starting clipboard monitor with main window...");
                _clipboardMonitor.Start(_mainWindow);
            }
            else
            {
                Console.WriteLine($"[App] ERROR: Cannot start clipboard monitoring - Monitor: {_clipboardMonitor != null}, Window: {_mainWindow != null}");
            }
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
                
                _notifyIcon?.ShowBalloonTip(
                    "ClipTitle", 
                    $"Auto-append {(settings.AutoAppend ? "enabled" : "disabled")}", 
                    BalloonIcon.Info);
            }
        }

        private async Task OpenSaveFolder()
        {
            var settingsService = _serviceProvider?.GetRequiredService<ISettingsService>();
            if (settingsService != null)
            {
                var settings = await settingsService.GetSettingsAsync();
                System.Diagnostics.Process.Start("explorer.exe", settings.SaveDirectory);
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
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = settings.LastFilePath,
                        UseShellExecute = true
                    });
                }
            }
        }

        private void TogglePause()
        {
            _clipboardMonitor?.TogglePause();
            var isPaused = _clipboardMonitor?.IsPaused ?? false;
            _notifyIcon?.ShowBalloonTip(
                "ClipTitle", 
                $"Clipboard monitoring {(isPaused ? "paused" : "resumed")}", 
                BalloonIcon.Info);
        }

        private void ShowSettings()
        {
            if (_mainWindow == null)
            {
                _mainWindow = new MainWindow();
            }
            
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }

        private void ExitApplication()
        {
            _clipboardMonitor?.Stop();
            _notifyIcon?.Dispose();
            Application.Current.Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            base.OnExit(e);
        }

        public static IServiceProvider? ServiceProvider => (Current as App)?._serviceProvider;
    }
}