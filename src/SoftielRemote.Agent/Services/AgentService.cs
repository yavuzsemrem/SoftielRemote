using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoftielRemote.Agent.Config;
using SoftielRemote.Agent.InputInjection;
using SoftielRemote.Agent.Networking;
using SoftielRemote.Agent.ScreenCapture;
using DirectXDesktopDuplicationService = SoftielRemote.Agent.ScreenCapture.DirectXDesktopDuplicationService;
using SoftielRemote.Core.Dtos;
using SoftielRemote.Core.Messages;
using SoftielRemote.Core.Utils;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SoftielRemote.Agent.Services;

/// <summary>
/// Ana Agent servisi - Backend kaydı, ekran yakalama ve frame gönderimi yönetir.
/// </summary>
public class AgentService : BackgroundService
{
    private readonly IBackendClientService _backendClient;
    private readonly IScreenCaptureService _screenCapture;
    private readonly VideoEncodingService? _videoEncoding;
    private readonly TcpStreamServer _tcpServer;
    private readonly SignalRClientService _signalRClient;
    private readonly WebRTCPeerService _webrtcPeer;
    private readonly IInputInjectionService _inputInjection;
    private readonly AgentConfig _config;
    private readonly ILogger<AgentService> _logger;
    private string? _deviceId;
    private HardwareEncoderType? _detectedEncoder;

    public AgentService(
        IBackendClientService backendClient,
        IScreenCaptureService screenCapture,
        TcpStreamServer tcpServer,
        SignalRClientService signalRClient,
        WebRTCPeerService webrtcPeer,
        IInputInjectionService inputInjection,
        AgentConfig config,
        ILogger<AgentService> logger,
        VideoEncodingService? videoEncoding = null)
    {
        _backendClient = backendClient;
        _screenCapture = screenCapture;
        _videoEncoding = videoEncoding;
        _tcpServer = tcpServer;
        _signalRClient = signalRClient;
        _webrtcPeer = webrtcPeer;
        _inputInjection = inputInjection;
        _config = config;
        _logger = logger;
        
        // Hardware encoder tespit et (eğer VideoEncodingService varsa)
        if (_videoEncoding != null && _config.UseH264Encoding)
        {
            _detectedEncoder = _videoEncoding.DetectHardwareEncoder();
            _logger.LogInformation("Hardware encoder tespit edildi: {EncoderType}", _detectedEncoder);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Agent servisi başlatılıyor...");

        // Backend'e kayıt ol
        var localIpAddress = NetworkHelper.GetLocalIpAddress();
        
        _logger.LogInformation("🔵 Local IP adresi bulunuyor...");
        _logger.LogInformation("🔵 Bulunan IP: {IpAddress}", localIpAddress ?? "NULL (Bulunamadı)");
        
        var registrationRequest = new AgentRegistrationRequest
        {
            DeviceId = _config.DeviceId,
            MachineName = Environment.MachineName,
            OperatingSystem = Environment.OSVersion.ToString(),
            IpAddress = localIpAddress,
            TcpPort = _config.TcpServerPort
        };
        
        _logger.LogInformation("🔵 Agent kayıt isteği hazırlanıyor: IP={IpAddress}, Port={Port}, DeviceId={DeviceId}", 
            localIpAddress ?? "NULL", _config.TcpServerPort, _config.DeviceId);

        var registrationResponse = await _backendClient.RegisterAsync(registrationRequest);
        
        if (!registrationResponse.Success)
        {
            _logger.LogError("Backend'e kayıt başarısız: {ErrorMessage}", registrationResponse.ErrorMessage);
            
            // Timeout durumunda yeni DeviceId üretme - mevcut DeviceId'yi kullan
            // Sadece DeviceId hiç yoksa makine bazlı ID üret (Agent ve App aynı ID'yi kullanmalı)
            if (string.IsNullOrWhiteSpace(_config.DeviceId))
            {
                _deviceId = Core.Utils.MachineIdGenerator.GenerateMachineBasedId();
                _config.DeviceId = _deviceId;
                _logger.LogWarning("Kayıt başarısız ve DeviceId yok, makine bazlı DeviceId üretildi: {DeviceId}", _deviceId);
                
                // Hemen kaydet (Agent ve App aynı dosyayı kullanacak)
                SaveDeviceIdToConfig(_deviceId);
            }
            else
            {
                _deviceId = _config.DeviceId;
                _logger.LogWarning("Kayıt başarısız, mevcut DeviceId kullanılıyor (timeout durumunda yeni DeviceId üretilmedi): {DeviceId}", _deviceId);
            }
        }
        else
        {
            _deviceId = registrationResponse.DeviceId;
            _config.DeviceId = _deviceId;
            
            // Device ID'yi console'a büyük ve görünür şekilde yazdır
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine($"✅ Agent başarıyla kaydedildi!");
            Console.WriteLine($"📱 Device ID: {_deviceId}");
            Console.WriteLine(new string('=', 60) + "\n");
            
            _logger.LogInformation("Agent başarıyla kaydedildi. Device ID: {DeviceId}", _deviceId);
            
            // Device ID'yi appsettings.json'a kaydet
            SaveDeviceIdToConfig(_deviceId);
        }

        // SignalR bağlantısını başlat
        try
        {
            await _signalRClient.ConnectAsync(_config.BackendBaseUrl, _deviceId);
            _signalRClient.OnSignalingMessageReceived += HandleWebRTCSignaling;
            _signalRClient.OnConnectionRequestReceived += HandleConnectionRequest;
            _logger.LogInformation("SignalR bağlantısı kuruldu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR bağlantısı kurulamadı");
        }

        // WebRTC peer connection'ı başlat
        try
        {
            var turnServerUrl = _config.TurnServerUrl;
            _webrtcPeer.Initialize(turnServerUrl);
            _webrtcPeer.OnIceCandidate += HandleIceCandidate;
            _webrtcPeer.OnConnectionStateChange += HandleWebRTCConnectionState;
            _logger.LogInformation("WebRTC peer connection başlatıldı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebRTC peer connection başlatılamadı");
        }

        // TCP Server'ı başlat
        try
        {
            // Client bağlantı event'ini dinle
            _tcpServer.OnClientConnected += OnTcpClientConnected;
            
            await _tcpServer.StartAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP Server başlatma hatası");
            return;
        }

        // Ekran yakalama servisini başlat (DirectX için StartCapture çağrısı gerekli)
        // NOT: Test frame alınmıyor - sadece servis başlatılıyor, frame yakalama client bağlı olduğunda başlayacak
        try
        {
            _logger.LogInformation("🔍 Ekran yakalama servisi başlatılıyor...");
            
            // DirectX Desktop Duplication için StartCapture çağrısı
            if (_screenCapture is DirectXDesktopDuplicationService directXService)
            {
                directXService.StartCapture();
                _logger.LogInformation("✅ DirectX Desktop Duplication başlatıldı (client bağlantısı bekleniyor)");
            }
            else
            {
                _logger.LogInformation("✅ Ekran yakalama servisi hazır (client bağlantısı bekleniyor)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ekran yakalama servisi başlatma hatası: {Message}", ex.Message);
            _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
        }

        // Ana döngü: Ekran yakalama ve frame gönderimi
        var frameInterval = TimeSpan.FromMilliseconds(_config.FrameIntervalMs);
        var lastFrameTime = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Client bağlı değilse bekle
                if (!_tcpServer.IsClientConnected)
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                // Frame interval kontrolü
                var now = DateTime.UtcNow;
                if (now - lastFrameTime < frameInterval)
                {
                    await Task.Delay(10, stoppingToken);
                    continue;
                }

                // Ekran yakalama (ekran boyutları 0 ise tam ekran yakala)
                var captureWidth = _config.ScreenWidth > 0 ? _config.ScreenWidth : 0;
                var captureHeight = _config.ScreenHeight > 0 ? _config.ScreenHeight : 0;
                var frame = await _screenCapture.CaptureScreenAsync(captureWidth, captureHeight);

                if (frame != null)
                {
                    // İlk 5 frame için log, sonra her 30 frame'de bir
                    if (frame.FrameNumber <= 5 || frame.FrameNumber % 30 == 0)
                    {
                        _logger.LogInformation("🖼️ Frame yakalandı: Width={Width}, Height={Height}, DataLength={DataLength}, FrameNumber={FrameNumber}", 
                            frame.Width, frame.Height, frame.ImageData?.Length ?? 0, frame.FrameNumber);
                    }
                    
                    // Frame'i TCP üzerinden gönder
                    await _tcpServer.SendFrameAsync(frame, stoppingToken);
                    
                    // Frame'i WebRTC'ye de gönder (eğer WebRTC bağlantısı varsa)
                    try
                    {
                        if (frame.ImageData != null && frame.ImageData.Length > 0)
                        {
                            // JPEG frame'den Bitmap oluştur
                            using var ms = new MemoryStream(frame.ImageData);
                            using var bitmap = new System.Drawing.Bitmap(ms);
                            
                            // Bitmap'i RGB24 byte array'e çevir
                            var rgbData = BitmapToRgb24(bitmap);
                            
                            // WebRTC'ye gönder (timestamp: frame number * 33ms = ~30 FPS)
                            var timestamp = (uint)(frame.FrameNumber * 33);
                            _webrtcPeer.SendVideoFrame(rgbData, frame.Width, frame.Height, timestamp);
                        }
                    }
                    catch (Exception webrtcEx)
                    {
                        _logger.LogDebug(webrtcEx, "WebRTC'ye frame gönderilemedi (normal, bağlantı yoksa)");
                    }
                    
                    lastFrameTime = now;
                }
                else
                {
                    // Frame yakalanamadı - sadece her 100 denemede bir log (spam önlemek için)
                    // Frame number yok, sadece uyarı ver
                    _logger.LogDebug("⚠️ Frame yakalanamadı (null) - DirectX timeout veya başka bir sorun");
                }

                // Input mesajlarını kontrol et (non-blocking, timeout ile)
                // Not: Bu blocking olmamalı, aksi halde frame gönderimi engellenir
                try
                {
                    var inputMessage = await _tcpServer.ReceiveInputAsync(stoppingToken);
                    if (inputMessage != null)
                    {
                        _logger.LogDebug("Input mesajı alındı: {Type}", inputMessage.Type);
                        // Input injection (WebRTC data channel'den de gelebilir)
                        await _inputInjection.InjectInputAsync(inputMessage);
                    }
                }
                catch (Exception inputEx)
                {
                    // Input okuma hatası frame gönderimini engellememeli
                    _logger.LogDebug(inputEx, "Input okuma hatası (normal, data yoksa)");
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ana döngü hatası");
                await Task.Delay(1000, stoppingToken);
            }
        }

        await _tcpServer.StopAsync();
        _logger.LogInformation("Agent servisi durduruldu");
    }

    /// <summary>
    /// Device ID'yi hem appsettings.json hem de ortak deviceid.json dosyasına kaydeder.
    /// Ortak dosya AppData'da saklanır (Agent ve App aynı dosyayı kullanır).
    /// </summary>
    private void SaveDeviceIdToConfig(string deviceId)
    {
        try
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            
            // 1. appsettings.json'a kaydet (local)
            var configPath = Path.Combine(baseDirectory, "appsettings.json");
            Dictionary<string, object>? config = null;
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                config = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            }
            
            if (config == null)
            {
                config = new Dictionary<string, object>();
            }
            
            config["DeviceId"] = deviceId;
            var options = new JsonSerializerOptions { WriteIndented = true };
            var newJson = JsonSerializer.Serialize(config, options);
            File.WriteAllText(configPath, newJson);
            
            _logger.LogInformation("Device ID appsettings.json'a kaydedildi: {DeviceId}", deviceId);
            
            // 2. Ortak deviceid.json'a kaydet (AppData - Agent ve App aynı dosyayı kullanır)
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var softielRemotePath = Path.Combine(appDataPath, "SoftielRemote");
            Directory.CreateDirectory(softielRemotePath); // Klasör yoksa oluştur
            
            var deviceIdPath = Path.Combine(softielRemotePath, "deviceid.json");
            var deviceIdConfig = new Dictionary<string, object>
            {
                ["DeviceId"] = deviceId,
                ["MachineName"] = Environment.MachineName,
                ["SavedAt"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
            var deviceIdJson = JsonSerializer.Serialize(deviceIdConfig, options);
            File.WriteAllText(deviceIdPath, deviceIdJson);
            
            _logger.LogInformation("Device ID ortak deviceid.json'a kaydedildi: {DeviceId}, Path={Path}", deviceId, deviceIdPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Device ID kaydedilemedi: {DeviceId}", deviceId);
        }
    }

    /// <summary>
    /// Device ID'yi önce ortak deviceid.json'dan (AppData), sonra local appsettings.json'dan okur.
    /// Ortak dosya Agent ve App tarafından paylaşılır.
    /// </summary>
    private string? LoadDeviceIdFromConfig()
    {
        try
        {
            // 1. Önce ortak deviceid.json'dan oku (AppData - Agent ve App aynı dosyayı kullanır)
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var softielRemotePath = Path.Combine(appDataPath, "SoftielRemote");
            var deviceIdPath = Path.Combine(softielRemotePath, "deviceid.json");
            
            if (File.Exists(deviceIdPath))
            {
                var json = File.ReadAllText(deviceIdPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (config != null && config.ContainsKey("DeviceId"))
                {
                    var deviceId = config["DeviceId"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(deviceId))
                    {
                        _logger.LogInformation("Device ID ortak deviceid.json'dan okundu: {DeviceId}, Path={Path}", deviceId, deviceIdPath);
                        return deviceId;
                    }
                }
            }
            
            // 2. Ortak dosya yoksa, local deviceid.json'dan oku (backward compatibility)
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var localDeviceIdPath = Path.Combine(baseDirectory, "deviceid.json");
            if (File.Exists(localDeviceIdPath))
            {
                var json = File.ReadAllText(localDeviceIdPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (config != null && config.ContainsKey("DeviceId"))
                {
                    var deviceId = config["DeviceId"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(deviceId))
                    {
                        _logger.LogInformation("Device ID local deviceid.json'dan okundu: {DeviceId}", deviceId);
                        // Ortak dosyaya da kaydet (migration)
                        SaveDeviceIdToConfig(deviceId);
                        return deviceId;
                    }
                }
            }
            
            // 3. deviceid.json yoksa appsettings.json'dan oku
            var configPath = Path.Combine(baseDirectory, "appsettings.json");
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (config != null && config.ContainsKey("DeviceId"))
                {
                    var deviceId = config["DeviceId"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(deviceId))
                    {
                        _logger.LogInformation("Device ID appsettings.json'dan okundu: {DeviceId}", deviceId);
                        // Ortak dosyaya da kaydet (migration)
                        SaveDeviceIdToConfig(deviceId);
                        return deviceId;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Device ID okunamadı");
        }
        
        return null;
    }

    /// <summary>
    /// WebRTC signaling mesajını işler.
    /// </summary>
    private async void HandleWebRTCSignaling(WebRTCSignalingMessage message)
    {
        try
        {
            _logger.LogInformation("WebRTC signaling mesajı alındı: Type={Type}", message.Type);

            switch (message.Type.ToLower())
            {
                case "offer":
                    // SDP offer alındı, answer oluştur
                    if (!string.IsNullOrEmpty(message.Sdp))
                    {
                        var answerSdp = await _webrtcPeer.CreateAnswerAsync(message.Sdp);
                        
                        // Answer'ı geri gönder
                        var answerMessage = new WebRTCSignalingMessage
                        {
                            Type = "answer",
                            TargetDeviceId = message.SenderDeviceId,
                            SenderDeviceId = _deviceId ?? string.Empty,
                            ConnectionId = message.ConnectionId,
                            Sdp = answerSdp
                        };
                        
                        await _signalRClient.SendWebRTCSignalingAsync(answerMessage);
                    }
                    break;

                case "ice-candidate":
                    // ICE candidate ekle
                    if (message.IceCandidate != null)
                    {
                        _logger.LogInformation("ICE candidate eklendi: {Candidate}", message.IceCandidate.Candidate);
                        _webrtcPeer.AddIceCandidate(message.IceCandidate);
                    }
                    break;

                default:
                    _logger.LogWarning("Bilinmeyen signaling mesaj tipi: {Type}", message.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebRTC signaling mesajı işlenemedi");
        }
    }

    /// <summary>
    /// ICE candidate'ı Backend'e gönderir.
    /// </summary>
    private async void HandleIceCandidate(IceCandidateDto candidate)
    {
        try
        {
            // Hedef Device ID'yi WebRTC peer service'ten al
            // Connection request geldiğinde hedef Device ID set edilmiş olmalı
            // Eğer hala bilinmiyorsa, candidate'ı sakla ve connection request geldiğinde gönder
            
            // Şimdilik connection request'teki requester ID'yi kullan
            // TODO: Daha iyi bir yönetim için pending candidate listesi tutulabilir
            
            _logger.LogInformation("ICE candidate alındı: {Candidate}, Type={Type}", 
                candidate.Candidate, candidate.Candidate.Contains("host") ? "host" : "srflx/relay");
            
            // ICE candidate'ı Backend'e gönder (eğer hedef Device ID biliniyorsa)
            // Connection request geldiğinde hedef Device ID set edilecek
            // Şimdilik candidate'ları göndermeyi connection request handler'ında yapacağız
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ICE candidate işlenemedi");
        }
    }

    /// <summary>
    /// WebRTC connection state değişikliğini işler.
    /// </summary>
    private void HandleWebRTCConnectionState(SIPSorcery.Net.RTCPeerConnectionState state)
    {
        _logger.LogInformation("WebRTC connection state: {State}", state);
        
        if (state == SIPSorcery.Net.RTCPeerConnectionState.connected)
        {
            _inputInjection.IsEnabled = true;
            _logger.LogInformation("Input injection aktif edildi");
        }
        else if (state == SIPSorcery.Net.RTCPeerConnectionState.disconnected ||
                 state == SIPSorcery.Net.RTCPeerConnectionState.failed)
        {
            _inputInjection.IsEnabled = false;
            _logger.LogInformation("Input injection devre dışı bırakıldı");
        }
    }

    /// <summary>
    /// Connection request'i işler ve kullanıcıya onay dialog gösterir.
    /// </summary>
    private async void HandleConnectionRequest(object requestData)
    {
        try
        {
            _logger.LogInformation("Connection request alındı: {RequestData}", requestData);
            
            // requestData'yı dynamic olarak parse et
            System.Text.Json.JsonElement? request = null;
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(requestData);
                request = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection request parse edilemedi");
                return;
            }

            if (request == null)
            {
                _logger.LogWarning("Connection request null");
                return;
            }

            var connectionId = request.Value.GetProperty("ConnectionId").GetString() ?? string.Empty;
            var requesterName = request.Value.GetProperty("RequesterName").GetString() ?? "Bilinmeyen";
            var requesterIp = request.Value.GetProperty("RequesterIp").GetString() ?? "Bilinmeyen";
            var requesterId = request.Value.GetProperty("RequesterId").GetString() ?? "Bilinmeyen";

            // Hem logger hem de console'a yaz (logger çalışmıyor olabilir)
            _logger.LogInformation("🔔 Connection request dialog gösteriliyor: ConnectionId={ConnectionId}, RequesterName={RequesterName}, RequesterIp={RequesterIp}", 
                connectionId, requesterName, requesterIp);
            Console.WriteLine($"🔔 Connection request dialog gösteriliyor: ConnectionId={connectionId}, RequesterName={requesterName}, RequesterIp={requesterIp}");

            // TCP Server'ı onay beklemeye al
            _tcpServer.WaitForApproval();
            Console.WriteLine("⏸️ TCP Server onay bekliyor...");

            // WPF UI thread'inde dialog göster
            Views.ConnectionRequestDialog? dialog = null;
            bool? dialogResult = null;
            var dialogResultEvent = new System.Threading.ManualResetEventSlim(false);
            
            // WPF Application instance'ını al (maksimum 10 saniye bekle - daha uzun süre)
            App? wpfApp = null;
            var maxWaitTime = DateTime.UtcNow.AddSeconds(10);
            var retryCount = 0;
            Console.WriteLine("🔍 WPF Application instance aranıyor...");
            while (wpfApp == null && DateTime.UtcNow < maxWaitTime)
            {
                wpfApp = App.Instance;
                if (wpfApp == null)
                {
                    retryCount++;
                    _logger.LogDebug("WPF Application instance bekleniyor... (Retry: {RetryCount})", retryCount);
                    Console.WriteLine($"⏳ WPF Application instance bekleniyor... (Retry: {retryCount})");
                    await Task.Delay(200); // 200ms bekle
                }
            }
            
            if (wpfApp == null)
            {
                var errorMsg = "WPF Application instance bulunamadı (timeout) - dialog gösterilemedi";
                _logger.LogError(errorMsg);
                _logger.LogError("App.Instance değeri: {Instance}", App.Instance?.ToString() ?? "NULL");
                Console.WriteLine($"❌ {errorMsg}");
                Console.WriteLine($"❌ App.Instance değeri: {App.Instance?.ToString() ?? "NULL"}");
                // Hata durumunda da TCP server'a reddet
                _tcpServer.RejectConnection();
                return;
            }
            
            _logger.LogInformation("✅ WPF Application instance bulundu, dialog gösteriliyor");
            Console.WriteLine("✅ WPF Application instance bulundu, dialog gösteriliyor");
            
            // Dispatcher'ın çalıştığından emin ol
            if (wpfApp.Dispatcher == null)
            {
                var errorMsg = "WPF Dispatcher null - dialog gösterilemedi";
                _logger.LogError(errorMsg);
                Console.WriteLine($"❌ {errorMsg}");
                _tcpServer.RejectConnection();
                return;
            }
            
            Console.WriteLine($"✅ WPF Dispatcher mevcut: ThreadId={System.Threading.Thread.CurrentThread.ManagedThreadId}");
            
            // BeginInvoke kullan (Invoke bloklayıcı ve deadlock oluşturabilir)
            var action = new Action(() =>
            {
                try
                {
                    _logger.LogInformation("Dialog oluşturuluyor: RequesterName={RequesterName}, RequesterIp={RequesterIp}", requesterName, requesterIp);
                    Console.WriteLine($"🔨 Dialog oluşturuluyor: RequesterName={requesterName}, RequesterIp={requesterIp}");
                    
                    dialog = new Views.ConnectionRequestDialog(requesterName, requesterIp, requesterId);
                    Console.WriteLine($"✅ Dialog oluşturuldu");
                    
                    // Bağlantı kesme event'ini dinle
                    var wpfAppForDisconnect = wpfApp; // Closure için local copy
                    dialog.OnDisconnectRequested += async (s, e) =>
                    {
                        _logger.LogInformation("Bağlantı kesme isteği alındı: ConnectionId={ConnectionId}", connectionId);
                        
                        // TCP bağlantısını kes
                        try
                        {
                            await _tcpServer.StopAsync();
                            _logger.LogInformation("TCP bağlantısı kesildi");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "TCP bağlantısı kesilirken hata oluştu");
                        }
                        
                        // Dialog'u kapat
                        wpfAppForDisconnect.Dispatcher.Invoke(() =>
                        {
                            dialog?.CloseDialog();
                        });
                    };
                    
                    // Dialog'un Result değişikliğini dinle
                    dialog.OnResultChanged += (s, e) =>
                    {
                        if (dialog != null && dialog.Result.HasValue)
                        {
                            dialogResult = dialog.Result;
                            dialogResultEvent.Set();
                        }
                    };
                    
                    // Dialog'u göster ve aktif et
                    dialog.Show();
                    dialog.Activate();
                    dialog.Focus();
                    dialog.BringIntoView();
                    dialog.Topmost = true;
                    dialog.WindowState = WindowState.Normal; // Normal durumda göster
                    dialog.ShowInTaskbar = true; // Taskbar'da göster
                    
                    // Dialog'un görünür olduğundan emin ol
                    dialog.Visibility = Visibility.Visible;
                    dialog.Opacity = 1.0;
                    
                    _logger.LogInformation("✅ Dialog gösterildi: Title={Title}, IsVisible={IsVisible}, IsLoaded={IsLoaded}", 
                        dialog.Title, dialog.IsVisible, dialog.IsLoaded);
                    Console.WriteLine($"✅ Dialog gösterildi: Title={dialog.Title}, IsVisible={dialog.IsVisible}, IsLoaded={dialog.IsLoaded}");
                    
                    // Win32 API ile pencereyi zorla öne getir (dialog gösterildikten sonra - biraz gecikme ile)
                    // Dispatcher thread'inde çalıştır
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // Dialog'un tamamen yüklenmesi için kısa bekleme
                            await Task.Delay(300);
                            
                            // Dispatcher thread'inde Win32 API çağrılarını yap
                            var dialogForWin32 = dialog; // Closure için local copy
                            wpfApp.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    var hwnd = new WindowInteropHelper(dialogForWin32).Handle;
                                    if (hwnd != IntPtr.Zero)
                                    {
                                        _logger.LogInformation("✅ Dialog HWND alındı: {HWND}", hwnd);
                                        Console.WriteLine($"✅ Dialog HWND alındı: {hwnd}");
                                        
                                        // Pencereyi öne getir
                                        SetForegroundWindow(hwnd);
                                        ShowWindow(hwnd, SW_RESTORE);
                                        SetWindowPos(hwnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                                        
                                        // Pencereyi flash yap (dikkat çekmek için)
                                        FlashWindow(hwnd, true);
                                        
                                        _logger.LogInformation("✅ Dialog Win32 API ile öne getirildi");
                                        Console.WriteLine("✅ Dialog Win32 API ile öne getirildi");
                                    }
                                    else
                                    {
                                        _logger.LogWarning("⚠️ Dialog HWND alınamadı (henüz hazır değil)");
                                        Console.WriteLine("⚠️ Dialog HWND alınamadı (henüz hazır değil)");
                                    }
                                }
                                catch (Exception win32Ex)
                                {
                                    _logger.LogWarning(win32Ex, "Win32 API ile pencere öne getirilemedi");
                                    Console.WriteLine($"⚠️ Win32 API hatası: {win32Ex.Message}");
                                }
                            }));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Win32 API çağrısı sırasında hata");
                            Console.WriteLine($"⚠️ Win32 API çağrısı hatası: {ex.Message}");
                        }
                    });
                    
                    _logger.LogInformation("✅ Dialog gösterildi ve aktif edildi");
                    Console.WriteLine("✅ Dialog gösterildi ve aktif edildi");
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Connection request dialog gösterilemedi: {ex.Message}";
                    _logger.LogError(ex, errorMsg);
                    _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                    Console.WriteLine($"❌ {errorMsg}");
                    Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                    Console.WriteLine($"❌ Inner exception: {ex.InnerException?.Message ?? "None"}");
                    dialogResultEvent.Set();
                }
            });
            
            wpfApp.Dispatcher.BeginInvoke(action);

            // Dialog'un kabul/reddet butonuna tıklanmasını bekle (maksimum 60 saniye)
            if (dialogResultEvent.Wait(TimeSpan.FromSeconds(60)))
            {
                // Dialog sonucunu Backend'e gönder
                if (dialogResult.HasValue)
                {
                    var accepted = dialogResult.Value;
                    _logger.LogInformation("Connection request yanıtı: ConnectionId={ConnectionId}, Accepted={Accepted}", connectionId, accepted);
                    
                    try
                    {
                        var success = await _backendClient.RespondToConnectionRequestAsync(connectionId, accepted);
                        if (success)
                        {
                            _logger.LogInformation("Connection request yanıtı Backend'e gönderildi: ConnectionId={ConnectionId}, Accepted={Accepted}", connectionId, accepted);
                            
                            if (accepted)
                            {
                                // Onay verildi - TCP Server'a onay ver
                                _tcpServer.ApproveConnection();
                                
                                // Hedef Device ID'yi WebRTC peer service'e set et (ICE candidate gönderimi için)
                                _webrtcPeer.SetTargetDeviceId(requesterId);
                                
                                // Dialog'u bağlantı kontrol moduna geçir
                                if (dialog != null)
                                {
                                    var wpfAppForState = App.Instance;
                                    if (wpfAppForState != null)
                                    {
                                        wpfAppForState.Dispatcher.Invoke(() =>
                                        {
                                            dialog.ShowConnectedState();
                                        });
                                    }
                                }
                            }
                            else
                            {
                                // Reddedildi - TCP Server'a reddet
                                _tcpServer.RejectConnection();
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Connection request yanıtı Backend'e gönderilemedi: ConnectionId={ConnectionId}", connectionId);
                            // Hata durumunda da reddet
                            _tcpServer.RejectConnection();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Connection request yanıtı gönderilirken hata oluştu: ConnectionId={ConnectionId}", connectionId);
                        // Hata durumunda da reddet
                        _tcpServer.RejectConnection();
                    }
                }
            }
            else
            {
                _logger.LogWarning("Connection request dialog timeout: ConnectionId={ConnectionId}", connectionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection request işlenemedi");
        }
    }

    /// <summary>
    /// TCP client bağlandığında ekran paylaşımı bildirimi gösterir.
    /// </summary>
    private void OnTcpClientConnected(string clientEndPoint)
    {
        try
        {
            _logger.LogInformation("TCP client bağlandı, ekran paylaşımı bildirimi gösteriliyor: {EndPoint}", clientEndPoint);
            
            // WPF UI thread'inde notification göster
            var wpfApp = App.Instance;
            if (wpfApp == null)
            {
                _logger.LogWarning("WPF Application instance null - notification gösterilemedi");
                return;
            }

            // BeginInvoke kullan (non-blocking)
            wpfApp.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                try
                {
                    var notification = new Views.ScreenSharingNotificationWindow();
                    notification.Show(); // ShowDialog değil, Show - modal olmayan
                    _logger.LogInformation("✅ Ekran paylaşımı bildirimi gösterildi");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ekran paylaşımı bildirimi gösterilemedi: {Message}", ex.Message);
                }
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP client bağlantı bildirimi işlenemedi: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Bitmap'i RGB24 formatında byte array'e çevirir (WebRTC için).
    /// </summary>
    private byte[] BitmapToRgb24(Bitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        var rgbData = new byte[width * height * 3]; // RGB24 = 3 bytes per pixel
        
        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);
        
        try
        {
            unsafe
            {
                var sourcePtr = (byte*)bitmapData.Scan0;
                var destIndex = 0;
                
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var pixelIndex = y * bitmapData.Stride + x * 3;
                        rgbData[destIndex++] = sourcePtr[pixelIndex + 2]; // R
                        rgbData[destIndex++] = sourcePtr[pixelIndex + 1]; // G
                        rgbData[destIndex++] = sourcePtr[pixelIndex];     // B
                    }
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
        
        return rgbData;
    }

    #region Win32 API - Pencere yönetimi için

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlashWindow(IntPtr hWnd, bool bInvert);

    private const int SW_RESTORE = 9;
    private static readonly IntPtr HWND_TOP = new IntPtr(0);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_SHOWWINDOW = 0x0040;

    #endregion
}

