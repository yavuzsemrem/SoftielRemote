using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;

namespace SoftielRemote.App.Controls;

/// <summary>
/// WebRTC video stream'ini gösteren WPF kontrolü.
/// </summary>
public partial class WebRTCVideoControl : UserControl
{
    private readonly ILogger<WebRTCVideoControl>? _logger;
    private WriteableBitmap? _videoBitmap;

    public WebRTCVideoControl()
    {
        InitializeComponent();
    }

    public WebRTCVideoControl(ILogger<WebRTCVideoControl> logger) : this()
    {
        _logger = logger;
    }

    /// <summary>
    /// Video frame'i gösterir.
    /// </summary>
    public void UpdateVideoFrame(WriteableBitmap? bitmap)
    {
        Dispatcher.Invoke(() =>
        {
            if (bitmap != null)
            {
                _videoBitmap = bitmap;
                VideoImage.Source = bitmap;
                StatusText.Visibility = Visibility.Collapsed;
                VideoImage.Visibility = Visibility.Visible;
            }
            else
            {
                VideoImage.Source = null;
                StatusText.Visibility = Visibility.Visible;
                VideoImage.Visibility = Visibility.Collapsed;
            }
        });
    }

    /// <summary>
    /// Durum mesajını gösterir.
    /// </summary>
    public void UpdateStatus(string status)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = status;
            StatusText.Visibility = Visibility.Visible;
        });
    }

    /// <summary>
    /// Video boyutunu ayarlar.
    /// </summary>
    public void SetVideoSize(int width, int height)
    {
        Dispatcher.Invoke(() =>
        {
            if (_videoBitmap == null || _videoBitmap.PixelWidth != width || _videoBitmap.PixelHeight != height)
            {
                _videoBitmap = new WriteableBitmap(
                    width,
                    height,
                    96, // DPI
                    96,
                    System.Windows.Media.PixelFormats.Bgr32,
                    null);

                VideoImage.Source = _videoBitmap;
            }
        });
    }

    /// <summary>
    /// Video frame'ine byte array'den veri yazar.
    /// </summary>
    public void WriteFrameData(byte[] frameData, int stride)
    {
        Dispatcher.Invoke(() =>
        {
            if (_videoBitmap != null)
            {
                _videoBitmap.Lock();
                try
                {
                    var rect = new Int32Rect(0, 0, _videoBitmap.PixelWidth, _videoBitmap.PixelHeight);
                    _videoBitmap.WritePixels(rect, frameData, stride, 0);
                }
                finally
                {
                    _videoBitmap.Unlock();
                }
            }
        });
    }
}



