using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SoftielRemote.Backend.Services;
using SoftielRemote.Core.Dtos;

namespace SoftielRemote.Backend.Controllers;

/// <summary>
/// Agent kayıt ve yönetim endpoint'leri.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(IAgentService agentService, ILogger<AgentsController> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    /// <summary>
    /// Agent'ı Backend'e kayıt eder.
    /// </summary>
    /// <remarks>
    /// Agent veya App, Backend'e kayıt olmak için bu endpoint'i kullanır.
    /// DeviceId verilmişse ve geçerliyse kullanılır, yoksa yeni bir DeviceId üretilir.
    /// 
    /// Örnek istek:
    /// 
    ///     POST /api/agents/register
    ///     {
    ///         "deviceId": "280969031",
    ///         "machineName": "DESKTOP-ABC123",
    ///         "operatingSystem": "Microsoft Windows NT 10.0.19045.0",
    ///         "ipAddress": "192.168.1.100",
    ///         "tcpPort": 8888
    ///     }
    /// 
    /// Rate Limit: 10 istek/dakika (IP bazlı)
    /// </remarks>
    /// <param name="request">Agent kayıt bilgileri</param>
    /// <returns>Kayıt başarılıysa DeviceId döner</returns>
    /// <response code="200">Kayıt başarılı</response>
    /// <response code="400">Geçersiz istek veya validation hatası</response>
    /// <response code="429">Rate limit aşıldı</response>
    [HttpPost("register")]
    [EnableRateLimiting("AgentRegisterPolicy")]
    [ProducesResponseType(typeof(AgentRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AgentRegistrationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AgentRegistrationResponse>> Register(
        [FromBody] AgentRegistrationRequest request)
    {
        if (request == null)
        {
            _logger.LogWarning("❌ Agent kayıt isteği null");
            return BadRequest("Request body is required");
        }

        _logger.LogInformation("🔵 Agent kayıt isteği alındı: DeviceId={DeviceId}, IpAddress={IpAddress}, TcpPort={TcpPort}, MachineName={MachineName}",
            request.DeviceId ?? "null", request.IpAddress ?? "null", request.TcpPort?.ToString() ?? "null", request.MachineName ?? "null");

        try
        {
            var response = await _agentService.RegisterAsync(request);
            
            if (!response.Success)
            {
                _logger.LogWarning("❌ Agent kayıt başarısız: {ErrorMessage}", response.ErrorMessage ?? "Bilinmeyen hata");
                return BadRequest(response);
            }

            _logger.LogInformation("✅ Agent kayıt başarılı: DeviceId={DeviceId}", response.DeviceId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Agent kayıt sırasında exception oluştu");
            return BadRequest(new AgentRegistrationResponse
            {
                Success = false,
                ErrorMessage = $"Internal server error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Agent'ın heartbeat göndermesi için endpoint.
    /// </summary>
    /// <remarks>
    /// Agent, online durumunu korumak için düzenli olarak (örn: her 30 saniyede bir) heartbeat gönderir.
    /// Bu endpoint Agent'ın LastSeen zamanını günceller ve online durumunu korur.
    /// 
    /// Örnek istek:
    /// 
    ///     POST /api/agents/heartbeat
    ///     {
    ///         "deviceId": "280969031",
    ///         "ipAddress": "192.168.1.100"
    ///     }
    /// </remarks>
    /// <param name="request">Heartbeat bilgileri</param>
    /// <returns>200 OK</returns>
    /// <response code="200">Heartbeat başarılı</response>
    /// <response code="400">Geçersiz istek</response>
    [HttpPost("heartbeat")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Heartbeat([FromBody] HeartbeatRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.DeviceId))
        {
            return BadRequest("DeviceId is required");
        }

        _logger.LogDebug("💓 Heartbeat alındı: DeviceId={DeviceId}, IpAddress={IpAddress}", 
            request.DeviceId, request.IpAddress ?? "null");
        
        // LastSeen ve IpAddress güncelle
        await _agentService.UpdateLastSeenAsync(request.DeviceId, request.IpAddress);
        
        // Agent'ın online durumunu kontrol et ve logla
        var isOnline = await _agentService.IsAgentOnlineAsync(request.DeviceId);
        _logger.LogDebug("💓 Heartbeat işlendi: DeviceId={DeviceId}, IsOnline={IsOnline}", request.DeviceId, isOnline);
        
        return Ok();
    }

    /// <summary>
    /// Agent'ın bu Backend'de olup olmadığını kontrol eder (Discovery için).
    /// </summary>
    /// <remarks>
    /// App veya başka bir Backend, belirli bir Agent'ın bu Backend'de kayıtlı olup olmadığını kontrol etmek için kullanır.
    /// 
    /// Örnek istek:
    /// 
    ///     GET /api/agents/discovery/280969031
    /// 
    /// </remarks>
    /// <param name="deviceId">Aranacak Agent'ın Device ID'si</param>
    /// <returns>Agent bulunduysa Backend URL ve online durumu döner</returns>
    /// <response code="200">Agent bulundu veya bulunamadı (Found=false)</response>
    /// <response code="400">Geçersiz DeviceId</response>
    [HttpGet("discovery/{deviceId}")]
    [ProducesResponseType(typeof(Core.Dtos.AgentDiscoveryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Core.Dtos.AgentDiscoveryResponse>> DiscoverAgent(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return BadRequest("DeviceId is required");
        }

        _logger.LogDebug("🔍 Agent keşif isteği: DeviceId={DeviceId}", deviceId);

        var agent = await _agentService.GetAgentInfoAsync(deviceId);
        
        if (agent == null)
        {
            _logger.LogDebug("❌ Agent bulunamadı: DeviceId={DeviceId}", deviceId);
            return Ok(new Core.Dtos.AgentDiscoveryResponse
            {
                Found = false
            });
        }

        // Bu Backend'in URL'ini al (Request'ten)
        var backendUrl = $"{Request.Scheme}://{Request.Host}";
        
        _logger.LogDebug("✅ Agent bulundu: DeviceId={DeviceId}, BackendUrl={BackendUrl}, IsOnline={IsOnline}", 
            deviceId, backendUrl, agent.IsOnline);

        return Ok(new Core.Dtos.AgentDiscoveryResponse
        {
            Found = true,
            BackendUrl = backendUrl,
            IsOnline = agent.IsOnline,
            MachineName = agent.MachineName
        });
    }
}

