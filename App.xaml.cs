using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using System.Windows;

namespace AnikiVisualPackCreator;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            Loc.Initialize();

            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"{Loc.Get("StartupCouldNotStart")}\n\n{exception.Message}",
                Loc.Get("StartupErrorTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(1);
        }
    }
}
