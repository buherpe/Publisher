using System.Windows;
using System.Windows.Threading;
using Tools;

namespace Publisher
{
    public partial class App
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public App()
        {
            Helper.MagickInitMethod($"http://buherpet.tk:9999/updates/{Helper.AppName}");

            Exit += App_Exit;
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception);
            MessageBox.Show($"{e.Exception.Message}\r\n\r\n\r\n{e.Exception}", $"Error :: {Helper.AppNameWithVersion}", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            Log.Bye(e.ApplicationExitCode);
        }
    }
}
