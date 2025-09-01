using UkiChat.Web;

namespace UkiChat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private HttpServer _server;

        public MainWindow()
        {
            InitializeComponent();
            InitHttpServer();
        }
        private void InitHttpServer()
        {
            _server = new HttpServer();
            _server.Start();
        }
    }
}