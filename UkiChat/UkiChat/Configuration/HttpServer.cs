using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using UkiChat.Hubs;

namespace UkiChat.Configuration;

public class HttpServer : IHttpServer, IDisposable
{
    private readonly IWebHost _host;
    public IHubContext<AppHub>? HubContext { get; private set; }

    public HttpServer(int port = 5000)
    {
        var staticFilesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        _host = new WebHostBuilder()
            .UseKestrel()
            .UseUrls($"http://localhost:{port}")
            .ConfigureServices(services =>
            {
                #if DEBUG
                services.AddCors(options =>
                {
                    options.AddDefaultPolicy(builder =>
                    {
                        builder
                            .WithOrigins("http://localhost:3000") // фронт WebStorm
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials(); // обязательно для SignalR                        
                    });

                });
                #endif
                services.AddSignalR();
            })
            .Configure(app =>
            {
                #if DEBUG
                app.UseCors(); // обязательно до MapHub
                #endif
                app.UseRouting();
                app.UseDefaultFiles();  // Раздаём статику Vue
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(staticFilesPath),
                    RequestPath = "",
                    ServeUnknownFileTypes = true
                });
                
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<AppHub>("/apphub");
                });
                
                // Достаём HubContext после старта
                var sp = app.ApplicationServices;
                HubContext = sp.GetRequiredService<IHubContext<AppHub>>();
            })
            .Build();
    }
    public Task StartAsync() => _host.StartAsync();
    public Task StopAsync() => _host.StopAsync();

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}