using Hardcodet.Wpf.TaskbarNotification;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Media;

namespace ClientCentralino_vs2
{
    public partial class App : Application
    {
        private TaskbarIcon trayIcon;

        private void TrayIcon_DoubleClick(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow;

            if (mainWindow != null)
            {
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.ShowInTaskbar = true;
                mainWindow.Activate();
            }
        }

        private void Menu_Open_Click(object sender, RoutedEventArgs e)
        {
            TrayIcon_DoubleClick(sender, e);
        }

        private void Menu_Exit_Click(object sender, RoutedEventArgs e)
        {
            trayIcon?.Dispose();
            Application.Current.Shutdown();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Recupera l'icona dalla risorsa con la chiave "MyTrayIcon"
            trayIcon = (TaskbarIcon)FindResource("MyTrayIcon");

            if (trayIcon != null)
            {
                trayIcon.TrayLeftMouseDown += TrayIcon_DoubleClick;
            }

            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.StateChanged += MainWindow_StateChanged;
            }
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            var window = sender as Window;
            if (window != null && window.WindowState == WindowState.Minimized)
            {
                window.Hide();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            trayIcon?.Dispose();
            base.OnExit(e);
        }
    }
}