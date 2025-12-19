using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftielRemote.Backend.Data;
using SoftielRemote.Backend.Models;

namespace SoftielRemote.Backend.Controllers;

/// <summary>
/// Backend Registry endpoint'leri (farklı network'lerdeki Backend'lerin keşfi için).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BackendRegistryController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BackendRegistryController> _logger;

    public BackendRegistryController(
        ApplicationDbContext context,
        ILogger<BackendRegistryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Aktif Backend URL'lerini döndürür (Agent/App'in keşfi için).
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<List<string>>> GetActiveBackends()
    {
        try
        {
            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
            var activeBackends = await _context.BackendRegistry
                .Where(b => b.IsActive && b.LastSeen >= fiveMinutesAgo)
                .OrderByDescending(b => b.LastSeen)
                .Select(b => b.PublicUrl)
                .Distinct()
                .ToListAsync();

            _logger.LogDebug("Aktif Backend'ler sorgulandı: {Count} adet", activeBackends.Count);
            return Ok(activeBackends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aktif Backend'ler sorgulanırken hata oluştu");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Backend'i kayıt eder veya günceller (Backend başladığında çağrılır).
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult> RegisterBackend([FromBody] BackendRegistrationRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.PublicUrl))
        {
            return BadRequest("PublicUrl is required");
        }

        try
        {
            var backendId = request.BackendId ?? GenerateBackendId();
            var existingBackend = await _context.BackendRegistry
                .FirstOrDefaultAsync(b => b.BackendId == backendId);

            if (existingBackend != null)
            {
                // Mevcut Backend'i güncelle
                existingBackend.PublicUrl = request.PublicUrl;
                existingBackend.LocalIp = request.LocalIp;
                existingBackend.LastSeen = DateTime.UtcNow;
                existingBackend.IsActive = true;
                existingBackend.Description = request.Description ?? existingBackend.Description;
            }
            else
            {
                // Yeni Backend ekle
                var newBackend = new BackendRegistryEntity
                {
                    BackendId = backendId,
                    PublicUrl = request.PublicUrl,
                    LocalIp = request.LocalIp,
                    LastSeen = DateTime.UtcNow,
                    IsActive = true,
                    Description = request.Description
                };
                _context.BackendRegistry.Add(newBackend);
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await _context.SaveChangesAsync(cts.Token);
            _logger.LogInformation("Backend kaydedildi: BackendId={BackendId}, PublicUrl={PublicUrl}", backendId, request.PublicUrl);
            
            return Ok(new { BackendId = backendId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backend kaydedilirken hata oluştu");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Backend heartbeat (Backend'in aktif olduğunu günceller).
    /// </summary>
    [HttpPost("heartbeat")]
    public async Task<ActionResult> Heartbeat([FromBody] BackendHeartbeatRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.BackendId))
        {
            return BadRequest("BackendId is required");
        }

        try
        {
            var backend = await _context.BackendRegistry
                .FirstOrDefaultAsync(b => b.BackendId == request.BackendId);

            if (backend != null)
            {
                backend.LastSeen = DateTime.UtcNow;
                backend.IsActive = true;
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await _context.SaveChangesAsync(cts.Token);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backend heartbeat işlenirken hata oluştu");
            return StatusCode(500, "Internal server error");
        }
    }

    private string GenerateBackendId()
    {
        // Backend ID'yi makine adı ve IP'den oluştur
        var machineName = Environment.MachineName;
        var timestamp = DateTime.UtcNow.Ticks;
        return $"{machineName}_{timestamp}";
    }
}

/// <summary>
/// Backend kayıt isteği DTO.
/// </summary>
public class BackendRegistrationRequest
{
    public string? BackendId { get; set; }
    public string PublicUrl { get; set; } = string.Empty;
    public string? LocalIp { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Backend heartbeat isteği DTO.
/// </summary>
public class BackendHeartbeatRequest
{
    public string BackendId { get; set; } = string.Empty;
}


