using SoftielRemote.Core.Dtos;

namespace SoftielRemote.Backend.Services;

/// <summary>
/// Agent işlemlerini yöneten service interface'i.
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// Agent'ı kayıt eder.
    /// </summary>
    Task<AgentRegistrationResponse> RegisterAsync(AgentRegistrationRequest request);

    /// <summary>
    /// Agent'ın online durumunu kontrol eder.
    /// </summary>
    Task<bool> IsAgentOnlineAsync(string deviceId);

    /// <summary>
    /// Agent bilgilerini getirir.
    /// </summary>
    Task<Models.AgentInfo?> GetAgentInfoAsync(string deviceId);

    /// <summary>
    /// Agent'ın LastSeen zamanını günceller (heartbeat).
    /// </summary>
    Task UpdateLastSeenAsync(string deviceId, string? ipAddress = null);

    /// <summary>
    /// Tüm Agent'ları getirir (debug için).
    /// </summary>
    Task<IEnumerable<Models.AgentInfo>> GetAllAgentsAsync();
}

