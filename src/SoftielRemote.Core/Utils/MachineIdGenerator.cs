using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace SoftielRemote.Core.Utils;

/// <summary>
/// Makine bazlı sabit Device ID üretmek için yardımcı sınıf.
/// Aynı makinede her zaman aynı ID'yi üretir.
/// </summary>
public static class MachineIdGenerator
{
    /// <summary>
    /// Makine bazlı sabit bir Device ID üretir.
    /// MAC adresi ve makine adına göre deterministik bir ID üretir.
    /// </summary>
    /// <returns>9 haneli sayısal Device ID</returns>
    public static string GenerateMachineBasedId()
    {
        try
        {
            // Makine adı
            var machineName = Environment.MachineName;
            
            // İlk aktif network interface'in MAC adresini al
            var macAddress = GetFirstMacAddress();
            
            // Makine adı + MAC adresi kombinasyonu
            var combined = $"{machineName}_{macAddress}";
            
            // SHA256 hash al
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
            
            // Hash'in ilk 4 byte'ını al ve 9 haneli sayıya çevir
            var hashValue = BitConverter.ToUInt32(hash, 0);
            var deviceId = (hashValue % 900000000) + 100000000; // 100000000 - 999999999 arası
            
            return deviceId.ToString();
        }
        catch
        {
            // Hata durumunda rastgele ID üret
            return DeviceIdGenerator.Generate();
        }
    }

    /// <summary>
    /// İlk aktif network interface'in MAC adresini alır.
    /// </summary>
    private static string GetFirstMacAddress()
    {
        try
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    var physicalAddress = networkInterface.GetPhysicalAddress();
                    if (physicalAddress != null && physicalAddress.ToString().Length > 0)
                    {
                        return physicalAddress.ToString();
                    }
                }
            }
        }
        catch
        {
            // Hata durumunda boş string döndür
        }
        
        return "DEFAULT";
    }
}

