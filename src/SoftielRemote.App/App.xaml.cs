using System;
using System.Windows;

namespace SoftielRemote.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Unhandled exception handling
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Bir hata oluştu: {e.Exception.Message}\n\nDetay: {e.Exception}", 
            "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        MessageBox.Show($"Kritik hata: {exception?.Message}\n\nDetay: {exception}", 
            "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

