using System;
using System.Windows;
using System.Windows.Threading;

namespace ClipTitle.Views
{
    public partial class ClipboardNotificationWindow : Window
    {
        private readonly string _content;
        private DispatcherTimer? _autoCloseTimer;
        
        public event EventHandler? AppendClicked;
        public event EventHandler? NewFileClicked;

        public ClipboardNotificationWindow(string title, string preview, string content)
        {
            InitializeComponent();
            
            _content = content;
            TitleText.Text = title;
            PreviewText.Text = preview;
            
            // Set up auto-close timer (10 seconds)
            _autoCloseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _autoCloseTimer.Tick += (s, e) => Close();
            _autoCloseTimer.Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Position window in bottom-right corner
            var workingArea = SystemParameters.WorkArea;
            this.Left = workingArea.Right - this.Width - 20;
            this.Top = workingArea.Bottom - this.Height - 20;
            
            // Fade in animation
            this.Opacity = 0;
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            this.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void AppendButton_Click(object sender, RoutedEventArgs e)
        {
            _autoCloseTimer?.Stop();
            AppendClicked?.Invoke(this, EventArgs.Empty);
            FadeOutAndClose();
        }

        private void NewFileButton_Click(object sender, RoutedEventArgs e)
        {
            _autoCloseTimer?.Stop();
            NewFileClicked?.Invoke(this, EventArgs.Empty);
            FadeOutAndClose();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _autoCloseTimer?.Stop();
            FadeOutAndClose();
        }

        private void FadeOutAndClose()
        {
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            fadeOut.Completed += (s, e) => Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}