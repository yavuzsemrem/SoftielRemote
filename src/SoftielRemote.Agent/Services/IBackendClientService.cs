using SoftielRemote.Core.Dtos;

namespace SoftielRemote.Agent.Services;

/// <summary>
/// Backend API ile iletişim için service interface'i.
/// </summary>
public interface IBackendClientService
{
    /// <summary>
    /// Agent'ı Backend'e kayıt eder.
    /// </summary>
    Task<AgentRegistrationResponse> RegisterAsync(AgentRegistrationRequest request);

    /// <summary>
    /// Agent'ın online durumunu Backend'e bildirir (heartbeat).
    /// </summary>
    Task<bool> SendHeartbeatAsync(string deviceId);
}

