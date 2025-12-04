using SoftielRemote.Backend.Models;
using SoftielRemote.Core.Enums;

namespace SoftielRemote.Backend.Repositories;

/// <summary>
/// In-memory bağlantı isteği repository implementasyonu.
/// </summary>
public class InMemoryConnectionRequestRepository : IConnectionRequestRepository
{
    private readonly Dictionary<string, PendingConnectionRequest> _requests = new();
    private readonly object _lock = new();

    public Task<PendingConnectionRequest?> GetByConnectionIdAsync(string connectionId)
    {
        lock (_lock)
        {
            _requests.TryGetValue(connectionId, out var request);
            return Task.FromResult(request);
        }
    }

    public Task<PendingConnectionRequest?> GetPendingByTargetDeviceIdAsync(string targetDeviceId)
    {
        lock (_lock)
        {
            var request = _requests.Values
                .FirstOrDefault(r => r.TargetDeviceId == targetDeviceId && r.Status == ConnectionStatus.Pending);
            return Task.FromResult(request);
        }
    }

    public Task<PendingConnectionRequest> CreateAsync(PendingConnectionRequest request)
    {
        lock (_lock)
        {
            _requests[request.ConnectionId] = request;
            return Task.FromResult(request);
        }
    }

    public Task<PendingConnectionRequest> UpdateAsync(PendingConnectionRequest request)
    {
        lock (_lock)
        {
            if (_requests.ContainsKey(request.ConnectionId))
            {
                _requests[request.ConnectionId] = request;
            }
            return Task.FromResult(request);
        }
    }

    public Task DeleteAsync(string connectionId)
    {
        lock (_lock)
        {
            _requests.Remove(connectionId);
            return Task.CompletedTask;
        }
    }
}

