using SoftielRemote.Core.Dtos;

namespace SoftielRemote.App.Services;

/// <summary>
/// Backend API ile iletişim için service interface'i.
/// </summary>
public interface IBackendClientService
{
    /// <summary>
    /// Agent/App'i Backend'e kayıt eder.
    /// </summary>
    Task<AgentRegistrationResponse> RegisterAsync(AgentRegistrationRequest request);

    /// <summary>
    /// Belirli bir Device ID'ye bağlantı isteği gönderir.
    /// </summary>
    Task<ConnectionResponse> RequestConnectionAsync(ConnectionRequest request);
}

