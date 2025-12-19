using System.Windows;

namespace SoftielRemote.Agent.Views;

/// <summary>
/// ConnectionRequestDialog.xaml için etkileşim mantığı
/// </summary>
public partial class ConnectionRequestDialog : Window
{
    private bool? _result;
    public bool? Result 
    { 
        get => _result; 
        private set 
        { 
            _result = value;
            OnResultChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public bool IsAccepted { get; private set; }
    public event EventHandler? OnDisconnectRequested;
    public event EventHandler? OnResultChanged;

    public ConnectionRequestDialog(string requesterName, string requesterIp, string requesterDeviceId)
    {
        InitializeComponent();
        
        RequesterNameText.Text = requesterName;
        RequesterIpText.Text = requesterIp;
        RequesterDeviceIdText.Text = requesterDeviceId;
        
        Result = null;
        IsAccepted = false;
        
        // Pencereyi görünür ve etkileşilebilir yap
        IsHitTestVisible = true;
        Focusable = true;
        ShowActivated = true;
        Visibility = Visibility.Visible;
        Opacity = 1.0;
        
        // Loaded event'inde pencereyi zorla öne getir
        Loaded += (s, e) =>
        {
            try
            {
                Console.WriteLine($"🔨 ConnectionRequestDialog Loaded event tetiklendi");
                Console.WriteLine($"   IsVisible={IsVisible}, IsLoaded={IsLoaded}, IsActive={IsActive}");
                
                Activate();
                Focus();
                BringIntoView();
                Topmost = true;
                Visibility = Visibility.Visible;
                Opacity = 1.0;
                
                Console.WriteLine($"✅ ConnectionRequestDialog Loaded: IsVisible={IsVisible}, IsLoaded={IsLoaded}, IsActive={IsActive}");
                System.Diagnostics.Debug.WriteLine($"✅ ConnectionRequestDialog Loaded: IsVisible={IsVisible}, IsLoaded={IsLoaded}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ConnectionRequestDialog Loaded hatası: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"⚠️ ConnectionRequestDialog Loaded hatası: {ex.Message}");
            }
        };
    }

    private void AcceptButton_Click(object sender, RoutedEventArgs e)
    {
        Result = true;
        IsAccepted = true;
        
        // Dialog'u kapatma, sadece durumu değiştir
        ShowConnectedState();
    }

    private void RejectButton_Click(object sender, RoutedEventArgs e)
    {
        Result = false;
        IsAccepted = false;
        DialogResult = false;
        Close();
    }

    private void DisconnectButton_Click(object sender, RoutedEventArgs e)
    {
        // Bağlantıyı kesme isteği
        OnDisconnectRequested?.Invoke(this, EventArgs.Empty);
        
        // Dialog'u kapat
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Bağlantı kabul edildiğinde dialog'u bağlantı kontrol moduna geçirir.
    /// </summary>
    public void ShowConnectedState()
    {
        // Request state'i gizle
        RequestStatePanel.Visibility = Visibility.Collapsed;
        RequestButtonsPanel.Visibility = Visibility.Collapsed;
        
        // Connected state'i göster
        ConnectedStatePanel.Visibility = Visibility.Visible;
        ConnectedButtonsPanel.Visibility = Visibility.Visible;
        
        // Header'ı güncelle
        HeaderText.Text = "Bağlantı Aktif";
        
        // Dialog'u kapatma (açık kalacak)
        DialogResult = null;
    }

    /// <summary>
    /// Dialog'u kapatır (bağlantı kesildiğinde).
    /// </summary>
    public void CloseDialog()
    {
        DialogResult = false;
        Close();
    }
}

