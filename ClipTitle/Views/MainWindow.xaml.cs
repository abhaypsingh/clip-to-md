using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ClipTitle.Views;

namespace ClipTitle.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        Title = "ClipTitle Settings";
        
        // Set initial size
        var bounds = new Windows.Graphics.RectInt32(100, 100, 800, 600);
        this.AppWindow.MoveAndResize(bounds);
        
        // Navigate to Settings by default
        ContentFrame.Navigate(typeof(SettingsPage));
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer != null)
        {
            var tag = args.SelectedItemContainer.Tag.ToString();
            
            switch (tag)
            {
                case "Settings":
                    ContentFrame.Navigate(typeof(SettingsPage));
                    break;
                case "About":
                    ContentFrame.Navigate(typeof(AboutPage));
                    break;
            }
        }
    }

    public void NavigateToSettings()
    {
        ContentFrame.Navigate(typeof(SettingsPage));
        NavView.SelectedItem = NavView.MenuItems[0];
    }
}