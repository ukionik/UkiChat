using System;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;

namespace UkiChat.ViewModels;

public class ProfileWindowViewModel : BindableBase
{
    private readonly string _webSource;

    public ProfileWindowViewModel()
    {
        WebSource = $"http://localhost:5000/profile?ts={DateTime.Now.Ticks}";
    }

    public string WebSource
    {
        get => _webSource;
        init => SetProperty(ref _webSource, value);
    }
}