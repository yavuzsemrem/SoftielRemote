using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SoftielRemote.Backend.Repositories;
using SoftielRemote.Backend.Services;
using SoftielRemote.Core.Dtos;

namespace SoftielRemote.Backend.Hubs;

/// <summary>
/// SignalR Hub - Agent ve Controller arasında WebRTC signaling için.
/// </summary>
[AllowAnonymous] // Agent ve App'in JWT token olmadan bağlanabilmesi için
public class ConnectionHub : Hub
{
    private readonly ILogger<ConnectionHub> _logger;
    private readonly IAgentRepository _agentRepository;
    private readonly IRedisStateService _redisState;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ConnectionHub(
        ILogger<ConnectionHub> logger,
        IAgentRepository agentRepository,
        IRedisStateService redisState,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _agentRepository = agentRepository;
        _redisState = redisState;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            _logger.LogInformation("Client bağlandı: {ConnectionId}", Context.ConnectionId);
            
            // Device ID'yi connection'a ekle (Context.Items kullanarak)
            // Önce query parameter'dan al, yoksa RegisterDevice metodundan gelecek
            var deviceId = Context.GetHttpContext()?.Request.Query["deviceId"].ToString();
            if (!string.IsNullOrEmpty(deviceId))
            {
                Context.Items["DeviceId"] = deviceId;
                
                // Database işlemlerini fire-and-forget olarak çalıştır (timeout bağlantıyı kapatmasın)
                // Yeni scope oluştur çünkü scoped DbContext dispose edilmiş olabilir
                var connectionId = Context.ConnectionId;
                _ = Task.Run(async () =>
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var agentRepository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
                    
                    try
                    {
                        // Agent'ın kayıtlı olup olmadığını kontrol et
                        var agent = await agentRepository.GetByDeviceIdAsync(deviceId);
                        if (agent != null)
                        {
                            await agentRepository.UpdateConnectionIdAsync(deviceId, connectionId);
                            
                            // Redis'te de connection ID'yi sakla
                            var redisState = scope.ServiceProvider.GetRequiredService<IRedisStateService>();
                            await redisState.SetAgentConnectionIdAsync(deviceId, connectionId, TimeSpan.FromHours(1));
                            await redisState.SetAgentOnlineAsync(deviceId, TimeSpan.FromMinutes(5));
                            
                            _logger.LogInformation("Device {DeviceId} connection ID güncellendi (query): {ConnectionId}", deviceId, connectionId);
                        }
                        else
                        {
                            _logger.LogWarning("Device {DeviceId} henüz kayıtlı değil, RegisterDevice metodu bekleniyor", deviceId);
                        }
                    }
                    catch (Exception dbEx)
                    {
                        _logger.LogWarning(dbEx, "Device {DeviceId} connection ID güncellenirken hata oluştu (bağlantı devam ediyor): {ConnectionId}", deviceId, connectionId);
                    }
                });
            }
            else
            {
                _logger.LogDebug("DeviceId query parameter'da bulunamadı, RegisterDevice metodu bekleniyor");
            }
            
            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnConnectedAsync sırasında kritik hata oluştu: {ConnectionId}", Context.ConnectionId);
            // Sadece kritik hatalarda exception fırlat
            throw;
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var deviceId = Context.Items["DeviceId"]?.ToString();
            if (!string.IsNullOrEmpty(deviceId))
            {
                try
                {
                    await _agentRepository.UpdateConnectionIdAsync(deviceId, null);
                    
                    // Redis'ten de connection ID'yi temizle ve offline işaretle
                    await _redisState.SetAgentOfflineAsync(deviceId);
                    
                    _logger.LogInformation("Device {DeviceId} connection ID temizlendi", deviceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Device {DeviceId} connection ID temizlenirken hata oluştu", deviceId);
                }
            }
            
            if (exception != null)
            {
                _logger.LogWarning(exception, "Client bağlantısı hata ile kesildi: {ConnectionId}", Context.ConnectionId);
            }
            else
            {
                _logger.LogInformation("Client bağlantısı kesildi: {ConnectionId}", Context.ConnectionId);
            }
            
            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnDisconnectedAsync sırasında hata oluştu: {ConnectionId}", Context.ConnectionId);
            // Exception'ı tekrar fırlatma
        }
    }

    /// <summary>
    /// WebRTC signaling mesajını hedef Device ID'ye iletir.
    /// </summary>
    public async Task SendWebRTCSignaling(WebRTCSignalingMessage message)
    {
        try
        {
            _logger.LogDebug("WebRTC signaling mesajı alındı: Type={Type}, Target={TargetDeviceId}, Sender={SenderDeviceId}",
                message.Type, message.TargetDeviceId, message.SenderDeviceId);

            // TargetDeviceId boşsa, sender'ın connection context'inden hedefi bul
            var targetDeviceId = message.TargetDeviceId;
            if (string.IsNullOrWhiteSpace(targetDeviceId))
            {
                // Sender'ın Device ID'sini al
                var senderDeviceId = Context.Items["DeviceId"]?.ToString();
                if (string.IsNullOrWhiteSpace(senderDeviceId))
                {
                    _logger.LogWarning("TargetDeviceId boş ve sender DeviceId bulunamadı");
                    await Clients.Caller.SendAsync("SignalingError", "Target device ID is required");
                    return;
                }

                // ICE candidate'lar için bu normal olabilir (connection request gelmeden önce)
                // Sadece loglama yap, hata gönderme
                if (message.Type.ToLower() == "ice-candidate")
                {
                    _logger.LogDebug("ICE candidate alındı ancak TargetDeviceId boş (connection request bekleniyor)");
                    return;
                }
                
                _logger.LogWarning("TargetDeviceId boş: Type={Type}, Sender={SenderDeviceId}", 
                    message.Type, senderDeviceId);
                await Clients.Caller.SendAsync("SignalingError", "Target device ID is required");
                return;
            }

            // Önce Redis'ten connection ID'yi kontrol et (daha hızlı)
            var connectionIdFromRedis = await _redisState.GetAgentConnectionIdAsync(targetDeviceId);
            
            // Hedef Agent'ı bul
            var targetAgent = await _agentRepository.GetByDeviceIdAsync(targetDeviceId);
            if (targetAgent == null)
            {
                _logger.LogWarning("Hedef Agent bulunamadı: {DeviceId}", targetDeviceId);
                await Clients.Caller.SendAsync("SignalingError", $"Target device {targetDeviceId} not found");
                return;
            }

            // Connection ID'yi Redis'ten veya PostgreSQL'den al
            var targetConnectionId = connectionIdFromRedis ?? targetAgent.ConnectionId;
            
            // Hedef Agent'ın connection ID'si var mı?
            if (string.IsNullOrEmpty(targetConnectionId))
            {
                _logger.LogWarning("Hedef Agent online değil: {DeviceId}", message.TargetDeviceId);
                await Clients.Caller.SendAsync("SignalingError", $"Target device {message.TargetDeviceId} is offline");
                return;
            }

            // Mesajı hedef Agent'a gönder
            await Clients.Client(targetConnectionId).SendAsync("WebRTCSignaling", message);
            _logger.LogInformation("WebRTC signaling mesajı gönderildi: {ConnectionId}", targetConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebRTC signaling mesajı gönderilemedi");
            await Clients.Caller.SendAsync("SignalingError", "Failed to send signaling message");
        }
    }

    /// <summary>
    /// Device ID'yi connection'a kaydeder (Agent veya App bağlandığında).
    /// </summary>
    public async Task RegisterDevice(string deviceId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                _logger.LogWarning("RegisterDevice çağrıldı ancak deviceId boş");
                await Clients.Caller.SendAsync("RegistrationError", "Device ID is required");
                return;
            }
            
            Context.Items["DeviceId"] = deviceId;
            
            // Database işlemlerini fire-and-forget olarak çalıştır (timeout bağlantıyı kapatmasın)
            // Önce başarılı yanıt gönder, sonra database işlemlerini yap
            await Clients.Caller.SendAsync("DeviceRegistered", deviceId);
            
            // Database işlemlerini background'da yap - yeni scope oluştur
            var connectionId = Context.ConnectionId;
            _ = Task.Run(async () =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var agentRepository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
                
                try
                {
                    // Agent'ın kayıtlı olup olmadığını kontrol et
                    var agent = await agentRepository.GetByDeviceIdAsync(deviceId);
                    if (agent != null)
                    {
                        await agentRepository.UpdateConnectionIdAsync(deviceId, connectionId);
                        
                        // Redis'te de connection ID'yi sakla
                        var redisState = scope.ServiceProvider.GetRequiredService<IRedisStateService>();
                        await redisState.SetAgentConnectionIdAsync(deviceId, connectionId, TimeSpan.FromHours(1));
                        await redisState.SetAgentOnlineAsync(deviceId, TimeSpan.FromMinutes(5));
                        
                        _logger.LogInformation("Device kaydedildi: {DeviceId} -> {ConnectionId}", deviceId, connectionId);
                    }
                    else
                    {
                        _logger.LogWarning("Device {DeviceId} henüz kayıtlı değil, önce REST API ile kayıt olmalı", deviceId);
                    }
                }
                catch (Exception dbEx)
                {
                    _logger.LogWarning(dbEx, "Device {DeviceId} kaydedilirken hata oluştu (bağlantı devam ediyor): {ConnectionId}", deviceId, connectionId);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RegisterDevice sırasında kritik hata oluştu: {DeviceId}", deviceId);
            try
            {
                await Clients.Caller.SendAsync("RegistrationError", "Failed to register device");
            }
            catch (Exception sendEx)
            {
                _logger.LogError(sendEx, "RegistrationError mesajı gönderilemedi");
            }
        }
    }
}

