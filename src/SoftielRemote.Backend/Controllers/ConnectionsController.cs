using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using SoftielRemote.Backend.Hubs;
using SoftielRemote.Backend.Repositories;
using SoftielRemote.Backend.Services;
using SoftielRemote.Core.Dtos;
using SoftielRemote.Core.Enums;

namespace SoftielRemote.Backend.Controllers;

/// <summary>
/// Bağlantı yönetimi endpoint'leri.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConnectionsController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IConnectionRequestRepository _connectionRequestRepository;
    private readonly IRedisStateService _redisState;
    private readonly IHubContext<ConnectionHub> _hubContext;
    private readonly ILogger<ConnectionsController> _logger;

    public ConnectionsController(
        IAgentService agentService,
        IConnectionRequestRepository connectionRequestRepository,
        IRedisStateService redisState,
        IHubContext<ConnectionHub> hubContext,
        ILogger<ConnectionsController> logger)
    {
        _agentService = agentService;
        _connectionRequestRepository = connectionRequestRepository;
        _redisState = redisState;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Belirli bir Device ID'ye bağlantı isteği gönderir.
    /// </summary>
    /// <remarks>
    /// Controller (App), belirli bir Agent'a bağlanmak için bu endpoint'i kullanır.
    /// Agent online olmalıdır, aksi takdirde istek reddedilir.
    /// 
    /// Örnek istek:
    /// 
    ///     POST /api/connections/request
    ///     {
    ///         "targetDeviceId": "280969031",
    ///         "requesterId": "662042270",
    ///         "requesterName": "Support Technician",
    ///         "qualityLevel": 1
    ///     }
    /// 
    /// Rate Limit: 5 istek/dakika (IP bazlı)
    /// </remarks>
    /// <param name="request">Bağlantı isteği bilgileri</param>
    /// <returns>Bağlantı isteği oluşturulduysa ConnectionId ve AgentEndpoint döner</returns>
    /// <response code="200">İstek başarılı (Success=true veya false)</response>
    /// <response code="400">Geçersiz istek veya validation hatası</response>
    /// <response code="429">Rate limit aşıldı</response>
    [HttpPost("request")]
    [EnableRateLimiting("ConnectionRequestPolicy")]
    [ProducesResponseType(typeof(ConnectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ConnectionResponse>> RequestConnection(
        [FromBody] ConnectionRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.TargetDeviceId))
        {
            return BadRequest("TargetDeviceId is required");
        }

        // Agent'ın online olup olmadığını kontrol et
        var agent = await _agentService.GetAgentInfoAsync(request.TargetDeviceId);
        var isOnline = agent?.IsOnline ?? false;
        
        _logger.LogInformation("🔵 Bağlantı isteği kontrolü: TargetDeviceId={TargetDeviceId}, AgentExists={AgentExists}, IsOnline={IsOnline}, LastSeen={LastSeen}, MinutesSinceLastSeen={MinutesSinceLastSeen}",
            request.TargetDeviceId, agent != null, isOnline, agent?.LastSeen ?? DateTime.MinValue, agent != null ? (DateTime.UtcNow - agent.LastSeen).TotalMinutes : -1);
        
        // Eğer Agent bulunamadıysa, kayıtlı tüm Agent'ları logla (debug için)
        if (agent == null)
        {
            var allAgents = await _agentService.GetAllAgentsAsync();
            _logger.LogWarning("❌ Agent bulunamadı: TargetDeviceId={TargetDeviceId}", request.TargetDeviceId);
            _logger.LogWarning("📋 Kayıtlı Agent'lar ({AgentCount}):", allAgents.Count());
            foreach (var a in allAgents)
            {
                var minutesAgo = (DateTime.UtcNow - a.LastSeen).TotalMinutes;
                _logger.LogWarning("  ✅ DeviceId: {DeviceId}, IsOnline: {IsOnline}, LastSeen: {LastSeen} ({MinutesAgo:F1} dakika önce), Machine: {MachineName}", 
                    a.DeviceId, a.IsOnline, a.LastSeen, minutesAgo, a.MachineName ?? "Bilinmiyor");
            }
            _logger.LogWarning("💡 İpucu: Yukarıdaki Device ID'lerden birini kullanın!");
        }

        if (!isOnline)
        {
            var errorMessage = agent == null 
                ? "Agent bulunamadı" 
                : $"Agent is not online (LastSeen: {agent.LastSeen:yyyy-MM-dd HH:mm:ss}, Minutes ago: {(DateTime.UtcNow - agent.LastSeen).TotalMinutes:F1})";
            
            _logger.LogWarning("Bağlantı isteği reddedildi: {ErrorMessage}", errorMessage);
            
            return Ok(new ConnectionResponse
            {
                Success = false,
                Status = ConnectionStatus.Error,
                ErrorMessage = "Agent is not online"
            });
        }

        // Agent bilgileri zaten alındı (yukarıda), tekrar kontrol et
        if (agent == null)
        {
            _logger.LogWarning("Agent bulunamadı: TargetDeviceId={TargetDeviceId}", request.TargetDeviceId);
            return Ok(new ConnectionResponse
            {
                Success = false,
                Status = ConnectionStatus.Error,
                ErrorMessage = "Agent bulunamadı"
            });
        }

        _logger.LogInformation("Agent bilgileri alındı: DeviceId={DeviceId}, IpAddress={IpAddress}, TcpPort={TcpPort}, IsOnline={IsOnline}",
            agent.DeviceId, agent.IpAddress ?? "null", agent.TcpPort, agent.IsOnline);

        // AgentEndpoint oluştur (IP:Port formatında)
        string agentEndpoint;
        var tcpPort = agent.TcpPort ?? 8888; // Default 8888 if null
        if (!string.IsNullOrEmpty(agent.IpAddress))
        {
            agentEndpoint = $"{agent.IpAddress}:{tcpPort}";
            _logger.LogInformation("AgentEndpoint oluşturuldu: {AgentEndpoint}", agentEndpoint);
        }
        else
        {
            // IP adresi yoksa localhost kullan (aynı makinede çalışıyorsa)
            agentEndpoint = $"localhost:{tcpPort}";
            _logger.LogWarning("Agent IP adresi bulunamadı, localhost kullanılıyor: DeviceId={DeviceId}, AgentEndpoint={AgentEndpoint}",
                agent.DeviceId, agentEndpoint);
        }

        // Bağlantı isteğini oluştur
        var connectionId = Guid.NewGuid().ToString();
        var pendingRequest = new Models.PendingConnectionRequest
        {
            ConnectionId = connectionId,
            TargetDeviceId = request.TargetDeviceId,
            RequesterId = request.RequesterId,
            RequesterName = request.RequesterName ?? Environment.MachineName,
            RequesterIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            RequestedAt = DateTime.UtcNow,
            Status = ConnectionStatus.Pending
        };

        await _connectionRequestRepository.CreateAsync(pendingRequest);
        
        // Connection request'i Redis'te de sakla (hızlı erişim için)
        var pendingRequestDto = new Core.Dtos.PendingConnectionRequest
        {
            ConnectionId = connectionId,
            TargetDeviceId = request.TargetDeviceId,
            RequesterId = request.RequesterId,
            RequesterName = pendingRequest.RequesterName,
            RequesterIp = pendingRequest.RequesterIp,
            RequestedAt = pendingRequest.RequestedAt,
            Status = pendingRequest.Status
        };
        await _redisState.CreateConnectionRequestAsync(pendingRequestDto);

        // Agent'a SignalR üzerinden connection request bildirimi gönder
        try
        {
            // Önce Redis'ten ConnectionId'yi kontrol et (hızlı)
            var agentConnectionId = await _redisState.GetAgentConnectionIdAsync(request.TargetDeviceId);
            
            // Redis'te yoksa PostgreSQL'den al (fallback)
            if (string.IsNullOrEmpty(agentConnectionId))
            {
                var agentInfo = await _agentService.GetAgentInfoAsync(request.TargetDeviceId);
                if (agentInfo != null && !string.IsNullOrEmpty(agentInfo.ConnectionId))
                {
                    agentConnectionId = agentInfo.ConnectionId;
                    _logger.LogInformation("Agent ConnectionId PostgreSQL'den alındı: {DeviceId} -> {ConnectionId}", 
                        request.TargetDeviceId, agentConnectionId);
                    
                    // Redis'e de kaydet (cache için)
                    await _redisState.SetAgentConnectionIdAsync(request.TargetDeviceId, agentConnectionId, TimeSpan.FromHours(1));
                }
            }
            
            if (!string.IsNullOrEmpty(agentConnectionId))
            {
                await _hubContext.Clients.Client(agentConnectionId).SendAsync("ConnectionRequest", new
                {
                    ConnectionId = connectionId,
                    RequesterId = request.RequesterId,
                    RequesterName = pendingRequest.RequesterName,
                    RequesterIp = pendingRequest.RequesterIp,
                    RequestedAt = pendingRequest.RequestedAt
                });
                _logger.LogInformation("Connection request SignalR ile Agent'a gönderildi: ConnectionId={ConnectionId}, AgentConnectionId={AgentConnectionId}", 
                    connectionId, agentConnectionId);
            }
            else
            {
                _logger.LogWarning("Agent'ın SignalR connection ID'si bulunamadı (Redis ve PostgreSQL'de yok), connection request bildirimi gönderilemedi: TargetDeviceId={TargetDeviceId}", 
                    request.TargetDeviceId);
                
                // ConnectionId bulunamadıysa, kısa bir süre bekle ve tekrar dene (race condition için)
                await Task.Delay(500);
                agentConnectionId = await _redisState.GetAgentConnectionIdAsync(request.TargetDeviceId);
                if (string.IsNullOrEmpty(agentConnectionId))
                {
                    var agentInfo = await _agentService.GetAgentInfoAsync(request.TargetDeviceId);
                    if (agentInfo != null && !string.IsNullOrEmpty(agentInfo.ConnectionId))
                    {
                        agentConnectionId = agentInfo.ConnectionId;
                    }
                }
                
                if (!string.IsNullOrEmpty(agentConnectionId))
                {
                    await _hubContext.Clients.Client(agentConnectionId).SendAsync("ConnectionRequest", new
                    {
                        ConnectionId = connectionId,
                        RequesterId = request.RequesterId,
                        RequesterName = pendingRequest.RequesterName,
                        RequesterIp = pendingRequest.RequesterIp,
                        RequestedAt = pendingRequest.RequestedAt
                    });
                    _logger.LogInformation("Connection request SignalR ile Agent'a gönderildi (retry sonrası): ConnectionId={ConnectionId}, AgentConnectionId={AgentConnectionId}", 
                        connectionId, agentConnectionId);
                }
                else
                {
                    _logger.LogError("Agent'ın SignalR connection ID'si retry sonrası da bulunamadı: TargetDeviceId={TargetDeviceId}", 
                        request.TargetDeviceId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection request SignalR bildirimi gönderilemedi (request kaydedildi)");
        }

        // Onay bekleniyor - AgentEndpoint'i şimdilik döndürme (onaylandıktan sonra döndürülecek)
        _logger.LogInformation("Bağlantı isteği oluşturuldu: ConnectionId={ConnectionId}, TargetDeviceId={TargetDeviceId}, RequesterId={RequesterId}, Status=Pending (onay bekleniyor)",
            connectionId, request.TargetDeviceId, request.RequesterId);

        return Ok(new ConnectionResponse
        {
            Success = true,
            Status = ConnectionStatus.Pending,
            ConnectionId = connectionId,
            AgentEndpoint = null // Onay bekleniyor, AgentEndpoint henüz verilmiyor
        });
    }

    /// <summary>
    /// Agent'ın bekleyen bağlantı isteklerini kontrol etmesi için endpoint.
    /// </summary>
    /// <remarks>
    /// Agent, kendisine gelen bekleyen bağlantı isteklerini kontrol etmek için bu endpoint'i kullanır.
    /// 
    /// Örnek istek:
    /// 
    ///     GET /api/connections/pending/280969031
    /// 
    /// </remarks>
    /// <param name="deviceId">Agent'ın Device ID'si</param>
    /// <returns>Bekleyen bağlantı isteği varsa döner, yoksa null</returns>
    /// <response code="200">Bekleyen istek bulundu veya bulunamadı (null)</response>
    [HttpGet("pending/{deviceId}")]
    [ProducesResponseType(typeof(Core.Dtos.PendingConnectionRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<Models.PendingConnectionRequest?>> GetPendingRequest(string deviceId)
    {
        var request = await _connectionRequestRepository.GetPendingByTargetDeviceIdAsync(deviceId);
        if (request == null)
        {
            return Ok((Core.Dtos.PendingConnectionRequest?)null);
        }
        
        // Backend model'ini Core DTO'ya çevir
        var dto = new Core.Dtos.PendingConnectionRequest
        {
            ConnectionId = request.ConnectionId,
            TargetDeviceId = request.TargetDeviceId,
            RequesterId = request.RequesterId,
            RequesterName = request.RequesterName,
            RequesterIp = request.RequesterIp,
            RequestedAt = request.RequestedAt,
            Status = request.Status
        };
        
        return Ok(dto);
    }

    /// <summary>
    /// Bağlantı isteğini onayla veya reddet.
    /// </summary>
    /// <remarks>
    /// Agent, kendisine gelen bağlantı isteğini onaylamak veya reddetmek için bu endpoint'i kullanır.
    /// 
    /// Örnek istek (Onay):
    /// 
    ///     POST /api/connections/response
    ///     {
    ///         "connectionId": "123e4567-e89b-12d3-a456-426614174000",
    ///         "accepted": true
    ///     }
    /// 
    /// Örnek istek (Red):
    /// 
    ///     POST /api/connections/response
    ///     {
    ///         "connectionId": "123e4567-e89b-12d3-a456-426614174000",
    ///         "accepted": false
    ///     }
    /// </remarks>
    /// <param name="responseRequest">Bağlantı yanıtı bilgileri</param>
    /// <returns>200 OK</returns>
    /// <response code="200">Yanıt başarılı</response>
    /// <response code="400">Geçersiz istek</response>
    /// <response code="404">Bağlantı isteği bulunamadı</response>
    [HttpPost("response")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RespondToConnection(
        [FromBody] ConnectionResponseRequest responseRequest)
    {
        if (string.IsNullOrWhiteSpace(responseRequest.ConnectionId))
        {
            return BadRequest("ConnectionId is required");
        }

        var request = await _connectionRequestRepository.GetByConnectionIdAsync(responseRequest.ConnectionId);
        if (request == null)
        {
            return NotFound("Connection request not found");
        }

        request.Status = responseRequest.Accepted ? ConnectionStatus.Connecting : ConnectionStatus.Rejected;
        await _connectionRequestRepository.UpdateAsync(request);

        // Redis'te de güncelle
        var pendingRequestDto = await _redisState.GetConnectionRequestAsync(responseRequest.ConnectionId);
        if (pendingRequestDto != null)
        {
            pendingRequestDto.Status = request.Status;
            await _redisState.UpdateConnectionRequestAsync(pendingRequestDto);
        }

        // Eğer kabul edildiyse, AgentEndpoint'i al
        string? agentEndpoint = null;
        if (responseRequest.Accepted)
        {
            var agent = await _agentService.GetAgentInfoAsync(request.TargetDeviceId);
            if (agent != null)
            {
                var tcpPort = agent.TcpPort ?? 8888;
                if (!string.IsNullOrEmpty(agent.IpAddress))
                {
                    agentEndpoint = $"{agent.IpAddress}:{tcpPort}";
                }
                else
                {
                    agentEndpoint = $"localhost:{tcpPort}";
                }
            }
        }

        // Controller'a SignalR üzerinden bildirim gönder
        try
        {
            var requesterId = request.RequesterId ?? string.Empty;
            var requesterConnectionId = await _redisState.GetControllerConnectionIdAsync(requesterId);
            if (!string.IsNullOrEmpty(requesterConnectionId))
            {
                await _hubContext.Clients.Client(requesterConnectionId).SendAsync("ConnectionResponse", new
                {
                    ConnectionId = responseRequest.ConnectionId,
                    Accepted = responseRequest.Accepted,
                    Status = request.Status.ToString(),
                    AgentEndpoint = agentEndpoint
                });
                _logger.LogInformation("Connection response SignalR ile Controller'a gönderildi: ConnectionId={ConnectionId}, RequesterConnectionId={RequesterConnectionId}, Accepted={Accepted}, AgentEndpoint={AgentEndpoint}", 
                    responseRequest.ConnectionId, requesterConnectionId, responseRequest.Accepted, agentEndpoint ?? "null");
            }
            else
            {
                _logger.LogWarning("Controller'ın SignalR connection ID'si bulunamadı, connection response bildirimi gönderilemedi: RequesterId={RequesterId}", 
                    request.RequesterId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection response SignalR bildirimi gönderilemedi (request güncellendi)");
        }

        _logger.LogInformation("Bağlantı isteği yanıtlandı: ConnectionId={ConnectionId}, Accepted={Accepted}",
            responseRequest.ConnectionId, responseRequest.Accepted);

        return Ok();
    }
}

/// <summary>
/// Bağlantı isteği yanıtı.
/// </summary>
public class ConnectionResponseRequest
{
    public string ConnectionId { get; set; } = string.Empty;
    public bool Accepted { get; set; }
}

