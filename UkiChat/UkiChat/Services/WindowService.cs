using System;
using System.Collections.Generic;
using System.Windows;
using Prism.Ioc;
using Prism.Mvvm;

namespace UkiChat.Services;

public class WindowService : IWindowService
{
    private readonly Dictionary<Type, Window> _windows = new();

    public void ShowWindow<TWindow>()
        where TWindow : Window
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            // Если окно уже открыто — активируем
            if (_windows.TryGetValue(typeof(TWindow), out var existingWindow) && existingWindow is { IsVisible: true })
            {
                existingWindow.Activate();
                return;
            }

            // Создаём окно через контейнер (DI)
            var window = ContainerLocator.Container.Resolve<TWindow>();

            // Привязываем ViewModel через ViewModelLocator
            if (window.DataContext == null)
            {
                var viewModel = ViewModelLocator.GetAutoWireViewModel(window);
                if (viewModel != null)
                {
                    window.DataContext = viewModel;
                }
            }

            _windows[typeof(TWindow)] = window;

            window.Closed += (s, e) => _windows.Remove(typeof(TWindow));

            window.Show();
        });
    }
}