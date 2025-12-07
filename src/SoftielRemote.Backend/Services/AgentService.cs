using SoftielRemote.Backend.Models;
using SoftielRemote.Backend.Repositories;
using SoftielRemote.Core.Dtos;
using SoftielRemote.Core.Utils;

namespace SoftielRemote.Backend.Services;

/// <summary>
/// Agent işlemlerini yöneten service implementasyonu.
/// </summary>
public class AgentService : IAgentService
{
    private readonly IAgentRepository _repository;
    private readonly IRedisStateService _redisState;
    private readonly ILogger<AgentService> _logger;

    public AgentService(IAgentRepository repository, IRedisStateService redisState, ILogger<AgentService> logger)
    {
        _repository = repository;
        _redisState = redisState;
        _logger = logger;
    }

    public async Task<AgentRegistrationResponse> RegisterAsync(AgentRegistrationRequest request)
    {
        try
        {
            string deviceId;

            // Eğer DeviceId verilmişse ve geçerliyse kullan, değilse yeni üret
            // Not: Agent ve App aynı makinede çalıştığı için makine bazlı ID üretiyorlar
            // Backend'de ise gelen DeviceId'yi kullanıyoruz veya yoksa rastgele üretiyoruz
            if (!string.IsNullOrWhiteSpace(request.DeviceId) && 
                DeviceIdGenerator.IsValid(request.DeviceId))
            {
                deviceId = request.DeviceId;
            }
            else
            {
                // DeviceId yoksa veya geçersizse yeni üret
                // (Normalde Agent/App'ten DeviceId gelmeli, bu durum nadir)
                deviceId = DeviceIdGenerator.Generate();
                _logger.LogWarning("DeviceId geçersiz veya yok, yeni Device ID üretildi: {DeviceId}", deviceId);
            }

            var agentInfo = new AgentInfo
            {
                DeviceId = deviceId,
                MachineName = request.MachineName ?? Environment.MachineName,
                OperatingSystem = request.OperatingSystem ?? Environment.OSVersion.ToString(),
                IpAddress = request.IpAddress,
                TcpPort = request.TcpPort ?? 8888, // App için null olabilir, default 8888
                LastSeen = DateTime.UtcNow
            };

            _logger.LogInformation("Agent kayıt bilgileri alındı: DeviceId={DeviceId}, IpAddress={IpAddress}, TcpPort={TcpPort}", 
                deviceId, request.IpAddress ?? "null", request.TcpPort?.ToString() ?? "null");

            await _repository.RegisterOrUpdateAsync(agentInfo);
            
            // Agent'ı Redis'te online olarak işaretle
            await _redisState.SetAgentOnlineAsync(deviceId, TimeSpan.FromMinutes(5));
            
            _logger.LogInformation("Agent kaydedildi: DeviceId={DeviceId}, IpAddress={IpAddress}, TcpPort={TcpPort}", 
                deviceId, agentInfo.IpAddress ?? "null", agentInfo.TcpPort);

            return new AgentRegistrationResponse
            {
                DeviceId = deviceId,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent kayıt hatası");
            return new AgentRegistrationResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> IsAgentOnlineAsync(string deviceId)
    {
        // Önce Redis'ten kontrol et (daha hızlı)
        var isOnlineInRedis = await _redisState.IsAgentOnlineAsync(deviceId);
        if (isOnlineInRedis)
        {
            return true;
        }

        // Redis'te yoksa PostgreSQL'den kontrol et
        var agent = await _repository.GetByDeviceIdAsync(deviceId);
        var isOnline = agent?.IsOnline ?? false;
        
        // Eğer PostgreSQL'de online ise Redis'e de kaydet
        if (isOnline)
        {
            await _redisState.SetAgentOnlineAsync(deviceId, TimeSpan.FromMinutes(5));
        }
        
        return isOnline;
    }

    public async Task<Models.AgentInfo?> GetAgentInfoAsync(string deviceId)
    {
        return await _repository.GetByDeviceIdAsync(deviceId);
    }

    public async Task UpdateLastSeenAsync(string deviceId, string? ipAddress = null)
    {
        try
        {
            // Repository'de direkt UpdateLastSeenAsync kullan (daha verimli)
            await _repository.UpdateLastSeenAsync(deviceId, ipAddress);
            
            // Redis'te de online durumunu güncelle
            await _redisState.SetAgentOnlineAsync(deviceId, TimeSpan.FromMinutes(5));
            
            _logger.LogDebug("LastSeen güncellendi: DeviceId={DeviceId}, IpAddress={IpAddress}", 
                deviceId, ipAddress ?? "null");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LastSeen güncelleme hatası: DeviceId={DeviceId}", deviceId);
        }
    }

    public async Task<IEnumerable<Models.AgentInfo>> GetAllAgentsAsync()
    {
        return await _repository.GetAllAgentsAsync();
    }
}

