using System.Threading.Tasks;

namespace UkiChat.Services;

public interface IAppInitializationService
{
    Task InitializeAsync();
    Task LoadTwitchChannelDataAsync(string channelName);
}