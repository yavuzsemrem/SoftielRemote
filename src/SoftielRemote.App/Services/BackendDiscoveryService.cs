using System.Net.Http;
using System.Net.Http.Json;
using SoftielRemote.Core.Dtos;

namespace SoftielRemote.App.Services;

/// <summary>
/// Backend URL'ini otomatik keşfetmek için servis.
/// </summary>
public class BackendDiscoveryService
{
    private static readonly string[] CommonBackendUrls = new[]
    {
        "http://localhost:5000",
        "http://localhost:5056",
        "http://127.0.0.1:5000",
        "http://127.0.0.1:5056"
    };

    private static readonly int[] CommonPorts = new[] { 5000, 5056 };

    /// <summary>
    /// Yaygın Backend URL'lerini dener ve çalışan birini bulur.
    /// </summary>
    public static async Task<string?> DiscoverBackendUrlAsync(CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2) // Hızlı timeout
        };

        foreach (var url in CommonBackendUrls)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 Backend URL deneniyor: {url}");
                var response = await httpClient.GetAsync($"{url}/api/agents/register", cancellationToken);
                
                // 405 Method Not Allowed veya 400 Bad Request bekleniyor (çünkü GET ile POST endpoint'ine istek atıyoruz)
                // Ama bu, Backend'in çalıştığını gösterir
                if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed || 
                    response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
                    response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Backend bulundu: {url}");
                    return url;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ {url} bağlantı hatası: {ex.Message}");
                // Devam et, bir sonraki URL'yi dene
            }
        }

        System.Diagnostics.Debug.WriteLine("❌ Hiçbir Backend URL'i bulunamadı");
        return null;
    }

    /// <summary>
    /// Belirli bir Agent ID'si için Backend URL'ini bulur.
    /// Sadece yaygın localhost URL'lerini dener (network tarama yapmaz).
    /// </summary>
    public static async Task<string?> DiscoverBackendUrlForAgentAsync(string agentDeviceId, CancellationToken cancellationToken = default)
    {
        using var handler = new System.Net.Http.HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        // Sadece yaygın localhost URL'lerini dene (network tarama yapmaz)
        foreach (var url in CommonBackendUrls)
        {
            var found = await TryBackendUrlAsync(url, agentDeviceId, handler, cancellationToken);
            if (found != null)
                return found;
        }

        System.Diagnostics.Debug.WriteLine("❌ Hiçbir Backend URL'i bulunamadı (sadece localhost denendi)");
        return null;
    }

    /// <summary>
    /// Belirli bir Backend URL'ini dener.
    /// </summary>
    private static async Task<string?> TryBackendUrlAsync(string url, string agentDeviceId, HttpClientHandler handler, CancellationToken cancellationToken)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔍 Backend URL deneniyor (Agent ID: {agentDeviceId}): {url}");
            
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(url),
                Timeout = TimeSpan.FromSeconds(2)
            };

            var connectionRequest = new ConnectionRequest
            {
                TargetDeviceId = agentDeviceId,
                QualityLevel = Core.Enums.QualityLevel.Medium
            };

            var response = await httpClient.PostAsJsonAsync("/api/connections/request", connectionRequest, cancellationToken);
            
            // 200 OK veya 400 Bad Request (Agent bulunamadı) bekleniyor
            // Her iki durumda da Backend çalışıyor demektir
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                System.Diagnostics.Debug.WriteLine($"✅ Backend bulundu: {url}");
                return url;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ {url} bağlantı hatası: {ex.Message}");
        }
        
        return null;
    }

}

