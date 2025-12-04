using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using SoftielRemote.Core.Messages;

namespace SoftielRemote.Agent.ScreenCapture;

/// <summary>
/// GDI+ kullanarak gerçek ekran yakalama servisi.
/// Basit ve çalışır, ancak performans düşük olabilir (production için DirectX önerilir).
/// </summary>
[SupportedOSPlatform("windows")]
public class GdiScreenCaptureService : IScreenCaptureService, IDisposable
{
    private long _frameNumber = 0;
    private readonly ILogger<GdiScreenCaptureService> _logger;
    private bool _disposed = false;

    public GdiScreenCaptureService(ILogger<GdiScreenCaptureService> logger)
    {
        _logger = logger;
    }

    public Task<RemoteFrameMessage?> CaptureScreenAsync(int width, int height)
    {
        if (_disposed)
        {
            return Task.FromResult<RemoteFrameMessage?>(null);
        }

        try
        {
            // Ekran boyutlarını al (Win32 API kullanarak)
            var screenWidth = GetSystemMetrics(0); // SM_CXSCREEN (genişlik)
            var screenHeight = GetSystemMetrics(1); // SM_CYSCREEN (yükseklik)
            
            // Eğer ekran boyutları alınamazsa varsayılan değerler kullan
            if (screenWidth <= 0) screenWidth = 1920;
            if (screenHeight <= 0) screenHeight = 1080;

            // İstenen boyutlar varsa onları kullan, yoksa ekran boyutlarını kullan
            var captureWidth = width > 0 ? width : screenWidth;
            var captureHeight = height > 0 ? height : screenHeight;

            // Bitmap oluştur
            using var bitmap = new Bitmap(captureWidth, captureHeight);
            using var graphics = Graphics.FromImage(bitmap);

            // Ekran görüntüsünü yakala (ekranın sol üst köşesinden)
            graphics.CopyFromScreen(
                0,
                0,
                0,
                0,
                new Size(captureWidth, captureHeight),
                CopyPixelOperation.SourceCopy);

            // Bitmap'i JPEG formatında byte array'e çevir (kalite: %80)
            byte[] imageData;
            using (var ms = new MemoryStream())
            {
                var encoder = ImageCodecInfo.GetImageEncoders()
                    .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
                
                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 80L);

                if (encoder != null)
                {
                    bitmap.Save(ms, encoder, encoderParams);
                }
                else
                {
                    // Fallback: encoder bulunamazsa standart JPEG kaydet
                    bitmap.Save(ms, ImageFormat.Jpeg);
                }

                imageData = ms.ToArray();
            }

            _frameNumber++;

            var frame = new RemoteFrameMessage
            {
                Width = captureWidth,
                Height = captureHeight,
                ImageData = imageData,
                Timestamp = DateTime.UtcNow,
                FrameNumber = _frameNumber
            };

            _logger.LogDebug("Ekran yakalandı: {Width}x{Height}, Frame #{FrameNumber}, Size: {Size} bytes",
                captureWidth, captureHeight, _frameNumber, imageData.Length);

            return Task.FromResult<RemoteFrameMessage?>(frame);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ekran yakalama hatası");
            return Task.FromResult<RemoteFrameMessage?>(null);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }

    // Win32 API - ekran boyutlarını almak için
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}

