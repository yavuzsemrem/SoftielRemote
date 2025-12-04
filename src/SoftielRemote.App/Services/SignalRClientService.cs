using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using SoftielRemote.Core.Dtos;

namespace SoftielRemote.App.Services;

/// <summary>
/// Backend SignalR Hub'a bağlanan client servisi (App tarafı).
/// WebRTC signaling mesajlarını alır ve gönderir.
/// </summary>
public class SignalRClientService : IDisposable
{
    private readonly ILogger<SignalRClientService> _logger;
    private HubConnection? _connection;
    private bool _disposed = false;
    private string? _currentTargetDeviceId;

    public SignalRClientService(ILogger<SignalRClientService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// SignalR bağlantısını başlatır.
    /// </summary>
    public async Task ConnectAsync(string backendUrl, string? deviceId = null)
    {
        try
        {
            // DeviceId varsa query parameter olarak ekle
            var hubUrl = string.IsNullOrEmpty(deviceId)
                ? $"{backendUrl.TrimEnd('/')}/hubs/connection"
                : $"{backendUrl.TrimEnd('/')}/hubs/connection?deviceId={deviceId}";
            
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            // Event handlers
            _connection.On<WebRTCSignalingMessage>("WebRTCSignaling", OnWebRTCSignaling);
            _connection.On<string>("DeviceRegistered", OnDeviceRegistered);
            _connection.On<string>("SignalingError", OnSignalingError);

            _connection.Reconnecting += error =>
            {
                _logger.LogWarning(error, "SignalR yeniden bağlanıyor...");
                return Task.CompletedTask;
            };

            _connection.Reconnected += connectionId =>
            {
                _logger.LogInformation("SignalR yeniden bağlandı: {ConnectionId}", connectionId);
                // Yeniden bağlandığında deviceId'yi tekrar kaydet
                if (!string.IsNullOrEmpty(deviceId) && _connection != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _connection.InvokeAsync("RegisterDevice", deviceId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Yeniden bağlantıda RegisterDevice hatası");
                        }
                    });
                }
                return Task.CompletedTask;
            };

            _connection.Closed += error =>
            {
                _logger.LogError(error, "SignalR bağlantısı kapandı");
                return Task.CompletedTask;
            };

            await _connection.StartAsync();
            
            // DeviceId varsa RegisterDevice metodunu çağır
            if (!string.IsNullOrEmpty(deviceId))
            {
                await _connection.InvokeAsync("RegisterDevice", deviceId);
                _logger.LogInformation("Device kaydedildi: {DeviceId}", deviceId);
            }
            
            _logger.LogInformation("SignalR bağlantısı kuruldu: {HubUrl}", hubUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR bağlantısı kurulamadı");
            throw;
        }
    }

    /// <summary>
    /// WebRTC signaling mesajını Backend'e gönderir.
    /// </summary>
    public async Task SendWebRTCSignalingAsync(WebRTCSignalingMessage message)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _connection.InvokeAsync("SendWebRTCSignaling", message);
                _logger.LogDebug("WebRTC signaling mesajı gönderildi: Type={Type}, Target={TargetDeviceId}", 
                    message.Type, message.TargetDeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebRTC signaling mesajı gönderilemedi");
            }
        }
        else
        {
            _logger.LogWarning("SignalR bağlantısı aktif değil, mesaj gönderilemedi");
        }
    }

    /// <summary>
    /// Hedef Device ID'yi set eder (connection request için).
    /// </summary>
    public void SetTargetDeviceId(string targetDeviceId)
    {
        _currentTargetDeviceId = targetDeviceId;
        _logger.LogDebug("Hedef Device ID set edildi: {DeviceId}", targetDeviceId);
    }

    private void OnWebRTCSignaling(WebRTCSignalingMessage message)
    {
        _logger.LogDebug("WebRTC signaling mesajı alındı: Type={Type}", message.Type);
        OnSignalingMessageReceived?.Invoke(message);
    }

    private void OnDeviceRegistered(string deviceId)
    {
        _logger.LogInformation("Device kaydedildi: {DeviceId}", deviceId);
    }

    private void OnSignalingError(string error)
    {
        _logger.LogError("Signaling hatası: {Error}", error);
        OnSignalingErrorReceived?.Invoke(error);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _connection?.DisposeAsync().AsTask().Wait();
        _disposed = true;
    }

    // Events
    public event Action<WebRTCSignalingMessage>? OnSignalingMessageReceived;
    public event Action<string>? OnSignalingErrorReceived;
}



