using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

// Pick a free port to avoid conflicts
var port = GetAvailablePort();
builder.WebHost.UseUrls($"http://localhost:{port}");

// Suppress console noise
builder.Logging.SetMinimumLevel(LogLevel.Warning);

var app = builder.Build();

app.UseStaticFiles();
app.UseBlazorFrameworkFiles();

// Heartbeat endpoint — the browser pings this to keep the server alive
var lastHeartbeat = DateTime.UtcNow;
app.MapGet("/api/heartbeat", () =>
{
    lastHeartbeat = DateTime.UtcNow;
    return Results.Ok();
});

// Immediate shutdown endpoint — called when the browser tab closes
app.MapPost("/api/shutdown", (IHostApplicationLifetime lifetime) =>
{
    Console.WriteLine("Game closed. Shutting down...");
    _ = Task.Run(async () =>
    {
        await Task.Delay(500);
        lifetime.StopApplication();
    });
    return Results.Ok();
});

app.MapFallbackToFile("index.html");

var url = $"http://localhost:{port}";
Console.WriteLine("=================================");
Console.WriteLine("  Frogmageddon is running!");
Console.WriteLine($"  {url}");
Console.WriteLine("=================================");
Console.WriteLine();
Console.WriteLine("The game should open in your browser.");
Console.WriteLine("This window will close automatically when you're done playing.");
Console.WriteLine("Or press Ctrl+C to stop manually.");

// Auto-open the browser
OpenBrowser(url);

// Start background task to shut down after idle timeout
var cts = new CancellationTokenSource();
_ = Task.Run(async () =>
{
    // Give the browser 30 seconds to connect initially
    await Task.Delay(TimeSpan.FromSeconds(30), cts.Token);

    while (!cts.Token.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
        var idle = DateTime.UtcNow - lastHeartbeat;
        if (idle > TimeSpan.FromSeconds(15))
        {
            Console.WriteLine("Browser disconnected. Shutting down...");
            await app.StopAsync();
            break;
        }
    }
}, cts.Token);

app.Lifetime.ApplicationStopping.Register(() => cts.Cancel());
app.Run();

static int GetAvailablePort()
{
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}

static void OpenBrowser(string url)
{
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch
    {
        // If browser launch fails, the URL is still printed to console
    }
}
