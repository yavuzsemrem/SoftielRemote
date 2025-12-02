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
    private readonly ILogger<AgentService> _logger;

    public AgentService(IAgentRepository repository, ILogger<AgentService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AgentRegistrationResponse> RegisterAsync(AgentRegistrationRequest request)
    {
        try
        {
            string deviceId;

            // Eğer DeviceId verilmişse ve geçerliyse kullan, değilse yeni üret
            if (!string.IsNullOrWhiteSpace(request.DeviceId) && 
                DeviceIdGenerator.IsValid(request.DeviceId))
            {
                deviceId = request.DeviceId;
            }
            else
            {
                deviceId = DeviceIdGenerator.Generate();
                _logger.LogInformation("Yeni Device ID üretildi: {DeviceId}", deviceId);
            }

            var agentInfo = new AgentInfo
            {
                DeviceId = deviceId,
                MachineName = request.MachineName ?? Environment.MachineName,
                OperatingSystem = request.OperatingSystem ?? Environment.OSVersion.ToString(),
                IpAddress = request.IpAddress,
                TcpPort = request.TcpPort > 0 ? request.TcpPort : 8888,
                LastSeen = DateTime.UtcNow
            };

            await _repository.RegisterOrUpdateAsync(agentInfo);

            // Password üret
            var password = PasswordGenerator.Generate();

            _logger.LogInformation("Agent kaydedildi: {DeviceId}, Machine: {MachineName}, Password: {Password}", 
                deviceId, agentInfo.MachineName, password);

            return new AgentRegistrationResponse
            {
                DeviceId = deviceId,
                Password = password,
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
        var agent = await _repository.GetByDeviceIdAsync(deviceId);
        return agent?.IsOnline ?? false;
    }

    public async Task<Models.AgentInfo?> GetAgentInfoAsync(string deviceId)
    {
        return await _repository.GetByDeviceIdAsync(deviceId);
    }
}

