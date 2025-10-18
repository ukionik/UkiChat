using System;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;

namespace UkiChat.ViewModels;

public class ProfileWindowViewModel : BindableBase
{
    private readonly string _webSource;
    private Visibility _visibility = Visibility.Collapsed;

    public ProfileWindowViewModel()
    {
        WebSource = $"http://localhost:5000/profile?ts={DateTime.Now.Ticks}";
        LoadedCommand = new DelegateCommand(OnLoaded);
    }

    public string WebSource
    {
        get => _webSource;
        init => SetProperty(ref _webSource, value);
    }

    public DelegateCommand LoadedCommand { get; }

    public Visibility Visibility
    {
        get => _visibility;
        set => SetProperty(ref _visibility, value);
    }
    
    private async void OnLoaded()
    {
        //Костыль на плохую отрисовку
        await Task.Delay(500);
        Visibility = Visibility.Visible;
    }
}