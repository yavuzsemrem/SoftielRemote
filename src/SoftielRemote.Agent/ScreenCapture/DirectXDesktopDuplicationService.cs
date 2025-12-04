using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SoftielRemote.Core.Messages;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using MapMode = SharpDX.Direct3D11.MapMode;

namespace SoftielRemote.Agent.ScreenCapture;

/// <summary>
/// DirectX Desktop Duplication API kullanarak ekran yakalama servisi (Production-ready).
/// Windows 8+ gerektirir, programatik kullanım için idealdir.
/// </summary>
public class DirectXDesktopDuplicationService : IScreenCaptureService, IDisposable
{
    private readonly ILogger<DirectXDesktopDuplicationService> _logger;
    private Device? _device;
    private OutputDuplication? _duplication;
    private Output1? _output;
    private OutputDescription _outputDescription;
    private Texture2D? _desktopImage;
    private bool _disposed = false;
    private long _frameNumber = 0;
    private readonly object _lock = new();

    public DirectXDesktopDuplicationService(ILogger<DirectXDesktopDuplicationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Ekran yakalamayı başlatır.
    /// </summary>
    public void StartCapture()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            if (_device == null)
            {
                Initialize();
            }
        }
    }

    private void Initialize()
    {
        try
        {
            // Direct3D11 device oluştur
            _device = new Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.None);

            // Adapter ve Output bul
            using var factory = new Factory1();
            var adapter = factory.GetAdapter1(0);
            _output = adapter.GetOutput(0).QueryInterface<Output1>();

            // Output description al
            _outputDescription = _output.Description;

            // Desktop Duplication oluştur
            _duplication = _output.DuplicateOutput(_device);

            var bounds = _outputDescription.DesktopBounds;
            _logger.LogInformation("DirectX Desktop Duplication başlatıldı. Çözünürlük: {Width}x{Height}",
                bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DirectX Desktop Duplication başlatılamadı");
        }
    }

    public Task<RemoteFrameMessage?> CaptureScreenAsync(int width, int height)
    {
        lock (_lock)
        {
            if (_disposed || _duplication == null || _device == null)
            {
                return Task.FromResult<RemoteFrameMessage?>(null);
            }

            try
            {
                // Frame yakala - SharpDX Desktop Duplication API
                // SharpDX'de AcquireNextFrame yerine TryAcquireNextFrame kullanılır
                var result = _duplication.TryAcquireNextFrame(100, out var frameInfo, out var desktopResource);

                if (result.Failure || desktopResource == null)
                {
                    return Task.FromResult<RemoteFrameMessage?>(null);
                }

                using (desktopResource)
                {
                    // Texture2D'ye dönüştür
                    using var screenTexture = desktopResource.QueryInterface<Texture2D>();

                    // Eğer boyut değiştiyse yeni texture oluştur
                    var textureDesc = screenTexture.Description;
                    if (_desktopImage == null || 
                        _desktopImage.Description.Width != textureDesc.Width || 
                        _desktopImage.Description.Height != textureDesc.Height)
                    {
                        _desktopImage?.Dispose();
                        _desktopImage = new Texture2D(_device, new Texture2DDescription
                        {
                            Width = textureDesc.Width,
                            Height = textureDesc.Height,
                            MipLevels = 1,
                            ArraySize = 1,
                            Format = Format.B8G8R8A8_UNorm,
                            SampleDescription = new SampleDescription(1, 0),
                            Usage = ResourceUsage.Staging,
                            CpuAccessFlags = CpuAccessFlags.Read,
                            OptionFlags = ResourceOptionFlags.None
                        });
                    }

                    // Ekran texture'ını staging texture'a kopyala
                    _device.ImmediateContext.CopyResource(screenTexture, _desktopImage);

                    // CPU'ya map et
                    var mapSource = _device.ImmediateContext.MapSubresource(_desktopImage, 0, MapMode.Read, MapFlags.None);

                    try
                    {
                        // Bitmap oluştur
                        using var bitmap = new Bitmap(textureDesc.Width, textureDesc.Height, PixelFormat.Format32bppArgb);
                        var bitmapData = bitmap.LockBits(
                            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                            ImageLockMode.WriteOnly,
                            bitmap.PixelFormat);

                        try
                        {
                            // Veriyi kopyala
                            var sourcePtr = mapSource.DataPointer;
                            var destPtr = bitmapData.Scan0;
                            var rowPitch = Math.Min(mapSource.RowPitch, bitmapData.Stride);

                            unsafe
                            {
                                for (int y = 0; y < bitmap.Height; y++)
                                {
                                    System.Buffer.MemoryCopy(
                                        (void*)(sourcePtr + y * mapSource.RowPitch),
                                        (void*)(destPtr + y * bitmapData.Stride),
                                        rowPitch,
                                        rowPitch);
                                }
                            }
                        }
                        finally
                        {
                            bitmap.UnlockBits(bitmapData);
                        }

                        // İstenen boyuta yeniden boyutlandır
                        Bitmap resizedBitmap;
                        if (bitmap.Width != width || bitmap.Height != height)
                        {
                            resizedBitmap = new Bitmap(bitmap, width, height);
                        }
                        else
                        {
                            resizedBitmap = new Bitmap(bitmap);
                        }

                        // JPEG formatında byte array'e çevir
                        byte[] imageData;
                        using (var ms = new System.IO.MemoryStream())
                        {
                            var encoder = ImageCodecInfo.GetImageEncoders()
                                .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
                            
                            var encoderParams = new EncoderParameters(1);
                            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 80L);

                            if (encoder != null)
                            {
                                resizedBitmap.Save(ms, encoder, encoderParams);
                            }
                            else
                            {
                                resizedBitmap.Save(ms, ImageFormat.Jpeg);
                            }
                            
                            imageData = ms.ToArray();
                        }

                        resizedBitmap.Dispose();

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
                    finally
                    {
                        _device.ImmediateContext.UnmapSubresource(_desktopImage, 0);
                    }
                }
            }
            catch (SharpDXException ex) when (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
            {
                // Timeout - normal, frame yok
                return Task.FromResult<RemoteFrameMessage?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ekran yakalama hatası");
                return Task.FromResult<RemoteFrameMessage?>(null);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            _duplication?.Dispose();
            _desktopImage?.Dispose();
            _output?.Dispose();
            _device?.Dispose();
            _disposed = true;
        }
    }
}

