using System.Security.Cryptography;

namespace SoftielRemote.Backend.Api.Infrastructure;

public static class DeviceCodeGenerator
{
    public static string Generate()
    {
        // 9 haneli kod: 3-3-3 format (örn: 123 456 789)
        var bytes = new byte[9];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        // Her byte'ı 0-9 aralığına map et
        var digits = new int[9];
        for (int i = 0; i < 9; i++)
        {
            digits[i] = bytes[i] % 10;
        }

        // 3-3-3 formatında string'e çevir
        return $"{digits[0]}{digits[1]}{digits[2]} {digits[3]}{digits[4]}{digits[5]} {digits[6]}{digits[7]}{digits[8]}";
    }
}

