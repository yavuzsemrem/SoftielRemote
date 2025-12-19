using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using SoftielRemote.Core.Dtos;

namespace SoftielRemote.Agent.Services;

/// <summary>
/// Backend SignalR Hub'a bağlanan client servisi.
/// WebRTC signaling mesajlarını alır ve gönderir.
/// </summary>
public class SignalRClientService : IDisposable
{
    private readonly ILogger<SignalRClientService> _logger;
    private HubConnection? _connection;
    private bool _disposed = false;

    public SignalRClientService(ILogger<SignalRClientService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// SignalR bağlantısını başlatır.
    /// </summary>
    public async Task ConnectAsync(string backendUrl, string deviceId)
    {
        try
        {
            var hubUrl = $"{backendUrl.TrimEnd('/')}/hubs/connection?deviceId={deviceId}";
            
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            // Event handlers
            _connection.On<WebRTCSignalingMessage>("WebRTCSignaling", OnWebRTCSignaling);
            _connection.On<string>("DeviceRegistered", OnDeviceRegistered);
            _connection.On<string>("SignalingError", OnSignalingError);
            _connection.On<object>("ConnectionRequest", OnConnectionRequest);

            _connection.Reconnecting += error =>
            {
                _logger.LogWarning(error, "⚠️ SignalR yeniden bağlanıyor...");
                Console.WriteLine($"⚠️ SignalR yeniden bağlanıyor: {error?.Message ?? "Bilinmeyen hata"}");
                return Task.CompletedTask;
            };

            _connection.Reconnected += connectionId =>
            {
                _logger.LogInformation("✅ SignalR yeniden bağlandı: {ConnectionId}", connectionId);
                Console.WriteLine($"✅ SignalR yeniden bağlandı: {connectionId}");
                // Yeniden bağlandığında Device ID'yi tekrar kaydet
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (_connection != null)
                        {
                            await _connection.InvokeAsync("RegisterDevice", deviceId);
                            _logger.LogInformation("✅ Device ID yeniden kaydedildi: {DeviceId}", deviceId);
                            Console.WriteLine($"✅ Device ID yeniden kaydedildi: {deviceId}");
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ SignalR connection null, Device ID yeniden kaydedilemedi: {DeviceId}", deviceId);
                            Console.WriteLine($"⚠️ SignalR connection null, Device ID yeniden kaydedilemedi: {deviceId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Device ID yeniden kaydedilemedi: {DeviceId}", deviceId);
                        Console.WriteLine($"❌ Device ID yeniden kaydedilemedi: {deviceId}, Hata: {ex.Message}");
                    }
                });
                return Task.CompletedTask;
            };

            _connection.Closed += error =>
            {
                _logger.LogError(error, "❌ SignalR bağlantısı kapandı");
                Console.WriteLine($"❌ SignalR bağlantısı kapandı: {error?.Message ?? "Bilinmeyen hata"}");
                return Task.CompletedTask;
            };

            await _connection.StartAsync();
            _logger.LogInformation("✅ SignalR StartAsync tamamlandı, connection state: {State}", _connection.State);
            Console.WriteLine($"✅ SignalR StartAsync tamamlandı, connection state: {_connection.State}");
            
            await _connection.InvokeAsync("RegisterDevice", deviceId);
            _logger.LogInformation("✅ RegisterDevice çağrıldı: {DeviceId}", deviceId);
            Console.WriteLine($"✅ RegisterDevice çağrıldı: {deviceId}");
            
            _logger.LogInformation("✅ SignalR bağlantısı kuruldu: {HubUrl}, ConnectionId: {ConnectionId}, State: {State}", 
                hubUrl, _connection.ConnectionId, _connection.State);
            Console.WriteLine($"✅ SignalR bağlantısı kuruldu: HubUrl={hubUrl}, ConnectionId={_connection.ConnectionId}, State={_connection.State}");
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
                _logger.LogDebug("WebRTC signaling mesajı gönderildi: Type={Type}", message.Type);
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

    private void OnConnectionRequest(object requestData)
    {
        _logger.LogInformation("🔔🔔🔔 Connection request SignalR'den alındı (OnConnectionRequest): {RequestData}", requestData);
        Console.WriteLine($"🔔🔔🔔 Connection request SignalR'den alındı (OnConnectionRequest): {requestData}");
        Console.WriteLine($"🔔 SignalR connection state: {_connection?.State}, ConnectionId: {_connection?.ConnectionId}");
        
        try
        {
            // requestData'yı JSON string'e çevir ve logla
            var json = System.Text.Json.JsonSerializer.Serialize(requestData);
            _logger.LogInformation("🔔 Connection request JSON: {Json}", json);
            Console.WriteLine($"🔔 Connection request JSON: {json}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Connection request JSON'a çevrilemedi: {Exception}", ex.Message);
            Console.WriteLine($"⚠️ Connection request JSON'a çevrilemedi: {ex.Message}");
        }
        
        try
        {
            _logger.LogInformation("🔔 Connection request event handler çağrılıyor...");
            Console.WriteLine("🔔 Connection request event handler çağrılıyor...");
            OnConnectionRequestReceived?.Invoke(requestData);
            _logger.LogInformation("✅ Connection request event handler çağrıldı");
            Console.WriteLine("✅ Connection request event handler çağrıldı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Connection request event handler hatası: {Exception}", ex.Message);
            Console.WriteLine($"❌ Connection request event handler hatası: {ex.Message}");
        }
    }
    
    /// <summary>
    /// SignalR bağlantı durumunu kontrol eder.
    /// </summary>
    public bool IsConnected()
    {
        return _connection?.State == HubConnectionState.Connected;
    }
    
    /// <summary>
    /// SignalR connection ID'sini döndürür.
    /// </summary>
    public string? GetConnectionId()
    {
        return _connection?.ConnectionId;
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
    public event Action<object>? OnConnectionRequestReceived;
}



