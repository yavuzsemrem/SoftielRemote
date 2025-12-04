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
    private string _backendBaseUrl;

    public BackendClientService(string backendBaseUrl = "http://localhost:5000")
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
        
        System.Diagnostics.Debug.WriteLine($"🔵 BackendClientService oluşturuldu. Backend URL: {_backendBaseUrl}");
    }

    /// <summary>
    /// Şu anda kullanılan Backend URL'sini döndürür (debug için).
    /// </summary>
    public string GetBackendUrl()
    {
        return _backendBaseUrl;
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
            System.Diagnostics.Debug.WriteLine($"🔵 RequestConnectionAsync çağrıldı. TargetDeviceId: {request.TargetDeviceId}, Backend URL: {_backendBaseUrl}");
            System.Diagnostics.Debug.WriteLine($"🔵 Full URL: {_httpClient.BaseAddress}/api/connections/request");
            
            var response = await _httpClient.PostAsJsonAsync("/api/connections/request", request);
            
            System.Diagnostics.Debug.WriteLine($"🔵 HTTP Response Status: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"❌ HTTP Error: {response.StatusCode} - {errorContent}");
                return new ConnectionResponse
                {
                    Success = false,
                    ErrorMessage = $"HTTP {response.StatusCode}: {errorContent}"
                };
            }
            
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ConnectionResponse>();
            
            if (result == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Response null!");
                return new ConnectionResponse
                {
                    Success = false,
                    ErrorMessage = "Yanıt alınamadı (null response)"
                };
            }
            
            System.Diagnostics.Debug.WriteLine($"✅ ConnectionResponse alındı: Success={result.Success}, AgentEndpoint={result.AgentEndpoint}, ErrorMessage={result.ErrorMessage}");
            
            return result;
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("connection") || ex.Message.Contains("refused"))
        {
            System.Diagnostics.Debug.WriteLine($"❌ RequestConnectionAsync HttpRequestException: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"🔍 Backend URL keşfi başlatılıyor (sadece localhost)...");
            
            // Sadece localhost URL'lerini dene (network tarama yapmaz)
            var discoveredUrl = await BackendDiscoveryService.DiscoverBackendUrlAsync();
            
            if (discoveredUrl != null && discoveredUrl != _backendBaseUrl)
            {
                System.Diagnostics.Debug.WriteLine($"✅ Yeni Backend URL bulundu: {discoveredUrl}");
                System.Diagnostics.Debug.WriteLine($"🔵 Eski Backend URL: {_backendBaseUrl}");
                
                // Bulunan URL'i appsettings.json'a kaydet
                try
                {
                    var configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                    var config = new System.Collections.Generic.Dictionary<string, object>();
                    if (System.IO.File.Exists(configPath))
                    {
                        var json = System.IO.File.ReadAllText(configPath);
                        config = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json) ?? config;
                    }
                    config["BackendBaseUrl"] = discoveredUrl;
                    var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                    System.IO.File.WriteAllText(configPath, System.Text.Json.JsonSerializer.Serialize(config, options));
                    System.Diagnostics.Debug.WriteLine($"✅ Backend URL kaydedildi: {discoveredUrl}");
                }
                catch (Exception saveEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Backend URL kaydedilemedi: {saveEx.Message}");
                }
                
                // Yeni URL ile HttpClient'i güncelle
                _backendBaseUrl = discoveredUrl;
                _httpClient.BaseAddress = new Uri(discoveredUrl);
                
                System.Diagnostics.Debug.WriteLine($"🔵 Backend URL güncellendi. Yeni URL: {_backendBaseUrl}");
                
                // Tekrar dene
                try
                {
                    System.Diagnostics.Debug.WriteLine($"🔵 Yeni Backend URL ile tekrar deneniyor: {_backendBaseUrl}");
                    var retryResponse = await _httpClient.PostAsJsonAsync("/api/connections/request", request);
                    
                    if (retryResponse.IsSuccessStatusCode)
                    {
                        var result = await retryResponse.Content.ReadFromJsonAsync<ConnectionResponse>();
                        if (result != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ ConnectionResponse alındı (yeni URL ile): Success={result.Success}, AgentEndpoint={result.AgentEndpoint}");
                            return result;
                        }
                    }
                }
                catch (Exception retryEx)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Yeni URL ile tekrar deneme başarısız: {retryEx.Message}");
                }
            }
            
            return new ConnectionResponse
            {
                Success = false,
                ErrorMessage = $"Backend'e bağlanılamadı. Lütfen Backend URL'ini kontrol edin. ({ex.Message})"
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ RequestConnectionAsync exception: {ex.GetType().Name} - {ex.Message}");
            return new ConnectionResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

