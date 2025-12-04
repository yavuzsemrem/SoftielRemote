using System.Windows;
using System.Windows.Input;
using SoftielRemote.App.ViewModels;

namespace SoftielRemote.App.Views;

/// <summary>
/// RemoteConnectionWindow.xaml için etkileşim mantığı
/// </summary>
public partial class RemoteConnectionWindow : Window
{
    public RemoteConnectionViewModel ViewModel { get; }

    public RemoteConnectionWindow(RemoteConnectionViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
        
        // Window kapatıldığında ViewModel'i temizle
        Closed += (s, e) => ViewModel?.Dispose();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.Disconnect();
        Close();
    }
}

