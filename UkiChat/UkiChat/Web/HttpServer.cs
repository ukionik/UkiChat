using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace UkiChat.Web;

public class HttpServer(int port = 5000)
{
    public void Start()
    {
        var host = new WebHostBuilder()
            .UseKestrel()
            .UseUrls($"http://localhost:{port}")
            .Configure(app =>
            {
                var staticFilesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                app.UseDefaultFiles(); // ищет index.html
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(staticFilesPath),
                    RequestPath = "",
                    ServeUnknownFileTypes = true
                });
            })
            .Build();

        host.RunAsync(); // запускаем в фоне, чтобы не блокировать UI
    }
}