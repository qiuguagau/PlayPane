using System;
using System.Linq;
using System.Windows;

namespace PlayPane
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            if (e.Args.Any(arg => string.Equals(arg, "--smoke-test", StringComparison.OrdinalIgnoreCase)))
            {
                new MainWindow();
                Shutdown(0);
                return;
            }

            MainWindow = new MainWindow();
            MainWindow.Show();
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "PlayPane", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
