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
    public static Task<List<string>> DiscoverBackendUrlsAsync(
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

            // 2. Eğer environment variable yoksa, Supabase REST API'den çek
            // Not: Bu için Supabase project URL'i ve anon key gerekir
            // Şimdilik environment variable kullanılacak
        }
        catch
        {
            // Hata durumunda boş liste döndür
        }

        return Task.FromResult(backendUrls);
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

