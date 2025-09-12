using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace UkiChat.Configuration;

public class HttpServer(int port = 5000) : IDisposable
{
    private IWebHost _host;

    public void Start()
    {
        _host = new WebHostBuilder()
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

        _host.RunAsync(); // запускаем в фоне, чтобы не блокировать UI
    }

    public void Dispose()
    {
        _host.Dispose();
    }
}