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
    Task<bool> SendHeartbeatAsync(string deviceId, string? ipAddress = null);

    /// <summary>
    /// Bekleyen bağlantı isteklerini kontrol eder.
    /// </summary>
    Task<PendingConnectionRequest?> GetPendingConnectionRequestAsync(string deviceId);

    /// <summary>
    /// Bağlantı isteğine yanıt verir (onay/red).
    /// </summary>
    Task<bool> RespondToConnectionRequestAsync(string connectionId, bool accepted);
}

