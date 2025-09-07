using Microsoft.UI.Xaml.Controls;
using ClipTitle.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ClipTitle.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        this.InitializeComponent();
        ViewModel = App.ServiceProvider?.GetRequiredService<SettingsViewModel>() 
            ?? new SettingsViewModel(null!, null!);
    }
}