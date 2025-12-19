namespace SoftielRemote.Core.Utils;

/// <summary>
/// Password üretmek için yardımcı sınıf.
/// </summary>
public static class PasswordGenerator
{
    /// <summary>
    /// Yeni bir password üretir.
    /// Format: 6 haneli sayısal password (örn: 123456)
    /// </summary>
    /// <returns>Password</returns>
    public static string Generate()
    {
        // 6 haneli rastgele sayı üret (100000 - 999999 arası)
        var random = new Random();
        var password = random.Next(100000, 999999);
        return password.ToString();
    }
}

