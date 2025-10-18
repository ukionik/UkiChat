using System;
using Prism.Mvvm;

namespace UkiChat.ViewModels;

public class ProfileWindowViewModel : BindableBase
{
    private readonly string _webSource;

    public string WebSource
    {
        get => _webSource;
        init => SetProperty(ref _webSource, value);
    }

    public ProfileWindowViewModel()
    {
        WebSource = $"http://localhost:5000/profile?ts={DateTime.Now.Ticks}";
        Console.WriteLine($"Web Source: {WebSource}");
    }
}