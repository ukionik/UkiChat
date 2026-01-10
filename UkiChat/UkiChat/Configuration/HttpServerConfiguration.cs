using System.IO;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using UkiChat.Hubs;

namespace UkiChat.Configuration;

public static class HttpServerConfiguration
{
    public static IWebHost CreateHost(int port = 5000)
    {
        var staticFilesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        
        return new WebHostBuilder()
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
                services.AddSignalR()
                    .AddJsonProtocol(options =>
                    {
                        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    })
                    ;
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
            })
            .Build();
    }
}