using System.Threading.Tasks;

namespace ClipTitle.Services
{
    public interface INotificationService
    {
        Task ShowClipboardNotificationAsync(string title, string content);
        Task ShowSuccessNotificationAsync(string message);
        Task ShowErrorNotificationAsync(string message);
    }
}