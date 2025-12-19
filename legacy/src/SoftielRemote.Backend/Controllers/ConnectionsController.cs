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
    // [EnableRateLimiting("ConnectionRequestPolicy")] // Geçici olarak devre dışı
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
        var isOnline = await _agentService.IsAgentOnlineAsync(request.TargetDeviceId);
        
        var lastSeenInfo = agent != null 
            ? $"{agent.LastSeen:yyyy-MM-dd HH:mm:ss} UTC ({(DateTime.UtcNow - agent.LastSeen).TotalMinutes:F1} dakika önce)"
            : "Bilinmiyor";
        
        _logger.LogInformation("🔵 Bağlantı isteği kontrolü: TargetDeviceId={TargetDeviceId}, AgentExists={AgentExists}, IsOnline={IsOnline}, LastSeen={LastSeen}",
            request.TargetDeviceId, agent != null, isOnline, lastSeenInfo);
        
        // Eğer Agent bulunamadıysa veya online değilse, kayıtlı tüm Agent'ları logla (debug için)
        if (agent == null || !isOnline)
        {
            var allAgents = await _agentService.GetAllAgentsAsync();
            _logger.LogWarning("❌ Agent bulunamadı veya offline: TargetDeviceId={TargetDeviceId}, AgentExists={AgentExists}, IsOnline={IsOnline}", 
                request.TargetDeviceId, agent != null, isOnline);
            _logger.LogWarning("📋 Kayıtlı tüm Agent'lar ({AgentCount}):", allAgents.Count());
            foreach (var a in allAgents)
            {
                var minutesAgo = (DateTime.UtcNow - a.LastSeen).TotalMinutes;
                var redisConnectionId = await _redisState.GetAgentConnectionIdAsync(a.DeviceId);
                _logger.LogWarning("  📱 DeviceId: {DeviceId}, IsOnline: {IsOnline}, LastSeen: {LastSeen} ({MinutesAgo:F1} dakika önce), ConnectionId: {ConnectionId}, RedisConnectionId: {RedisConnectionId}, Machine: {MachineName}", 
                    a.DeviceId, a.IsOnline, a.LastSeen, minutesAgo, a.ConnectionId ?? "null", redisConnectionId ?? "null", a.MachineName ?? "Bilinmiyor");
            }
            _logger.LogWarning("💡 İpucu: Yukarıdaki Device ID'lerden birini kullanın!");
        }

        if (!isOnline)
        {
            var errorMessage = agent == null 
                ? "Agent bulunamadı" 
                : $"Agent is not online (LastSeen: {agent.LastSeen:yyyy-MM-dd HH:mm:ss}, Minutes ago: {(DateTime.UtcNow - agent.LastSeen).TotalMinutes:F1})";
            
            _logger.LogWarning("Bağlantı isteği reddedildi: {ErrorMessage}", errorMessage);
            
            var errorResponse = new ConnectionResponse
            {
                Success = false,
                Status = ConnectionStatus.Error,
                ErrorMessage = "Agent is not online",
                ConnectionId = null // Agent online olmadığı için ConnectionId yok
            };
            
            _logger.LogInformation("Connection response döndürülüyor: Success={Success}, Status={Status}, ErrorMessage={ErrorMessage}, ConnectionId={ConnectionId}",
                errorResponse.Success, errorResponse.Status, errorResponse.ErrorMessage, errorResponse.ConnectionId);
            
            return Ok(errorResponse);
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
            _logger.LogInformation("🔵 Agent'a connection request gönderiliyor: TargetDeviceId={TargetDeviceId}, ConnectionId={ConnectionId}", 
                request.TargetDeviceId, connectionId);
            
            // Önce Redis'ten ConnectionId'yi kontrol et (hızlı)
            var agentConnectionId = await _redisState.GetAgentConnectionIdAsync(request.TargetDeviceId);
            _logger.LogDebug("🔍 Redis'ten Agent connection ID kontrolü: TargetDeviceId={TargetDeviceId}, ConnectionId={ConnectionId}", 
                request.TargetDeviceId, agentConnectionId ?? "null");
            
            // Redis'te yoksa PostgreSQL'den al (fallback)
            if (string.IsNullOrEmpty(agentConnectionId))
            {
                var agentInfo = await _agentService.GetAgentInfoAsync(request.TargetDeviceId);
                if (agentInfo != null && !string.IsNullOrEmpty(agentInfo.ConnectionId))
                {
                    agentConnectionId = agentInfo.ConnectionId;
                    _logger.LogInformation("✅ Agent ConnectionId PostgreSQL'den alındı: {DeviceId} -> {ConnectionId}", 
                        request.TargetDeviceId, agentConnectionId);
                    
                    // Redis'e de kaydet (cache için)
                    await _redisState.SetAgentConnectionIdAsync(request.TargetDeviceId, agentConnectionId, TimeSpan.FromHours(1));
                }
                else
                {
                    _logger.LogWarning("⚠️ Agent bilgisi bulunamadı veya ConnectionId null: TargetDeviceId={TargetDeviceId}, AgentInfo={AgentInfo}, ConnectionId={ConnectionId}", 
                        request.TargetDeviceId, agentInfo != null ? "Var" : "Yok", agentInfo?.ConnectionId ?? "null");
                }
            }
            else
            {
                _logger.LogDebug("✅ Agent connection ID Redis'ten bulundu: TargetDeviceId={TargetDeviceId}, ConnectionId={ConnectionId}", 
                    request.TargetDeviceId, agentConnectionId);
            }
            
            if (!string.IsNullOrEmpty(agentConnectionId))
            {
                // Agent'ın son heartbeat zamanını kontrol et - eğer çok eskiyse connection ID geçersiz olabilir
                var agentInfo = await _agentService.GetAgentInfoAsync(request.TargetDeviceId);
                if (agentInfo != null)
                {
                    var minutesSinceLastSeen = (DateTime.UtcNow - agentInfo.LastSeen).TotalMinutes;
                    if (minutesSinceLastSeen > 5) // 5 dakikadan eskiyse connection ID muhtemelen geçersiz
                    {
                        _logger.LogWarning("⚠️ Agent'ın son heartbeat'i çok eski ({MinutesAgo:F1} dakika önce), connection ID geçersiz olabilir: TargetDeviceId={TargetDeviceId}, AgentConnectionId={AgentConnectionId}", 
                            minutesSinceLastSeen, request.TargetDeviceId, agentConnectionId);
                        // Connection ID'yi Redis'ten sil ve PostgreSQL'den tekrar al
                        await _redisState.SetAgentConnectionIdAsync(request.TargetDeviceId, string.Empty, TimeSpan.Zero);
                        agentConnectionId = agentInfo.ConnectionId; // PostgreSQL'den güncel connection ID'yi al
                        if (!string.IsNullOrEmpty(agentConnectionId))
                        {
                            await _redisState.SetAgentConnectionIdAsync(request.TargetDeviceId, agentConnectionId, TimeSpan.FromHours(1));
                            _logger.LogInformation("✅ Agent connection ID PostgreSQL'den güncellendi: {DeviceId} -> {ConnectionId}", 
                                request.TargetDeviceId, agentConnectionId);
                        }
                    }
                }
                
                try
                {
                    // SignalR client'ın bağlı olup olmadığını kontrol et
                    var client = _hubContext.Clients.Client(agentConnectionId);
                    if (client == null)
                    {
                        _logger.LogWarning("⚠️ SignalR client bulunamadı: AgentConnectionId={AgentConnectionId}, TargetDeviceId={TargetDeviceId}", 
                            agentConnectionId, request.TargetDeviceId);
                    }
                    else
                    {
                        _logger.LogInformation("🔵 SignalR client bulundu, connection request gönderiliyor: AgentConnectionId={AgentConnectionId}, TargetDeviceId={TargetDeviceId}", 
                            agentConnectionId, request.TargetDeviceId);
                    }
                    
                    // Connection request'i timeout ile gönder (5 saniye)
                    var sendTask = _hubContext.Clients.Client(agentConnectionId).SendAsync("ConnectionRequest", new
                    {
                        ConnectionId = connectionId,
                        RequesterId = request.RequesterId,
                        RequesterName = pendingRequest.RequesterName,
                        RequesterIp = pendingRequest.RequesterIp,
                        RequestedAt = pendingRequest.RequestedAt
                    });
                    
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                    var completedTask = await Task.WhenAny(sendTask, timeoutTask);
                    
                    if (completedTask == timeoutTask)
                    {
                        _logger.LogError("❌ Connection request gönderimi timeout (5 saniye): AgentConnectionId={AgentConnectionId}, TargetDeviceId={TargetDeviceId}", 
                            agentConnectionId, request.TargetDeviceId);
                        // Connection ID'yi geçersiz say ve Redis'ten sil
                        await _redisState.SetAgentConnectionIdAsync(request.TargetDeviceId, string.Empty, TimeSpan.Zero);
                    }
                    else
                    {
                        await sendTask; // SendAsync tamamlanmasını bekle
                        _logger.LogInformation("✅✅✅ Connection request SignalR ile Agent'a gönderildi: ConnectionId={ConnectionId}, AgentConnectionId={AgentConnectionId}, TargetDeviceId={TargetDeviceId}, RequesterId={RequesterId}", 
                            connectionId, agentConnectionId, request.TargetDeviceId, request.RequesterId ?? "null");
                    }
                }
                catch (Exception sendEx)
                {
                    _logger.LogError(sendEx, "❌❌❌ Connection request SignalR ile gönderilirken hata: ConnectionId={ConnectionId}, AgentConnectionId={AgentConnectionId}, TargetDeviceId={TargetDeviceId}, Exception={Exception}", 
                        connectionId, agentConnectionId, request.TargetDeviceId, sendEx.Message);
                    
                    // Hata durumunda connection ID'nin geçersiz olup olmadığını kontrol et
                    // Eğer connection ID geçersizse, Redis'ten sil
                    if (sendEx.Message.Contains("not found") || sendEx.Message.Contains("does not exist") || 
                        sendEx.Message.Contains("timeout") || sendEx.Message.Contains("disconnected"))
                    {
                        _logger.LogWarning("⚠️ Connection ID geçersiz görünüyor, Redis'ten siliniyor: AgentConnectionId={AgentConnectionId}, TargetDeviceId={TargetDeviceId}", 
                            agentConnectionId, request.TargetDeviceId);
                        try
                        {
                            await _redisState.SetAgentConnectionIdAsync(request.TargetDeviceId, string.Empty, TimeSpan.Zero);
                        }
                        catch (Exception redisEx)
                        {
                            _logger.LogWarning(redisEx, "Redis'ten connection ID silinirken hata: TargetDeviceId={TargetDeviceId}", request.TargetDeviceId);
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("⚠️ Agent'ın SignalR connection ID'si bulunamadı (Redis ve PostgreSQL'de yok), connection request bildirimi gönderilemedi: TargetDeviceId={TargetDeviceId}", 
                    request.TargetDeviceId);
                
                // ConnectionId bulunamadıysa, birkaç kez dene (race condition için)
                // ConnectionHub'da Redis'e kayıt async olarak yapıldığı için biraz zaman alabilir
                var maxRetries = 10; // 10 retry (2 saniye toplam)
                var retryDelay = 200; // 200ms
                _logger.LogInformation("🔄 Agent connection ID bulunamadı, retry başlatılıyor: TargetDeviceId={TargetDeviceId}, MaxRetries={MaxRetries}", 
                    request.TargetDeviceId, maxRetries);
                
                for (int retry = 0; retry < maxRetries; retry++)
                {
                    await Task.Delay(retryDelay);
                    
                    // Önce Redis'ten kontrol et
                    agentConnectionId = await _redisState.GetAgentConnectionIdAsync(request.TargetDeviceId);
                    _logger.LogDebug("🔄 Retry {Retry}/{MaxRetries}: Redis'ten connection ID kontrolü: TargetDeviceId={TargetDeviceId}, ConnectionId={ConnectionId}", 
                        retry + 1, maxRetries, request.TargetDeviceId, agentConnectionId ?? "null");
                    
                    // Redis'te yoksa PostgreSQL'den al
                    if (string.IsNullOrEmpty(agentConnectionId))
                    {
                        var agentInfo = await _agentService.GetAgentInfoAsync(request.TargetDeviceId);
                        if (agentInfo != null && !string.IsNullOrEmpty(agentInfo.ConnectionId))
                        {
                            agentConnectionId = agentInfo.ConnectionId;
                            _logger.LogInformation("✅ Retry {Retry}/{MaxRetries}: Agent ConnectionId PostgreSQL'den alındı: {DeviceId} -> {ConnectionId}", 
                                retry + 1, maxRetries, request.TargetDeviceId, agentConnectionId);
                            
                            // Redis'e de kaydet (cache için)
                            await _redisState.SetAgentConnectionIdAsync(request.TargetDeviceId, agentConnectionId, TimeSpan.FromHours(1));
                        }
                        else
                        {
                            _logger.LogDebug("🔄 Retry {Retry}/{MaxRetries}: Agent bilgisi bulunamadı: TargetDeviceId={TargetDeviceId}, AgentInfo={AgentInfo}, ConnectionId={ConnectionId}", 
                                retry + 1, maxRetries, request.TargetDeviceId, agentInfo != null ? "Var" : "Yok", agentInfo?.ConnectionId ?? "null");
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(agentConnectionId))
                    {
                        try
                        {
                            await _hubContext.Clients.Client(agentConnectionId).SendAsync("ConnectionRequest", new
                            {
                                ConnectionId = connectionId,
                                RequesterId = request.RequesterId,
                                RequesterName = pendingRequest.RequesterName,
                                RequesterIp = pendingRequest.RequesterIp,
                                RequestedAt = pendingRequest.RequestedAt
                            });
                            _logger.LogInformation("✅ Connection request SignalR ile Agent'a gönderildi (retry {Retry} sonrası): ConnectionId={ConnectionId}, AgentConnectionId={AgentConnectionId}, TargetDeviceId={TargetDeviceId}", 
                                retry + 1, connectionId, agentConnectionId, request.TargetDeviceId);
                        }
                        catch (Exception sendEx)
                        {
                            _logger.LogError(sendEx, "❌ Retry {Retry}/{MaxRetries}: Connection request SignalR ile gönderilirken hata: ConnectionId={ConnectionId}, AgentConnectionId={AgentConnectionId}, TargetDeviceId={TargetDeviceId}", 
                                retry + 1, maxRetries, connectionId, agentConnectionId, request.TargetDeviceId);
                        }
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(agentConnectionId))
                {
                    _logger.LogError("❌ Agent'ın SignalR connection ID'si {MaxRetries} retry sonrası da bulunamadı: TargetDeviceId={TargetDeviceId}. Agent SignalR'a bağlanmamış olabilir.", 
                        maxRetries, request.TargetDeviceId);
                    
                    // Tüm kayıtlı Agent'ları logla (debug için)
                    var allAgents = await _agentService.GetAllAgentsAsync();
                    _logger.LogWarning("📋 Kayıtlı tüm Agent'lar ({AgentCount}):", allAgents.Count());
                    foreach (var a in allAgents)
                    {
                        var minutesAgo = (DateTime.UtcNow - a.LastSeen).TotalMinutes;
                        _logger.LogWarning("  📱 DeviceId: {DeviceId}, IsOnline: {IsOnline}, LastSeen: {LastSeen} ({MinutesAgo:F1} dakika önce), ConnectionId: {ConnectionId}, Machine: {MachineName}", 
                            a.DeviceId, a.IsOnline, a.LastSeen, minutesAgo, a.ConnectionId ?? "null", a.MachineName ?? "Bilinmiyor");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection request SignalR bildirimi gönderilemedi (request kaydedildi)");
        }

        // Onay bekleniyor - AgentEndpoint'i şimdilik döndürme (onaylandıktan sonra döndürülecek)
        var successResponse = new ConnectionResponse
        {
            Success = true,
            Status = ConnectionStatus.Pending,
            ConnectionId = connectionId,
            AgentEndpoint = null // Onay bekleniyor, AgentEndpoint henüz verilmiyor
        };
        
        _logger.LogInformation("Bağlantı isteği oluşturuldu: ConnectionId={ConnectionId}, TargetDeviceId={TargetDeviceId}, RequesterId={RequesterId}, Status=Pending (onay bekleniyor)",
            connectionId, request.TargetDeviceId, request.RequesterId);
        
        _logger.LogInformation("Connection response döndürülüyor: Success={Success}, Status={Status}, ConnectionId={ConnectionId}, AgentEndpoint={AgentEndpoint}",
            successResponse.Success, successResponse.Status, successResponse.ConnectionId, successResponse.AgentEndpoint ?? "null");
        
        return Ok(successResponse);
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
            
            // Önce Controller connection ID'sini kontrol et
            var requesterConnectionId = await _redisState.GetControllerConnectionIdAsync(requesterId);
            
            // Eğer Controller connection ID bulunamadıysa, Agent connection ID'sini kontrol et
            // (Flutter App aynı Device ID ile hem Agent hem Controller olabilir)
            if (string.IsNullOrEmpty(requesterConnectionId))
            {
                requesterConnectionId = await _redisState.GetAgentConnectionIdAsync(requesterId);
                _logger.LogDebug("Controller connection ID bulunamadı, Agent connection ID kullanılıyor: RequesterId={RequesterId}, ConnectionId={ConnectionId}", 
                    requesterId, requesterConnectionId ?? "null");
            }
            
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
                _logger.LogWarning("Controller'ın SignalR connection ID'si bulunamadı (hem Controller hem Agent kontrol edildi), connection response bildirimi gönderilemedi: RequesterId={RequesterId}", 
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

