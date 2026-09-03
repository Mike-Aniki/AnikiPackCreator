using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using System.Windows;
using System.Windows.Threading;

namespace AnikiVisualPackCreator;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        base.OnStartup(e);

        try
        {
            Loc.Initialize();

            var window = new PackCreatorWindow();
            MainWindow = window;
            window.Show();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"{Loc.Get("StartupCouldNotStart")}\n\n{exception}",
                Loc.Get("StartupErrorTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(1);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            MessageBox.Show(
                $"Aniki Pack Creator encountered an error.\n\n{e.Exception}",
                "Aniki Pack Creator",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
        }

        e.Handled = true;
    }
}
