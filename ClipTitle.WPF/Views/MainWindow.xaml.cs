using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using ClipTitle.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClipTitle.Views
{
    public partial class MainWindow : Window
    {
        private ISettingsService? _settingsService;
        private IOllamaService? _ollamaService;
        
        public MainWindow()
        {
            InitializeComponent();
            _settingsService = App.ServiceProvider?.GetRequiredService<ISettingsService>();
            _ollamaService = App.ServiceProvider?.GetRequiredService<IOllamaService>();
            LoadSettings();
        }

        private async void LoadSettings()
        {
            if (_settingsService == null) return;
            
            var settings = await _settingsService.GetSettingsAsync();
            
            // General Settings
            SaveDirectoryTextBox.Text = settings.SaveDirectory;
            AskEveryTimeRadio.IsChecked = settings.AskEveryTime;
            AutoAppendRadio.IsChecked = settings.AutoAppend;
            MinLengthTextBox.Text = settings.MinimumLength.ToString();
            IgnorePatternsTextBox.Text = string.Join("\n", settings.IgnorePatterns);
            
            // AI Settings
            OllamaUrlTextBox.Text = settings.OllamaSettings.BaseUrl;
            OllamaModelTextBox.Text = settings.OllamaSettings.Model;
            OllamaTimeoutTextBox.Text = settings.OllamaSettings.TimeoutMs.ToString();
            PromptTemplateTextBox.Text = settings.OllamaSettings.TitlePromptTemplate ?? 
                "Generate a concise title (3-8 words) for: {content}";
        }

        private async void SaveSettings()
        {
            if (_settingsService == null) return;
            
            var settings = await _settingsService.GetSettingsAsync();
            
            // General Settings
            settings.SaveDirectory = SaveDirectoryTextBox.Text;
            settings.AskEveryTime = AskEveryTimeRadio.IsChecked ?? true;
            settings.AutoAppend = AutoAppendRadio.IsChecked ?? false;
            
            if (int.TryParse(MinLengthTextBox.Text, out int minLength))
            {
                settings.MinimumLength = minLength;
            }
            
            settings.IgnorePatterns = IgnorePatternsTextBox.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            // AI Settings
            settings.OllamaSettings.BaseUrl = OllamaUrlTextBox.Text;
            settings.OllamaSettings.Model = OllamaModelTextBox.Text;
            
            if (int.TryParse(OllamaTimeoutTextBox.Text, out int timeout))
            {
                settings.OllamaSettings.TimeoutMs = timeout;
            }
            
            settings.OllamaSettings.TitlePromptTemplate = PromptTemplateTextBox.Text;
            
            await _settingsService.SaveSettingsAsync(settings);
            StatusText.Text = "Settings saved successfully";
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select save directory for clips";
                dialog.SelectedPath = SaveDirectoryTextBox.Text;
                
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    SaveDirectoryTextBox.Text = dialog.SelectedPath;
                    SaveSettings();
                }
            }
        }

        private async void TestOllama_Click(object sender, RoutedEventArgs e)
        {
            if (_ollamaService == null) return;
            
            TestResultText.Text = "Testing...";
            
            try
            {
                var isConnected = await _ollamaService.TestConnectionAsync();
                TestResultText.Text = isConnected 
                    ? "✓ Connected successfully" 
                    : "✗ Connection failed";
            }
            catch (Exception ex)
            {
                TestResultText.Text = $"✗ Error: {ex.Message}";
            }
        }

        private void OpenGitHub_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/yourusername/cliptitle",
                UseShellExecute = true
            });
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Don't close, just hide
            e.Cancel = true;
            this.Hide();
            SaveSettings();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Register for clipboard notifications
            var source = PresentationSource.FromVisual(this) as System.Windows.Interop.HwndSource;
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Handle window messages if needed
            return IntPtr.Zero;
        }
    }
}