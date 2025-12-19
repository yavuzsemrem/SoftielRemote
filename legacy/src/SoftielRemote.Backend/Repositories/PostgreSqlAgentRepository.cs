using Microsoft.EntityFrameworkCore;
using SoftielRemote.Backend.Data;
using SoftielRemote.Backend.Models;

namespace SoftielRemote.Backend.Repositories;

/// <summary>
/// PostgreSQL tabanlı Agent repository implementasyonu (Production-ready).
/// </summary>
public class PostgreSqlAgentRepository : IAgentRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PostgreSqlAgentRepository> _logger;

    public PostgreSqlAgentRepository(
        ApplicationDbContext context,
        ILogger<PostgreSqlAgentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AgentInfo> RegisterOrUpdateAsync(AgentInfo agent)
    {
        const int maxRetries = 2;
        const int retryDelayMs = 500;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (attempt > 1)
                {
                    _logger.LogDebug("RegisterOrUpdateAsync tekrar deneniyor (Deneme {Attempt}/{MaxRetries}): DeviceId={DeviceId}", 
                        attempt, maxRetries, agent.DeviceId);
                    await Task.Delay(retryDelayMs);
                }
                
                var existingEntity = await _context.Agents
                    .FirstOrDefaultAsync(a => a.DeviceId == agent.DeviceId);

                if (existingEntity != null)
                {
                    // Mevcut Agent'ı güncelle
                    // TCP port'u koru (sadece null veya 0 ise)
                    if ((!agent.TcpPort.HasValue || agent.TcpPort.Value == 0) && existingEntity.TcpPort > 0)
                    {
                        agent.TcpPort = existingEntity.TcpPort;
                    }

                    // Entity'yi güncelle
                    existingEntity.MachineName = agent.MachineName;
                    existingEntity.OperatingSystem = agent.OperatingSystem;
                    existingEntity.LastSeen = agent.LastSeen;
                    
                    // ConnectionId güncelle (null ise de güncelle, bağlantı kesildiğinde null olabilir)
                    existingEntity.ConnectionId = agent.ConnectionId;
                    
                    // IpAddress güncelle: Yeni IpAddress varsa mutlaka güncelle
                    // Eğer yeni IpAddress null/empty ise ama mevcut IpAddress varsa, mevcut değeri koru
                    // Eğer yeni IpAddress null/empty ise ve mevcut IpAddress de null ise, hiçbir şey yapma
                    var oldIpAddress = existingEntity.IpAddress;
                    if (!string.IsNullOrEmpty(agent.IpAddress))
                    {
                        // Yeni IpAddress geldi, mutlaka güncelle (mevcut IpAddress null olsa bile)
                        existingEntity.IpAddress = agent.IpAddress;
                        _logger.LogDebug("IpAddress güncellendi: DeviceId={DeviceId}, NewIpAddress={NewIpAddress}, OldIpAddress={OldIpAddress}", 
                            agent.DeviceId, agent.IpAddress, oldIpAddress ?? "null");
                    }
                    else if (string.IsNullOrEmpty(oldIpAddress))
                    {
                        // Hem yeni hem mevcut IpAddress null, hiçbir şey yapma
                        _logger.LogDebug("IpAddress güncellenmedi (hem yeni hem mevcut null): DeviceId={DeviceId}", agent.DeviceId);
                    }
                    else
                    {
                        // Yeni IpAddress null ama mevcut IpAddress var, mevcut değeri koru
                        _logger.LogDebug("IpAddress korundu (yeni null, mevcut var): DeviceId={DeviceId}, ExistingIpAddress={ExistingIpAddress}", 
                            agent.DeviceId, oldIpAddress);
                    }
                    
                    if (agent.TcpPort.HasValue && agent.TcpPort.Value > 0)
                    {
                        existingEntity.TcpPort = agent.TcpPort.Value;
                    }

                    // SaveChangesAsync - explicit timeout ile (120 saniye - Command timeout ile uyumlu)
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
                    _logger.LogDebug("SaveChangesAsync başlatılıyor: DeviceId={DeviceId}", agent.DeviceId);
                    var saveStartTime = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cts.Token);
                    var saveDuration = (DateTime.UtcNow - saveStartTime).TotalMilliseconds;
                    _logger.LogDebug("SaveChangesAsync tamamlandı: DeviceId={DeviceId}, Duration={Duration}ms", agent.DeviceId, saveDuration);
                    return existingEntity.ToAgentInfo();
                }
                else
                {
                    // Yeni Agent ekle
                    var newEntity = AgentEntity.FromAgentInfo(agent);
                    _context.Agents.Add(newEntity);
                    // SaveChangesAsync - explicit timeout ile (120 saniye - Command timeout ile uyumlu)
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
                    _logger.LogDebug("SaveChangesAsync başlatılıyor (yeni kayıt): DeviceId={DeviceId}", agent.DeviceId);
                    var saveStartTime = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cts.Token);
                    var saveDuration = (DateTime.UtcNow - saveStartTime).TotalMilliseconds;
                    _logger.LogDebug("SaveChangesAsync tamamlandı (yeni kayıt): DeviceId={DeviceId}, Duration={Duration}ms", agent.DeviceId, saveDuration);
                    return newEntity.ToAgentInfo();
                }
            }
            catch (OperationCanceledException ex) when (attempt < maxRetries)
            {
                // Timeout durumunda retry yap
                _logger.LogWarning(ex, "SaveChangesAsync timeout oldu, tekrar deneniyor (Deneme {Attempt}/{MaxRetries}): DeviceId={DeviceId}", 
                    attempt, maxRetries, agent.DeviceId);
                // Retry için döngü devam edecek
            }
            catch (OperationCanceledException ex)
            {
                // Son deneme de timeout oldu, kayıt başarılı olmuş olabilir - kontrol et
                _logger.LogWarning(ex, "SaveChangesAsync timeout oldu (son deneme), kayıt kontrol ediliyor: DeviceId={DeviceId}", agent.DeviceId);
                try
                {
                    var savedEntity = await _context.Agents
                        .FirstOrDefaultAsync(a => a.DeviceId == agent.DeviceId);
                    if (savedEntity != null)
                    {
                        _logger.LogInformation("Kayıt başarılı (timeout sonrası kontrol): DeviceId={DeviceId}", agent.DeviceId);
                        return savedEntity.ToAgentInfo();
                    }
                }
                catch (Exception checkEx)
                {
                    _logger.LogError(checkEx, "Timeout sonrası kayıt kontrolü başarısız: DeviceId={DeviceId}", agent.DeviceId);
                }
                throw;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                // Diğer hatalarda da retry yap
                _logger.LogWarning(ex, "RegisterOrUpdateAsync hatası, tekrar deneniyor (Deneme {Attempt}/{MaxRetries}): DeviceId={DeviceId}", 
                    attempt, maxRetries, agent.DeviceId);
                // Retry için döngü devam edecek
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering/updating agent {DeviceId}", agent.DeviceId);
                throw;
            }
        }
        
        // Buraya gelmemeli ama yine de güvenlik için
        throw new InvalidOperationException($"RegisterOrUpdateAsync {maxRetries} deneme sonrası başarısız oldu: DeviceId={agent.DeviceId}");
    }

    public async Task<AgentInfo?> GetByDeviceIdAsync(string deviceId)
    {
        try
        {
            var entity = await _context.Agents
                .FirstOrDefaultAsync(a => a.DeviceId == deviceId);

            return entity?.ToAgentInfo();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent by device ID {DeviceId}", deviceId);
            throw;
        }
    }

    public async Task<IEnumerable<AgentInfo>> GetOnlineAgentsAsync()
    {
        try
        {
            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
            var entities = await _context.Agents
                .Where(a => a.LastSeen >= fiveMinutesAgo)
                .ToListAsync();

            return entities.Select(e => e.ToAgentInfo()).Where(a => a.IsOnline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting online agents");
            throw;
        }
    }

    public async Task<IEnumerable<AgentInfo>> GetAllAgentsAsync()
    {
        try
        {
            var entities = await _context.Agents.ToListAsync();
            return entities.Select(e => e.ToAgentInfo());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all agents");
            throw;
        }
    }

    public async Task UpdateConnectionIdAsync(string deviceId, string? connectionId)
    {
        try
        {
            var entity = await _context.Agents
                .FirstOrDefaultAsync(a => a.DeviceId == deviceId);

            if (entity != null)
            {
                entity.ConnectionId = connectionId;
                entity.LastSeen = DateTime.UtcNow;
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
                await _context.SaveChangesAsync(cts.Token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating connection ID for agent {DeviceId}", deviceId);
            throw;
        }
    }

    public async Task UpdateLastSeenAsync(string deviceId, string? ipAddress = null)
    {
        try
        {
            var entity = await _context.Agents
                .FirstOrDefaultAsync(a => a.DeviceId == deviceId);

            if (entity != null)
            {
                entity.LastSeen = DateTime.UtcNow;
                
                // IpAddress güncelle (varsa ve null değilse)
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    entity.IpAddress = ipAddress;
                }
                
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
                await _context.SaveChangesAsync(cts.Token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last seen for agent {DeviceId}", deviceId);
            throw;
        }
    }
}



