using System.Net.Http;
using System.Net.NetworkInformation;

namespace SoftielRemote.Core.Utils;

/// <summary>
/// Backend URL'inin erişilebilir olup olmadığını test eden servis.
/// Hem local network hem de internet üzerinden erişilebilirliği kontrol eder.
/// </summary>
public static class BackendUrlAccessibilityTester
{
    /// <summary>
    /// Backend URL'inin erişilebilir olup olmadığını test eder.
    /// </summary>
    /// <param name="url">Test edilecek Backend URL'i</param>
    /// <param name="timeoutSeconds">Timeout süresi (saniye)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Erişilebilirse true, değilse false</returns>
    public static async Task<bool> TestAccessibilityAsync(string url, int timeoutSeconds = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
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
                // Health check başarısız olursa diğer endpoint'leri dene
            }

            // Health check başarısız olursa register endpoint'ini dene
            try
            {
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
        }
        catch
        {
            // Hata durumunda false döndür
        }
        
        return false;
    }

    /// <summary>
    /// URL'in local network'te erişilebilir olup olmadığını kontrol eder.
    /// </summary>
    /// <param name="url">Test edilecek URL</param>
    /// <returns>Local network'te erişilebilirse true</returns>
    public static bool IsLocalNetworkUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var lowerUrl = url.ToLowerInvariant();
        
        // Localhost kontrolü
        if (lowerUrl.Contains("localhost") || lowerUrl.Contains("127.0.0.1"))
            return true;

        // Local IP aralıkları kontrolü
        try
        {
            var uri = new Uri(url);
            var host = uri.Host;
            
            // Private IP aralıkları:
            // 10.0.0.0/8
            // 172.16.0.0/12
            // 192.168.0.0/16
            // 169.254.0.0/16 (APIPA)
            
            if (System.Net.IPAddress.TryParse(host, out var ipAddress))
            {
                var bytes = ipAddress.GetAddressBytes();
                
                // 10.0.0.0/8
                if (bytes[0] == 10)
                    return true;
                
                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                    return true;
                
                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168)
                    return true;
                
                // 169.254.0.0/16 (APIPA)
                if (bytes[0] == 169 && bytes[1] == 254)
                    return true;
            }
            
            // Mevcut network interface'lerinden birinde bu IP var mı?
            var localIps = GetLocalNetworkIps();
            if (localIps.Any(ip => host.Contains(ip)))
                return true;
        }
        catch
        {
            // Parse hatası durumunda false döndür
        }

        return false;
    }

    /// <summary>
    /// Local network IP adreslerini döndürür.
    /// </summary>
    private static List<string> GetLocalNetworkIps()
    {
        var ips = new List<string>();
        
        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (var ni in networkInterfaces)
            {
                var ipProperties = ni.GetIPProperties();
                foreach (var addr in ipProperties.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        var ip = addr.Address.ToString();
                        if (!ip.StartsWith("169.254.")) // APIPA adresleri hariç
                        {
                            ips.Add(ip);
                        }
                    }
                }
            }
        }
        catch
        {
            // Hata durumunda boş liste döndür
        }

        return ips;
    }

}

