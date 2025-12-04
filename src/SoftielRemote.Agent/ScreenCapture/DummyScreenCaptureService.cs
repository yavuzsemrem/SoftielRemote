using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using SoftielRemote.Core.Messages;

namespace SoftielRemote.Agent.ScreenCapture;

/// <summary>
/// Dummy ekran yakalama servisi (Faz 1 için).
/// Basit bir test görüntüsü üretir.
/// </summary>
[SupportedOSPlatform("windows")]
public class DummyScreenCaptureService : IScreenCaptureService
{
    private long _frameNumber = 0;
    private readonly ILogger<DummyScreenCaptureService> _logger;

    public DummyScreenCaptureService(ILogger<DummyScreenCaptureService> logger)
    {
        _logger = logger;
    }

    public Task<RemoteFrameMessage?> CaptureScreenAsync(int width, int height)
    {
        try
        {
            // Basit bir test görüntüsü oluştur
            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);

            // Arka plan rengi
            graphics.Clear(Color.LightGray);

            // Test metni
            var font = new Font("Arial", 24, FontStyle.Bold);
            var text = $"Frame #{_frameNumber}\n{DateTime.Now:HH:mm:ss}";
            var textSize = graphics.MeasureString(text, font);
            var x = (width - textSize.Width) / 2;
            var y = (height - textSize.Height) / 2;
            
            graphics.DrawString(text, font, Brushes.Black, x, y);

            // Çerçeve çiz
            graphics.DrawRectangle(Pens.Blue, 0, 0, width - 1, height - 1);

            // Bitmap'i JPEG formatında byte array'e çevir
            byte[] imageData;
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Jpeg);
                imageData = ms.ToArray();
            }

            _frameNumber++;

            var frame = new RemoteFrameMessage
            {
                Width = width,
                Height = height,
                ImageData = imageData,
                Timestamp = DateTime.UtcNow,
                FrameNumber = _frameNumber
            };

            return Task.FromResult<RemoteFrameMessage?>(frame);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ekran yakalama hatası");
            return Task.FromResult<RemoteFrameMessage?>(null);
        }
    }
}

