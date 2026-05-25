using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using UkiChat.Diagnostics;
using UkiChat.Hubs;

namespace UkiChat.Configuration;

public static class HttpServerConfiguration
{
    public static IWebHost CreateHost(int port = 5000)
    {
        var staticFilesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        StartupDiagnostics.Log("kestrel", $"CreateHost(port={port}) staticFilesPath={staticFilesPath}");
        StartupDiagnostics.Log("kestrel", $"wwwroot exists: {Directory.Exists(staticFilesPath)}");

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
                // Подписываемся на lifetime-события, чтобы видеть момент, когда Kestrel реально начал слушать порт
                var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
                lifetime.ApplicationStarted.Register(() =>
                    StartupDiagnostics.Log("kestrel", "ApplicationStarted (server is listening)"));
                lifetime.ApplicationStopping.Register(() =>
                    StartupDiagnostics.Log("kestrel", "ApplicationStopping"));
                lifetime.ApplicationStopped.Register(() =>
                    StartupDiagnostics.Log("kestrel", "ApplicationStopped"));

                // Логируем каждый HTTP-запрос с длительностью и статусом.
                // На этапе диагностики дороговизна не важна — пишем всё.
                app.Use(async (context, next) =>
                {
                    var sw = Stopwatch.StartNew();
                    var method = context.Request.Method;
                    var path = context.Request.Path + context.Request.QueryString;
                    var remote = context.Connection.RemoteIpAddress?.ToString() ?? "?";
                    var localPort = context.Connection.LocalPort;
                    StartupDiagnostics.Log("kestrel-req",
                        $"-> {method} {path} from {remote} (localPort={localPort})");
                    try
                    {
                        await next();
                        sw.Stop();
                        StartupDiagnostics.Log("kestrel-req",
                            $"<- {method} {path} status={context.Response.StatusCode} took={sw.ElapsedMilliseconds} ms");
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        StartupDiagnostics.LogError("kestrel-req",
                            $"!! {method} {path} took={sw.ElapsedMilliseconds} ms", ex);
                        throw;
                    }
                });

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
