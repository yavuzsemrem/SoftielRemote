using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SoftielRemote.App.Services;
using SoftielRemote.App.ViewModels;
using SoftielRemote.App.Views;
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
    private IBackendClientService _backendClient;
    private readonly ITcpStreamClient _tcpClient;
    private SignalRClientService? _signalRClient;
    private WebRTCClientService? _webrtcClient;

    [ObservableProperty]
    private string _yourDeviceId = "---";
    
    partial void OnYourDeviceIdChanged(string value)
    {
        // YourDeviceId değiştiğinde FormattedDeviceId'yi de güncelle
        OnPropertyChanged(nameof(FormattedDeviceId));
    }

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
    
    partial void OnRemoteDeviceIdChanged(string value)
    {
        // RemoteDeviceId değiştiğinde ConnectCommand'in CanExecute durumunu güncelle
        ConnectCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    private bool _isConnected = false;
    
    partial void OnIsConnectedChanged(bool value)
    {
        // IsConnected değiştiğinde ConnectCommand'in CanExecute durumunu güncelle
        ConnectCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    private BitmapImage? _remoteScreenImage;

    [ObservableProperty]
    private string _statusMessage = "Hazır";

    public MainViewModel(IBackendClientService backendClient, ITcpStreamClient tcpClient)
    {
        _backendClient = backendClient;
        _tcpClient = tcpClient;
        
        System.Diagnostics.Debug.WriteLine("🔵 MainViewModel constructor çağrıldı");
        
        // Uygulama açıldığında Backend'e kayıt ol
        _ = Task.Run(async () =>
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
                
                // UI thread'inde hata mesajını göster
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"Hata: {ex.Message}";
                    YourDeviceId = "Hata";
                    Password = "---";
                });
            }
        });
    }

    /// <summary>
    /// Backend client'ı günceller ve gerekirse tekrar kayıt yapar.
    /// </summary>
    public void UpdateBackendClient(IBackendClientService newBackendClient)
    {
        var oldBackendUrl = (_backendClient as Services.BackendClientService)?.GetBackendUrl();
        var newBackendUrl = (newBackendClient as Services.BackendClientService)?.GetBackendUrl();
        
        System.Diagnostics.Debug.WriteLine($"🔵 Backend client güncelleniyor: {oldBackendUrl} -> {newBackendUrl}");
        
        _backendClient = newBackendClient;
        
        // Eğer URL değiştiyse ve daha önce kayıt yapıldıysa, yeni Backend'e kayıt yap
        if (oldBackendUrl != newBackendUrl && !string.IsNullOrEmpty(YourDeviceId) && YourDeviceId != "---" && YourDeviceId != "Bağlanamadı" && YourDeviceId != "Hata")
        {
            System.Diagnostics.Debug.WriteLine("🟡 Backend URL değişti, yeni Backend'e kayıt yapılıyor...");
            _ = Task.Run(async () =>
            {
                try
                {
                    await InitializeAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Yeni Backend'e kayıt hatası: {ex.Message}");
                }
            });
        }
    }

    /// <summary>
    /// Uygulama başlangıcında local Device ID oluşturur (Backend'e kayıt olmaz).
    /// App kayıt olmaz - sadece Agent'lar kayıt olur.
    /// "Allow Access" modu için local Device ID oluşturulur (Backend'e kayıt olmadan).
    /// </summary>
    private async Task InitializeAsync()
    {
        System.Diagnostics.Debug.WriteLine("🟡 InitializeAsync başladı");

        // Agent'ın Device ID'sini önce oku (hızlı işlem - dosya okuma)
        var savedDeviceId = LoadDeviceIdFromConfig();
        System.Diagnostics.Debug.WriteLine($"🟡 Kaydedilmiş Device ID: {savedDeviceId ?? "Yok"}");

        // Eğer Device ID yoksa, makine bazlı ID üret (Agent ve App aynı ID'yi kullanmalı)
        if (string.IsNullOrWhiteSpace(savedDeviceId))
        {
            savedDeviceId = Core.Utils.MachineIdGenerator.GenerateMachineBasedId();
            SaveDeviceIdToConfig(savedDeviceId);
            System.Diagnostics.Debug.WriteLine($"🟡 Makine bazlı Device ID oluşturuldu (Agent ve App aynı ID'yi kullanacak): {savedDeviceId}");
        }

        // ÖNEMLİ: Device ID'yi hemen UI'a göster (Backend kaydı beklenmeden)
        var initDispatcher = System.Windows.Application.Current?.Dispatcher;
        if (initDispatcher != null)
        {
            await initDispatcher.InvokeAsync(() =>
            {
                YourDeviceId = savedDeviceId; // Device ID'yi hemen göster
                Password = "---"; // Password henüz yok
                StatusMessage = "Hazırlanıyor...";
            });
        }
        else
        {
            YourDeviceId = savedDeviceId; // Device ID'yi hemen göster
            Password = "---"; // Password henüz yok
            StatusMessage = "Hazırlanıyor...";
        }

        var backendUrl = (_backendClient as Services.BackendClientService)?.GetBackendUrl() ?? "http://localhost:5000";
        System.Diagnostics.Debug.WriteLine($"🟡 Backend URL: {backendUrl}");

        // App'i Backend'e Agent olarak kaydet (database'de görünmesi için) - ARKA PLANDA
        // Device ID zaten gösterildi, Backend kaydı arka planda yapılabilir
        try
        {
            // Status mesajını güncelle ama Device ID zaten gösterildi
            if (initDispatcher != null)
            {
                await initDispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Backend'e kayıt olunuyor...";
                });
            }
            else
            {
                StatusMessage = "Backend'e kayıt olunuyor...";
            }
            var localIp = Core.Utils.NetworkHelper.GetLocalIpAddress();
            var registrationRequest = new AgentRegistrationRequest
            {
                DeviceId = savedDeviceId,
                MachineName = Environment.MachineName,
                OperatingSystem = Environment.OSVersion.ToString(),
                IpAddress = localIp,
                TcpPort = null // App TCP server çalıştırmaz, TcpPort nullable
            };

            var registrationResponse = await _backendClient.RegisterAsync(registrationRequest);
            
            if (registrationResponse.Success)
            {
                System.Diagnostics.Debug.WriteLine($"✅ App Backend'e kaydedildi. Gönderilen Device ID: {savedDeviceId}, Backend'den gelen Device ID: {registrationResponse.DeviceId}, Password: {registrationResponse.Password}");
                
                // ÖNEMLİ: Her zaman kendi okuduğumuz DeviceId'yi kullan (Backend'den gelen DeviceId'yi değil)
                // Backend, gelen DeviceId'yi kullanır ve aynı DeviceId ile kayıt varsa mevcut kaydı günceller
                // Bu sayede Agent ve App aynı DeviceId ile kayıt olduğunda DB'de tek kayıt olur
                var deviceIdToUse = savedDeviceId; // Kendi okuduğumuz DeviceId'yi kullan
                
                // UI thread'inde güncelle
                var uiDispatcher = System.Windows.Application.Current?.Dispatcher;
                if (uiDispatcher != null)
                {
                    await uiDispatcher.InvokeAsync(() =>
                    {
                        YourDeviceId = deviceIdToUse; // Backend'den gelen DeviceId değil, kendi okuduğumuz DeviceId
                        Password = registrationResponse.Password ?? "---";
                        StatusMessage = "Hazır";
                    });
                }
                else
                {
                    YourDeviceId = deviceIdToUse; // Backend'den gelen DeviceId değil, kendi okuduğumuz DeviceId
                    Password = registrationResponse.Password ?? "---";
                    StatusMessage = "Hazır";
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ App kayıt başarısız: {registrationResponse.ErrorMessage}");
                // Kayıt başarısız olsa bile local Device ID'yi kullan
                var uiDispatcher = System.Windows.Application.Current?.Dispatcher;
                if (uiDispatcher != null)
                {
                    await uiDispatcher.InvokeAsync(() =>
                    {
                        YourDeviceId = savedDeviceId;
                        Password = "---";
                        StatusMessage = "Hazır (kayıt başarısız)";
                    });
                }
                else
                {
                    YourDeviceId = savedDeviceId;
                    Password = "---";
                    StatusMessage = "Hazır (kayıt başarısız)";
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ App kayıt hatası: {ex.Message}");
            // Hata olsa bile local Device ID'yi kullan
            var uiDispatcher = System.Windows.Application.Current?.Dispatcher;
            if (uiDispatcher != null)
            {
                await uiDispatcher.InvokeAsync(() =>
                {
                    YourDeviceId = savedDeviceId;
                    Password = "---";
                    StatusMessage = "Hazır (kayıt hatası)";
                });
            }
            else
            {
                YourDeviceId = savedDeviceId;
                Password = "---";
                StatusMessage = "Hazır (kayıt hatası)";
            }
        }

        // SignalR bağlantısını kur (Device ID ile, ama Backend'e kayıt olmadan)
        try
        {
            if (_signalRClient == null)
            {
                var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<Services.SignalRClientService>();
                _signalRClient = new Services.SignalRClientService(logger);
                _signalRClient.OnSignalingMessageReceived += HandleWebRTCSignaling;
                _signalRClient.OnSignalingErrorReceived += HandleSignalingError;
            }
            
            await _signalRClient.ConnectAsync(backendUrl, savedDeviceId);
            System.Diagnostics.Debug.WriteLine($"✅ SignalR bağlantısı kuruldu (Local Device ID: {savedDeviceId})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ SignalR bağlantısı kurulamadı: {ex.Message}");
            // SignalR olmadan da devam et (TCP fallback)
        }
    }

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        // Boşlukları temizle (formatlanmış ID'ler için: "123 456 789" -> "123456789")
        var input = RemoteDeviceId?.Replace(" ", "").Trim();
        
        if (string.IsNullOrWhiteSpace(input))
        {
            StatusMessage = "Lütfen Remote ID girin";
            System.Diagnostics.Debug.WriteLine("⚠️ RemoteDeviceId boş!");
            return;
        }

        // Device ID formatını parse et: "DeviceID@BackendURL" veya sadece "DeviceID"
        string cleanDeviceId;
        string? backendUrlFromInput = null;
        
        if (input.Contains("@"))
        {
            // Format: "DeviceID@BackendURL"
            var parts = input.Split('@');
            if (parts.Length == 2)
            {
                cleanDeviceId = parts[0].Trim();
                backendUrlFromInput = parts[1].Trim();
                
                // URL formatını düzelt
                if (!backendUrlFromInput.StartsWith("http://") && !backendUrlFromInput.StartsWith("https://"))
                {
                    backendUrlFromInput = "http://" + backendUrlFromInput;
                }
                
                System.Diagnostics.Debug.WriteLine($"🔵 Device ID formatı algılandı: DeviceID={cleanDeviceId}, BackendURL={backendUrlFromInput}");
                
                // Backend URL'i güncelle
                if (!string.IsNullOrWhiteSpace(backendUrlFromInput))
                {
                    var newBackendClient = new Services.BackendClientService(backendUrlFromInput);
                    UpdateBackendClient(newBackendClient);
                    SaveBackendUrlToConfig(backendUrlFromInput);
                    System.Diagnostics.Debug.WriteLine($"✅ Backend URL güncellendi: {backendUrlFromInput}");
                }
            }
            else
            {
                StatusMessage = "Geçersiz format. Örnek: 311819501@192.168.1.100:5000";
                return;
            }
        }
        else
        {
            // Sadece Device ID
            cleanDeviceId = input;
        }
        
        if (string.IsNullOrWhiteSpace(cleanDeviceId))
        {
            StatusMessage = "Lütfen geçerli bir Device ID girin";
            System.Diagnostics.Debug.WriteLine("⚠️ Device ID boş!");
            return;
        }

        // Kendi Device ID'sine bağlanmaya çalışıyorsa uyarı göster
        var yourDeviceIdClean = YourDeviceId?.Replace(" ", "").Trim();
        System.Diagnostics.Debug.WriteLine($"🔵 Device ID karşılaştırması: cleanDeviceId={cleanDeviceId}, yourDeviceIdClean={yourDeviceIdClean}, YourDeviceId={YourDeviceId}");
        
        if (!string.IsNullOrEmpty(yourDeviceIdClean) && 
            !string.IsNullOrEmpty(cleanDeviceId) && 
            cleanDeviceId == yourDeviceIdClean)
        {
            StatusMessage = "Kendi Device ID'nize bağlanamazsınız. Lütfen Agent'ın Device ID'sini girin.";
            System.Diagnostics.Debug.WriteLine($"⚠️ Kendi Device ID'sine bağlanmaya çalışılıyor: cleanDeviceId={cleanDeviceId}, yourDeviceIdClean={yourDeviceIdClean}, YourDeviceId={YourDeviceId}");
            
            // MessageBox ile uyarı göster
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show(
                    "Kendi Device ID'nize bağlanamazsınız.\n\nLütfen Agent'ın Device ID'sini girin.\n\nAgent'ın Device ID'si Agent çıktısında görünür:\n\"Agent başarıyla kaydedildi. Device ID: XXXXXXX\"",
                    "Uyarı",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            });
            return;
        }

        System.Diagnostics.Debug.WriteLine($"🔵 ConnectAsync başlatılıyor. Device ID: {cleanDeviceId}");

        try
        {
            StatusMessage = "Backend aranıyor...";
            IsConnected = false;
            
            // UI'ı güncelle
            ConnectCommand.NotifyCanExecuteChanged();

            // Önce Agent'ın hangi Backend'de olduğunu bul (AnyDesk benzeri)
            StatusMessage = "Agent aranıyor...";
            
            var currentBackendUrl = (_backendClient as Services.BackendClientService)?.GetBackendUrl() ?? "http://localhost:5000";
            System.Diagnostics.Debug.WriteLine($"🔵 Mevcut Backend URL: {currentBackendUrl}");
            System.Diagnostics.Debug.WriteLine($"🔵 Agent keşfi başlatılıyor: DeviceId={cleanDeviceId}");
            
            // Backend URL listesini al (appsettings.json'dan + varsayılanlar)
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            var backendUrls = Core.Utils.BackendDiscoveryService.GetBackendUrlsFromConfig(currentBackendUrl, configPath);
            System.Diagnostics.Debug.WriteLine($"🔵 Denenecek Backend URL'leri ({backendUrls.Count} adet): {string.Join(", ", backendUrls)}");
            
            // Device ID ile Agent'ın hangi Backend'de olduğunu bul
            var agentBackendUrl = await Core.Utils.BackendDiscoveryService.DiscoverBackendForAgentAsync(
                cleanDeviceId, 
                backendUrls);
            
            if (agentBackendUrl != null && agentBackendUrl != currentBackendUrl)
            {
                // Agent farklı bir Backend'de bulundu, Backend URL'ini güncelle
                System.Diagnostics.Debug.WriteLine($"✅ Agent farklı Backend'de bulundu: {agentBackendUrl}");
                StatusMessage = "Backend'e bağlanılıyor...";
                
                var newBackendClient = new Services.BackendClientService(agentBackendUrl);
                UpdateBackendClient(newBackendClient);
                SaveBackendUrlToConfig(agentBackendUrl);
                
                _backendClient = newBackendClient;
                currentBackendUrl = agentBackendUrl;
            }
            else if (agentBackendUrl == null)
            {
                // Agent hiçbir Backend'de bulunamadı
                System.Diagnostics.Debug.WriteLine($"⚠️ Agent hiçbir Backend'de bulunamadı, mevcut Backend ile denenecek");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"✅ Agent mevcut Backend'de bulundu: {currentBackendUrl}");
            }

            // SignalR client'ı başlat (eğer henüz başlatılmadıysa)
            // Not: InitializeAsync'den sonra SignalR bağlantısı kurulacak (Device ID ile)
            // Burada sadece client'ı oluştur, bağlantıyı InitializeAsync'den sonra kur
            if (_signalRClient == null)
            {
                try
                {
                    // Basit logger oluştur (Microsoft.Extensions.Logging.Abstractions kullanarak)
                    var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<Services.SignalRClientService>();
                    _signalRClient = new Services.SignalRClientService(logger);
                    _signalRClient.OnSignalingMessageReceived += HandleWebRTCSignaling;
                    _signalRClient.OnSignalingErrorReceived += HandleSignalingError;
                    System.Diagnostics.Debug.WriteLine("✅ SignalR client oluşturuldu (bağlantı InitializeAsync'den sonra kurulacak)");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ SignalR client oluşturulamadı: {ex.Message}");
                    // SignalR olmadan da devam et (TCP fallback)
                }
            }

            // WebRTC client'ı başlat (eğer henüz başlatılmadıysa)
            if (_webrtcClient == null)
            {
                try
                {
                    // Basit logger oluştur
                    var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<Services.WebRTCClientService>();
                    _webrtcClient = new Services.WebRTCClientService(logger);
                    _webrtcClient.Initialize();
                    _webrtcClient.OnIceCandidate += HandleIceCandidate;
                    _webrtcClient.OnConnectionStateChange += HandleWebRTCConnectionState;
                    System.Diagnostics.Debug.WriteLine("✅ WebRTC client başlatıldı");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ WebRTC client başlatılamadı: {ex.Message}");
                    // WebRTC olmadan da devam et (TCP fallback)
                }
            }

            // Backend'e bağlantı isteği gönder
            var connectionRequest = new ConnectionRequest
            {
                TargetDeviceId = cleanDeviceId!,
                RequesterId = YourDeviceId,
                RequesterName = Environment.MachineName,
                QualityLevel = QualityLevel.Medium
            };
            
            System.Diagnostics.Debug.WriteLine($"🔵 Backend'e bağlantı isteği gönderiliyor: {cleanDeviceId}");
            System.Diagnostics.Debug.WriteLine($"🔵 Backend URL: {currentBackendUrl}");
            System.Diagnostics.Debug.WriteLine($"🔵 Kendi Device ID: {YourDeviceId}");
            System.Diagnostics.Debug.WriteLine($"🔵 Bağlanılacak Device ID: {cleanDeviceId}");

            var response = await _backendClient.RequestConnectionAsync(connectionRequest);

            if (!response.Success)
            {
                // Eğer "Agent bulunamadı" hatası ise, Backend URL'i yanlış olabilir
                // Kullanıcıya Backend URL'i girmesi için dialog göster
                if (response.ErrorMessage?.Contains("Agent bulunamadı") == true || 
                    response.ErrorMessage?.Contains("not online") == true ||
                    response.ErrorMessage?.Contains("connection") == true ||
                    response.ErrorMessage?.Contains("refused") == true)
                {
                    // Backend URL'i bulunamadı veya yanlış, kullanıcıya ayarlar penceresi göster
                    var backendUrl = await ShowBackendSettingsDialogAsync(currentBackendUrl);
                    
                    if (backendUrl != null)
                    {
                        // Yeni Backend URL ile tekrar dene
                        var newBackendClient = new Services.BackendClientService(backendUrl);
                        UpdateBackendClient(newBackendClient);
                        SaveBackendUrlToConfig(backendUrl);
                        
                        // Tekrar bağlantı isteği gönder
                        response = await newBackendClient.RequestConnectionAsync(connectionRequest);
                        
                        if (!response.Success)
                        {
                            StatusMessage = $"Bağlantı hatası: {response.ErrorMessage}";
                            System.Diagnostics.Debug.WriteLine($"❌ Backend bağlantı hatası (yeni URL ile): {response.ErrorMessage}");
                            
                            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                            {
                                System.Windows.MessageBox.Show(
                                    $"Backend'e bağlanılamadı:\n\n{response.ErrorMessage}\n\n" +
                                    $"Backend URL: {backendUrl}\n" +
                                    $"Device ID: {cleanDeviceId}\n\n" +
                                    $"Lütfen:\n" +
                                    $"1. Backend'in çalıştığından emin olun\n" +
                                    $"2. Doğru Device ID'yi girdiğinizden emin olun\n" +
                                    $"3. Device ID formatı: 311819501 veya 311819501@192.168.1.100:5000",
                                    "Bağlantı Hatası",
                                    System.Windows.MessageBoxButton.OK,
                                    System.Windows.MessageBoxImage.Error);
                            });
                            return;
                        }
                    }
                    else
                    {
                        // Kullanıcı iptal etti
                        StatusMessage = "Bağlantı iptal edildi";
                        return;
                    }
                }
                else
                {
                    StatusMessage = $"Bağlantı hatası: {response.ErrorMessage}";
                    System.Diagnostics.Debug.WriteLine($"❌ Backend bağlantı hatası: {response.ErrorMessage}");
                    
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show(
                            $"Bağlantı hatası:\n\n{response.ErrorMessage}\n\n" +
                            $"Backend URL: {currentBackendUrl}\n" +
                            $"Device ID: {cleanDeviceId}",
                            "Bağlantı Hatası",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                    });
                    return;
                }
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
                    System.Diagnostics.Debug.WriteLine($"✅ AgentEndpoint bulundu: {host}:{port}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ AgentEndpoint formatı hatalı: {response.AgentEndpoint}");
                }
            }
            else
            {
                // AgentEndpoint yoksa localhost kullan (geriye dönük uyumluluk)
                System.Diagnostics.Debug.WriteLine("⚠️ AgentEndpoint bulunamadı, localhost kullanılıyor");
                StatusMessage = "Agent IP adresi bulunamadı";
                
                // MessageBox ile uyarı göster
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(
                        "Agent IP adresi bulunamadı.\n\nBackend'den AgentEndpoint alınamadı. Lütfen Backend loglarını kontrol edin.",
                        "Uyarı",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                });
                return;
            }

            // WebRTC bağlantısı kur (SignalR üzerinden)
            if (_signalRClient != null && _webrtcClient != null)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("🔵 WebRTC bağlantısı kuruluyor (SignalR üzerinden)...");
                    
                    // Hedef Device ID'yi set et
                    _signalRClient.SetTargetDeviceId(cleanDeviceId!);
                    
                    // SDP offer oluştur
                    var offerSdp = await _webrtcClient.CreateOfferAsync();
                    
                    // Offer'ı SignalR üzerinden gönder
                    var offerMessage = new WebRTCSignalingMessage
                    {
                        Type = "offer",
                        TargetDeviceId = cleanDeviceId!,
                        SenderDeviceId = YourDeviceId ?? string.Empty,
                        ConnectionId = response.ConnectionId ?? Guid.NewGuid().ToString(),
                        Sdp = offerSdp
                    };
                    
                    await _signalRClient.SendWebRTCSignalingAsync(offerMessage);
                    System.Diagnostics.Debug.WriteLine("✅ WebRTC offer gönderildi");
                    StatusMessage = "WebRTC bağlantısı kuruluyor...";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ WebRTC bağlantısı kurulamadı: {ex.Message}");
                    // WebRTC başarısız olursa TCP fallback kullan
                }
            }

            System.Diagnostics.Debug.WriteLine($"🔵 Yeni bağlantı penceresi açılıyor: {host}:{port}");
            
            // Yeni bağlantı penceresini aç (TCP fallback için)
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Yeni TCP client oluştur (her bağlantı için ayrı)
                    var tcpClient = new Services.TcpStreamClient(null);
                    var backendClient = _backendClient; // Mevcut backend client'ı kullan
                    
                    // WebRTC client'ı oluştur (eğer yoksa)
                    if (_webrtcClient == null)
                    {
                        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<Services.WebRTCClientService>();
                        _webrtcClient = new Services.WebRTCClientService(logger);
                        _webrtcClient.Initialize();
                    }
                    
                    // ViewModel oluştur
                    var connectionViewModel = new RemoteConnectionViewModel(
                        backendClient,
                        tcpClient,
                        cleanDeviceId!,
                        response.AgentEndpoint ?? $"{host}:{port}",
                        _webrtcClient);
                    
                    // Yeni pencereyi aç
                    var connectionWindow = new Views.RemoteConnectionWindow(connectionViewModel);
                    connectionWindow.Show();
                    
                    // Bağlantıyı başlat (async)
                    _ = Task.Run(async () => await connectionViewModel.ConnectAsync());
                    
                    StatusMessage = "Bağlantı penceresi açıldı";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Pencere açma hatası: {ex.Message}");
                    System.Windows.MessageBox.Show(
                        $"Bağlantı penceresi açılamadı:\n\n{ex.Message}",
                        "Hata",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ ConnectAsync exception: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
            StatusMessage = $"Hata: {ex.Message}";
        }
        finally
        {
            // UI'ı güncelle
            ConnectCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanConnect()
    {
        return !IsConnected && !string.IsNullOrWhiteSpace(RemoteDeviceId);
    }

    [RelayCommand]
    private void Disconnect()
    {
        // Artık bağlantı yeni pencerede yönetiliyor, bu metod kullanılmıyor
        // Ama interface uyumluluğu için bırakıyoruz
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


    /// <summary>
    /// Device ID'yi önce ortak deviceid.json'dan (AppData), sonra local dosyalardan okur.
    /// Ortak dosya Agent ve App tarafından paylaşılır (aynı Device ID).
    /// </summary>
    private string? LoadDeviceIdFromConfig()
    {
        try
        {
            // 1. Önce ortak deviceid.json'dan oku (AppData - Agent ve App aynı dosyayı kullanır)
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var softielRemotePath = Path.Combine(appDataPath, "SoftielRemote");
            var deviceIdPath = Path.Combine(softielRemotePath, "deviceid.json");
            
            if (File.Exists(deviceIdPath))
            {
                var json = File.ReadAllText(deviceIdPath);
                var deviceIdConfig = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json);
                if (deviceIdConfig != null && deviceIdConfig.ContainsKey("DeviceId"))
                {
                    var deviceId = deviceIdConfig["DeviceId"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(deviceId))
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Device ID ortak deviceid.json'dan okundu: {deviceId}, Path={deviceIdPath}");
                        return deviceId;
                    }
                }
            }
            
            // 2. Ortak dosya yoksa, local deviceid.json'dan oku (backward compatibility)
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var localDeviceIdPath = Path.Combine(baseDirectory, "deviceid.json");
            if (File.Exists(localDeviceIdPath))
            {
                var json = File.ReadAllText(localDeviceIdPath);
                var deviceIdConfig = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json);
                if (deviceIdConfig != null && deviceIdConfig.ContainsKey("DeviceId"))
                {
                    var deviceId = deviceIdConfig["DeviceId"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(deviceId))
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Device ID local deviceid.json'dan okundu: {deviceId}");
                        // Ortak dosyaya da kaydet (migration)
                        SaveDeviceIdToConfig(deviceId);
                        return deviceId;
                    }
                }
            }
            
            // 3. deviceid.json yoksa appsettings.json'dan oku
            var configPath = Path.Combine(baseDirectory, "appsettings.json");
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var config = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json);
                
                if (config != null && config.ContainsKey("DeviceId"))
                {
                    var deviceId = config["DeviceId"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(deviceId))
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Device ID appsettings.json'dan okundu: {deviceId}");
                        // Ortak dosyaya da kaydet (migration)
                        SaveDeviceIdToConfig(deviceId);
                        return deviceId;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Device ID okunamadı: {ex.Message}");
        }
        
        return null;
    }

    /// <summary>
    /// Backend URL ayarları dialog'unu gösterir.
    /// </summary>
    private async Task<string?> ShowBackendSettingsDialogAsync(string? currentBackendUrl)
    {
        string? result = null;
        
        var app = System.Windows.Application.Current;
        if (app?.Dispatcher != null)
        {
            await app.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new Views.BackendSettingsDialog(currentBackendUrl);
                var dialogResult = dialog.ShowDialog();
                
                if (dialogResult == true && !dialog.IsCancelled && !string.IsNullOrWhiteSpace(dialog.BackendUrl))
                {
                    result = dialog.BackendUrl;
                }
            });
        }
        
        return result;
    }

    /// <summary>
    /// Backend URL'ini appsettings.json dosyasına kaydeder.
    /// </summary>
    private void SaveBackendUrlToConfig(string url)
    {
        try
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            var config = new System.Collections.Generic.Dictionary<string, object>();
            
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                config = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json) ?? config;
            }
            
            config["BackendBaseUrl"] = url;
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(configPath, System.Text.Json.JsonSerializer.Serialize(config, options));
            
            System.Diagnostics.Debug.WriteLine($"💾 Backend URL appsettings.json'a kaydedildi: {url}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Backend URL kaydedilemedi: {ex.Message}");
        }
    }

    /// <summary>
    /// Device ID'yi hem appsettings.json hem de ortak deviceid.json dosyasına kaydeder.
    /// Ortak dosya AppData'da saklanır (Agent ve App aynı dosyayı kullanır).
    /// </summary>
    private void SaveDeviceIdToConfig(string deviceId)
    {
        try
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            
            // 1. appsettings.json'a kaydet (local)
            var configPath = Path.Combine(baseDirectory, "appsettings.json");
            
            // Mevcut config'i oku
            System.Collections.Generic.Dictionary<string, object>? config = null;
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                config = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json);
            }
            
            // Config yoksa yeni oluştur
            if (config == null)
            {
                config = new System.Collections.Generic.Dictionary<string, object>();
            }
            
            // DeviceId'yi güncelle
            config["DeviceId"] = deviceId;
            
            // JSON'a çevir ve kaydet
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var newJson = System.Text.Json.JsonSerializer.Serialize(config, options);
            File.WriteAllText(configPath, newJson);
            
            System.Diagnostics.Debug.WriteLine($"✅ Device ID appsettings.json'a kaydedildi: {deviceId}");
            
            // 2. Ortak deviceid.json'a kaydet (AppData - Agent ve App aynı dosyayı kullanır)
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var softielRemotePath = Path.Combine(appDataPath, "SoftielRemote");
            Directory.CreateDirectory(softielRemotePath); // Klasör yoksa oluştur
            
            var deviceIdPath = Path.Combine(softielRemotePath, "deviceid.json");
            var deviceIdConfig = new System.Collections.Generic.Dictionary<string, object>
            {
                ["DeviceId"] = deviceId,
                ["MachineName"] = Environment.MachineName,
                ["SavedAt"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
            var deviceIdJson = System.Text.Json.JsonSerializer.Serialize(deviceIdConfig, options);
            File.WriteAllText(deviceIdPath, deviceIdJson);
            
            System.Diagnostics.Debug.WriteLine($"✅ Device ID ortak deviceid.json'a kaydedildi: {deviceId}, Path={deviceIdPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Device ID kaydedilemedi: {ex.Message}");
        }
    }

    /// <summary>
    /// WebRTC signaling mesajını işler.
    /// </summary>
    private void HandleWebRTCSignaling(WebRTCSignalingMessage message)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔵 WebRTC signaling mesajı alındı: Type={message.Type}");

            switch (message.Type.ToLower())
            {
                case "answer":
                    // SDP answer alındı
                    if (!string.IsNullOrEmpty(message.Sdp) && _webrtcClient != null)
                    {
                        _webrtcClient.SetAnswer(message.Sdp);
                        System.Diagnostics.Debug.WriteLine("✅ WebRTC answer ayarlandı");
                    }
                    break;

                case "ice-candidate":
                    // ICE candidate ekle
                    if (message.IceCandidate != null && _webrtcClient != null)
                    {
                        _webrtcClient.AddIceCandidate(message.IceCandidate);
                    }
                    break;

                default:
                    System.Diagnostics.Debug.WriteLine($"⚠️ Bilinmeyen signaling mesaj tipi: {message.Type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ WebRTC signaling mesajı işlenemedi: {ex.Message}");
        }
    }

    /// <summary>
    /// ICE candidate'ı Backend'e gönderir.
    /// </summary>
    private async void HandleIceCandidate(IceCandidateDto candidate)
    {
        try
        {
            if (_signalRClient != null && !string.IsNullOrEmpty(RemoteDeviceId))
            {
                var signalingMessage = new WebRTCSignalingMessage
                {
                    Type = "ice-candidate",
                    TargetDeviceId = RemoteDeviceId,
                    SenderDeviceId = YourDeviceId,
                    ConnectionId = string.Empty, // Connection context'ten alınacak
                    IceCandidate = candidate
                };

                await _signalRClient.SendWebRTCSignalingAsync(signalingMessage);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ ICE candidate gönderilemedi: {ex.Message}");
        }
    }

    /// <summary>
    /// WebRTC connection state değişikliğini işler.
    /// </summary>
    private void HandleWebRTCConnectionState(SIPSorcery.Net.RTCPeerConnectionState state)
    {
        System.Diagnostics.Debug.WriteLine($"🔵 WebRTC connection state: {state}");
        
        if (state == SIPSorcery.Net.RTCPeerConnectionState.connected)
        {
            StatusMessage = "WebRTC bağlantısı kuruldu";
            IsConnected = true;
        }
        else if (state == SIPSorcery.Net.RTCPeerConnectionState.disconnected ||
                 state == SIPSorcery.Net.RTCPeerConnectionState.failed)
        {
            StatusMessage = "WebRTC bağlantısı kesildi";
            IsConnected = false;
        }
    }

    /// <summary>
    /// Signaling hatasını işler.
    /// </summary>
    private void HandleSignalingError(string error)
    {
        System.Diagnostics.Debug.WriteLine($"❌ Signaling hatası: {error}");
        StatusMessage = $"Signaling hatası: {error}";
    }
}

