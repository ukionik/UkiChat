using System.Windows;

namespace UkiChat.Services;

public class WindowService : IWindowService
{
    private Window? _settingsWindow;
    public void ShowSettingsWindow(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_settingsWindow is not { IsVisible: true })
            {
                _settingsWindow = new SettingsWindow();
                _settingsWindow.DataContext = message;

                // Когда окно закрывается — обнуляем ссылку
                _settingsWindow.Closed += (s, e) => _settingsWindow = null;

                _settingsWindow.Show();
            }
            else
            {
                // Если окно уже открыто, просто активируем его
                _settingsWindow.Activate();
            }
        });
    }
}