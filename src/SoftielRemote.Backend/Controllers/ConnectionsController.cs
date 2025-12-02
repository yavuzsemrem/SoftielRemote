using Microsoft.AspNetCore.Mvc;
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
    private readonly ILogger<ConnectionsController> _logger;

    public ConnectionsController(IAgentService agentService, ILogger<ConnectionsController> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    /// <summary>
    /// Belirli bir Device ID'ye bağlantı isteği gönderir.
    /// </summary>
    [HttpPost("request")]
    public async Task<ActionResult<ConnectionResponse>> RequestConnection(
        [FromBody] ConnectionRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.TargetDeviceId))
        {
            return BadRequest("TargetDeviceId is required");
        }

        // Agent'ın online olup olmadığını kontrol et
        var isOnline = await _agentService.IsAgentOnlineAsync(request.TargetDeviceId);

        if (!isOnline)
        {
            return Ok(new ConnectionResponse
            {
                Success = false,
                Status = ConnectionStatus.Error,
                ErrorMessage = "Agent is not online"
            });
        }

        // Agent bilgilerini al
        var agent = await _agentService.GetAgentInfoAsync(request.TargetDeviceId);
        
        if (agent == null)
        {
            return Ok(new ConnectionResponse
            {
                Success = false,
                Status = ConnectionStatus.Error,
                ErrorMessage = "Agent bulunamadı"
            });
        }

        // AgentEndpoint oluştur (IP:Port formatında)
        string? agentEndpoint = null;
        if (!string.IsNullOrEmpty(agent.IpAddress))
        {
            agentEndpoint = $"{agent.IpAddress}:{agent.TcpPort}";
        }

        // Faz 1 için basit bir yanıt döndür
        // Faz 2'de SignalR ile signaling yapılacak
        _logger.LogInformation("Bağlantı isteği alındı: TargetDeviceId={TargetDeviceId}, RequesterId={RequesterId}, AgentEndpoint={AgentEndpoint}",
            request.TargetDeviceId, request.RequesterId, agentEndpoint);

        return Ok(new ConnectionResponse
        {
            Success = true,
            Status = ConnectionStatus.Pending,
            ConnectionId = Guid.NewGuid().ToString(),
            AgentEndpoint = agentEndpoint
        });
    }
}

