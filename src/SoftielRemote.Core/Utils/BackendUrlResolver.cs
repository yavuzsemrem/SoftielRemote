using System.Net.Http;

namespace SoftielRemote.Core.Utils;

/// <summary>
/// Backend URL'ini çözmek için yardımcı sınıf.
/// Development modunda otomatik keşif yapar, production modunda appsettings.json'dan okur.
/// </summary>
public static class BackendUrlResolver
{
    private static readonly string[] CommonBackendUrls = new[]
    {
        "http://localhost:5000",
        "http://localhost:5056",
        "http://127.0.0.1:5000",
        "http://127.0.0.1:5056"
    };

    /// <summary>
    /// Backend URL'ini çözer. Öncelik sırası:
    /// 1. Environment variable (SOFTIELREMOTE_BACKEND_URL)
    /// 2. appsettings.json'dan okunan URL
    /// 3. Supabase'den aktif Backend URL'leri
    /// 4. Localhost otomatik keşif
    /// </summary>
    /// <param name="configuredUrl">appsettings.json'dan okunan URL (null olabilir)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Çalışan Backend URL'i veya null</returns>
    public static async Task<string?> ResolveBackendUrlAsync(string? configuredUrl, CancellationToken cancellationToken = default)
    {
        // 1. Önce environment variable'dan oku (en yüksek öncelik)
        var envBackendUrl = Environment.GetEnvironmentVariable("SOFTIELREMOTE_BACKEND_URL");
        if (!string.IsNullOrWhiteSpace(envBackendUrl) && await TryBackendUrlAsync(envBackendUrl, cancellationToken))
        {
            return envBackendUrl;
        }

        // 2. appsettings.json'dan okunan URL'i dene
        if (!string.IsNullOrWhiteSpace(configuredUrl) && !IsLocalhostUrl(configuredUrl))
        {
            if (await TryBackendUrlAsync(configuredUrl, cancellationToken))
            {
                return configuredUrl;
            }
        }

        // 3. Supabase'den aktif Backend URL'lerini çek
        var supabaseUrls = await SupabaseBackendDiscovery.DiscoverBackendUrlsAsync(cancellationToken: cancellationToken);
        if (supabaseUrls.Count > 0)
        {
            // Önce environment variable'dan gelen URL'leri dene
            foreach (var url in supabaseUrls)
            {
                if (await TryBackendUrlAsync(url, cancellationToken))
                {
                    return url;
                }
            }

            // Eğer environment variable'dan gelen URL'ler çalışmıyorsa,
            // bilinen Backend URL'lerinden aktif olanları bul
            var activeBackends = await SupabaseBackendDiscovery.DiscoverFromKnownUrlsAsync(supabaseUrls, cancellationToken);
            if (activeBackends.Count > 0)
            {
                return activeBackends.First();
            }
        }

        // 4. Localhost otomatik keşif (development fallback)
        if (string.IsNullOrWhiteSpace(configuredUrl) || IsLocalhostUrl(configuredUrl))
        {
            return await DiscoverBackendUrlAsync(cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// URL'in localhost olup olmadığını kontrol eder.
    /// </summary>
    private static bool IsLocalhostUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        var lowerUrl = url.ToLowerInvariant();
        return lowerUrl.Contains("localhost") || 
               lowerUrl.Contains("127.0.0.1") ||
               lowerUrl.StartsWith("http://localhost") ||
               lowerUrl.StartsWith("https://localhost") ||
               lowerUrl.StartsWith("http://127.0.0.1") ||
               lowerUrl.StartsWith("https://127.0.0.1");
    }

    /// <summary>
    /// Yaygın Backend URL'lerini dener ve çalışan birini bulur.
    /// </summary>
    private static async Task<string?> DiscoverBackendUrlAsync(CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2) // Hızlı timeout
        };

        foreach (var url in CommonBackendUrls)
        {
            if (await TryBackendUrlAsync(url, cancellationToken))
            {
                return url;
            }
        }

        return null;
    }

    /// <summary>
    /// Belirli bir Backend URL'inin çalışıp çalışmadığını kontrol eder.
    /// </summary>
    private static async Task<bool> TryBackendUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(3)
            };

            // Önce health check endpoint'ini dene (daha güvenilir)
            try
            {
                var healthResponse = await httpClient.GetAsync($"{url.TrimEnd('/')}/health", cancellationToken);
                if (healthResponse.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch
            {
                // Health check başarısız olursa register endpoint'ini dene
            }

            // Health check başarısız olursa register endpoint'ini dene
            var response = await httpClient.GetAsync($"{url.TrimEnd('/')}/api/agents/register", cancellationToken);
            
            // 405 Method Not Allowed, 400 Bad Request veya 200 OK bekleniyor
            // Bu, Backend'in çalıştığını gösterir
            if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed || 
                response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
                response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return true;
            }
        }
        catch
        {
            // Hata durumunda false döndür
        }
        
        return false;
    }
}

