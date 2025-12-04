using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SoftielRemote.Core.Dtos;

namespace SoftielRemote.Backend.Services;

/// <summary>
/// Redis state management servisi implementasyonu.
/// Agent online/offline durumu ve connection request'leri için.
/// </summary>
public class RedisStateService : IRedisStateService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RedisStateService> _logger;
    private readonly bool _isAvailable;

    public RedisStateService(IConnectionMultiplexer? redis, ILogger<RedisStateService> logger)
    {
        _redis = redis;
        _logger = logger;
        
        // Redis bağlantısını lazy olarak kontrol et (test komutu göndermeden)
        // IsConnected kontrolü yeterli değil, çünkü AbortOnConnectFail=false ile bağlantı kurulmaya çalışılır
        // Gerçek bağlantı testi ilk kullanımda yapılacak
        _isAvailable = _redis != null;
        
        if (!_isAvailable)
        {
            _logger.LogInformation("Redis connection multiplexer yok, state management PostgreSQL'e fallback edecek");
        }
        else
        {
            _logger.LogInformation("Redis connection multiplexer mevcut (lazy connection - gerçek bağlantı ilk kullanımda test edilecek)");
        }
    }

    public async Task SetAgentOnlineAsync(string deviceId, TimeSpan? expiration = null)
    {
        if (!_isAvailable || _redis == null) return;

        try
        {
            // Redis bağlantısını kontrol et (lazy connection)
            if (!_redis.IsConnected)
            {
                return; // Redis yoksa sessizce fallback'e geç
            }
            
            var db = _redis.GetDatabase();
            var key = $"agent:online:{deviceId}";
            await db.StringSetAsync(key, "1", expiration ?? TimeSpan.FromMinutes(5));
            _logger.LogDebug("Agent online durumu Redis'e kaydedildi: {DeviceId}", deviceId);
        }
        catch
        {
            // Redis bağlantısı başarısız, sessizce fallback'e geç (log spam'ini önle)
        }
    }

    public async Task SetAgentOfflineAsync(string deviceId)
    {
        if (!_isAvailable || _redis == null) return;

        try
        {
            if (!_redis.IsConnected) return;
            
            var db = _redis.GetDatabase();
            var key = $"agent:online:{deviceId}";
            await db.KeyDeleteAsync(key);
            _logger.LogDebug("Agent offline durumu Redis'te işaretlendi: {DeviceId}", deviceId);
        }
        catch
        {
            // Redis bağlantısı başarısız, sessizce fallback'e geç
        }
    }

    public async Task<bool> IsAgentOnlineAsync(string deviceId)
    {
        if (!_isAvailable || _redis == null) return false;

        try
        {
            if (!_redis.IsConnected) return false;
            
            var db = _redis.GetDatabase();
            var key = $"agent:online:{deviceId}";
            var value = await db.StringGetAsync(key);
            return value.HasValue && value == "1";
        }
        catch
        {
            // Redis bağlantısı başarısız, false döndür (PostgreSQL fallback kullanılacak)
            return false;
        }
    }

    public async Task CreateConnectionRequestAsync(PendingConnectionRequest request)
    {
        if (!_isAvailable || _redis == null) return;

        try
        {
            if (!_redis.IsConnected) return;
            
            var db = _redis.GetDatabase();
            var key = $"connection:request:{request.ConnectionId}";
            var json = JsonConvert.SerializeObject(request);
            await db.StringSetAsync(key, json, TimeSpan.FromMinutes(10));
            _logger.LogDebug("Connection request Redis'e kaydedildi: {ConnectionId}", request.ConnectionId);
        }
        catch
        {
            // Redis bağlantısı başarısız, sessizce fallback'e geç
        }
    }

    public async Task SetConnectionRequestAsync(string connectionId, string targetDeviceId, string requesterId, TimeSpan? expiration = null)
    {
        if (!_isAvailable || _redis == null) return;

        try
        {
            if (!_redis.IsConnected) return;
            
            var db = _redis.GetDatabase();
            var key = $"connection:request:{connectionId}";
            var value = $"{targetDeviceId}:{requesterId}";
            await db.StringSetAsync(key, value, expiration ?? TimeSpan.FromMinutes(10));
            _logger.LogDebug("Connection request Redis'e kaydedildi: {ConnectionId}", connectionId);
        }
        catch
        {
            // Redis bağlantısı başarısız, sessizce fallback'e geç
        }
    }

    public async Task<string?> GetConnectionRequestAsync(string connectionId)
    {
        if (!_isAvailable || _redis == null) return null;

        try
        {
            if (!_redis.IsConnected) return null;
            
            var db = _redis.GetDatabase();
            var key = $"connection:request:{connectionId}";
            var value = await db.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch
        {
            // Redis bağlantısı başarısız, null döndür (PostgreSQL fallback kullanılacak)
            return null;
        }
    }

    public async Task RemoveConnectionRequestAsync(string connectionId)
    {
        if (!_isAvailable || _redis == null) return;

        try
        {
            if (!_redis.IsConnected) return;
            
            var db = _redis.GetDatabase();
            var key = $"connection:request:{connectionId}";
            await db.KeyDeleteAsync(key);
            _logger.LogDebug("Connection request Redis'ten silindi: {ConnectionId}", connectionId);
        }
        catch
        {
            // Redis bağlantısı başarısız, sessizce fallback'e geç
        }
    }

    public async Task SetAgentConnectionIdAsync(string deviceId, string connectionId, TimeSpan? expiration = null)
    {
        if (!_isAvailable || _redis == null) return;

        try
        {
            if (!_redis.IsConnected) return;
            
            var db = _redis.GetDatabase();
            var key = $"agent:connection:{deviceId}";
            await db.StringSetAsync(key, connectionId, expiration ?? TimeSpan.FromHours(1));
            _logger.LogDebug("Agent connection ID Redis'e kaydedildi: {DeviceId} -> {ConnectionId}", deviceId, connectionId);
        }
        catch
        {
            // Redis bağlantısı başarısız, sessizce fallback'e geç
        }
    }

    public async Task<string?> GetAgentConnectionIdAsync(string deviceId)
    {
        if (!_isAvailable || _redis == null) return null;

        try
        {
            if (!_redis.IsConnected) return null;
            
            var db = _redis.GetDatabase();
            var key = $"agent:connection:{deviceId}";
            var value = await db.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch
        {
            // Redis bağlantısı başarısız, null döndür (PostgreSQL fallback kullanılacak)
            return null;
        }
    }
}

