using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SoftielRemote.Core.Utils;

/// <summary>
/// Ağ işlemleri için yardımcı sınıf.
/// </summary>
public static class NetworkHelper
{
    /// <summary>
    /// Makinenin yerel IP adresini bulur (localhost hariç).
    /// </summary>
    public static string? GetLocalIpAddress()
    {
        try
        {
            // Önce hostname'den IP almayı dene
            var hostName = Dns.GetHostName();
            var hostEntry = Dns.GetHostEntry(hostName);
            
            // IPv4 adreslerini filtrele (localhost ve loopback hariç)
            var ipAddress = hostEntry.AddressList
                .FirstOrDefault(ip => 
                    ip.AddressFamily == AddressFamily.InterNetwork && 
                    !IPAddress.IsLoopback(ip) &&
                    !ip.ToString().StartsWith("169.254.")); // APIPA adresleri hariç

            if (ipAddress != null)
            {
                return ipAddress.ToString();
            }

            // Alternatif: NetworkInterface'lerden IP al
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                var properties = networkInterface.GetIPProperties();
                foreach (var address in properties.UnicastAddresses)
                {
                    if (address.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(address.Address) &&
                        !address.Address.ToString().StartsWith("169.254."))
                    {
                        return address.Address.ToString();
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}


