using System;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;

namespace UkiChat.ViewModels;

public class SettingsWindowViewModel : BindableBase
{
    private readonly string _webSource;

    public string WebSource
    {
        get => _webSource;
        init => SetProperty(ref _webSource, value);
    }

    public SettingsWindowViewModel()
    {
        WebSource = $"http://localhost:5000/settings?ts={DateTime.Now.Ticks}";
    }
}