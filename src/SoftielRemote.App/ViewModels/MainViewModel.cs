using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SoftielRemote.App.Services;
using SoftielRemote.Core.Dtos;
using SoftielRemote.Core.Enums;
using SoftielRemote.Core.Messages;
using System.Windows.Media.Imaging;

namespace SoftielRemote.App.ViewModels;

/// <summary>
/// Ana ekran ViewModel'i.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IBackendClientService _backendClient;
    private readonly ITcpStreamClient _tcpClient;

    [ObservableProperty]
    private string _yourDeviceId = "---";

    /// <summary>
    /// Device ID'yi 3'er karakter arasına boşluk ekleyerek formatlar (örn: 123 456 789)
    /// </summary>
    public string FormattedDeviceId
    {
        get
        {
            if (string.IsNullOrEmpty(YourDeviceId) || YourDeviceId == "---" || 
                YourDeviceId == "Bağlanamadı" || YourDeviceId == "Hata")
            {
                return YourDeviceId;
            }

            // Boşlukları temizle ve sadece rakamları al
            var cleanId = new string(YourDeviceId.Where(char.IsDigit).ToArray());
            
            if (cleanId.Length != 9)
            {
                return YourDeviceId; // 9 karakter değilse formatlamadan döndür
            }

            // 3'er karakter gruplara ayır
            return $"{cleanId.Substring(0, 3)} {cleanId.Substring(3, 3)} {cleanId.Substring(6, 3)}";
        }
    }

    [ObservableProperty]
    private string _password = "---";

    [ObservableProperty]
    private string _remoteDeviceId = string.Empty;

    [ObservableProperty]
    private bool _isConnected = false;

    [ObservableProperty]
    private BitmapImage? _remoteScreenImage;

    [ObservableProperty]
    private string _statusMessage = "Hazır";

    private CancellationTokenSource? _frameReceiveCancellation;

    public MainViewModel(IBackendClientService backendClient, ITcpStreamClient tcpClient)
    {
        _backendClient = backendClient;
        _tcpClient = tcpClient;
        
        System.Diagnostics.Debug.WriteLine("🔵 MainViewModel constructor çağrıldı");
        
        // Uygulama açıldığında Backend'e kayıt ol
        Task.Run(async () =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🟢 InitializeAsync başlatılıyor...");
                await InitializeAsync();
                System.Diagnostics.Debug.WriteLine("🟢 InitializeAsync tamamlandı");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🔴 InitializeAsync exception: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"🔴 StackTrace: {ex.StackTrace}");
            }
        });
    }

    /// <summary>
    /// Uygulama başlangıcında Backend'e kayıt olur ve Device ID alır.
    /// </summary>
    private async Task InitializeAsync()
    {
        const int maxRetries = 5;
        const int retryDelayMs = 2000; // 2 saniye

        System.Diagnostics.Debug.WriteLine("🟡 InitializeAsync başladı");

        // Başlangıç değerleri
        StatusMessage = "Backend'e bağlanılıyor...";
        YourDeviceId = "---";
        Password = "---";
        OnPropertyChanged(nameof(FormattedDeviceId)); // Formatted property'yi güncelle

        System.Diagnostics.Debug.WriteLine($"🟡 Backend URL: {(_backendClient as BackendClientService)?.GetType().Name ?? "Unknown"}");

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🟡 Deneme {attempt}/{maxRetries} başlıyor...");
                StatusMessage = $"Backend'e bağlanılıyor... (Deneme {attempt}/{maxRetries})";
                
                var registrationRequest = new AgentRegistrationRequest
                {
                    MachineName = Environment.MachineName,
                    OperatingSystem = Environment.OSVersion.ToString()
                };

                System.Diagnostics.Debug.WriteLine($"🟡 RegisterAsync çağrılıyor...");
                var response = await _backendClient.RegisterAsync(registrationRequest);
                System.Diagnostics.Debug.WriteLine($"🟡 RegisterAsync yanıt aldı. Success: {response.Success}, DeviceId: {response.DeviceId}, Password: {response.Password}");

                if (response.Success && !string.IsNullOrEmpty(response.DeviceId))
                {
                    YourDeviceId = response.DeviceId;
                    Password = !string.IsNullOrEmpty(response.Password) ? response.Password : "---";
                    StatusMessage = "Hazır";
                    OnPropertyChanged(nameof(FormattedDeviceId)); // Formatted property'yi güncelle
                    System.Diagnostics.Debug.WriteLine($"✅ Backend'e başarıyla kayıt olundu. Device ID: {YourDeviceId}, Password: {Password}");
                    
                    return; // Başarılı, çık
                }
                else
                {
                    var errorMsg = response.ErrorMessage ?? "Bilinmeyen hata";
                    System.Diagnostics.Debug.WriteLine($"❌ Backend kayıt hatası (Deneme {attempt}/{maxRetries}): {errorMsg}");
                    
                    // Son denemede hata mesajını göster
                    if (attempt == maxRetries)
                    {
                        StatusMessage = $"Bağlantı başarısız: {errorMsg}";
                        YourDeviceId = "Bağlanamadı";
                        Password = "---";
                        OnPropertyChanged(nameof(FormattedDeviceId)); // Formatted property'yi güncelle
                        
                        // Backend çalışmıyor olabilir
                        if (errorMsg.Contains("refused") || errorMsg.Contains("reddetti") || 
                            errorMsg.Contains("No connection") || errorMsg.Contains("could not be resolved") ||
                            errorMsg.Contains("actively refused") || errorMsg.Contains("Connection refused"))
                        {
                            StatusMessage = "Backend çalışmıyor. Lütfen Backend'i başlatın (http://localhost:5056)";
                        }
                        
                        return;
                    }
                }
                
                // Retry için bekle
                System.Diagnostics.Debug.WriteLine($"🟡 {retryDelayMs}ms bekleniyor...");
                await Task.Delay(retryDelayMs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Exception (Deneme {attempt}/{maxRetries}): {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                
                // Son denemede hata mesajını göster
                if (attempt == maxRetries)
                {
                    StatusMessage = $"Bağlantı hatası: {ex.Message}";
                    YourDeviceId = "Hata";
                    Password = "---";
                    OnPropertyChanged(nameof(FormattedDeviceId)); // Formatted property'yi güncelle
                    
                    if (ex.Message.Contains("refused") || ex.Message.Contains("reddetti") || 
                        ex.Message.Contains("No connection") || ex.Message.Contains("could not be resolved") ||
                        ex.Message.Contains("actively refused") || ex.Message.Contains("Connection refused"))
                    {
                        StatusMessage = "Backend çalışmıyor. Lütfen Backend'i başlatın (http://localhost:5056)";
                    }
                    
                    return;
                }
                
                // Retry için bekle
                await Task.Delay(retryDelayMs);
            }
        }
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(RemoteDeviceId))
        {
            StatusMessage = "Lütfen Remote ID girin";
            return;
        }

        try
        {
            StatusMessage = "Bağlanılıyor...";
            IsConnected = false;

            // Backend'e bağlantı isteği gönder
            var connectionRequest = new ConnectionRequest
            {
                TargetDeviceId = RemoteDeviceId,
                QualityLevel = QualityLevel.Medium
            };

            var response = await _backendClient.RequestConnectionAsync(connectionRequest);

            if (!response.Success)
            {
                StatusMessage = $"Bağlantı hatası: {response.ErrorMessage}";
                return;
            }

            // AgentEndpoint'i parse et (IP:Port formatında)
            string host = "localhost";
            int port = 8888;
            
            if (!string.IsNullOrEmpty(response.AgentEndpoint))
            {
                var parts = response.AgentEndpoint.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out port))
                {
                    host = parts[0];
                }
            }
            else
            {
                // AgentEndpoint yoksa localhost kullan (geriye dönük uyumluluk)
                System.Diagnostics.Debug.WriteLine("⚠️ AgentEndpoint bulunamadı, localhost kullanılıyor");
            }

            System.Diagnostics.Debug.WriteLine($"🔵 Agent'a bağlanılıyor: {host}:{port}");
            var connected = await _tcpClient.ConnectAsync(host, port);

            if (!connected)
            {
                StatusMessage = "Agent'a bağlanılamadı";
                return;
            }

            IsConnected = true;
            StatusMessage = "Bağlandı";

            // Frame alma döngüsünü başlat
            _frameReceiveCancellation = new CancellationTokenSource();
            _ = Task.Run(async () => await ReceiveFramesAsync(_frameReceiveCancellation.Token));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hata: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Disconnect()
    {
        _frameReceiveCancellation?.Cancel();
        _tcpClient.Disconnect();
        IsConnected = false;
        RemoteScreenImage = null;
        StatusMessage = "Bağlantı kesildi";
    }

    /// <summary>
    /// Belirli bir Device ID'ye bağlanır (session card'dan tıklandığında).
    /// </summary>
    public void ConnectToDevice(string deviceId)
    {
        RemoteDeviceId = deviceId;
        ConnectCommand.Execute(null);
    }

    private async Task ReceiveFramesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _tcpClient.IsConnected)
        {
            try
            {
                var frame = await _tcpClient.ReceiveFrameAsync(cancellationToken);
                
                if (frame == null)
                {
                    await Task.Delay(10, cancellationToken);
                    continue;
                }

                // Frame'i BitmapImage'e çevir
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        using var ms = new MemoryStream(frame.ImageData);
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = ms;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        RemoteScreenImage = bitmap;
                    }
                    catch (Exception ex)
                    {
                        // Görüntü yükleme hatası, sessizce devam et
                        System.Diagnostics.Debug.WriteLine($"Frame yükleme hatası: {ex.Message}");
                    }
                });
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Frame alma hatası: {ex.Message}");
                await Task.Delay(100, cancellationToken);
            }
        }
    }
}

