using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace SoftielRemote.Core.Utils;

/// <summary>
/// Backend URL'ini çözmek için yardımcı sınıf.
/// Tek bir mantık ile hem development hem production ortamlarında çalışır.
/// </summary>
public static class BackendUrlResolver
{
    /// <summary>
    /// Backend URL'ini çözer. Öncelik sırası (her durumda aynı):
    /// 1. Environment variable (SOFTIELREMOTE_BACKEND_URL)
    /// 2. Supabase REST API'den aktif Backend URL'leri (hem local hem public network'ler)
    /// 3. Discovery URL'lerinden aktif Backend listesi (merkezi discovery servisi)
    /// 4. appsettings.json'dan okunan URL (fallback)
    /// </summary>
    /// <param name="configuredUrl">appsettings.json'dan okunan URL (null olabilir)</param>
    /// <param name="configuration">IConfiguration instance (appsettings.json'dan Supabase bilgilerini okumak için)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Çalışan Backend URL'i veya null</returns>
    public static async Task<string?> ResolveBackendUrlAsync(string? configuredUrl, IConfiguration? configuration = null, CancellationToken cancellationToken = default)
    {
        // 1. Önce environment variable'dan oku (en yüksek öncelik)
        var envBackendUrl = Environment.GetEnvironmentVariable("SOFTIELREMOTE_BACKEND_URL");
        if (!string.IsNullOrWhiteSpace(envBackendUrl))
        {
            Console.WriteLine($"🔵 Environment variable SOFTIELREMOTE_BACKEND_URL bulundu: {envBackendUrl}");
            Console.Out.Flush();
            if (await BackendUrlAccessibilityTester.TestAccessibilityAsync(envBackendUrl, timeoutSeconds: 5, cancellationToken))
            {
                Console.WriteLine($"✅ Environment variable'dan Backend URL erişilebilir: {envBackendUrl}");
                Console.Out.Flush();
                return envBackendUrl;
            }
            Console.WriteLine($"❌ Environment variable'dan Backend URL erişilebilir değil: {envBackendUrl}");
            Console.Out.Flush();
        }
        else
        {
            Console.WriteLine("🔵 Environment variable SOFTIELREMOTE_BACKEND_URL bulunamadı, Supabase discovery deneniyor...");
            Console.Out.Flush();
        }

        // 2. Supabase REST API'den aktif Backend'leri çek (hem local hem public network'ler)
        Console.WriteLine("🔵 Supabase discovery deneniyor...");
        Console.Out.Flush();
        var supabaseBackendUrls = await DiscoverBackendsFromSupabaseAsync(configuration, cancellationToken);
        Console.WriteLine($"🔵 Supabase discovery sonucu: {(supabaseBackendUrls != null ? supabaseBackendUrls.Count.ToString() : "null")} URL bulundu");
        Console.Out.Flush();
        if (supabaseBackendUrls != null && supabaseBackendUrls.Count > 0)
        {
            // Supabase'den gelen URL'leri sırayla dene (local/public ayrımı yok)
            foreach (var backendUrl in supabaseBackendUrls)
            {
                Console.WriteLine($"🔵 Supabase'den bulunan URL test ediliyor: {backendUrl}");
                if (!string.IsNullOrWhiteSpace(backendUrl) && 
                    await BackendUrlAccessibilityTester.TestAccessibilityAsync(backendUrl, timeoutSeconds: 5, cancellationToken))
                {
                    Console.WriteLine($"✅ Supabase'den bulunan URL erişilebilir: {backendUrl}");
                    return backendUrl;
                }
                Console.WriteLine($"❌ Supabase'den bulunan URL erişilebilir değil: {backendUrl}");
            }
        }
        else
        {
            Console.WriteLine("⚠️ Supabase discovery başarısız veya Backend bulunamadı");
        }
        
        // 3. Discovery URL'lerinden aktif Backend listesi (merkezi discovery servisi)
        var discoveryUrls = GetDiscoveryUrls();
        if (discoveryUrls != null && discoveryUrls.Count > 0)
        {
            foreach (var discoveryUrl in discoveryUrls)
            {
                if (!string.IsNullOrWhiteSpace(discoveryUrl))
                {
                    var activeBackendsList = await GetActiveBackendsFromBackendAsync(discoveryUrl, cancellationToken);
                    if (activeBackendsList != null && activeBackendsList.Count > 0)
                    {
                        // Discovery URL'den gelen Backend'leri sırayla dene (local/public ayrımı yok)
                        foreach (var activeUrl in activeBackendsList)
                        {
                            if (!string.IsNullOrWhiteSpace(activeUrl) && 
                                await BackendUrlAccessibilityTester.TestAccessibilityAsync(activeUrl, timeoutSeconds: 5, cancellationToken))
                            {
                                return activeUrl;
                            }
                        }
                    }
                }
            }
        }
        
        // 4. Son çare: appsettings.json'dan okunan URL'i dene
        if (!string.IsNullOrWhiteSpace(configuredUrl))
        {
            if (await BackendUrlAccessibilityTester.TestAccessibilityAsync(configuredUrl, timeoutSeconds: 3, cancellationToken))
            {
                return configuredUrl;
            }
        }

        return null;
    }


    /// <summary>
    /// Belirli bir Backend'den aktif Backend URL'lerini çeker.
    /// </summary>
    private static async Task<List<string>?> GetActiveBackendsFromBackendAsync(string backendUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };

            var response = await httpClient.GetAsync($"{backendUrl.TrimEnd('/')}/api/backendregistry/active", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var backendUrls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(content);
                return backendUrls;
            }
        }
        catch
        {
            // Hata durumunda null döndür
        }
        
        return null;
    }

    /// <summary>
    /// Supabase REST API'den aktif Backend URL'lerini çeker.
    /// Hem local hem public network'lerdeki Backend'leri bulmak için kullanılır.
    /// </summary>
    /// <param name="configuration">IConfiguration instance (appsettings.json'dan Supabase bilgilerini okumak için)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    private static async Task<List<string>?> DiscoverBackendsFromSupabaseAsync(IConfiguration? configuration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Supabase project URL ve anon key'i önce environment variable'dan al
            var supabaseProjectUrl = Environment.GetEnvironmentVariable("SOFTIELREMOTE_SUPABASE_PROJECT_URL");
            var supabaseAnonKey = Environment.GetEnvironmentVariable("SOFTIELREMOTE_SUPABASE_ANON_KEY");
            
            // Environment variable yoksa, appsettings.json'dan oku
            if (string.IsNullOrWhiteSpace(supabaseProjectUrl) && configuration != null)
            {
                supabaseProjectUrl = configuration["Supabase:ProjectUrl"] 
                    ?? configuration["SupabaseProjectUrl"];
                Console.WriteLine($"🔵 Supabase ProjectUrl appsettings.json'dan okundu: {(string.IsNullOrWhiteSpace(supabaseProjectUrl) ? "null" : supabaseProjectUrl)}");
            }
            
            if (string.IsNullOrWhiteSpace(supabaseAnonKey) && configuration != null)
            {
                supabaseAnonKey = configuration["Supabase:AnonKey"] 
                    ?? configuration["SupabaseAnonKey"];
                Console.WriteLine($"🔵 Supabase AnonKey appsettings.json'dan okundu: {(string.IsNullOrWhiteSpace(supabaseAnonKey) ? "null" : "***")}");
            }
            
            if (string.IsNullOrWhiteSpace(supabaseProjectUrl) || string.IsNullOrWhiteSpace(supabaseAnonKey))
            {
                // Hala yoksa, Supabase connection string'den project URL'i çıkar
                var connectionString = Environment.GetEnvironmentVariable("SOFTIELREMOTE_SUPABASE_CONNECTION_STRING");
                
                // Connection string environment variable yoksa, appsettings.json'dan oku
                if (string.IsNullOrWhiteSpace(connectionString) && configuration != null)
                {
                    connectionString = configuration["ConnectionStrings:SupabaseConnection"] 
                        ?? configuration["ConnectionStrings:PostgreSQLConnection"];
                }
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    // Connection string formatı: Host=xxx.supabase.co;Port=5432;...
                    // Project URL'i çıkarmak için Host'u parse et
                    var hostMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Host=([^;]+)");
                    if (hostMatch.Success)
                    {
                        var host = hostMatch.Groups[1].Value;
                        // Pooler host'u normal host'a çevir (örn: aws-1-ap-southeast-1.pooler.supabase.com -> xxx.supabase.co)
                        if (host.Contains(".pooler.supabase.com"))
                        {
                            // Pooler host'tan project URL'i çıkaramayız, bu yüzden null döndür
                            return null;
                        }
                        else if (host.Contains(".supabase.co"))
                        {
                            supabaseProjectUrl = $"https://{host.Replace(".supabase.co", "")}.supabase.co";
                        }
                    }
                }
            }
            
            if (string.IsNullOrWhiteSpace(supabaseProjectUrl) || string.IsNullOrWhiteSpace(supabaseAnonKey))
            {
                Console.WriteLine("❌ Supabase ProjectUrl veya AnonKey bulunamadı");
                return null;
            }
            
            Console.WriteLine($"🔵 Supabase API çağrısı yapılıyor: {supabaseProjectUrl}");
            
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            
            // Supabase REST API'den aktif Backend'leri çek (hem PublicUrl hem LocalIp)
            // LastSeen filtresini 30 dakikaya çıkar (Backend heartbeat her 2 dakikada bir çalışıyor)
            var apiUrl = $"{supabaseProjectUrl.TrimEnd('/')}/rest/v1/BackendRegistry?IsActive=eq.true&LastSeen=gte.{DateTime.UtcNow.AddMinutes(-30):yyyy-MM-ddTHH:mm:ss}Z&select=PublicUrl,LocalIp&order=LastSeen.desc";
            
            httpClient.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseAnonKey}");
            
            Console.WriteLine($"🔵 Supabase API URL: {apiUrl}");
            
            var response = await httpClient.GetAsync(apiUrl, cancellationToken);
            Console.WriteLine($"🔵 Supabase API response: {response.StatusCode}");
            Console.Out.Flush();
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.WriteLine($"🔵 Supabase API response content (ilk 500 karakter): {content.Substring(0, Math.Min(500, content.Length))}");
                Console.Out.Flush();
                
                var backendData = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content);
                
                if (backendData != null)
                {
                    Console.WriteLine($"🔵 Supabase'den {backendData.Count} Backend kaydı bulundu");
                    Console.Out.Flush();
                    var backendUrls = new List<string>();
                    
                    foreach (var backend in backendData)
                    {
                        // PublicUrl ve LocalIp'i oku
                        var publicUrl = backend.ContainsKey("PublicUrl") ? backend["PublicUrl"]?.ToString() : null;
                        var localIp = backend.ContainsKey("LocalIp") ? backend["LocalIp"]?.ToString() : null;
                        Console.WriteLine($"🔵 Backend kaydı: PublicUrl={publicUrl ?? "null"}, LocalIp={localIp ?? "null"}");
                        Console.Out.Flush();
                        
                        // PublicUrl'i ekle
                        if (!string.IsNullOrWhiteSpace(publicUrl) && !backendUrls.Contains(publicUrl))
                        {
                            backendUrls.Add(publicUrl);
                            Console.WriteLine($"✅ PublicUrl eklendi: {publicUrl}");
                            Console.Out.Flush();
                        }
                        
                        // LocalIp varsa ve local network URL'i ise ekle
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
                    
                    Console.WriteLine($"🔵 Toplam {backendUrls.Count} Backend URL bulundu");
                    Console.Out.Flush();
                    return backendUrls.Count > 0 ? backendUrls : null;
                }
                else
                {
                    Console.WriteLine("⚠️ Supabase API response parse edilemedi (backendData null)");
                    Console.Out.Flush();
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.WriteLine($"❌ Supabase API response başarısız: {response.StatusCode}");
                Console.WriteLine($"❌ Error content: {errorContent}");
                Console.Out.Flush();
            }
        }
        catch (Exception ex)
        {
            // Supabase discovery hatası - logla
            Console.WriteLine($"❌ Supabase discovery hatası: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
            }
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            Console.Out.Flush();
        }
        
        return null;
    }

    /// <summary>
    /// Bilinen discovery URL'lerini döndürür (merkezi discovery servisi için).
    /// </summary>
    private static List<string> GetDiscoveryUrls()
    {
        var discoveryUrls = new List<string>();
        
        // Environment variable'dan discovery URL'lerini al
        var discoveryUrlEnv = Environment.GetEnvironmentVariable("SOFTIELREMOTE_DISCOVERY_URL");
        if (!string.IsNullOrWhiteSpace(discoveryUrlEnv))
        {
            discoveryUrls.Add(discoveryUrlEnv);
        }
        
        // Birden fazla discovery URL için (virgülle ayrılmış)
        var discoveryUrlsEnv = Environment.GetEnvironmentVariable("SOFTIELREMOTE_DISCOVERY_URLS");
        if (!string.IsNullOrWhiteSpace(discoveryUrlsEnv))
        {
            var urls = discoveryUrlsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var url in urls)
            {
                if (!string.IsNullOrWhiteSpace(url) && !discoveryUrls.Contains(url))
                {
                    discoveryUrls.Add(url);
                }
            }
        }
        
        return discoveryUrls;
    }

}

