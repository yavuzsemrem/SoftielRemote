using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using SoftielRemote.Core.Dtos;

namespace SoftielRemote.App.Services;

/// <summary>
/// Backend API ile iletişim için service implementasyonu.
/// </summary>
public class BackendClientService : IBackendClientService
{
    private readonly HttpClient _httpClient;
    private readonly string _backendBaseUrl;

    public BackendClientService(string backendBaseUrl = "http://localhost:5056")
    {
        _backendBaseUrl = backendBaseUrl;
        
        // SSL sertifika doğrulamasını atla (development için)
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_backendBaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<AgentRegistrationResponse> RegisterAsync(AgentRegistrationRequest request)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔵 BackendClientService.RegisterAsync çağrıldı. URL: {_backendBaseUrl}/api/agents/register");
            System.Diagnostics.Debug.WriteLine($"🔵 Request: MachineName={request.MachineName}, OS={request.OperatingSystem}");
            
            var response = await _httpClient.PostAsJsonAsync("/api/agents/register", request);
            
            System.Diagnostics.Debug.WriteLine($"🔵 HTTP Response Status: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"❌ HTTP Error: {response.StatusCode} - {errorContent}");
                return new AgentRegistrationResponse
                {
                    Success = false,
                    ErrorMessage = $"HTTP {response.StatusCode}: {errorContent}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<AgentRegistrationResponse>();
            
            if (result == null)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Response null!");
                return new AgentRegistrationResponse
                {
                    Success = false,
                    ErrorMessage = "Yanıt alınamadı (null response)"
                };
            }
            
            System.Diagnostics.Debug.WriteLine($"✅ Response alındı: Success={result.Success}, DeviceId={result.DeviceId}, Password={result.Password}");
            return result;
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ HttpRequestException: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ InnerException: {ex.InnerException?.Message}");
            return new AgentRegistrationResponse
            {
                Success = false,
                ErrorMessage = $"Bağlantı hatası: {ex.Message}"
            };
        }
        catch (TaskCanceledException ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ TaskCanceledException (Timeout): {ex.Message}");
            return new AgentRegistrationResponse
            {
                Success = false,
                ErrorMessage = $"Timeout: Backend yanıt vermiyor. {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Exception: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
            return new AgentRegistrationResponse
            {
                Success = false,
                ErrorMessage = $"Hata: {ex.GetType().Name} - {ex.Message}"
            };
        }
    }

    public async Task<ConnectionResponse> RequestConnectionAsync(ConnectionRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/connections/request", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ConnectionResponse>();
            return result ?? new ConnectionResponse
            {
                Success = false,
                ErrorMessage = "Yanıt alınamadı"
            };
        }
        catch (Exception ex)
        {
            return new ConnectionResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

