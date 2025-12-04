using System.Windows;

namespace SoftielRemote.App.Views;

/// <summary>
/// BackendSettingsDialog.xaml için etkileşim mantığı
/// </summary>
public partial class BackendSettingsDialog : Window
{
    public string? BackendUrl { get; private set; }
    public bool IsCancelled { get; private set; } = true;

    public BackendSettingsDialog(string? currentBackendUrl = null)
    {
        InitializeComponent();
        
        if (!string.IsNullOrWhiteSpace(currentBackendUrl))
        {
            BackendUrlTextBox.Text = currentBackendUrl;
        }
        
        BackendUrlTextBox.Focus();
        BackendUrlTextBox.SelectAll();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var url = BackendUrlTextBox.Text?.Trim();
        
        if (string.IsNullOrWhiteSpace(url))
        {
            MessageBox.Show("Lütfen geçerli bir Backend URL'i girin.", 
                "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // URL formatını kontrol et
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = "http://" + url;
        }
        
        // Basit URL validasyonu
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            MessageBox.Show("Geçersiz URL formatı. Örnek: http://192.168.1.100:5000", 
                "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        BackendUrl = url;
        IsCancelled = false;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsCancelled = true;
        DialogResult = false;
        Close();
    }
}




