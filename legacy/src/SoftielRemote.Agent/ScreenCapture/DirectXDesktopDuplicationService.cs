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
    private readonly CursorCaptureService _cursorCapture;
    private bool _needsReinitialize = false;

    public DirectXDesktopDuplicationService(ILogger<DirectXDesktopDuplicationService> logger)
    {
        _logger = logger;
        _cursorCapture = new CursorCaptureService();
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
            // Eğer device zaten varsa, önce temizle
            if (_duplication != null)
            {
                try
                {
                    _duplication.Dispose();
                }
                catch
                {
                    // Ignore dispose errors
                }
                _duplication = null;
            }

            // Eğer device yoksa oluştur
            if (_device == null)
            {
                _device = new Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.None);
                _logger.LogInformation("Direct3D11 device oluşturuldu");
            }

            // Adapter ve Output bul
            using var factory = new Factory1();
            var adapter = factory.GetAdapter1(0);
            _logger.LogInformation("Adapter bulundu: {Description}", adapter.Description.Description);
            
            _output = adapter.GetOutput(0).QueryInterface<Output1>();
            _logger.LogInformation("Output bulundu");

            // Output description al
            _outputDescription = _output.Description;

            // Desktop Duplication oluştur
            _duplication = _output.DuplicateOutput(_device);
            _logger.LogInformation("Desktop Duplication oluşturuldu");

            var bounds = _outputDescription.DesktopBounds;
            _logger.LogInformation("✅ DirectX Desktop Duplication başlatıldı. Çözünürlük: {Width}x{Height}",
                bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
            
            _needsReinitialize = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ DirectX Desktop Duplication başlatılamadı: {Message}", ex.Message);
            _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
            // Hata durumunda servis null kalacak, fallback mekanizması çalışacak
            _needsReinitialize = true;
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

            // Eğer reinitialize gerekiyorsa, önce reinitialize et
            if (_needsReinitialize)
            {
                try
                {
                    _logger.LogWarning("DirectX duplication reinitialize ediliyor (AccessLost hatası nedeniyle)");
                    _duplication?.Dispose();
                    _duplication = null;
                    Initialize();
                    _needsReinitialize = false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DirectX duplication reinitialize edilemedi");
                    return Task.FromResult<RemoteFrameMessage?>(null);
                }
            }

            try
            {
                // Frame yakala - SharpDX Desktop Duplication API
                // SharpDX'de AcquireNextFrame yerine TryAcquireNextFrame kullanılır
                // Timeout: 100ms (daha uzun timeout, frame yakalama garantisi için)
                if (_duplication == null)
                {
                    return Task.FromResult<RemoteFrameMessage?>(null);
                }
                
                var result = _duplication.TryAcquireNextFrame(100, out var frameInfo, out var desktopResource);

                if (result.Failure || desktopResource == null)
                {
                    // Timeout veya başka bir hata - normal durum, null döndür
                    return Task.FromResult<RemoteFrameMessage?>(null);
                }

                try
                {
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

                            // İstenen boyuta yeniden boyutlandır (width/height 0 ise orijinal boyutu kullan)
                            Bitmap resizedBitmap;
                            var targetWidth = width > 0 ? width : bitmap.Width;
                            var targetHeight = height > 0 ? height : bitmap.Height;
                            
                            if (bitmap.Width != targetWidth || bitmap.Height != targetHeight)
                            {
                                resizedBitmap = new Bitmap(bitmap, targetWidth, targetHeight);
                            }
                            else
                            {
                                resizedBitmap = new Bitmap(bitmap);
                            }

                            // Cursor'ı resize edilmiş bitmap'e çiz (resize'dan sonra, cursor pozisyonu scale edilmeli)
                            // NOT: DirectX Desktop Duplication API cursor'ı otomatik olarak yakalamaz
                            // Bu yüzden cursor'ı manuel olarak çizmemiz gerekiyor
                            try
                            {
                                // Cursor pozisyonunu scale et
                                var scaleX = (double)targetWidth / bitmap.Width;
                                var scaleY = (double)targetHeight / bitmap.Height;
                                
                                // Cursor'ı çiz (log yok - sadece gerektiğinde çiz)
                                _cursorCapture.DrawCursorOnBitmap(resizedBitmap, scaleX, scaleY);
                            }
                            catch (Exception cursorEx)
                            {
                                // Cursor çizme hatası - frame'i bozmadan devam et
                                // İlk 5 hatada log (debug için)
                                if (_frameNumber <= 5)
                                {
                                    _logger.LogWarning(cursorEx, "Cursor çizme hatası (frame devam ediyor): {Message}", cursorEx.Message);
                                }
                                else
                                {
                                    _logger.LogDebug(cursorEx, "Cursor çizme hatası (frame devam ediyor)");
                                }
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
                                Width = targetWidth,
                                Height = targetHeight,
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
                finally
                {
                    // KRİTİK: Desktop Duplication API'de her AcquireNextFrame sonrası ReleaseFrame çağrılmalı
                    // Aksi halde frame'ler yakalanamaz ve "Frame yakalanamadı" hatası alınır
                    try
                    {
                        if (_duplication != null)
                        {
                            _duplication.ReleaseFrame();
                        }
                    }
                    catch (SharpDXException dxEx) when (dxEx.ResultCode.Code == SharpDX.DXGI.ResultCode.AccessLost.Result.Code)
                    {
                        // AccessLost hatası - duplication'ı yeniden initialize etmeliyiz
                        _logger.LogWarning("DirectX AccessLost hatası - duplication yeniden initialize edilecek");
                        _needsReinitialize = true;
                        // Duplication'ı temizle
                        try
                        {
                            _duplication?.Dispose();
                            _duplication = null;
                        }
                        catch
                        {
                            // Ignore dispose errors
                        }
                    }
                    catch (Exception releaseEx)
                    {
                        _logger.LogDebug(releaseEx, "Frame release hatası (normal olabilir)");
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
                _logger.LogError(ex, "Ekran yakalama hatası: {Message}", ex.Message);
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

