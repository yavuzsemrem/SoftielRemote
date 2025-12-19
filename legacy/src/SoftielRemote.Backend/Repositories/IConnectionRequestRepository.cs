using SoftielRemote.Backend.Models;

namespace SoftielRemote.Backend.Repositories;

/// <summary>
/// Bağlantı isteklerini yöneten repository interface'i.
/// </summary>
public interface IConnectionRequestRepository
{
    Task<PendingConnectionRequest?> GetByConnectionIdAsync(string connectionId);
    Task<PendingConnectionRequest?> GetPendingByTargetDeviceIdAsync(string targetDeviceId);
    Task<PendingConnectionRequest> CreateAsync(PendingConnectionRequest request);
    Task<PendingConnectionRequest> UpdateAsync(PendingConnectionRequest request);
    Task DeleteAsync(string connectionId);
}

