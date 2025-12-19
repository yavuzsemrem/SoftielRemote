using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using SoftielRemote.Agent;
using SoftielRemote.Agent.Config;
using SoftielRemote.Agent.InputInjection;
using SoftielRemote.Agent.Networking;
using SoftielRemote.Agent.ScreenCapture;
using SoftielRemote.Agent.Services;
using SoftielRemote.Core.Utils;

namespace SoftielRemote.Agent
{
    class Program
    {
        [STAThread]
        static async Task Main(string[] args)
        {
            // Console window'u kesinlikle açık tut
            try
            {
                Console.WriteLine("========================================");
                Console.WriteLine("SoftielRemote Agent Başlatılıyor...");
                Console.WriteLine("========================================");
                Console.WriteLine($"OS Version: {Environment.OSVersion}");
                Console.WriteLine($"Machine Name: {Environment.MachineName}");
                Console.WriteLine($"User Name: {Environment.UserName}");
                Console.WriteLine($"Current Directory: {Environment.CurrentDirectory}");
                Console.WriteLine($"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
                Console.WriteLine("========================================\n");
            }
            catch { }

            // Global exception handler - EN BAŞTA
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                try
                {
                    Console.WriteLine("\n========================================");
                    Console.WriteLine("❌ UNHANDLED EXCEPTION");
                    Console.WriteLine("========================================");
                    Console.WriteLine($"Exception: {ex?.Message ?? "Unknown"}");
                    Console.WriteLine($"Stack trace: {ex?.StackTrace ?? "N/A"}");
                    if (ex?.InnerException != null)
                    {
                        Console.WriteLine($"Inner: {ex.InnerException.Message}");
                    }
                    Console.WriteLine("========================================\n");
                }
                catch { }

                // Exe yanına crash log yaz
                try
                {
                    var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    var crashLog = Path.Combine(exeDir, $"agent_crash_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                    File.WriteAllText(crashLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CRASH{Environment.NewLine}");
                    File.AppendAllText(crashLog, $"Exception: {ex?.Message ?? "Unknown"}{Environment.NewLine}");
                    File.AppendAllText(crashLog, $"Stack trace: {ex?.StackTrace ?? "N/A"}{Environment.NewLine}");
                    if (ex?.InnerException != null)
                    {
                        File.AppendAllText(crashLog, $"Inner: {ex.InnerException.Message}{Environment.NewLine}");
                    }
                    Console.WriteLine($"📝 Crash log: {crashLog}");
                }
                catch { }

                Console.WriteLine("\n⚠️ Herhangi bir tuşa basın...");
                try { Console.ReadKey(); } catch { }
            };

            // Tüm kodu try-catch ile sarmala
            try
            {
                await RunAgentAsync(args);
            }
            catch (Exception ex)
            {
                try
                {
                    Console.WriteLine("\n========================================");
                    Console.WriteLine("❌ FATAL ERROR");
                    Console.WriteLine("========================================");
                    Console.WriteLine($"Exception: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner: {ex.InnerException.Message}");
                    }
                    Console.WriteLine("========================================\n");

                    // Exe yanına crash log yaz
                    var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    var crashLog = Path.Combine(exeDir, $"agent_crash_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                    File.WriteAllText(crashLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] FATAL ERROR{Environment.NewLine}");
                    File.AppendAllText(crashLog, $"Exception: {ex.Message}{Environment.NewLine}");
                    File.AppendAllText(crashLog, $"Stack trace: {ex.StackTrace}{Environment.NewLine}");
                    if (ex.InnerException != null)
                    {
                        File.AppendAllText(crashLog, $"Inner: {ex.InnerException.Message}{Environment.NewLine}");
                    }
                    Console.WriteLine($"📝 Crash log: {crashLog}");
                }
                catch { }

                Console.WriteLine("\n⚠️ Herhangi bir tuşa basın...");
                try { Console.ReadKey(); } catch { }
                Environment.Exit(1);
            }
        }

        static async Task RunAgentAsync(string[] args)
        {
            Console.WriteLine("🔵 HostApplicationBuilder oluşturuluyor...");
            var builder = Host.CreateApplicationBuilder(args);

            Console.WriteLine("🔵 Logging yapılandırılıyor...");
            builder.Logging.ClearProviders();
            
            // Console logging'i düzgün yapılandır - AddSimpleConsole kullan
            builder.Logging.AddSimpleConsole(options =>
            {
                options.SingleLine = false;
                options.IncludeScopes = false;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
            });
            
            // Information seviyesi yeterli (Debug çok fazla log üretir)
            builder.Logging.SetMinimumLevel(LogLevel.Information);
            
            // Console.WriteLine'ların da çalıştığından emin ol
            Console.Out.Flush();
            Console.WriteLine("✅ Logging yapılandırıldı (MinimumLevel: Information)");

            // Backend URL
            Console.WriteLine("🔵 Backend URL çözülüyor...");
            var configuredBackendUrl = builder.Configuration["BackendBaseUrl"];
            Console.WriteLine($"🔵 Configured Backend URL (appsettings.json): {configuredBackendUrl ?? "null"}");
            
            // Supabase bilgilerini kontrol et
            var supabaseProjectUrl = builder.Configuration["Supabase:ProjectUrl"] ?? builder.Configuration["SupabaseProjectUrl"];
            var supabaseAnonKey = builder.Configuration["Supabase:AnonKey"] ?? builder.Configuration["SupabaseAnonKey"];
            Console.WriteLine($"🔵 Supabase ProjectUrl: {(string.IsNullOrWhiteSpace(supabaseProjectUrl) ? "null" : supabaseProjectUrl)}");
            Console.WriteLine($"🔵 Supabase AnonKey: {(string.IsNullOrWhiteSpace(supabaseAnonKey) ? "null" : "***")}");
            
            var resolvedBackendUrl = await BackendUrlResolver.ResolveBackendUrlAsync(configuredBackendUrl, builder.Configuration);
            
            if (resolvedBackendUrl == null)
            {
                Console.WriteLine("⚠️ Backend URL bulunamadı, fallback kullanılıyor");
                resolvedBackendUrl = configuredBackendUrl ?? "http://localhost:5000";
            }
            Console.WriteLine($"✅ Backend URL çözüldü: {resolvedBackendUrl}");

            // DeviceId
            Console.WriteLine("🔵 Device ID okunuyor...");
            string? deviceId = null;
            try
            {
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
                        Console.WriteLine($"🔵 Device ID okundu: {deviceId}");
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

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                deviceId = MachineIdGenerator.GenerateMachineBasedId();
                Console.WriteLine($"🔵 Device ID üretildi: {deviceId}");
                
                try
                {
                    var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var softielRemotePath = Path.Combine(appDataPath, "SoftielRemote");
                    Directory.CreateDirectory(softielRemotePath);
                    
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Device ID kaydedilemedi: {ex.Message}");
                }
            }

            // Config
            var config = new AgentConfig
            {
                BackendBaseUrl = resolvedBackendUrl,
                DeviceId = deviceId,
                FrameIntervalMs = int.Parse(builder.Configuration["FrameIntervalMs"] ?? "33"),
                ScreenWidth = int.Parse(builder.Configuration["ScreenWidth"] ?? "800"),
                ScreenHeight = int.Parse(builder.Configuration["ScreenHeight"] ?? "600"),
                TcpServerPort = int.Parse(builder.Configuration["TcpServerPort"] ?? "8888")
            };

            builder.Services.AddSingleton(config);
            builder.Services.AddHttpClient<IBackendClientService, BackendClientService>();

            // Screen Capture Service
            Console.WriteLine("🔵 Screen Capture servisi yapılandırılıyor...");
            builder.Services.AddSingleton<IScreenCaptureService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IScreenCaptureService>>();
                try
                {
                    Console.WriteLine("🔵 DirectX deneniyor...");
                    var directXService = new DirectXDesktopDuplicationService(
                        sp.GetRequiredService<ILogger<DirectXDesktopDuplicationService>>());
                    directXService.StartCapture();
                    
                    var testFrame = directXService.CaptureScreenAsync(1920, 1080).GetAwaiter().GetResult();
                    if (testFrame != null)
                    {
                        Console.WriteLine("✅ DirectX başarıyla başlatıldı");
                        return directXService;
                    }
                    else
                    {
                        Console.WriteLine("⚠️ DirectX test frame null, GDI+ fallback");
                        directXService.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ DirectX başlatılamadı: {ex.Message}, GDI+ fallback");
                }
                
                Console.WriteLine("🔄 GDI+ Screen Capture kullanılıyor");
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

            // WPF Application - STA thread'de
            Console.WriteLine("🔵 WPF Application başlatılıyor...");
            var wpfAppReady = new ManualResetEventSlim(false);
            App? wpfAppInstance = null;
            
            var wpfAppThread = new Thread(() =>
            {
                try
                {
                    Console.WriteLine($"🔵 WPF Thread ID: {Thread.CurrentThread.ManagedThreadId}");
                    var app = new App();
                    app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    wpfAppInstance = App.Instance;
                    Thread.Sleep(300);
                    
                    if (wpfAppInstance != null)
                    {
                        Console.WriteLine("✅ WPF Application hazır");
                        wpfAppReady.Set();
                    }
                    else
                    {
                        Console.WriteLine("⚠️ WPF Application instance null");
                        wpfAppReady.Set();
                    }
                    
                    app.Run();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ WPF Application hatası: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    wpfAppReady.Set();
                }
            })
            {
                IsBackground = false,
                Name = "WPF Application Thread"
            };

            wpfAppThread.SetApartmentState(ApartmentState.STA);
            wpfAppThread.Start();

            if (!wpfAppReady.Wait(TimeSpan.FromSeconds(5)))
            {
                Console.WriteLine("⚠️ WPF Application timeout");
            }

            Console.WriteLine("✅ SoftielRemote Agent başlatıldı");
            Console.WriteLine($"Backend URL: {config.BackendBaseUrl}");
            Console.WriteLine($"TCP Port: {config.TcpServerPort}");
            Console.WriteLine("Çıkmak için Ctrl+C tuşlarına basın.\n");

            // Host'u çalıştır
            Console.WriteLine("🚀 Host başlatılıyor (host.RunAsync)...");
            Console.WriteLine("⏳ HostedService'ler başlatılacak (AgentService dahil)...");
            Console.Out.Flush(); // Buffer'ı temizle
            
            try
            {
                // Host'u başlatmadan önce bir log daha yaz
                var logger = host.Services.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("🚀 Host başlatılıyor...");
                logger.LogInformation("⏳ HostedService'ler başlatılacak (AgentService dahil)...");
                
                Console.WriteLine("✅ Logger test edildi, host başlatılıyor...");
                Console.Out.Flush();
                
                // Host'u başlat - RunAsync blocking bir çağrıdır
                Console.WriteLine("⏳ host.RunAsync() çağrılıyor (bu blocking bir çağrıdır)...");
                Console.Out.Flush();
                
                await host.RunAsync();
                
                // Buraya gelmemeli (RunAsync blocking)
                Console.WriteLine("⚠️ host.RunAsync() tamamlandı (beklenmeyen durum)");
                Console.Out.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Host çalıştırma hatası: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
                }
                Console.Out.Flush();
                throw;
            }
        }
    }
}
