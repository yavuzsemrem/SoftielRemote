using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace SoftielRemote.Agent.ScreenCapture;

/// <summary>
/// Windows API kullanarak mouse cursor'ı yakalayan ve bitmap'e çizen servis.
/// </summary>
public class CursorCaptureService
{
    private readonly ILogger<CursorCaptureService>? _logger;

    private static int _drawCount = 0;
    private static int _errorCount = 0;

    public CursorCaptureService(ILogger<CursorCaptureService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Cursor'ın görünür olup olmadığını kontrol eder.
    /// </summary>
    public bool IsCursorVisible()
    {
        try
        {
            var cursorInfo = new CursorInfo();
            cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);
            
            if (!GetCursorInfo(out cursorInfo))
            {
                return false;
            }

            // Cursor görünür mü?
            return (cursorInfo.flags & 0x00000001) != 0; // CURSOR_SHOWING
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Cursor'ı bitmap'e çizer.
    /// </summary>
    public void DrawCursorOnBitmap(Bitmap bitmap, double scaleX = 1.0, double scaleY = 1.0)
    {
        try
        {
            // Cursor bilgilerini al
            var cursorInfo = new CursorInfo();
            cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);
            
            if (!GetCursorInfo(out cursorInfo))
            {
                return; // Cursor bilgisi alınamadı
            }

            // Cursor görünür değilse çizme
            if ((cursorInfo.flags & 0x00000001) == 0) // CURSOR_SHOWING
            {
                return;
            }

            // Cursor pozisyonunu al
            var cursorPos = new Point();
            if (!GetCursorPos(out cursorPos))
            {
                return;
            }

            // Cursor handle'ını al
            var cursorHandle = cursorInfo.hCursor;
            if (cursorHandle == IntPtr.Zero)
            {
                return;
            }

            // Cursor icon bilgilerini al
            var iconInfo = new IconInfo();
            if (!GetIconInfo(cursorHandle, out iconInfo))
            {
                return;
            }
            
            // İlk 5 çizimde log (debug için)
            var drawCount = System.Threading.Interlocked.Increment(ref _drawCount);
            if (drawCount <= 5)
            {
                _logger?.LogInformation("Cursor çiziliyor: Pos=({X},{Y}), Scale=({ScaleX},{ScaleY}), BitmapSize=({Width},{Height})", 
                    cursorPos.X, cursorPos.Y, scaleX, scaleY, bitmap.Width, bitmap.Height);
            }

            try
            {
                // Cursor bitmap'ini al
                using var cursorBitmap = Bitmap.FromHicon(cursorHandle);
                
                // Cursor'un hotspot (tıklama noktası) pozisyonunu al
                var hotspotX = iconInfo.xHotspot;
                var hotspotY = iconInfo.yHotspot;

                // Cursor pozisyonu ekran koordinatlarında (0,0 ekranın sol üst köşesi)
                // Bitmap de ekranın tamamını temsil ediyor (resize edilmiş olsa bile)
                // Cursor pozisyonunu bitmap koordinatlarına çevir (scale uygula)
                // Hotspot'u da scale et, sonra cursor pozisyonundan çıkar
                var scaledHotspotX = hotspotX * scaleX;
                var scaledHotspotY = hotspotY * scaleY;
                var drawX = (int)(cursorPos.X * scaleX - scaledHotspotX);
                var drawY = (int)(cursorPos.Y * scaleY - scaledHotspotY);

                // Cursor bitmap boyutlarını al (scale uygula)
                var cursorWidth = (int)(cursorBitmap.Width * scaleX);
                var cursorHeight = (int)(cursorBitmap.Height * scaleY);

                // Cursor bitmap'inin bitmap sınırları içinde olup olmadığını kontrol et
                if (drawX + cursorWidth < 0 || drawX >= bitmap.Width ||
                    drawY + cursorHeight < 0 || drawY >= bitmap.Height)
                {
                    return; // Cursor ekran dışında
                }

                // Graphics context oluştur ve cursor'ı çiz
                using var graphics = Graphics.FromImage(bitmap);
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                
                // Cursor için en iyi ayarlar
                if (scaleX == 1.0 && scaleY == 1.0)
                {
                    // Scale yoksa, cursor'ı orijinal boyutunda çiz (daha net)
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    graphics.DrawImageUnscaled(cursorBitmap, drawX, drawY);
                }
                else
                {
                    // Scale varsa, cursor'ı scale edilmiş boyutlarla çiz
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    graphics.DrawImage(cursorBitmap, drawX, drawY, cursorWidth, cursorHeight);
                }
            }
            finally
            {
                // IconInfo'daki bitmap handle'larını temizle
                if (iconInfo.hbmMask != IntPtr.Zero)
                {
                    DeleteObject(iconInfo.hbmMask);
                }
                if (iconInfo.hbmColor != IntPtr.Zero)
                {
                    DeleteObject(iconInfo.hbmColor);
                }
            }
        }
        catch (Exception ex)
        {
            var errorCount = System.Threading.Interlocked.Increment(ref _errorCount);
            // İlk 5 hatada log (debug için)
            if (errorCount <= 5)
            {
                _logger?.LogWarning(ex, "Cursor çizme hatası: {Message}", ex.Message);
            }
            else
            {
                _logger?.LogDebug(ex, "Cursor çizme hatası (normal olabilir)");
            }
        }
    }

    #region Win32 API

    [StructLayout(LayoutKind.Sequential)]
    private struct CursorInfo
    {
        public int cbSize;
        public int flags;
        public IntPtr hCursor;
        public Point ptScreenPos;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IconInfo
    {
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorInfo(out CursorInfo pci);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point lpPoint);

    [DllImport("user32.dll")]
    private static extern bool GetIconInfo(IntPtr hIcon, out IconInfo piconinfo);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    #endregion
}

