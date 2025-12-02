using SoftielRemote.Backend.Models;

namespace SoftielRemote.Backend.Repositories;

/// <summary>
/// In-memory Agent repository implementasyonu (MVP için).
/// </summary>
public class InMemoryAgentRepository : IAgentRepository
{
    private readonly Dictionary<string, AgentInfo> _agents = new();
    private readonly object _lock = new();

    public Task<AgentInfo> RegisterOrUpdateAsync(AgentInfo agent)
    {
        lock (_lock)
        {
            _agents[agent.DeviceId] = agent;
            return Task.FromResult(agent);
        }
    }

    public Task<AgentInfo?> GetByDeviceIdAsync(string deviceId)
    {
        lock (_lock)
        {
            _agents.TryGetValue(deviceId, out var agent);
            return Task.FromResult(agent);
        }
    }

    public Task<IEnumerable<AgentInfo>> GetOnlineAgentsAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_agents.Values.Where(a => a.IsOnline).AsEnumerable());
        }
    }

    public Task UpdateConnectionIdAsync(string deviceId, string? connectionId)
    {
        lock (_lock)
        {
            if (_agents.TryGetValue(deviceId, out var agent))
            {
                agent.ConnectionId = connectionId;
                agent.LastSeen = DateTime.UtcNow;
            }
        }
        return Task.CompletedTask;
    }

    public Task UpdateLastSeenAsync(string deviceId)
    {
        lock (_lock)
        {
            if (_agents.TryGetValue(deviceId, out var agent))
            {
                agent.LastSeen = DateTime.UtcNow;
            }
        }
        return Task.CompletedTask;
    }
}

