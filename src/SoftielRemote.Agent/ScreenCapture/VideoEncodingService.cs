using FFMpegCore;
using FFMpegCore.Enums;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SoftielRemote.Agent.ScreenCapture;

/// <summary>
/// Video encoding servisi (FFmpeg kullanarak H.264/H.265 encoding).
/// Hardware encoding desteği (NVENC, QuickSync, VCE).
/// </summary>
public class VideoEncodingService : IDisposable
{
    private readonly ILogger<VideoEncodingService> _logger;
    private bool _disposed = false;

    public VideoEncodingService(ILogger<VideoEncodingService> logger)
    {
        _logger = logger;
        
        // FFmpeg path kontrolü
        var ffmpegPath = FindFFmpegPath();
        if (!string.IsNullOrEmpty(ffmpegPath))
        {
            GlobalFFOptions.Configure(new FFOptions { BinaryFolder = ffmpegPath });
            _logger.LogInformation("FFmpeg bulundu: {Path}", ffmpegPath);
        }
        else
        {
            _logger.LogWarning("FFmpeg bulunamadı. PATH'te veya yaygın konumlarda aranacak.");
        }
    }

    /// <summary>
    /// FFmpeg binary'sinin yolunu bulur.
    /// </summary>
    private string? FindFFmpegPath()
    {
        // Önce PATH'te ara
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            var paths = pathEnv.Split(Path.PathSeparator);
            foreach (var path in paths)
            {
                var ffmpegPath = Path.Combine(path, "ffmpeg.exe");
                if (File.Exists(ffmpegPath))
                {
                    return path;
                }
            }
        }

        // Yaygın konumları kontrol et
        var commonPaths = new[]
        {
            @"C:\ffmpeg\bin",
            @"C:\Program Files\ffmpeg\bin",
            @"C:\Program Files (x86)\ffmpeg\bin",
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "ffmpeg")
        };

        foreach (var commonPath in commonPaths)
        {
            var ffmpegPath = Path.Combine(commonPath, "ffmpeg.exe");
            if (File.Exists(ffmpegPath))
            {
                return commonPath;
            }
        }

        return null;
    }

    /// <summary>
    /// Hardware encoder tipini tespit eder.
    /// </summary>
    public HardwareEncoderType DetectHardwareEncoder()
    {
        try
        {
            // FFmpeg'in hardware encoder desteğini kontrol et
            var ffmpegPath = GlobalFFOptions.Current.BinaryFolder;
            if (string.IsNullOrEmpty(ffmpegPath))
            {
                ffmpegPath = "ffmpeg";
            }
            else
            {
                ffmpegPath = Path.Combine(ffmpegPath, "ffmpeg.exe");
            }

            // NVENC kontrolü (NVIDIA)
            var nvencCheck = RunFFmpegCommand($"{ffmpegPath} -encoders | findstr nvenc");
            if (nvencCheck.Contains("h264_nvenc") || nvencCheck.Contains("hevc_nvenc"))
            {
                _logger.LogInformation("NVENC (NVIDIA) hardware encoder bulundu");
                return HardwareEncoderType.NVENC;
            }

            // QuickSync kontrolü (Intel)
            var qsvCheck = RunFFmpegCommand($"{ffmpegPath} -encoders | findstr qsv");
            if (qsvCheck.Contains("h264_qsv") || qsvCheck.Contains("hevc_qsv"))
            {
                _logger.LogInformation("QuickSync (Intel) hardware encoder bulundu");
                return HardwareEncoderType.QuickSync;
            }

            // VCE kontrolü (AMD)
            var amfCheck = RunFFmpegCommand($"{ffmpegPath} -encoders | findstr amf");
            if (amfCheck.Contains("h264_amf") || amfCheck.Contains("hevc_amf"))
            {
                _logger.LogInformation("VCE (AMD) hardware encoder bulundu");
                return HardwareEncoderType.VCE;
            }

            _logger.LogInformation("Hardware encoder bulunamadı, software encoding kullanılacak");
            return HardwareEncoderType.Software;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hardware encoder tespit edilemedi, software encoding kullanılacak");
            return HardwareEncoderType.Software;
        }
    }

    private string RunFFmpegCommand(string command)
    {
        try
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
        }
        catch
        {
            // Hata durumunda boş string döndür
        }

        return string.Empty;
    }

    /// <summary>
    /// Bitmap'i H.264 formatında encode eder (hardware encoding varsa kullanır).
    /// </summary>
    public async Task<byte[]?> EncodeFrameAsync(Bitmap bitmap, int width, int height, HardwareEncoderType encoderType, int bitrateKbps = 2000)
    {
        if (_disposed || bitmap == null)
            return null;

        try
        {
            // Geçici dosya oluştur (input ve output için)
            var tempInputPath = Path.Combine(Path.GetTempPath(), $"frame_input_{Guid.NewGuid()}.bmp");
            var tempOutputPath = Path.Combine(Path.GetTempPath(), $"frame_output_{Guid.NewGuid()}.h264");

            try
            {
                // Bitmap'i geçici dosyaya kaydet
                bitmap.Save(tempInputPath, ImageFormat.Bmp);

                // FFmpeg encoding komutu oluştur
                var encoder = GetEncoderName(encoderType);
                var ffmpegArgs = $"-i \"{tempInputPath}\" " +
                                $"-vf scale={width}:{height} " +
                                $"-c:v {encoder} " +
                                $"-b:v {bitrateKbps}k " +
                                $"-preset fast " +
                                $"-tune zerolatency " +
                                $"-f h264 " +
                                $"\"{tempOutputPath}\" " +
                                $"-y"; // Overwrite output file

                var ffmpegPath = GlobalFFOptions.Current.BinaryFolder;
                if (string.IsNullOrEmpty(ffmpegPath))
                {
                    ffmpegPath = "ffmpeg";
                }
                else
                {
                    ffmpegPath = Path.Combine(ffmpegPath, "ffmpeg.exe");
                }

                // FFmpeg'i çalıştır
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = ffmpegArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode == 0 && File.Exists(tempOutputPath))
                    {
                        var encodedData = await File.ReadAllBytesAsync(tempOutputPath);
                        _logger.LogDebug("Frame encode edildi: {Width}x{Height}, Encoder={Encoder}, Size={Size} bytes", 
                            width, height, encoder, encodedData.Length);
                        return encodedData;
                    }
                    else
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        _logger.LogWarning("FFmpeg encoding başarısız: ExitCode={ExitCode}, Error={Error}", 
                            process.ExitCode, error);
                    }
                }
            }
            finally
            {
                // Geçici dosyaları temizle
                try
                {
                    if (File.Exists(tempInputPath))
                        File.Delete(tempInputPath);
                    if (File.Exists(tempOutputPath))
                        File.Delete(tempOutputPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Frame encoding hatası");
        }

        return null;
    }

    /// <summary>
    /// Hardware encoder tipine göre FFmpeg encoder adını döndürür.
    /// </summary>
    private string GetEncoderName(HardwareEncoderType encoderType)
    {
        return encoderType switch
        {
            HardwareEncoderType.NVENC => "h264_nvenc",
            HardwareEncoderType.QuickSync => "h264_qsv",
            HardwareEncoderType.VCE => "h264_amf",
            _ => "libx264" // Software encoder
        };
    }

    /// <summary>
    /// Bitmap'i H.264 formatında encode eder (otomatik hardware encoder tespiti ile).
    /// </summary>
    public async Task<byte[]?> EncodeFrameAsync(Bitmap bitmap, int width, int height, int bitrateKbps = 2000)
    {
        var encoderType = DetectHardwareEncoder();
        return await EncodeFrameAsync(bitmap, width, height, encoderType, bitrateKbps);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }
}

/// <summary>
/// Hardware encoder tipi.
/// </summary>
public enum HardwareEncoderType
{
    /// <summary>
    /// Software encoding (CPU).
    /// </summary>
    Software = 0,

    /// <summary>
    /// NVIDIA NVENC.
    /// </summary>
    NVENC = 1,

    /// <summary>
    /// Intel QuickSync.
    /// </summary>
    QuickSync = 2,

    /// <summary>
    /// AMD VCE.
    /// </summary>
    VCE = 3
}

