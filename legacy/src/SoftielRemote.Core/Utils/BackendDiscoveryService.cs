using System.Net.Http;
using System.Net.Http.Json;
using SoftielRemote.Core.Dtos;

namespace SoftielRemote.Core.Utils;

/// <summary>
/// Device ID ile Backend keşfi yapan servis (AnyDesk benzeri).
/// Farklı Backend'lerde Agent'ı arar.
/// </summary>
public static class BackendDiscoveryService
{
    /// <summary>
    /// Device ID ile Agent'ın hangi Backend'de olduğunu bulur.
    /// Birden fazla Backend URL'ini dener.
    /// </summary>
    /// <param name="deviceId">Aranacak Device ID</param>
    /// <param name="backendUrls">Denenecek Backend URL'leri listesi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Agent'ın bulunduğu Backend URL'i veya null</returns>
    public static async Task<string?> DiscoverBackendForAgentAsync(
        string deviceId, 
        IEnumerable<string> backendUrls,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return null;

        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        // Paralel olarak tüm Backend'leri dene
        var tasks = backendUrls.Select(url => TryDiscoverAgentAsync(url, deviceId, handler, cancellationToken));
        var results = await Task.WhenAll(tasks);

        // İlk bulunan Backend URL'ini döndür
        var foundBackend = results.FirstOrDefault(r => r != null);
        
        if (foundBackend != null)
        {
            System.Diagnostics.Debug.WriteLine($"✅ Agent bulundu: DeviceId={deviceId}, BackendUrl={foundBackend}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"❌ Agent bulunamadı: DeviceId={deviceId}");
        }

        return foundBackend;
    }

    /// <summary>
    /// Belirli bir Backend'de Agent'ı arar.
    /// </summary>
    private static async Task<string?> TryDiscoverAgentAsync(
        string backendUrl, 
        string deviceId, 
        HttpClientHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(backendUrl),
                Timeout = TimeSpan.FromSeconds(3)
            };

            System.Diagnostics.Debug.WriteLine($"🔍 Backend deneniyor: {backendUrl} (DeviceId: {deviceId})");

            var response = await httpClient.GetAsync($"/api/agents/discovery/{deviceId}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var discoveryResponse = await response.Content.ReadFromJsonAsync<AgentDiscoveryResponse>(cancellationToken: cancellationToken);
            
            if (discoveryResponse?.Found == true && !string.IsNullOrWhiteSpace(discoveryResponse.BackendUrl))
            {
                System.Diagnostics.Debug.WriteLine($"✅ Agent bulundu: {discoveryResponse.BackendUrl} (IsOnline: {discoveryResponse.IsOnline})");
                return discoveryResponse.BackendUrl;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Backend keşif hatası ({backendUrl}): {ex.Message}");
        }
        
        return null;
    }

    /// <summary>
    /// Backend URL listesini appsettings.json'dan veya varsayılan değerlerden alır.
    /// </summary>
    public static List<string> GetBackendUrlsFromConfig(string? configuredBackendUrl = null, string? configPath = null)
    {
        var urls = new List<string>();

        // appsettings.json'dan BackendUrls listesini oku
        if (!string.IsNullOrWhiteSpace(configPath) && System.IO.File.Exists(configPath))
        {
            try
            {
                var json = System.IO.File.ReadAllText(configPath);
                var config = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json);
                
                if (config != null && config.ContainsKey("BackendUrls"))
                {
                    var backendUrlsJson = config["BackendUrls"];
                    if (backendUrlsJson != null)
                    {
                        var backendUrlsArray = System.Text.Json.JsonSerializer.Deserialize<string[]>(
                            backendUrlsJson.ToString() ?? "[]");
                        
                        if (backendUrlsArray != null)
                        {
                            urls.AddRange(backendUrlsArray.Where(u => !string.IsNullOrWhiteSpace(u)));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ BackendUrls okunamadı: {ex.Message}");
            }
        }

        // Önce yapılandırılmış URL'i ekle (eğer listede yoksa)
        if (!string.IsNullOrWhiteSpace(configuredBackendUrl) && !urls.Contains(configuredBackendUrl))
        {
            urls.Insert(0, configuredBackendUrl); // En üste ekle (öncelikli)
        }

        // Varsayılan localhost URL'lerini ekle (eğer listede yoksa)
        var defaultUrls = new[]
        {
            "http://localhost:5000",
            "http://localhost:5056",
            "http://127.0.0.1:5000",
            "http://127.0.0.1:5056"
        };

        foreach (var defaultUrl in defaultUrls)
        {
            if (!urls.Contains(defaultUrl))
            {
                urls.Add(defaultUrl);
            }
        }

        // Tekrarları kaldır ve döndür
        return urls.Distinct().ToList();
    }
}

