using System.Windows;

namespace SoftielRemote.App.Views;

/// <summary>
/// ConnectionRequestDialog.xaml için etkileşim mantığı
/// </summary>
public partial class ConnectionRequestDialog : Window
{
    public bool? Result { get; private set; }

    public ConnectionRequestDialog(string requesterName, string requesterIp, string requesterDeviceId)
    {
        InitializeComponent();
        
        // Data binding için
        DataContext = new
        {
            RequesterName = requesterName,
            RequesterIp = requesterIp,
            RequesterDeviceId = requesterDeviceId
        };
    }

    private void AcceptButton_Click(object sender, RoutedEventArgs e)
    {
        Result = true;
        DialogResult = true;
        Close();
    }

    private void RejectButton_Click(object sender, RoutedEventArgs e)
    {
        Result = false;
        DialogResult = false;
        Close();
    }
}

