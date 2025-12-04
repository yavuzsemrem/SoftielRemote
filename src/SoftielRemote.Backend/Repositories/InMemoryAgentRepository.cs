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
            // Eğer Agent zaten varsa, IP adresini koru (App kayıt olurken IP göndermeyebilir)
            if (_agents.TryGetValue(agent.DeviceId, out var existingAgent))
            {
                // Yeni kayıtta IP adresi yoksa, mevcut IP'yi koru
                if (string.IsNullOrEmpty(agent.IpAddress) && !string.IsNullOrEmpty(existingAgent.IpAddress))
                {
                    agent.IpAddress = existingAgent.IpAddress;
                }
                // Yeni kayıtta TCP port yoksa, mevcut port'u koru
                if (agent.TcpPort == 0 && existingAgent.TcpPort > 0)
                {
                    agent.TcpPort = existingAgent.TcpPort;
                }
            }
            
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

    public Task<IEnumerable<AgentInfo>> GetAllAgentsAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_agents.Values.AsEnumerable());
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

    public Task UpdateLastSeenAsync(string deviceId, string? ipAddress = null)
    {
        lock (_lock)
        {
            if (_agents.TryGetValue(deviceId, out var agent))
            {
                agent.LastSeen = DateTime.UtcNow;
                
                // IpAddress güncelle (varsa ve null değilse)
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    agent.IpAddress = ipAddress;
                }
            }
        }
        return Task.CompletedTask;
    }
}

