using Microsoft.EntityFrameworkCore;
using SoftielRemote.Backend.Data;
using SoftielRemote.Backend.Models;
using SoftielRemote.Core.Enums;

namespace SoftielRemote.Backend.Repositories;

/// <summary>
/// PostgreSQL tabanlı Connection Request repository implementasyonu (Production-ready).
/// </summary>
public class PostgreSqlConnectionRequestRepository : IConnectionRequestRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PostgreSqlConnectionRequestRepository> _logger;

    public PostgreSqlConnectionRequestRepository(
        ApplicationDbContext context,
        ILogger<PostgreSqlConnectionRequestRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PendingConnectionRequest?> GetByConnectionIdAsync(string connectionId)
    {
        try
        {
            var entity = await _context.ConnectionRequests
                .FirstOrDefaultAsync(r => r.ConnectionId == connectionId);

            return entity?.ToPendingConnectionRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connection request {ConnectionId}", connectionId);
            throw;
        }
    }

    public async Task<PendingConnectionRequest?> GetPendingByTargetDeviceIdAsync(string targetDeviceId)
    {
        try
        {
            var entity = await _context.ConnectionRequests
                .Where(r => r.TargetDeviceId == targetDeviceId && 
                           r.Status == ConnectionStatus.Pending.ToString())
                .OrderByDescending(r => r.RequestedAt)
                .FirstOrDefaultAsync();

            return entity?.ToPendingConnectionRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connection request by target device ID {TargetDeviceId}", targetDeviceId);
            throw;
        }
    }

    public async Task<PendingConnectionRequest> CreateAsync(PendingConnectionRequest request)
    {
        try
        {
            // Backend.Models.PendingConnectionRequest'ten Core.Dtos.PendingConnectionRequest'e dönüştür
            var coreRequest = new Core.Dtos.PendingConnectionRequest
            {
                ConnectionId = request.ConnectionId,
                TargetDeviceId = request.TargetDeviceId,
                RequesterId = request.RequesterId,
                RequesterName = request.RequesterName,
                RequesterIp = request.RequesterIp,
                RequestedAt = request.RequestedAt,
                Status = request.Status
            };
            
            var entity = ConnectionRequestEntity.FromPendingConnectionRequest(coreRequest);
            _context.ConnectionRequests.Add(entity);
            await _context.SaveChangesAsync();
            return entity.ToPendingConnectionRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating connection request {ConnectionId}", request.ConnectionId);
            throw;
        }
    }

    public async Task<PendingConnectionRequest> UpdateAsync(PendingConnectionRequest request)
    {
        try
        {
            var entity = await _context.ConnectionRequests
                .FirstOrDefaultAsync(r => r.ConnectionId == request.ConnectionId);

            if (entity != null)
            {
                entity.TargetDeviceId = request.TargetDeviceId;
                entity.RequesterId = request.RequesterId;
                entity.RequesterName = request.RequesterName;
                entity.RequesterIp = request.RequesterIp;
                entity.Status = request.Status.ToString();
                await _context.SaveChangesAsync();
                return entity.ToPendingConnectionRequest();
            }

            // Eğer bulunamazsa yeni oluştur
            return await CreateAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating connection request {ConnectionId}", request.ConnectionId);
            throw;
        }
    }

    public async Task DeleteAsync(string connectionId)
    {
        try
        {
            var entity = await _context.ConnectionRequests
                .FirstOrDefaultAsync(r => r.ConnectionId == connectionId);

            if (entity != null)
            {
                _context.ConnectionRequests.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting connection request {ConnectionId}", connectionId);
            throw;
        }
    }
}

