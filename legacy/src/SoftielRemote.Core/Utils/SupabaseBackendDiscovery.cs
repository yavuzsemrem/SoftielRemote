using System.Net.Http;
using System.Text.Json;

namespace SoftielRemote.Core.Utils;

/// <summary>
/// Supabase üzerinden Backend URL'lerini keşfeden servis.
/// Farklı network'lerdeki Backend'lerin otomatik bulunması için.
/// </summary>
public static class SupabaseBackendDiscovery
{
    /// <summary>
    /// Supabase'den aktif Backend URL'lerini çeker.
    /// Önce environment variable'dan okur, sonra Supabase REST API'den çeker.
    /// </summary>
    /// <param name="supabaseConnectionString">Supabase connection string (PostgreSQL) - şu an kullanılmıyor</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Aktif Backend URL'leri listesi</returns>
    public static async Task<List<string>> DiscoverBackendUrlsAsync(
        string? supabaseConnectionString = null,
        CancellationToken cancellationToken = default)
    {
        var backendUrls = new List<string>();

        try
        {
            // 1. Önce environment variable'dan Backend URL'lerini oku
            var backendUrlEnv = Environment.GetEnvironmentVariable("SOFTIELREMOTE_BACKEND_URL");
            if (!string.IsNullOrWhiteSpace(backendUrlEnv))
            {
                backendUrls.Add(backendUrlEnv);
            }

            // Birden fazla URL için (virgülle ayrılmış)
            var backendUrlsEnv = Environment.GetEnvironmentVariable("SOFTIELREMOTE_BACKEND_URLS");
            if (!string.IsNullOrWhiteSpace(backendUrlsEnv))
            {
                var urls = backendUrlsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var url in urls)
                {
                    if (!string.IsNullOrWhiteSpace(url) && !backendUrls.Contains(url))
                    {
                        backendUrls.Add(url);
                    }
                }
            }

            // 2. Supabase REST API'den aktif Backend URL'lerini çek (farklı network'ler için)
            // Bu, BackendUrlResolver.DiscoverBackendsFromSupabaseAsync ile aynı mantık
            var supabaseProjectUrl = Environment.GetEnvironmentVariable("SOFTIELREMOTE_SUPABASE_PROJECT_URL");
            var supabaseAnonKey = Environment.GetEnvironmentVariable("SOFTIELREMOTE_SUPABASE_ANON_KEY");
            
            if (!string.IsNullOrWhiteSpace(supabaseProjectUrl) && !string.IsNullOrWhiteSpace(supabaseAnonKey))
            {
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };
                
                // Supabase REST API'den aktif Backend'leri çek (hem PublicUrl hem LocalIp)
                var apiUrl = $"{supabaseProjectUrl.TrimEnd('/')}/rest/v1/BackendRegistry?IsActive=eq.true&LastSeen=gte.{DateTime.UtcNow.AddMinutes(-5):yyyy-MM-ddTHH:mm:ss}Z&select=PublicUrl,LocalIp&order=LastSeen.desc";
                
                httpClient.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseAnonKey}");
                
                var response = await httpClient.GetAsync(apiUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var backendData = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content);
                    
                    if (backendData != null)
                    {
                        foreach (var backend in backendData)
                        {
                            // PublicUrl'i ekle
                            var publicUrl = backend.ContainsKey("PublicUrl") ? backend["PublicUrl"]?.ToString() : null;
                            if (!string.IsNullOrWhiteSpace(publicUrl) && !backendUrls.Contains(publicUrl))
                            {
                                backendUrls.Add(publicUrl);
                            }
                            
                            // LocalIp varsa ve local network URL'i ise ekle
                            var localIp = backend.ContainsKey("LocalIp") ? backend["LocalIp"]?.ToString() : null;
                            if (!string.IsNullOrWhiteSpace(localIp))
                            {
                                // Port'u URL'den çıkar veya varsayılan 5000 kullan
                                var port = "5000";
                                if (!string.IsNullOrWhiteSpace(publicUrl))
                                {
                                    try
                                    {
                                        var uri = new Uri(publicUrl);
                                        port = uri.Port.ToString();
                                    }
                                    catch
                                    {
                                        // Port çıkarılamazsa varsayılan kullan
                                    }
                                }
                                
                                var localUrl = $"http://{localIp}:{port}";
                                if (BackendUrlAccessibilityTester.IsLocalNetworkUrl(localUrl) && !backendUrls.Contains(localUrl))
                                {
                                    backendUrls.Add(localUrl);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Hata durumunda boş liste döndür
        }

        return backendUrls;
    }

    /// <summary>
    /// Bilinen Backend URL'lerinden aktif olanları bulur.
    /// </summary>
    /// <param name="knownBackendUrls">Bilinen Backend URL'leri</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Aktif Backend URL'leri listesi</returns>
    public static async Task<List<string>> DiscoverFromKnownUrlsAsync(
        IEnumerable<string> knownBackendUrls,
        CancellationToken cancellationToken = default)
    {
        var activeBackends = new List<string>();

        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(3)
        };

        var tasks = knownBackendUrls.Select(async url =>
        {
            try
            {
                var response = await httpClient.GetAsync($"{url}/api/backendregistry/active", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return url;
                }
            }
            catch
            {
                // Hata durumunda null döndür
            }
            return null;
        });

        var results = await Task.WhenAll(tasks);
        activeBackends.AddRange(results.Where(r => r != null)!);

        return activeBackends;
    }
}

