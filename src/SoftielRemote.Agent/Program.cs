using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoftielRemote.Agent;
using SoftielRemote.Agent.Config;
using SoftielRemote.Agent.InputInjection;
using SoftielRemote.Agent.Networking;
using SoftielRemote.Agent.ScreenCapture;
using SoftielRemote.Agent.Services;
using SoftielRemote.Core.Utils;

var builder = Host.CreateApplicationBuilder(args);

// Logging yapılandırması
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Backend URL'ini çöz (development modunda otomatik keşif yapar)
var configuredBackendUrl = builder.Configuration["BackendBaseUrl"];
var resolvedBackendUrl = await BackendUrlResolver.ResolveBackendUrlAsync(configuredBackendUrl);

if (resolvedBackendUrl == null)
{
    Console.WriteLine("⚠️ Backend URL bulunamadı. appsettings.json'da BackendBaseUrl belirtin veya Backend'i başlatın.");
    Console.WriteLine("Örnek: {\"BackendBaseUrl\": \"http://192.168.1.100:5000\"}");
    resolvedBackendUrl = configuredBackendUrl ?? "http://localhost:5000"; // Fallback
}

Console.WriteLine($"🔵 Backend URL: {resolvedBackendUrl}");

// DeviceId'yi önce ortak deviceid.json'dan (AppData), sonra local dosyalardan oku
// Eğer yoksa makine bazlı ID üret (Agent ve App aynı ID'yi kullanmalı)
string? deviceId = null;
try
{
    // 1. Önce ortak deviceid.json'dan oku (AppData - Agent ve App aynı dosyayı kullanır)
    var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var softielRemotePath = Path.Combine(appDataPath, "SoftielRemote");
    var deviceIdPath = Path.Combine(softielRemotePath, "deviceid.json");
    
    if (File.Exists(deviceIdPath))
    {
        var json = File.ReadAllText(deviceIdPath);
        var deviceIdConfig = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        if (deviceIdConfig != null && deviceIdConfig.ContainsKey("DeviceId"))
        {
            deviceId = deviceIdConfig["DeviceId"]?.ToString();
            Console.WriteLine($"🔵 Device ID ortak deviceid.json'dan okundu: {deviceId}");
        }
    }
    
    // 2. Ortak dosya yoksa, local deviceid.json'dan oku (backward compatibility)
    if (string.IsNullOrWhiteSpace(deviceId))
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var localDeviceIdPath = Path.Combine(baseDirectory, "deviceid.json");
        if (File.Exists(localDeviceIdPath))
        {
            var json = File.ReadAllText(localDeviceIdPath);
            var deviceIdConfig = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (deviceIdConfig != null && deviceIdConfig.ContainsKey("DeviceId"))
            {
                deviceId = deviceIdConfig["DeviceId"]?.ToString();
                Console.WriteLine($"🔵 Device ID local deviceid.json'dan okundu: {deviceId}");
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Device ID okunamadı: {ex.Message}");
}

if (string.IsNullOrWhiteSpace(deviceId))
{
    deviceId = builder.Configuration["DeviceId"];
}

// Eğer hala yoksa, makine bazlı ID üret (Agent ve App aynı ID'yi kullanacak)
if (string.IsNullOrWhiteSpace(deviceId))
{
    deviceId = MachineIdGenerator.GenerateMachineBasedId();
    Console.WriteLine($"🔵 Makine bazlı Device ID üretildi: {deviceId}");
    
    // Hemen ortak dosyaya kaydet (Agent ve App aynı dosyayı kullanacak)
    try
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var softielRemotePath = Path.Combine(appDataPath, "SoftielRemote");
        Directory.CreateDirectory(softielRemotePath); // Klasör yoksa oluştur
        
        var deviceIdPath = Path.Combine(softielRemotePath, "deviceid.json");
        var deviceIdConfig = new Dictionary<string, object>
        {
            ["DeviceId"] = deviceId,
            ["MachineName"] = Environment.MachineName,
            ["GeneratedAt"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };
        var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
        var json = System.Text.Json.JsonSerializer.Serialize(deviceIdConfig, options);
        File.WriteAllText(deviceIdPath, json);
        Console.WriteLine($"🔵 Device ID ortak deviceid.json'a kaydedildi: {deviceId}, Path={deviceIdPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Device ID kaydedilemedi: {ex.Message}");
    }
}

Console.WriteLine($"🔵 Device ID: {deviceId}");

// Config
var config = new AgentConfig
{
    BackendBaseUrl = resolvedBackendUrl,
    DeviceId = deviceId, // deviceid.json veya appsettings.json'dan DeviceId oku
    FrameIntervalMs = int.Parse(builder.Configuration["FrameIntervalMs"] ?? "33"), // 30 FPS için 33ms
    ScreenWidth = int.Parse(builder.Configuration["ScreenWidth"] ?? "800"),
    ScreenHeight = int.Parse(builder.Configuration["ScreenHeight"] ?? "600"),
    TcpServerPort = int.Parse(builder.Configuration["TcpServerPort"] ?? "8888")
};

builder.Services.AddSingleton(config);

// HttpClient
builder.Services.AddHttpClient<IBackendClientService, BackendClientService>();

// Services
// Ekran yakalama servisi: DirectX Desktop Duplication (production-ready, yüksek performans)
// Eğer DirectX başarısız olursa GDI+ fallback kullanılacak
builder.Services.AddSingleton<IScreenCaptureService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<IScreenCaptureService>>();
    try
    {
        // Önce DirectX'i dene
        var directXService = new DirectXDesktopDuplicationService(
            sp.GetRequiredService<ILogger<DirectXDesktopDuplicationService>>());
        directXService.StartCapture();
        
        // Test frame al
        var testFrame = directXService.CaptureScreenAsync(1920, 1080).GetAwaiter().GetResult();
        if (testFrame != null)
        {
            Console.WriteLine("✅ DirectX Desktop Duplication başarıyla başlatıldı");
            return directXService;
        }
        else
        {
            Console.WriteLine("⚠️ DirectX test frame null, GDI+ fallback'e geçiliyor");
            directXService.Dispose();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ DirectX başlatılamadı, GDI+ fallback'e geçiliyor: {ex.Message}");
    }
    
    // Fallback: GDI+ kullan
    Console.WriteLine("🔄 GDI+ Screen Capture servisi kullanılıyor");
    return new GdiScreenCaptureService(sp.GetRequiredService<ILogger<GdiScreenCaptureService>>());
});
builder.Services.AddSingleton<VideoEncodingService>();
builder.Services.AddSingleton<TcpStreamServer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<TcpStreamServer>>();
    return new TcpStreamServer(config.TcpServerPort, logger);
});
builder.Services.AddSingleton<SignalRClientService>();
builder.Services.AddSingleton<WebRTCPeerService>();
builder.Services.AddSingleton<IInputInjectionService, WindowsInputInjectionService>();
builder.Services.AddHostedService<AgentService>();

var host = builder.Build();

// WPF Application'ı STA thread'de başlat (popup'lar için gerekli)
// WPF Application'lar STA (Single Threaded Apartment) thread'de çalışmalı
var wpfAppReady = new System.Threading.ManualResetEventSlim(false);
App? wpfAppInstance = null;
var wpfAppThread = new System.Threading.Thread(() =>
{
    try
    {
        Console.WriteLine("🔨 WPF Application thread başlatılıyor (STA)...");
        Console.WriteLine($"   Thread ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
        Console.WriteLine($"   Apartment State: {System.Threading.Thread.CurrentThread.GetApartmentState()}");
        
        // STA thread'de WPF Application başlat
        var app = new App();
        Console.WriteLine("✅ WPF Application oluşturuldu");
        
        app.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown; // Otomatik kapanmayı engelle
        Console.WriteLine("✅ WPF Application ShutdownMode ayarlandı");
        
        // App.Instance constructor'da set edildi, burada kontrol et
        wpfAppInstance = App.Instance;
        Console.WriteLine($"✅ App.Instance kontrol edildi: {(wpfAppInstance != null ? "Mevcut" : "NULL")}");
        
        // Dispatcher'ın mevcut olduğunu kontrol et
        if (wpfAppInstance != null && wpfAppInstance.Dispatcher != null)
        {
            Console.WriteLine($"✅ WPF Dispatcher mevcut: ThreadId={wpfAppInstance.Dispatcher.Thread.ManagedThreadId}");
        }
        else
        {
            Console.WriteLine("⚠️ WPF Dispatcher henüz mevcut değil (app.Run() sonrası hazır olacak)");
        }
        
        // Application_Startup event'inin tetiklenmesi için kısa bekleme
        System.Threading.Thread.Sleep(300);
        
        // Instance'ın set edildiğini doğrula
        if (wpfAppInstance != null)
        {
            wpfAppReady.Set();
            Console.WriteLine("✅ WPF Application instance hazır ve signal verildi");
        }
        else
        {
            Console.WriteLine("⚠️ WPF Application instance null, yine de devam ediliyor");
            wpfAppReady.Set();
        }
        
        Console.WriteLine("🔄 WPF Application message loop başlatılıyor (app.Run())...");
        app.Run(); // WPF message loop'u başlat (bu bloklayıcı)
        Console.WriteLine("🛑 WPF Application message loop sonlandı");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ WPF Application başlatma hatası: {ex.Message}");
        Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
        }
        wpfAppReady.Set(); // Hata durumunda da signal ver
    }
})
{
    IsBackground = false, // Ana thread kapanırsa WPF Application da kapansın
    Name = "WPF Application Thread"
};

wpfAppThread.SetApartmentState(System.Threading.ApartmentState.STA); // STA thread olarak ayarla
wpfAppThread.Start();

// WPF Application'ın başlamasını bekle (maksimum 5 saniye)
if (!wpfAppReady.Wait(TimeSpan.FromSeconds(5)))
{
    Console.WriteLine("⚠️ WPF Application başlatılamadı (timeout)");
}
else
{
    // Instance'ı tekrar kontrol et
    if (App.Instance != null)
    {
        Console.WriteLine("✅ WPF Application başlatıldı ve instance hazır");
    }
    else
    {
        Console.WriteLine("⚠️ WPF Application başlatıldı ama instance null");
    }
}

Console.WriteLine("SoftielRemote Agent başlatılıyor...");
Console.WriteLine($"Backend URL: {config.BackendBaseUrl}");
Console.WriteLine($"TCP Port: {config.TcpServerPort}");
Console.WriteLine("Çıkmak için Ctrl+C tuşlarına basın.\n");

// Host'u çalıştır (bu bloklayıcı)
await host.RunAsync();
