using System.Windows;

namespace UkiChat.Services;

public interface IWindowService
{
    void ShowWindow<TWindow>()
        where TWindow : Window, new();
}