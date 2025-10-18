using System.Windows;

namespace UkiChat.Services;

public class WindowService : IWindowService
{
    private Window? _window;

    public void ShowWindow<TWindow>()
        where TWindow : Window, new()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_window is not { IsVisible: true })
            {
                _window = new TWindow();

                // Когда окно закрывается — обнуляем ссылку
                _window.Closed += (s, e) => _window = null;
                _window.Show();
            }
            else
            {
                // Если окно уже открыто, просто активируем его
                _window.Activate();
            }
        });
    }
}