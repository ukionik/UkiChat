using System;
using Microsoft.AspNetCore.SignalR;
using UkiChat.Configuration;
using UkiChat.ViewModels;

namespace UkiChat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow(MainViewModel viewModel
        , IDatabaseContext databaseContext
        , IHttpServer server)
        {
            InitializeComponent();
            DataContext = viewModel;
            var twitchGlobalSettings = databaseContext.TwitchGlobalSettingsRepository.Get();
            Console.WriteLine(twitchGlobalSettings.Id);
            Console.WriteLine(twitchGlobalSettings.TwitchChatBotUsername);
            Console.WriteLine(twitchGlobalSettings.TwitchChatBotAccessToken);
            server.HubContext.Clients.All.SendAsync("ReceiveMessage", "Hello from the server");
        }
    }
}