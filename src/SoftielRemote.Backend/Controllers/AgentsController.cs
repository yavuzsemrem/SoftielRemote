using Microsoft.AspNetCore.Mvc;
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
    [HttpPost("register")]
    public async Task<ActionResult<AgentRegistrationResponse>> Register(
        [FromBody] AgentRegistrationRequest request)
    {
        if (request == null)
        {
            return BadRequest("Request body is required");
        }

        var response = await _agentService.RegisterAsync(request);
        
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}

