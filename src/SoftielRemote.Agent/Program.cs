using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoftielRemote.Agent.Config;
using SoftielRemote.Agent.Networking;
using SoftielRemote.Agent.ScreenCapture;
using SoftielRemote.Agent.Services;

var builder = Host.CreateApplicationBuilder(args);

// Logging yapılandırması
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Config
var config = new AgentConfig
{
    BackendBaseUrl = builder.Configuration["BackendBaseUrl"] ?? "http://localhost:5056",
    FrameIntervalMs = int.Parse(builder.Configuration["FrameIntervalMs"] ?? "200"),
    ScreenWidth = int.Parse(builder.Configuration["ScreenWidth"] ?? "800"),
    ScreenHeight = int.Parse(builder.Configuration["ScreenHeight"] ?? "600"),
    TcpServerPort = int.Parse(builder.Configuration["TcpServerPort"] ?? "8888")
};

builder.Services.AddSingleton(config);

// HttpClient
builder.Services.AddHttpClient<IBackendClientService, BackendClientService>();

// Services
builder.Services.AddSingleton<IScreenCaptureService, DummyScreenCaptureService>();
builder.Services.AddSingleton<TcpStreamServer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<TcpStreamServer>>();
    return new TcpStreamServer(config.TcpServerPort, logger);
});
builder.Services.AddHostedService<AgentService>();

var host = builder.Build();

Console.WriteLine("SoftielRemote Agent başlatılıyor...");
Console.WriteLine($"Backend URL: {config.BackendBaseUrl}");
Console.WriteLine($"TCP Port: {config.TcpServerPort}");
Console.WriteLine("Çıkmak için Ctrl+C tuşlarına basın.\n");

await host.RunAsync();
