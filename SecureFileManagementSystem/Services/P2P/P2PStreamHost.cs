using System.Net;
using System.Net.Sockets;
using System.Threading;

public class P2PStreamHost
{
    private readonly byte[] _fileData;
    private readonly string _fileName;
    private readonly int _port;
    private HttpListener? _listener;
    private Timer? _shutdownTimer;

    public P2PStreamHost(byte[] fileData, string fileName, int port = 0)
    {
        _fileData = fileData ?? throw new ArgumentNullException(nameof(fileData));
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        _port = port > 0 ? port : GetAvailablePort();
    }

    private int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public void Start()
    {
        _listener = new HttpListener();
        string localIP = GetLocalIPAddress();
        string prefix = $"http://{localIP}:{_port}/p2p/download/";
        _listener.Prefixes.Add(prefix);
        _listener.Start();

        // Shutdown after 5 minutes if no connection
        _shutdownTimer = new Timer(_ => Stop(), null, TimeSpan.FromMinutes(5), Timeout.InfiniteTimeSpan);

        Task.Run(async () =>
        {
            try
            {
                while (_listener.IsListening)
                {
                    var context = await _listener.GetContextAsync();
                    await HandleRequestAsync(context);
                }
            }
            catch (HttpListenerException)
            {
                // Listener stopped normally
            }
            catch (Exception ex)
            {
                Console.WriteLine($"P2P Listener error: {ex.Message}");
            }
        });
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            var response = context.Response;
            response.ContentType = "application/octet-stream";
            response.AddHeader("Content-Disposition", $"attachment; filename=\"{_fileName}\"");

            using var stream = new MemoryStream(_fileData);
            byte[] buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await response.OutputStream.WriteAsync(buffer, 0, bytesRead);
            }

            response.OutputStream.Close();

            //Stop after one download
            Stop();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during file stream: {ex.Message}");
        }
    }

    public void Stop()
    {
        _shutdownTimer?.Dispose();
        _shutdownTimer = null;

        _listener?.Stop();
        _listener?.Close();
        _listener = null;
    }

    public string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                return ip.ToString();
        }
        return "127.0.0.1";
    }

    public string GetDownloadUrl() =>
        $"http://{GetLocalIPAddress()}:{_port}/p2p/download";
}
