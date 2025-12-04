using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SoftielRemote.App.Services;
using SoftielRemote.Core.Dtos;
using SoftielRemote.Core.Messages;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace SoftielRemote.App.ViewModels;

/// <summary>
/// Uzak bağlantı penceresi için ViewModel.
/// </summary>
public partial class RemoteConnectionViewModel : ObservableObject, IDisposable
{
    private readonly IBackendClientService _backendClient;
    private readonly ITcpStreamClient _tcpClient;
    private readonly WebRTCClientService? _webrtcClient;
    private CancellationTokenSource? _frameReceiveCancellation;

    [ObservableProperty]
    private bool _isConnected = false;

    [ObservableProperty]
    private bool _isConnecting = false;

    [ObservableProperty]
    private string _connectionStatus = "Bağlantı bekleniyor...";

    [ObservableProperty]
    private BitmapImage? _remoteScreenImage;

    public RemoteConnectionViewModel(
        IBackendClientService backendClient,
        ITcpStreamClient tcpClient,
        string targetDeviceId,
        string agentEndpoint,
        WebRTCClientService? webrtcClient = null)
    {
        _backendClient = backendClient;
        _tcpClient = tcpClient;
        _webrtcClient = webrtcClient;
        TargetDeviceId = targetDeviceId;
        AgentEndpoint = agentEndpoint;
        
        // WebRTC client varsa video frame event'ine subscribe ol
        if (_webrtcClient != null)
        {
            _webrtcClient.OnVideoFrameReceived += HandleWebRTCVideoFrame;
        }
    }

    public string TargetDeviceId { get; }
    public string AgentEndpoint { get; }

    /// <summary>
    /// Bağlantıyı başlatır.
    /// </summary>
    public async Task ConnectAsync()
    {
        IsConnecting = true;
        ConnectionStatus = "Agent'a bağlanılıyor...";

        try
        {
            // AgentEndpoint'i parse et (IP:Port formatında)
            var parts = AgentEndpoint.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out var port))
            {
                ConnectionStatus = "Geçersiz Agent adresi";
                IsConnecting = false;
                return;
            }

            var host = parts[0];
            ConnectionStatus = $"Bağlanılıyor: {host}:{port}";

            // TCP bağlantısı kur
            var connected = await _tcpClient.ConnectAsync(host, port);

            if (!connected)
            {
                ConnectionStatus = $"Bağlantı başarısız: {host}:{port}";
                IsConnecting = false;
                return;
            }

            IsConnected = true;
            IsConnecting = false;
            ConnectionStatus = "Bağlandı";

            // Frame alma döngüsünü başlat
            _frameReceiveCancellation = new CancellationTokenSource();
            _ = Task.Run(async () => await ReceiveFramesAsync(_frameReceiveCancellation.Token));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ ConnectAsync exception: {ex.GetType().Name} - {ex.Message}");
            ConnectionStatus = $"Hata: {ex.Message}";
            IsConnecting = false;
        }
    }

    private async Task ReceiveFramesAsync(CancellationToken cancellationToken)
    {
        System.Diagnostics.Debug.WriteLine("🟢 ReceiveFramesAsync başlatıldı");
        int frameCount = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!_tcpClient.IsConnected)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ TCP bağlantısı kapalı, döngü sonlandırılıyor");
                    break;
                }

                var frame = await _tcpClient.ReceiveFrameAsync(cancellationToken);

                if (frame == null)
                {
                    await Task.Delay(10, cancellationToken);
                    continue;
                }

                frameCount++;
                System.Diagnostics.Debug.WriteLine($"✅ Frame alındı! Frame #{frameCount}");

                // Frame'i BitmapImage'e çevir (UI thread'de)
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher != null)
                {
                    await dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            if (frame.ImageData == null || frame.ImageData.Length == 0)
                            {
                                return;
                            }

                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = new System.IO.MemoryStream(frame.ImageData);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();

                            RemoteScreenImage = bitmap;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Frame yükleme hatası: {ex.Message}");
                        }
                    });
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("🟡 ReceiveFramesAsync iptal edildi");
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Frame alma hatası: {ex.Message}");
                await Task.Delay(100, cancellationToken);
            }
        }

        System.Diagnostics.Debug.WriteLine($"🔴 ReceiveFramesAsync sonlandı. Toplam {frameCount} frame alındı.");
    }

    /// <summary>
    /// WebRTC'den gelen video frame'i işler.
    /// </summary>
    private void HandleWebRTCVideoFrame(System.Windows.Media.Imaging.WriteableBitmap? bitmap)
    {
        if (bitmap == null)
            return;
        
        // WriteableBitmap'i BitmapImage'e çevir
        try
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher != null)
            {
                dispatcher.Invoke(() =>
                {
                    // WriteableBitmap'i BitmapImage'e çevirmek için encode et
                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                    
                    using var ms = new System.IO.MemoryStream();
                    encoder.Save(ms);
                    ms.Position = 0;
                    
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = ms;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                    
                    RemoteScreenImage = bitmapImage;
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ WebRTC frame işleme hatası: {ex.Message}");
        }
    }

    [RelayCommand]
    public void Disconnect()
    {
        _frameReceiveCancellation?.Cancel();
        _tcpClient?.Disconnect();
        
        // WebRTC client varsa event subscription'ı kaldır
        if (_webrtcClient != null)
        {
            _webrtcClient.OnVideoFrameReceived -= HandleWebRTCVideoFrame;
            _webrtcClient.Close();
        }
        
        IsConnected = false;
        ConnectionStatus = "Bağlantı kesildi";
    }

    public void Dispose()
    {
        Disconnect();
        _frameReceiveCancellation?.Dispose();
    }
}

