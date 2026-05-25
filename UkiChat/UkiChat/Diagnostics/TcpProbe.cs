using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace UkiChat.Diagnostics;

/// <summary>
///     Проверяет TCP-доступность Kestrel через IPv4 (127.0.0.1) и IPv6 (::1) loopback.
///     Помогает диагностировать ситуацию, когда WebView2/Chromium резолвит "localhost"
///     в один из стэков, а Kestrel слушает только другой (либо стэк тупит).
/// </summary>
public static class TcpProbe
{
    public static async Task ProbeLoopbackAsync(int port = 5000)
    {
        StartupDiagnostics.Log("tcp-probe", $"Probing loopback :{port} ...");

        await Task.WhenAll(
            ProbeOneAsync("127.0.0.1", IPAddress.Loopback, port),
            ProbeOneAsync("[::1]", IPAddress.IPv6Loopback, port));

        // И через имя "localhost" — чтобы увидеть, что выдаёт резолвер
        try
        {
            var sw = Stopwatch.StartNew();
            var addrs = await Dns.GetHostAddressesAsync("localhost");
            sw.Stop();
            StartupDiagnostics.Log("tcp-probe",
                $"Dns.GetHostAddressesAsync(\"localhost\") -> [{string.Join(", ", addrs.AsEnumerable())}] took={sw.ElapsedMilliseconds} ms");
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogError("tcp-probe", "Dns.GetHostAddressesAsync(\"localhost\") failed", ex);
        }
    }

    private static async Task ProbeOneAsync(string label, IPAddress ip, int port)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var client = new TcpClient(ip.AddressFamily);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await client.ConnectAsync(ip, port, cts.Token);
            sw.Stop();
            StartupDiagnostics.Log("tcp-probe",
                $"  {label}:{port} CONNECTED in {sw.ElapsedMilliseconds} ms (connected={client.Connected})");
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            StartupDiagnostics.LogError("tcp-probe",
                $"  {label}:{port} TIMEOUT after {sw.ElapsedMilliseconds} ms");
        }
        catch (SocketException ex)
        {
            sw.Stop();
            StartupDiagnostics.LogError("tcp-probe",
                $"  {label}:{port} SOCKET_ERROR ({ex.SocketErrorCode}) after {sw.ElapsedMilliseconds} ms", ex);
        }
        catch (Exception ex)
        {
            sw.Stop();
            StartupDiagnostics.LogError("tcp-probe",
                $"  {label}:{port} FAIL after {sw.ElapsedMilliseconds} ms", ex);
        }
    }
}
