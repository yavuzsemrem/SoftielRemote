using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SoftielRemote.Agent.Config;
using SoftielRemote.Core.Dtos;

namespace SoftielRemote.Agent.Services;

/// <summary>
/// Backend API ile iletişim için service implementasyonu.
/// </summary>
public class BackendClientService : IBackendClientService
{
    private readonly HttpClient _httpClient;
    private readonly AgentConfig _config;
    private readonly ILogger<BackendClientService> _logger;

    public BackendClientService(HttpClient httpClient, AgentConfig config, ILogger<BackendClientService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri(_config.BackendBaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(120); // Database işlemleri uzun sürebilir
    }

    public async Task<AgentRegistrationResponse> RegisterAsync(AgentRegistrationRequest request)
    {
        const int maxRetries = 3;
        const int retryDelayMs = 2000; // 2 saniye
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (attempt > 1)
                {
                    _logger.LogInformation("Kayıt isteği tekrar deneniyor (Deneme {Attempt}/{MaxRetries})...", attempt, maxRetries);
                    await Task.Delay(retryDelayMs * (attempt - 1)); // Exponential backoff
                }
                else
                {
                    _logger.LogInformation("Backend'e kayıt isteği gönderiliyor...");
                }
                
                _logger.LogInformation("🔵 Request içeriği: DeviceId={DeviceId}, IpAddress={IpAddress}, TcpPort={TcpPort}, MachineName={MachineName}",
                    request.DeviceId ?? "null", request.IpAddress ?? "null", request.TcpPort, request.MachineName ?? "null");
                
                var response = await _httpClient.PostAsJsonAsync("/api/agents/register", request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<AgentRegistrationResponse>();
                
                if (result?.Success == true)
                {
                    _logger.LogInformation("Agent başarıyla kaydedildi. Device ID: {DeviceId}", result.DeviceId);
                    return result;
                }
                else
                {
                    _logger.LogWarning("Agent kaydı başarısız: {ErrorMessage}", result?.ErrorMessage);
                    return result ?? new AgentRegistrationResponse { Success = false };
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning(ex, "Kayıt isteği timeout oldu (Deneme {Attempt}/{MaxRetries})", attempt, maxRetries);
                
                if (attempt == maxRetries)
                {
                    _logger.LogError("Kayıt isteği {MaxRetries} deneme sonrası başarısız oldu. Timeout nedeniyle yeni DeviceId üretilmemeli - mevcut DeviceId kullanılmalı.", maxRetries);
                    return new AgentRegistrationResponse
                    {
                        Success = false,
                        ErrorMessage = $"Kayıt isteği {maxRetries} deneme sonrası timeout oldu. Mevcut DeviceId kullanılacak: {request.DeviceId}"
                    };
                }
                // Retry için döngü devam edecek
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "HTTP isteği başarısız (Deneme {Attempt}/{MaxRetries})", attempt, maxRetries);
                
                if (attempt == maxRetries)
                {
                    return new AgentRegistrationResponse
                    {
                        Success = false,
                        ErrorMessage = $"HTTP isteği başarısız: {ex.Message}"
                    };
                }
                // Retry için döngü devam edecek
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backend kayıt hatası (Deneme {Attempt}/{MaxRetries})", attempt, maxRetries);
                
                if (attempt == maxRetries)
                {
                    return new AgentRegistrationResponse
                    {
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }
                // Retry için döngü devam edecek
            }
        }
        
        // Buraya gelmemeli ama yine de güvenlik için
        return new AgentRegistrationResponse
        {
            Success = false,
            ErrorMessage = "Kayıt isteği başarısız oldu"
        };
    }

    public async Task<bool> SendHeartbeatAsync(string deviceId, string? ipAddress = null)
    {
        try
        {
            var request = new Core.Dtos.HeartbeatRequest 
            { 
                DeviceId = deviceId,
                IpAddress = ipAddress
            };
            var response = await _httpClient.PostAsJsonAsync("/api/agents/heartbeat", request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Heartbeat gönderildi: DeviceId={DeviceId}, IpAddress={IpAddress}", 
                    deviceId, ipAddress ?? "null");
                return true;
            }
            
            _logger.LogWarning("Heartbeat gönderme başarısız: StatusCode={StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Heartbeat gönderme hatası");
            return false;
        }
    }

    public async Task<PendingConnectionRequest?> GetPendingConnectionRequestAsync(string deviceId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/connections/pending/{deviceId}");
            
            if (response.IsSuccessStatusCode)
            {
                var request = await response.Content.ReadFromJsonAsync<PendingConnectionRequest>();
                return request;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bekleyen bağlantı isteği kontrol hatası");
            return null;
        }
    }

    public async Task<bool> RespondToConnectionRequestAsync(string connectionId, bool accepted)
    {
        try
        {
            var response = new
            {
                ConnectionId = connectionId,
                Accepted = accepted
            };
            
            var httpResponse = await _httpClient.PostAsJsonAsync("/api/connections/response", response);
            return httpResponse.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bağlantı isteği yanıt hatası");
            return false;
        }
    }
}

