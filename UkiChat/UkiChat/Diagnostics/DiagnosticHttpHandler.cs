using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace UkiChat.Diagnostics;

/// <summary>
///     DelegatingHandler, который логирует каждый HTTP-запрос с длительностью и статусом.
///     Используется как inner handler для всех HttpClient внешних API.
/// </summary>
public class DiagnosticHttpHandler : DelegatingHandler
{
    private readonly string _serviceName;

    public DiagnosticHttpHandler(string serviceName) : base(new HttpClientHandler())
    {
        _serviceName = serviceName;
    }

    public DiagnosticHttpHandler(string serviceName, HttpMessageHandler innerHandler) : base(innerHandler)
    {
        _serviceName = serviceName;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.ToString() ?? "?";
        var method = request.Method.Method;
        StartupDiagnostics.Log($"http:{_serviceName}", $"-> {method} {url}");

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            sw.Stop();
            StartupDiagnostics.Log($"http:{_serviceName}",
                $"<- {method} {url} status={(int)response.StatusCode} took={sw.ElapsedMilliseconds} ms");
            return response;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            StartupDiagnostics.LogError($"http:{_serviceName}",
                $"TIMEOUT {method} {url} took={sw.ElapsedMilliseconds} ms", ex);
            throw;
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            StartupDiagnostics.LogError($"http:{_serviceName}",
                $"HTTP_ERROR {method} {url} took={sw.ElapsedMilliseconds} ms inner={ex.InnerException?.GetType().Name}",
                ex);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            StartupDiagnostics.LogError($"http:{_serviceName}",
                $"FAIL {method} {url} took={sw.ElapsedMilliseconds} ms", ex);
            throw;
        }
    }
}
