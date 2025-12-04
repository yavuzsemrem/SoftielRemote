using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoftielRemote.Agent.Config;
using SoftielRemote.Agent.InputInjection;
using SoftielRemote.Agent.Networking;
using SoftielRemote.Agent.ScreenCapture;
using SoftielRemote.Core.Dtos;
using SoftielRemote.Core.Messages;
using SoftielRemote.Core.Utils;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

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
            if (!string.IsNullOrEmpty(registrationResponse.Password))
            {
                Console.WriteLine($"🔑 Password: {registrationResponse.Password}");
            }
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
            await _tcpServer.StartAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP Server başlatma hatası");
            return;
        }

        // Ekran yakalama servisini test et (bir kez çalıştır)
        try
        {
            _logger.LogInformation("🔍 Ekran yakalama servisi test ediliyor...");
            var testFrame = await _screenCapture.CaptureScreenAsync(
                _config.ScreenWidth > 0 ? _config.ScreenWidth : 800,
                _config.ScreenHeight > 0 ? _config.ScreenHeight : 600);
            
            if (testFrame != null)
            {
                _logger.LogInformation("✅ Ekran yakalama servisi çalışıyor: {Width}x{Height}, Size: {Size} bytes",
                    testFrame.Width, testFrame.Height, testFrame.ImageData?.Length ?? 0);
            }
            else
            {
                _logger.LogWarning("⚠️ Ekran yakalama servisi test frame'i null döndü");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ekran yakalama servisi test hatası: {Message}", ex.Message);
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

                // Ekran yakalama
                var frame = await _screenCapture.CaptureScreenAsync(
                    _config.ScreenWidth, 
                    _config.ScreenHeight);

                if (frame != null)
                {
                    _logger.LogInformation("🖼️ Frame yakalandı: Width={Width}, Height={Height}, DataLength={DataLength}, FrameNumber={FrameNumber}", 
                        frame.Width, frame.Height, frame.ImageData?.Length ?? 0, frame.FrameNumber);
                    
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
                    _logger.LogWarning("⚠️ Frame yakalanamadı (null)");
                }

                // Input mesajlarını kontrol et (non-blocking)
                var inputMessage = await _tcpServer.ReceiveInputAsync(stoppingToken);
                if (inputMessage != null)
                {
                    _logger.LogDebug("Input mesajı alındı: {Type}", inputMessage.Type);
                    // Input injection (WebRTC data channel'den de gelebilir)
                    await _inputInjection.InjectInputAsync(inputMessage);
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
            // ICE candidate gönderilirken hedef Device ID henüz bilinmiyor
            // Bu normal bir durum - connection request geldiğinde hedef Device ID set edilecek
            // Şimdilik sadece loglama yap, gerçek signaling connection request geldiğinde olacak
            _logger.LogDebug("ICE candidate alındı, ancak hedef Device ID henüz bilinmiyor (connection request bekleniyor)");
            
            // TODO: Connection request geldiğinde hedef Device ID'yi set et ve ICE candidate'ları gönder
            // Şimdilik ICE candidate'ları sakla veya connection request geldiğinde gönder
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

            _logger.LogInformation("Connection request dialog gösteriliyor: ConnectionId={ConnectionId}, RequesterName={RequesterName}, RequesterIp={RequesterIp}", 
                connectionId, requesterName, requesterIp);

            // WPF UI thread'inde dialog göster
            bool? dialogResult = null;
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var dialog = new Views.ConnectionRequestDialog(requesterName, requesterIp, requesterId);
                    dialog.ShowDialog();
                    dialogResult = dialog.Result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Connection request dialog gösterilemedi");
                }
            });

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
                    }
                    else
                    {
                        _logger.LogWarning("Connection request yanıtı Backend'e gönderilemedi: ConnectionId={ConnectionId}", connectionId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Connection request yanıtı gönderilirken hata oluştu: ConnectionId={ConnectionId}", connectionId);
                }
            }
            else
            {
                _logger.LogWarning("Connection request dialog sonucu alınamadı: ConnectionId={ConnectionId}", connectionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection request işlenemedi");
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
}

