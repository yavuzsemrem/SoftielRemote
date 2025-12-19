using SoftielRemote.Backend.Models;

namespace SoftielRemote.Backend.Repositories;

/// <summary>
/// Agent verilerini yönetmek için repository interface'i.
/// </summary>
public interface IAgentRepository
{
    /// <summary>
    /// Agent'ı kayıt eder veya günceller.
    /// </summary>
    Task<AgentInfo> RegisterOrUpdateAsync(AgentInfo agent);

    /// <summary>
    /// Device ID'ye göre Agent'ı bulur.
    /// </summary>
    Task<AgentInfo?> GetByDeviceIdAsync(string deviceId);

    /// <summary>
    /// Tüm online Agent'ları getirir.
    /// </summary>
    Task<IEnumerable<AgentInfo>> GetOnlineAgentsAsync();

    /// <summary>
    /// Tüm Agent'ları getirir (debug için).
    /// </summary>
    Task<IEnumerable<AgentInfo>> GetAllAgentsAsync();

    /// <summary>
    /// Agent'ın connection ID'sini günceller.
    /// </summary>
    Task UpdateConnectionIdAsync(string deviceId, string? connectionId);

    /// <summary>
    /// Agent'ın heartbeat'ini günceller.
    /// </summary>
    Task UpdateLastSeenAsync(string deviceId, string? ipAddress = null);
}

