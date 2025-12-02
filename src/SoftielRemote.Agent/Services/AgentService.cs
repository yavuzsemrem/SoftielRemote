using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoftielRemote.Agent.Config;
using SoftielRemote.Agent.Networking;
using SoftielRemote.Agent.ScreenCapture;
using SoftielRemote.Core.Dtos;
using SoftielRemote.Core.Messages;
using SoftielRemote.Core.Utils;

namespace SoftielRemote.Agent.Services;

/// <summary>
/// Ana Agent servisi - Backend kaydı, ekran yakalama ve frame gönderimi yönetir.
/// </summary>
public class AgentService : BackgroundService
{
    private readonly IBackendClientService _backendClient;
    private readonly IScreenCaptureService _screenCapture;
    private readonly TcpStreamServer _tcpServer;
    private readonly AgentConfig _config;
    private readonly ILogger<AgentService> _logger;
    private string? _deviceId;

    public AgentService(
        IBackendClientService backendClient,
        IScreenCaptureService screenCapture,
        TcpStreamServer tcpServer,
        AgentConfig config,
        ILogger<AgentService> logger)
    {
        _backendClient = backendClient;
        _screenCapture = screenCapture;
        _tcpServer = tcpServer;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Agent servisi başlatılıyor...");

        // Backend'e kayıt ol
        var localIpAddress = NetworkHelper.GetLocalIpAddress();
        var registrationRequest = new AgentRegistrationRequest
        {
            DeviceId = _config.DeviceId,
            MachineName = Environment.MachineName,
            OperatingSystem = Environment.OSVersion.ToString(),
            IpAddress = localIpAddress,
            TcpPort = _config.TcpServerPort
        };
        
        _logger.LogInformation("Agent kayıt bilgileri: IP={IpAddress}, Port={Port}", 
            localIpAddress ?? "Bulunamadı", _config.TcpServerPort);

        var registrationResponse = await _backendClient.RegisterAsync(registrationRequest);
        
        if (!registrationResponse.Success)
        {
            _logger.LogError("Backend'e kayıt başarısız: {ErrorMessage}", registrationResponse.ErrorMessage);
            return;
        }

        _deviceId = registrationResponse.DeviceId;
        _config.DeviceId = _deviceId;
        _logger.LogInformation("Agent başarıyla kaydedildi. Device ID: {DeviceId}", _deviceId);

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
                    // Frame'i gönder
                    await _tcpServer.SendFrameAsync(frame, stoppingToken);
                    lastFrameTime = now;
                }

                // Input mesajlarını kontrol et (non-blocking)
                var inputMessage = await _tcpServer.ReceiveInputAsync(stoppingToken);
                if (inputMessage != null)
                {
                    _logger.LogDebug("Input mesajı alındı: {Type}", inputMessage.Type);
                    // Faz 1'de input injection yok, sadece loglama
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
}

