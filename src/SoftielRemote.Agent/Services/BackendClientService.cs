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
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<AgentRegistrationResponse> RegisterAsync(AgentRegistrationRequest request)
    {
        try
        {
            _logger.LogInformation("Backend'e kayıt isteği gönderiliyor...");
            
            var response = await _httpClient.PostAsJsonAsync("/api/agents/register", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AgentRegistrationResponse>();
            
            if (result?.Success == true)
            {
                _logger.LogInformation("Agent başarıyla kaydedildi. Device ID: {DeviceId}", result.DeviceId);
            }
            else
            {
                _logger.LogWarning("Agent kaydı başarısız: {ErrorMessage}", result?.ErrorMessage);
            }

            return result ?? new AgentRegistrationResponse { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backend kayıt hatası");
            return new AgentRegistrationResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> SendHeartbeatAsync(string deviceId)
    {
        try
        {
            // Faz 1 için basit bir heartbeat endpoint'i yok, ileride eklenebilir
            // Şimdilik sadece true döndürüyoruz
            await Task.Delay(1);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Heartbeat gönderme hatası");
            return false;
        }
    }
}

