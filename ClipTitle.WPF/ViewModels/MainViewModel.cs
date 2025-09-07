using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using ClipTitle.Services;

namespace ClipTitle.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _title = "ClipTitle";

    [ObservableProperty]
    private string _statusText = "Ready";

    public MainViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        StatusText = "Initializing...";
        await _settingsService.LoadSettingsAsync();
        StatusText = "Ready - Monitoring clipboard";
    }
}