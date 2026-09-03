using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;

namespace AnikiVisualPackCreator;

public partial class AboutWindow : UserControl
{
    private const string DiscordUrl = "https://discord.gg/BrtABqe";
    private const string KofiUrl = "https://ko-fi.com/mikeaniki";
    private const string CreatorGitHubUrl = "https://github.com/Mike-Aniki/AnikiPackCreator";
    private const string ThemeGitHubUrl = "https://github.com/Mike-Aniki/Aniki-ReMake";

    public AboutWindow()
    {
        InitializeComponent();
        var version = typeof(AboutWindow).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
        VersionTextBlock.Text = Loc.Format("AboutVersion", version);
    }

    private void StartCreatingClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is PackCreatorWindow creatorWindow)
        {
            creatorWindow.NavigateToVisualPack();
        }
    }

    private void DiscordClick(object sender, RoutedEventArgs e) => OpenUrl(DiscordUrl);
    private void KofiClick(object sender, RoutedEventArgs e) => OpenUrl(KofiUrl);
    private void CreatorGitHubClick(object sender, RoutedEventArgs e) => OpenUrl(CreatorGitHubUrl);
    private void ThemeGitHubClick(object sender, RoutedEventArgs e) => OpenUrl(ThemeGitHubUrl);

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                Window.GetWindow(this),
                Loc.Get("AboutLinkOpenError") + "\n\n" + exception.Message,
                "Aniki Pack Creator",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
