using UkiChat.Services;

namespace UkiChat.ViewModels;

public class MainViewModel(IMessageService messageService)
{
    public string Message { get; } = messageService.GetMessage();
}