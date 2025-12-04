namespace SoftielRemote.Backend.Services;

/// <summary>
/// Redis state management servisi interface'i.
/// Agent online/offline durumu ve connection request'leri için.
/// </summary>
public interface IRedisStateService
{
    /// <summary>
    /// Agent'ın online durumunu Redis'te saklar.
    /// </summary>
    Task SetAgentOnlineAsync(string deviceId, TimeSpan? expiration = null);

    /// <summary>
    /// Agent'ın offline durumunu Redis'te işaretler.
    /// </summary>
    Task SetAgentOfflineAsync(string deviceId);

    /// <summary>
    /// Agent'ın online olup olmadığını kontrol eder.
    /// </summary>
    Task<bool> IsAgentOnlineAsync(string deviceId);

    /// <summary>
    /// Connection request'i Redis'te saklar (geçici olarak).
    /// </summary>
    Task CreateConnectionRequestAsync(Core.Dtos.PendingConnectionRequest request);

    /// <summary>
    /// Connection request'i Redis'te saklar (geçici olarak) - eski metod (backward compatibility).
    /// </summary>
    Task SetConnectionRequestAsync(string connectionId, string targetDeviceId, string requesterId, TimeSpan? expiration = null);

    /// <summary>
    /// Connection request'i Redis'ten alır.
    /// </summary>
    Task<string?> GetConnectionRequestAsync(string connectionId);

    /// <summary>
    /// Connection request'i Redis'ten siler.
    /// </summary>
    Task RemoveConnectionRequestAsync(string connectionId);

    /// <summary>
    /// Agent'ın connection ID'sini Redis'te saklar.
    /// </summary>
    Task SetAgentConnectionIdAsync(string deviceId, string connectionId, TimeSpan? expiration = null);

    /// <summary>
    /// Agent'ın connection ID'sini Redis'ten alır.
    /// </summary>
    Task<string?> GetAgentConnectionIdAsync(string deviceId);
}

