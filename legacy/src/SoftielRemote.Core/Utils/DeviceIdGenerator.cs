namespace SoftielRemote.Core.Utils;

/// <summary>
/// Benzersiz Device ID üretmek için yardımcı sınıf.
/// </summary>
public static class DeviceIdGenerator
{
    /// <summary>
    /// Yeni bir benzersiz Device ID üretir.
    /// Format: 9 haneli sayısal ID (örn: 123456789)
    /// </summary>
    /// <returns>Benzersiz Device ID</returns>
    public static string Generate()
    {
        // 9 haneli rastgele sayı üret (100000000 - 999999999 arası)
        var random = new Random();
        var deviceId = random.Next(100000000, 999999999);
        return deviceId.ToString();
    }

    /// <summary>
    /// Device ID'nin geçerli formatda olup olmadığını kontrol eder.
    /// </summary>
    /// <param name="deviceId">Kontrol edilecek Device ID</param>
    /// <returns>Geçerli ise true</returns>
    public static bool IsValid(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return false;

        // 9 haneli sayı olmalı
        if (deviceId.Length != 9)
            return false;

        return int.TryParse(deviceId, out _);
    }
}

