using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Prism.Ioc;
using UkiChat.Diagnostics;
using UkiChat.Hubs;
using UkiChat.Services;

namespace UkiChat.Configuration;

public static class HttpServerConfiguration
{
    /// <summary>
    ///     Порт зашит намеренно: на него зарегистрированы redirect_uri OAuth у Twitch и
    ///     Donation Alerts, поэтому подобрать свободный на лету нельзя.
    /// </summary>
    public const int Port = 5000;

    /// <summary>
    ///     Сколько Kestrel ждёт закрытия живых соединений при остановке. По умолчанию 5 секунд,
    ///     и всё это время процесс держит порт: закрыв окно, пользователь не мог сразу запустить
    ///     приложение снова — новый экземпляр упирался в "address already in use". Ждать здесь
    ///     нечего: единственный клиент — наш же WebView2, который к этому моменту уже закрыт.
    /// </summary>
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(1);

    public static IWebHost CreateHost(int port = Port)
    {
        var staticFilesPath = AppPaths.WwwRoot;
        StartupDiagnostics.Log("kestrel", $"CreateHost(port={port}) staticFilesPath={staticFilesPath}");
        StartupDiagnostics.Log("kestrel", $"wwwroot exists: {Directory.Exists(staticFilesPath)}");

        return new WebHostBuilder()
            .UseKestrel()
            .UseShutdownTimeout(ShutdownTimeout)
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

                    // OAuth callback Twitch: ловим code и обмениваем на токены.
                    // ITwitchAuthService резолвим из Prism-контейнера (как в AppHub) —
                    // он отдельный от service collection этого WebHost.
                    endpoints.MapGet("/twitch/auth/callback", async context =>
                    {
                        var error = context.Request.Query["error"].ToString();
                        var code = context.Request.Query["code"].ToString();
                        var state = context.Request.Query["state"].ToString();

                        var success = false;
                        if (string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(code))
                        {
                            var authService = ContainerLocator.Container.Resolve<ITwitchAuthService>();
                            success = await authService.HandleCallbackAsync(code, state);
                        }

                        context.Response.ContentType = "text/html; charset=utf-8";
                        await context.Response.WriteAsync(BuildCallbackHtml(success));
                    });

                    // OAuth callback Donation Alerts.
                    endpoints.MapGet("/donationalerts/auth/callback", async context =>
                    {
                        var error = context.Request.Query["error"].ToString();
                        var code = context.Request.Query["code"].ToString();
                        var state = context.Request.Query["state"].ToString();

                        var success = false;
                        if (string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(code))
                        {
                            var donationAlertsService = ContainerLocator.Container.Resolve<IDonationAlertsService>();
                            success = await donationAlertsService.HandleCallbackAsync(code, state);
                        }

                        context.Response.ContentType = "text/html; charset=utf-8";
                        await context.Response.WriteAsync(BuildCallbackHtml(success));
                    });
                });
            })
            .Build();
    }

    /// <summary>
    ///     Ждёт, пока порт освободится, и возвращает false, если этого так и не произошло.
    ///     Нужно на старте: предыдущий экземпляр приложения мог ещё не закрыть свой Kestrel,
    ///     и тогда Kestrel нового падает с "address already in use" — приложение поднимает окно,
    ///     но без бэкенда. Пары секунд ожидания хватает, чтобы старый процесс успел уйти.
    /// </summary>
    public static async Task<bool> WaitForFreePortAsync(TimeSpan timeout, int port = Port)
    {
        var deadline = DateTime.UtcNow + timeout;
        var reported = false;

        while (true)
        {
            if (IsPortFree(port))
                return true;

            if (DateTime.UtcNow >= deadline)
            {
                StartupDiagnostics.Log("kestrel",
                    $"Порт {port} не освободился за {timeout.TotalSeconds:F0} с");
                return false;
            }

            if (!reported)
            {
                StartupDiagnostics.Log("kestrel", $"Порт {port} занят — ждём освобождения");
                reported = true;
            }

            await Task.Delay(200);
        }
    }

    /// <summary>Kestrel слушает localhost и по IPv4, и по IPv6 — свободными нужны оба адреса.</summary>
    private static bool IsPortFree(int port)
    {
        if (!CanBind(IPAddress.Loopback, port))
            return false;

        return !Socket.OSSupportsIPv6 || CanBind(IPAddress.IPv6Loopback, port);
    }

    private static bool CanBind(IPAddress address, int port)
    {
        try
        {
            using var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(address, port));
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static string BuildCallbackHtml(bool success)
    {
        var title = success ? "Авторизация успешна" : "Ошибка авторизации";
        var message = success
            ? "Можно закрыть эту вкладку и вернуться в приложение."
            : "Не удалось завершить авторизацию. Закройте вкладку и попробуйте ещё раз.";
        var color = success ? "#22c55e" : "#ef4444";

        return $$"""
                 <!DOCTYPE html>
                 <html lang="ru">
                 <head>
                     <meta charset="utf-8">
                     <title>{{title}}</title>
                     <style>
                         body { margin: 0; height: 100vh; display: flex; align-items: center; justify-content: center;
                                font-family: system-ui, sans-serif; background: #0a0a0a; color: #e5e5e5; }
                         .card { text-align: center; padding: 2rem 3rem; }
                         h1 { color: {{color}}; margin-bottom: 0.5rem; }
                         p { opacity: 0.8; }
                     </style>
                 </head>
                 <body>
                     <div class="card">
                         <h1>{{title}}</h1>
                         <p>{{message}}</p>
                     </div>
                 </body>
                 </html>
                 """;
    }
}
