using AnikiVisualPackCreator.Models;
using AnikiVisualPackCreator.Services;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using Microsoft.Win32;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml.Linq;

namespace AnikiVisualPackCreator;

public partial class ColorPackWindow : UserControl, INotifyPropertyChanged
{
    private string badges = "#21242A";
    private string activeButtons = "#21242A";
    private string outgoingMessages = "#21242A";
    private Dictionary<string, string> expertOverrides = new();
    private string? previousAutomaticXaml;
    private Action? undoPalette;
    private HashSet<string> lastSimpleChanges = new();
    private string primaryAccent = ColorPackPalette.DefaultPrimaryAccent;
    private string secondaryAccent = ColorPackPalette.DefaultSecondaryAccent;
    private string focusColor = ColorPackPalette.DefaultFocus;
    private string actionButtonsColor = ColorPackPalette.DefaultActionButtons;
    private string progressColor = ColorPackPalette.DefaultProgress;

    private string backgroundColor = ColorPackPalette.DefaultBackground;
    private string barsColor = ColorPackPalette.DefaultBars;
    private string menusColor = ColorPackPalette.DefaultMenus;
    private string cinematicOverlayColor = ColorPackPalette.DefaultCinematicOverlay;
    private string menuHeaderColor = ColorPackPalette.DefaultMenuHeader;
    private string cardsColor = ColorPackPalette.DefaultCards;
    private string borderColor = ColorPackPalette.DefaultBorder;
    private string notificationsColor = ColorPackPalette.DefaultNotifications;
    private string primaryText = ColorPackPalette.DefaultPrimaryText;
    private string secondaryText = ColorPackPalette.DefaultSecondaryText;
    private string highlightText = ColorPackPalette.DefaultHighlightText;
    private string statusText = Loc.Get("ColorStatusReady");
    private string? currentProjectPath;
    private string currentPackId = string.Empty;
    private bool suppressDirtyState = true;
    private bool isDirty;
    private string selectedPreviewFamilyKey = "PrimaryAccent";

    private Brush previewBackgroundBrush = Brushes.Black;
    private Brush previewSurfaceBrush = Brushes.Black;
    private Brush previewTopBarBrush = Brushes.Black;
    private Brush previewBottomBarBrush = Brushes.Black;
    private Brush previewFocusBrush = Brushes.Gold;
    private Brush previewPlayBrush = Brushes.Black;
    private Brush previewTextBrush = Brushes.White;
    private Brush previewSecondaryTextBrush = Brushes.LightGray;
    private Brush previewHighlightTextBrush = Brushes.Gold;
    private Brush previewBorderBrush = Brushes.Gray;
    private Brush previewAccentBrush = Brushes.Gold;
    private Brush previewProgressBackgroundBrush = Brushes.Black;
    private Brush previewNotificationBrush = Brushes.Black;
    private Brush previewNotificationBorderBrush = Brushes.Gold;

    public ColorPackWindow()
    {
        InitializeComponent();
        DataContext = this;
        RefreshPreview();
        SelectPreviewFamily("PrimaryAccent", switchScene: false);
        suppressDirtyState = false;
        isDirty = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Badges
    {
        get => badges;
        set => SetPaletteValue(ref badges, value, nameof(Badges), nameof(BadgesBrush));
    }

    public string ActiveButtons
    {
        get => activeButtons;
        set => SetPaletteValue(ref activeButtons, value, nameof(ActiveButtons), nameof(ActiveButtonsBrush));
    }

    public string OutgoingMessages
    {
        get => outgoingMessages;
        set => SetPaletteValue(ref outgoingMessages, value, nameof(OutgoingMessages), nameof(OutgoingMessagesBrush));
    }

    public string PrimaryAccent
    {
        get => primaryAccent;
        set => SetPaletteValue(ref primaryAccent, value, nameof(PrimaryAccent), nameof(PrimaryAccentBrush));
    }

    public string SecondaryAccent
    {
        get => secondaryAccent;
        set => SetPaletteValue(ref secondaryAccent, value, nameof(SecondaryAccent), nameof(SecondaryAccentBrush));
    }

    public string FocusColor
    {
        get => focusColor;
        set => SetPaletteValue(ref focusColor, value, nameof(FocusColor), nameof(FocusColorBrush));
    }

    public string ActionButtonsColor
    {
        get => actionButtonsColor;
        set => SetPaletteValue(ref actionButtonsColor, value, nameof(ActionButtonsColor), nameof(ActionButtonsColorBrush));
    }

    public string ProgressColor
    {
        get => progressColor;
        set => SetPaletteValue(ref progressColor, value, nameof(ProgressColor), nameof(ProgressColorBrush));
    }

    public string BackgroundColor
    {
        get => backgroundColor;
        set => SetPaletteValue(ref backgroundColor, value, nameof(BackgroundColor), nameof(BackgroundBrush));
    }

    public string BarsColor
    {
        get => barsColor;
        set => SetPaletteValue(ref barsColor, value, nameof(BarsColor), nameof(BarsColorBrush));
    }

    public string MenusColor
    {
        get => menusColor;
        set => SetPaletteValue(ref menusColor, value, nameof(MenusColor), nameof(MenusColorBrush));
    }

    public string CinematicOverlayColor
    {
        get => cinematicOverlayColor;
        set => SetPaletteValue(ref cinematicOverlayColor, value, nameof(CinematicOverlayColor), nameof(CinematicOverlayColorBrush));
    }

    public string MenuHeaderColor
    {
        get => menuHeaderColor;
        set => SetPaletteValue(ref menuHeaderColor, value, nameof(MenuHeaderColor), nameof(MenuHeaderColorBrush));
    }

    public string CardsColor
    {
        get => cardsColor;
        set => SetPaletteValue(ref cardsColor, value, nameof(CardsColor), nameof(CardsColorBrush));
    }

    public string BorderColor
    {
        get => borderColor;
        set => SetPaletteValue(ref borderColor, value, nameof(BorderColor), nameof(BorderColorBrush));
    }

    public string NotificationsColor
    {
        get => notificationsColor;
        set => SetPaletteValue(ref notificationsColor, value, nameof(NotificationsColor), nameof(NotificationsColorBrush));
    }

    public string PrimaryText
    {
        get => primaryText;
        set => SetPaletteValue(ref primaryText, value, nameof(PrimaryText), nameof(PrimaryTextBrush));
    }

    public string SecondaryText
    {
        get => secondaryText;
        set => SetPaletteValue(ref secondaryText, value, nameof(SecondaryText), nameof(SecondaryTextBrush));
    }

    public string HighlightText
    {
        get => highlightText;
        set => SetPaletteValue(ref highlightText, value, nameof(HighlightText), nameof(HighlightTextBrush));
    }

    public Brush BadgesBrush => BrushFromMaster(Badges, "#21242A");
    public Brush ActiveButtonsBrush => BrushFromMaster(ActiveButtons, "#21242A");
    public Brush OutgoingMessagesBrush => BrushFromMaster(OutgoingMessages, "#21242A");
    public Brush PrimaryAccentBrush => BrushFromMaster(PrimaryAccent, ColorPackPalette.DefaultPrimaryAccent);
    public Brush SecondaryAccentBrush => BrushFromMaster(SecondaryAccent, ColorPackPalette.DefaultSecondaryAccent);
    public Brush FocusColorBrush => BrushFromMaster(FocusColor, ColorPackPalette.DefaultFocus);
    public Brush ActionButtonsColorBrush => BrushFromMaster(ActionButtonsColor, ColorPackPalette.DefaultActionButtons);
    public Brush ProgressColorBrush => BrushFromMaster(ProgressColor, ColorPackPalette.DefaultProgress);

    public Brush BackgroundBrush => BrushFromMaster(BackgroundColor, ColorPackPalette.DefaultBackground);
    public Brush BarsColorBrush => BrushFromMaster(BarsColor, ColorPackPalette.DefaultBars);
    public Brush MenusColorBrush => BrushFromMaster(MenusColor, ColorPackPalette.DefaultMenus);
    public Brush CinematicOverlayColorBrush => BrushFromMaster(CinematicOverlayColor, ColorPackPalette.DefaultCinematicOverlay);
    public Brush MenuHeaderColorBrush => BrushFromMaster(MenuHeaderColor, ColorPackPalette.DefaultMenuHeader);
    public Brush CardsColorBrush => BrushFromMaster(CardsColor, ColorPackPalette.DefaultCards);
    public Brush BorderColorBrush => BrushFromMaster(BorderColor, ColorPackPalette.DefaultBorder);
    public Brush NotificationsColorBrush => BrushFromMaster(NotificationsColor, ColorPackPalette.DefaultNotifications);
    public Brush PrimaryTextBrush => BrushFromMaster(PrimaryText, ColorPackPalette.DefaultPrimaryText);
    public Brush SecondaryTextBrush => BrushFromMaster(SecondaryText, ColorPackPalette.DefaultSecondaryText);
    public Brush HighlightTextBrush => BrushFromMaster(HighlightText, ColorPackPalette.DefaultHighlightText);

    public string DetailsPreviewTabText
    {
        get
        {
            var localized = Loc.Get("ColorPreviewTabDetails");
            if (!string.Equals(localized, "ColorPreviewTabDetails", StringComparison.Ordinal))
            {
                return localized;
            }

            return Loc.ActiveLanguage switch
            {
                "fr" => "DÉTAILS",
                "es" => "DETALLES",
                _ => "DETAILS"
            };
        }
    }

    public string PreviewSelectionTitle => Loc.Format(
        "ColorPreviewEditingFormat",
        Loc.Get(GetPreviewFamilyLabelKey(selectedPreviewFamilyKey)));

    public string PreviewSelectionDescription => Loc.Get(GetPreviewFamilyDescriptionKey(selectedPreviewFamilyKey));

    public Brush PreviewSelectionBrush => BrushFromMaster(
        GetMasterColor(selectedPreviewFamilyKey),
        GetDefaultColor(selectedPreviewFamilyKey));

    public string PreviewSuggestedSceneText => Loc.Format(
        "ColorPreviewSuggestedViewFormat",
        Loc.Get(GetPreviewSuggestedSceneLabelKey(selectedPreviewFamilyKey)));

    public Brush PreviewBackgroundBrush => previewBackgroundBrush;
    public Brush PreviewSurfaceBrush => previewSurfaceBrush;
    public Brush PreviewTopBarBrush => previewTopBarBrush;
    public Brush PreviewBottomBarBrush => previewBottomBarBrush;
    public Brush PreviewFocusBrush => previewFocusBrush;
    public Brush PreviewPlayBrush => previewPlayBrush;
    public Brush PreviewTextBrush => previewTextBrush;
    public Brush PreviewSecondaryTextBrush => previewSecondaryTextBrush;
    public Brush PreviewHighlightTextBrush => previewHighlightTextBrush;
    public Brush PreviewBorderBrush => previewBorderBrush;
    public Brush PreviewAccentBrush => previewAccentBrush;
    public Brush PreviewProgressBackgroundBrush => previewProgressBackgroundBrush;
    public Brush PreviewNotificationBrush => previewNotificationBrush;
    public Brush PreviewNotificationBorderBrush => previewNotificationBorderBrush;

    public string GeneratedColorCountText => Loc.Format(
        "ColorGeneratedCountFormat",
        ColorPackGeneratorService.CountTemplateColors());

    public string StatusText
    {
        get => statusText;
        private set
        {
            if (statusText == value)
            {
                return;
            }

            statusText = value;
            OnPropertyChanged();
        }
    }

    private void SetPaletteValue(ref string field, string? value, string propertyName, string brushPropertyName)
    {
        var next = value ?? string.Empty;
        if (field == next)
        {
            return;
        }

        var previousValue = field;
        var previousOverrides = new Dictionary<string, string>(expertOverrides);
        if (!suppressDirtyState)
            undoPalette = () => { SetMasterColor(PropertyNameToPreviewFamilyKey(propertyName)!, previousValue); expertOverrides = previousOverrides; RefreshPreview(); };
        field = next;
        OnPropertyChanged(propertyName);
        OnPropertyChanged(brushPropertyName);
        MarkDirty();

        var familyKey = PropertyNameToPreviewFamilyKey(propertyName);
        if (familyKey is not null)
        {
            SelectPreviewFamily(familyKey);
        }

        if (CurrentColorsAreValid())
        {
            if (!suppressDirtyState) ReconcileExpertOverrides();
            RefreshPreview();
            StatusText = expertOverrides.Count == 0 ? Loc.Get("ColorStatusPaletteUpdated") : $"Personnalisé · {expertOverrides.Count} ressources expertes";

        }
    }

    private void ChooseColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string key)
        {
            return;
        }

        SelectPreviewFamily(key);

        var current = GetMasterColor(key);
        var normalized = ColorPackGeneratorService.IsValidMasterColor(current)
            ? ColorPackGeneratorService.NormalizeMasterColor(current)
            : GetDefaultColor(key);
        var mediaColor = ParseMediaColor(normalized);

        using var dialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            AnyColor = true,
            SolidColorOnly = true,
            Color = System.Drawing.Color.FromArgb(mediaColor.R, mediaColor.G, mediaColor.B)
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        var selected = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        SetMasterColor(key, selected);
    }

    private void PaletteFieldGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is string key)
        {
            SelectPreviewFamily(key);
        }
    }

    private void SelectPreviewFamily(string key, bool switchScene = true)
    {
        selectedPreviewFamilyKey = key;
        OnPropertyChanged(nameof(PreviewSelectionTitle));
        OnPropertyChanged(nameof(PreviewSelectionDescription));
        OnPropertyChanged(nameof(PreviewSelectionBrush));
        OnPropertyChanged(nameof(PreviewSuggestedSceneText));

        if (!switchScene)
        {
            return;
        }

        var scene = GetPreviewSuggestedScene(key);
        switch (scene)
        {
            case "Menus":
                PreviewMenusTab.IsChecked = true;
                if (key == "CinematicOverlay" && !suppressDirtyState)
                {
                    PreviewCinematicOverlay.IsChecked = true;
                }
                break;
            case "Hub":
                PreviewHubTab.IsChecked = true;
                break;
            case "Social":
                PreviewSocialTab.IsChecked = true;
                break;
            case "Details":
                PreviewDetailsTab.IsChecked = true;
                break;
            default:
                PreviewMainTab.IsChecked = true;
                break;
        }
    }

    private static string? PropertyNameToPreviewFamilyKey(string propertyName) => propertyName switch
    {
        nameof(Badges) => "Badges",
        nameof(ActiveButtons) => "ActiveButtons",
        nameof(OutgoingMessages) => "OutgoingMessages",
        nameof(PrimaryAccent) => "PrimaryAccent",
        nameof(SecondaryAccent) => "SecondaryAccent",
        nameof(FocusColor) => "Focus",
        nameof(ActionButtonsColor) => "ActionButtons",
        nameof(ProgressColor) => "Progress",
        nameof(BackgroundColor) => "Background",
        nameof(BarsColor) => "Bars",
        nameof(MenusColor) => "Menus",
        nameof(CinematicOverlayColor) => "CinematicOverlay",
        nameof(MenuHeaderColor) => "MenuHeader",
        nameof(CardsColor) => "Cards",
        nameof(BorderColor) => "Border",
        nameof(NotificationsColor) => "Notifications",
        nameof(PrimaryText) => "PrimaryText",
        nameof(SecondaryText) => "SecondaryText",
        nameof(HighlightText) => "HighlightText",
        _ => null
    };

    private static string GetPreviewFamilyLabelKey(string key) => key switch
    {
        "Badges" => "ColorBadges",
        "ActiveButtons" => "ColorActiveButtons",
        "OutgoingMessages" => "ColorOutgoingMessages",
        "PrimaryAccent" => "ColorPrimaryAccent",
        "SecondaryAccent" => "ColorSecondaryAccent",
        "Focus" => "ColorFocus",
        "ActionButtons" => "ColorActionButtons",
        "Progress" => "ColorProgress",
        "Background" => "ColorBackground",
        "Bars" => "ColorBars",
        "Menus" => "ColorMenus",
        "CinematicOverlay" => "ColorCinematicOverlay",
        "MenuHeader" => "ColorMenuHeader",
        "Cards" => "ColorCards",
        "Border" => "ColorBorder",
        "Notifications" => "ColorNotifications",
        "PrimaryText" => "ColorPrimaryText",
        "SecondaryText" => "ColorSecondaryText",
        "HighlightText" => "ColorHighlightText",
        _ => "ColorPrimaryAccent"
    };

    private static string GetPreviewFamilyDescriptionKey(string key) => $"ColorPreviewImpact_{key}";

    private static string GetPreviewSuggestedScene(string key) => key switch
    {
        "Menus" or "CinematicOverlay" or "Cards" or "Focus" or "Border" or "Bars" or
            "PrimaryText" or "SecondaryText" or "HighlightText" => "Menus",
        "Background" or "SecondaryAccent" => "Hub",
        "Progress" => "Details",
        "Badges" or "ActiveButtons" or "OutgoingMessages" => "Social",
        _ => "Main"
    };

    private static string GetPreviewSuggestedSceneLabelKey(string key) => GetPreviewSuggestedScene(key) switch
    {
        "Menus" => "ColorPreviewTabMenus",
        "Hub" => "ColorPreviewTabHub",
        "Details" => "ColorPreviewTabDetails",
        "Social" => "ColorPreviewTabSocial",
        _ => "ColorPreviewTabMain"
    };

    private void ResetPaletteClick(object sender, RoutedEventArgs e)
    {
        expertOverrides.Clear(); previousAutomaticXaml = null;
        Badges = "#21242A";
        ActiveButtons = "#21242A";
        OutgoingMessages = "#21242A";
        PrimaryAccent = ColorPackPalette.DefaultPrimaryAccent;
        SecondaryAccent = ColorPackPalette.DefaultSecondaryAccent;
        FocusColor = ColorPackPalette.DefaultFocus;
        ActionButtonsColor = ColorPackPalette.DefaultActionButtons;
        ProgressColor = ColorPackPalette.DefaultProgress;
        BackgroundColor = ColorPackPalette.DefaultBackground;
        BarsColor = ColorPackPalette.DefaultBars;
        MenusColor = ColorPackPalette.DefaultMenus;
        CinematicOverlayColor = ColorPackPalette.DefaultCinematicOverlay;
        MenuHeaderColor = ColorPackPalette.DefaultMenuHeader;
        CardsColor = ColorPackPalette.DefaultCards;
        BorderColor = ColorPackPalette.DefaultBorder;
        NotificationsColor = ColorPackPalette.DefaultNotifications;
        PrimaryText = ColorPackPalette.DefaultPrimaryText;
        SecondaryText = ColorPackPalette.DefaultSecondaryText;
        HighlightText = ColorPackPalette.DefaultHighlightText;
        SelectPreviewFamily("PrimaryAccent");
        StatusText = Loc.Get("ColorStatusPaletteReset");
    }

    private void NewProjectClick(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardChanges())
        {
            return;
        }

        suppressDirtyState = true;
        try
        {
            currentPackId = string.Empty;
            currentProjectPath = null;
            PackNameTextBox.Text = Loc.Get("DefaultColorPackName");
            AuthorTextBox.Text = string.Empty;
            VersionTextBox.Text = Loc.Get("DefaultVersion");
            DescriptionTextBox.Text = string.Empty;
            expertOverrides.Clear(); previousAutomaticXaml = null;
            Badges = "#21242A";
            ActiveButtons = "#21242A";
            OutgoingMessages = "#21242A";
            PrimaryAccent = ColorPackPalette.DefaultPrimaryAccent;
            SecondaryAccent = ColorPackPalette.DefaultSecondaryAccent;
            FocusColor = ColorPackPalette.DefaultFocus;
            ActionButtonsColor = ColorPackPalette.DefaultActionButtons;
            ProgressColor = ColorPackPalette.DefaultProgress;
            BackgroundColor = ColorPackPalette.DefaultBackground;
            BarsColor = ColorPackPalette.DefaultBars;
            MenusColor = ColorPackPalette.DefaultMenus;
            CinematicOverlayColor = ColorPackPalette.DefaultCinematicOverlay;
            MenuHeaderColor = ColorPackPalette.DefaultMenuHeader;
            CardsColor = ColorPackPalette.DefaultCards;
            BorderColor = ColorPackPalette.DefaultBorder;
            NotificationsColor = ColorPackPalette.DefaultNotifications;
            PrimaryText = ColorPackPalette.DefaultPrimaryText;
            SecondaryText = ColorPackPalette.DefaultSecondaryText;
            HighlightText = ColorPackPalette.DefaultHighlightText;
            RefreshPreview();
            SelectPreviewFamily("PrimaryAccent");
            isDirty = false;
            StatusText = Loc.Get("ColorStatusNewProject");
        }
        finally
        {
            suppressDirtyState = false;
        }
    }

    private void OpenProjectClick(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardChanges())
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = Loc.Get("DialogOpenColorProject"),
            Filter = Loc.Get("FilterOpenColorProject"),
            CheckFileExists = true
        };

        if (dialog.ShowDialog(OwnerWindow) != true)
        {
            return;
        }

        try
        {
            var document = ColorPackProjectService.Load(dialog.FileName);
            suppressDirtyState = true;
            currentPackId = string.IsNullOrWhiteSpace(document.PackId)
                ? CreatePackId(document.PackName)
                : document.PackId.Trim();
            PackNameTextBox.Text = document.PackName;
            AuthorTextBox.Text = document.Author;
            VersionTextBox.Text = document.Version;
            DescriptionTextBox.Text = document.Description;
            Badges = document.Badges;
            ActiveButtons = document.ActiveButtons;
            OutgoingMessages = document.OutgoingMessages;
            expertOverrides = new(document.ExpertOverrides); previousAutomaticXaml = null;
            PrimaryAccent = document.PrimaryAccent;
            SecondaryAccent = document.SecondaryAccent;
            FocusColor = document.Focus;
            ActionButtonsColor = document.ActionButtons;
            ProgressColor = document.Progress;
            BackgroundColor = document.Background;
            BarsColor = document.Bars;
            MenusColor = document.Menus;
            CinematicOverlayColor = document.CinematicOverlay;
            MenuHeaderColor = document.MenuHeader;
            CardsColor = document.Cards;
            BorderColor = document.Border;
            NotificationsColor = document.Notifications;
            PrimaryText = document.PrimaryText;
            SecondaryText = document.SecondaryText;
            HighlightText = document.HighlightText;
            currentProjectPath = dialog.FileName;
            isDirty = string.IsNullOrWhiteSpace(document.PackId);
            RefreshPreview();
            StatusText = Loc.Format("StatusProjectOpened", Path.GetFileName(dialog.FileName));
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorColorProjectOpen"), exception);
        }
        finally
        {
            suppressDirtyState = false;
        }
    }

    private void SaveProjectClick(object sender, RoutedEventArgs e)
    {
        SaveProject(false);
    }

    private bool SaveProject(bool forceChoosePath)
    {
        if (!ValidateColors(showMessage: true))
        {
            return false;
        }

        var path = forceChoosePath ? null : currentProjectPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            var dialog = new SaveFileDialog
            {
                Title = Loc.Get("DialogSaveColorProject"),
                Filter = Loc.Get("FilterSaveColorProject"),
                DefaultExt = ".acpc",
                AddExtension = true,
                FileName = MakeSafeFileName(PackNameTextBox.Text) + ".acpc"
            };

            if (dialog.ShowDialog(OwnerWindow) != true)
            {
                return false;
            }

            path = dialog.FileName;
        }

        try
        {
            EnsurePackId();
            ColorPackProjectService.Save(path, BuildProjectDocument());
            currentProjectPath = path;
            isDirty = false;
            StatusText = Loc.Format("StatusProjectSaved", Path.GetFileName(path));
            return true;
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorColorProjectSave"), exception);
            return false;
        }
    }

    private void ExportZipClick(object sender, RoutedEventArgs e)
    {
        if (!ValidateProject())
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = Loc.Get("DialogExportColorPack"),
            Filter = Loc.Get("FilterColorPackZip"),
            DefaultExt = ".zip",
            AddExtension = true,
            FileName = MakeSafeFileName(PackNameTextBox.Text) + ".zip"
        };

        if (dialog.ShowDialog(OwnerWindow) != true)
        {
            return;
        }

        try
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            StatusText = Loc.Get("ColorStatusGeneratingPack");
            EnsurePackId();
            ColorPackExportService.Export(
                dialog.FileName,
                currentPackId,
                PackNameTextBox.Text,
                AuthorTextBox.Text,
                VersionTextBox.Text,
                DescriptionTextBox.Text,
                BuildPalette());

            StatusText = Loc.Format("ColorStatusPackExported", Path.GetFileName(dialog.FileName));
            MessageBox.Show(
                OwnerWindow,
                Loc.Format("ColorExportSuccessMessage", dialog.FileName),
                Loc.Get("ExportCompleteTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorColorPackExport"), exception);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }


    private void ShareCommunityPackClick(object sender, RoutedEventArgs e)
    {
        const string submissionUrl = "https://github.com/Mike-Aniki/AnikiCommunityPacks/issues/new?template=color-pack-submission.yml";

        try
        {
            Process.Start(new ProcessStartInfo(submissionUrl)
            {
                UseShellExecute = true
            });
            StatusText = Loc.Get("StatusCommunitySubmissionOpened");
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorCommunitySubmissionOpen"), exception);
        }
    }

    private bool ValidateProject()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(PackNameTextBox.Text))
        {
            errors.Add(Loc.Get("ColorValidationEnterPackName"));
        }

        if (string.IsNullOrWhiteSpace(VersionTextBox.Text))
        {
            errors.Add(Loc.Get("ColorValidationEnterVersion"));
        }
        else if (!IsValidPackVersion(VersionTextBox.Text))
        {
            errors.Add(Loc.Get("ValidationInvalidVersion"));
        }

        AddColorValidationErrors(errors);

        if (errors.Count == 0)
        {
            StatusText = Loc.Get("ColorStatusProjectValid");
            return true;
        }

        StatusText = Loc.Get("StatusValidationFailed");
        MessageBox.Show(
            OwnerWindow,
            string.Join("\n\n", errors),
            Loc.Get("ColorValidationIncompleteTitle"),
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return false;
    }

    private bool ValidateColors(bool showMessage)
    {
        var errors = new List<string>();
        AddColorValidationErrors(errors);
        if (errors.Count == 0)
        {
            return true;
        }

        if (showMessage)
        {
            MessageBox.Show(
                OwnerWindow,
                string.Join("\n", errors),
                Loc.Get("ColorValidationIncompleteTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        return false;
    }

    private void AddColorValidationErrors(List<string> errors)
    {
        AddColorError(errors, Loc.Get("ColorBadges"), Badges);
        AddColorError(errors, Loc.Get("ColorActiveButtons"), ActiveButtons);
        AddColorError(errors, Loc.Get("ColorOutgoingMessages"), OutgoingMessages);
        AddColorError(errors, Loc.Get("ColorPrimaryAccent"), PrimaryAccent);
        AddColorError(errors, Loc.Get("ColorSecondaryAccent"), SecondaryAccent);
        AddColorError(errors, Loc.Get("ColorFocus"), FocusColor);
        AddColorError(errors, Loc.Get("ColorActionButtons"), ActionButtonsColor);
        AddColorError(errors, Loc.Get("ColorProgress"), ProgressColor);
        AddColorError(errors, Loc.Get("ColorBackground"), BackgroundColor);
        AddColorError(errors, Loc.Get("ColorBars"), BarsColor);
        AddColorError(errors, Loc.Get("ColorMenus"), MenusColor);
        AddColorError(errors, Loc.Get("ColorCinematicOverlay"), CinematicOverlayColor);
        AddColorError(errors, Loc.Get("ColorMenuHeader"), MenuHeaderColor);
        AddColorError(errors, Loc.Get("ColorCards"), CardsColor);
        AddColorError(errors, Loc.Get("ColorBorder"), BorderColor);
        AddColorError(errors, Loc.Get("ColorNotifications"), NotificationsColor);
        AddColorError(errors, Loc.Get("ColorPrimaryText"), PrimaryText);
        AddColorError(errors, Loc.Get("ColorSecondaryText"), SecondaryText);
        AddColorError(errors, Loc.Get("ColorHighlightText"), HighlightText);
    }

    private static void AddColorError(List<string> errors, string label, string value)
    {
        if (!ColorPackGeneratorService.IsValidMasterColor(value))
        {
            errors.Add(Loc.Format("ColorValidationInvalidHex", label));
        }
    }

    private bool CurrentColorsAreValid()
    {
        return ColorPackGeneratorService.IsValidMasterColor(PrimaryAccent) &&
               ColorPackGeneratorService.IsValidMasterColor(Badges) &&
               ColorPackGeneratorService.IsValidMasterColor(ActiveButtons) &&
               ColorPackGeneratorService.IsValidMasterColor(OutgoingMessages) &&
               ColorPackGeneratorService.IsValidMasterColor(SecondaryAccent) &&
               ColorPackGeneratorService.IsValidMasterColor(FocusColor) &&
               ColorPackGeneratorService.IsValidMasterColor(ActionButtonsColor) &&
               ColorPackGeneratorService.IsValidMasterColor(ProgressColor) &&
               ColorPackGeneratorService.IsValidMasterColor(BackgroundColor) &&
               ColorPackGeneratorService.IsValidMasterColor(BarsColor) &&
               ColorPackGeneratorService.IsValidMasterColor(MenusColor) &&
               ColorPackGeneratorService.IsValidMasterColor(CinematicOverlayColor) &&
               ColorPackGeneratorService.IsValidMasterColor(MenuHeaderColor) &&
               ColorPackGeneratorService.IsValidMasterColor(CardsColor) &&
               ColorPackGeneratorService.IsValidMasterColor(BorderColor) &&
               ColorPackGeneratorService.IsValidMasterColor(NotificationsColor) &&
               ColorPackGeneratorService.IsValidMasterColor(PrimaryText) &&
               ColorPackGeneratorService.IsValidMasterColor(SecondaryText) &&
               ColorPackGeneratorService.IsValidMasterColor(HighlightText);
    }

    private ColorPackPalette BuildPalette()
    {
        return new ColorPackPalette
        {
            Badges = ColorPackGeneratorService.NormalizeMasterColor(Badges),
            ActiveButtons = ColorPackGeneratorService.NormalizeMasterColor(ActiveButtons),
            OutgoingMessages = ColorPackGeneratorService.NormalizeMasterColor(OutgoingMessages),
            ExpertOverrides = new(expertOverrides),
            PrimaryAccent = ColorPackGeneratorService.NormalizeMasterColor(PrimaryAccent),
            SecondaryAccent = ColorPackGeneratorService.NormalizeMasterColor(SecondaryAccent),
            Focus = ColorPackGeneratorService.NormalizeMasterColor(FocusColor),
            ActionButtons = ColorPackGeneratorService.NormalizeMasterColor(ActionButtonsColor),
            Progress = ColorPackGeneratorService.NormalizeMasterColor(ProgressColor),
            Background = ColorPackGeneratorService.NormalizeMasterColor(BackgroundColor),
            Bars = ColorPackGeneratorService.NormalizeMasterColor(BarsColor),
            Menus = ColorPackGeneratorService.NormalizeMasterColor(MenusColor),
            CinematicOverlay = ColorPackGeneratorService.NormalizeMasterColor(CinematicOverlayColor),
            MenuHeader = ColorPackGeneratorService.NormalizeMasterColor(MenuHeaderColor),
            Cards = ColorPackGeneratorService.NormalizeMasterColor(CardsColor),
            Border = ColorPackGeneratorService.NormalizeMasterColor(BorderColor),
            Notifications = ColorPackGeneratorService.NormalizeMasterColor(NotificationsColor),
            PrimaryText = ColorPackGeneratorService.NormalizeMasterColor(PrimaryText),
            SecondaryText = ColorPackGeneratorService.NormalizeMasterColor(SecondaryText),
            HighlightText = ColorPackGeneratorService.NormalizeMasterColor(HighlightText)
        };
    }

    private ColorPackProjectDocument BuildProjectDocument()
    {
        var palette = BuildPalette();
        return new ColorPackProjectDocument
        {
            PackId = currentPackId,
            PackName = PackNameTextBox.Text.Trim(),
            Author = AuthorTextBox.Text.Trim(),
            Version = VersionTextBox.Text.Trim(),
            Description = DescriptionTextBox.Text.Trim(),
            TemplateId = ColorPackPalette.ReferenceTemplateId,
            Badges = palette.Badges,
            ActiveButtons = palette.ActiveButtons,
            OutgoingMessages = palette.OutgoingMessages,
            ExpertOverrides = new(expertOverrides),
            PrimaryAccent = palette.PrimaryAccent,
            SecondaryAccent = palette.SecondaryAccent,
            Focus = palette.Focus,
            ActionButtons = palette.ActionButtons,
            Progress = palette.Progress,
            Background = palette.Background,
            Bars = palette.Bars,
            Menus = palette.Menus,
            CinematicOverlay = palette.CinematicOverlay,
            MenuHeader = palette.MenuHeader,
            Cards = palette.Cards,
            Border = palette.Border,
            Notifications = palette.Notifications,
            PrimaryText = palette.PrimaryText,
            SecondaryText = palette.SecondaryText,
            HighlightText = palette.HighlightText
        };
    }

    private string AutomaticXaml()
    {
        var palette = BuildPalette();
        palette.ExpertOverrides.Clear();
        return ColorPackGeneratorService.Generate(palette);
    }

    private void ReconcileExpertOverrides()
    {
        var next = AutomaticXaml();
        if (previousAutomaticXaml != null)
        {
            var before = ExpertColorService.Resources(previousAutomaticXaml);
            lastSimpleChanges = ExpertColorService.Resources(next).Where(p => !before.TryGetValue(p.Key, out var old) || !XNode.DeepEquals(old, p.Value)).Select(p => p.Key).ToHashSet();
            ExpertColorService.RemoveAffectedOverrides(previousAutomaticXaml, next, expertOverrides);
        }
        previousAutomaticXaml = next;
    }

    private void ExpertModeClick(object sender, RoutedEventArgs e)
    {
        if (!CurrentColorsAreValid()) return;
        var editor = new ExpertPaletteWindow(AutomaticXaml(), expertOverrides, lastSimpleChanges) { Owner = Window.GetWindow(this) };
        if (editor.ShowDialog() == true)
        {
            var old = new Dictionary<string, string>(expertOverrides);
            undoPalette = () => { expertOverrides = old; RefreshPreview(); };
            expertOverrides = editor.Result;
            MarkDirty();
            RefreshPreview();
            StatusText = $"Personnalisé · {expertOverrides.Count} ressources expertes";
        }
    }

    private void UndoPaletteClick(object sender, RoutedEventArgs e)
    {
        var undo = undoPalette;
        undo?.Invoke();
        undoPalette = null;
        StatusText = Loc.Get("ColorStatusPaletteUpdated");
    }

    // The expert studio renders the same scenes as the simplified editor.
    internal System.Windows.Media.ImageSource RenderExpertScene(string xaml, string scene)
    {
        PreviewMainTab.IsChecked = scene == "Bibliothèque";
        PreviewHubTab.IsChecked = scene == "Accueil";
        PreviewMenusTab.IsChecked = scene == "Panneaux";
        PreviewDetailsTab.IsChecked = scene == "Fiche de jeu";
        PreviewSocialTab.IsChecked = scene == "Social";
        var dictionary = ParsePreviewResourceDictionary(xaml);
        AddCurrentThemeSurfaces(dictionary);
        PreviewResourceHost.Resources.MergedDictionaries.Clear();
        PreviewResourceHost.Resources.MergedDictionaries.Add(dictionary);
        Width = 1600; Height = 1050;
        Measure(new Size(1600,1050)); Arrange(new Rect(0,0,1600,1050)); UpdateLayout();
        var width = Math.Max(1, PreviewResourceHost.ActualWidth);
        var height = Math.Max(1, PreviewResourceHost.ActualHeight);
        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(width*2), (int)Math.Ceiling(height*2), 192,192,PixelFormats.Pbgra32);
        var drawing = new DrawingVisual();
        using (var context = drawing.RenderOpen())
            context.DrawRectangle(new VisualBrush(PreviewResourceHost) { Stretch=Stretch.Fill }, null, new Rect(0,0,width,height));
        bitmap.Render(drawing); bitmap.Freeze();
        return bitmap;
    }

    private void RefreshPreview()
    {
        try
        {
            previousAutomaticXaml = AutomaticXaml();
            var generated = ColorPackGeneratorService.Generate(BuildPalette());
            var dictionary = ParsePreviewResourceDictionary(generated);
            AddCurrentThemeSurfaces(dictionary);
            UpdateContrastWarning(dictionary);

            PreviewResourceHost.Resources.MergedDictionaries.Clear();
            PreviewResourceHost.Resources.MergedDictionaries.Add(dictionary);
        }
        catch
        {
        }
    }

    private string contrastWarning = string.Empty;
    public string ContrastWarning
    {
        get => contrastWarning;
        private set
        {
            contrastWarning = value;
            OnPropertyChanged();
        }
    }

    // Mirrors Aniki Helper's SettingsPaletteResources. These brushes belong to the theme,
    // so they are injected into the preview only and never exported in colors.xaml.
    private static void AddCurrentThemeSurfaces(ResourceDictionary resources)
    {
        Color ReadColor(string key) => (Color)resources[key];
        Color Opaque(Color color) => Color.FromRgb(color.R, color.G, color.B);
        Color Lighten(Color color) => Color.FromRgb(
            (byte)(color.R + (255 - color.R) * 0.06),
            (byte)(color.G + (255 - color.G) * 0.06),
            (byte)(color.B + (255 - color.B) * 0.06));

        var row = Opaque(ReadColor("OverlayMenu_Mid"));
        var choice = Opaque(ReadColor("OverlayMenu_Bottom"));
        resources["AnikiSettingsFlatRowBackground"] = new SolidColorBrush(row);
        resources["AnikiSettingsFlatRowFocus"] = new SolidColorBrush(Lighten(row));
        resources["AnikiSettingsChoiceBackground"] = new SolidColorBrush(choice);
        resources["AnikiSettingsChoiceFocus"] = new SolidColorBrush(Lighten(choice));
        resources["AnikiSettingsFlatRowLine"] = new SolidColorBrush(ReadColor("TextColor")) { Opacity = 0.12 };
        resources["PreviewDarkTextBrush"] = new SolidColorBrush(ReadColor("TextColorDark"));
    }

    private void UpdateContrastWarning(ResourceDictionary resources)
    {
        var foreground = (Color)resources["TextColor"];
        var secondary = (Color)resources["TextSecondaryColor"];
        var row = ((SolidColorBrush)resources["AnikiSettingsFlatRowBackground"]).Color;
        var focus = ((SolidColorBrush)resources["AnikiSettingsFlatRowFocus"]).Color;
        var menuTop = (Color)resources["MenuBackground_Top"];
        var menuBottom = (Color)resources["MenuBackground_Bottom"];
        var page = (Color)resources["SecondaryViewBackground_Mid"];
        var surfaces = new[] { row, focus, menuTop, menuBottom, page };
        var minimum = surfaces.Min(background =>
            Math.Min(ContrastRatio(foreground, background), ContrastRatio(secondary, background)));
        ContrastWarning = minimum < 4.5
            ? Loc.Format("ColorContrastWarning", minimum.ToString("0.0", CultureInfo.CurrentCulture))
            : string.Empty;
    }

    private static double ContrastRatio(Color foreground, Color background)
    {
        // Composite text alpha over the surface before calculating relative luminance.
        var alpha = foreground.A / 255.0;
        var composite = Color.FromRgb(
            (byte)Math.Round(foreground.R * alpha + background.R * (1 - alpha)),
            (byte)Math.Round(foreground.G * alpha + background.G * (1 - alpha)),
            (byte)Math.Round(foreground.B * alpha + background.B * (1 - alpha)));
        static double Luminance(Color color)
        {
            static double Linear(byte value)
            {
                var channel = value / 255.0;
                return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
        }
        var text = Luminance(composite);
        var surface = Luminance(background);
        return (Math.Max(text, surface) + 0.05) / (Math.Min(text, surface) + 0.05);
    }

    private static ResourceDictionary ParsePreviewResourceDictionary(string xaml)
    {
        try
        {
            return XamlReader.Parse(xaml) as ResourceDictionary
                ?? throw new InvalidDataException("The generated Color Pack XAML is not a ResourceDictionary.");
        }
        catch
        {
            var document = XDocument.Parse(xaml, LoadOptions.PreserveWhitespace);
            var root = document.Root
                ?? throw new InvalidDataException("The generated Color Pack XAML has no root element.");

            root.Elements()
                .Where(element =>
                    string.Equals(element.Name.LocalName, "Style", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(element.Name.LocalName, "Int32", StringComparison.OrdinalIgnoreCase))
                .Remove();

            var previewXaml = document.ToString(SaveOptions.DisableFormatting);
            return XamlReader.Parse(previewXaml) as ResourceDictionary
                ?? throw new InvalidDataException("The generated Color Pack preview XAML is not a ResourceDictionary.");
        }
    }

    private static Dictionary<string, string> ReadGeneratedColors(string xaml)
    {
        var document = XDocument.Parse(xaml);
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        return document.Descendants(presentation + "Color")
            .Select(element => new
            {
                Key = element.Attribute(x + "Key")?.Value,
                Value = element.Value.Trim()
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Key))
            .ToDictionary(item => item.Key!, item => item.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static Brush Solid(IReadOnlyDictionary<string, string> colors, string key, string fallback)
    {
        var value = colors.TryGetValue(key, out var found) ? found : fallback;
        var brush = new SolidColorBrush(ParseMediaColor(value));
        brush.Freeze();
        return brush;
    }

    private static Brush Gradient(
        IReadOnlyDictionary<string, string> colors,
        string firstKey,
        string secondKey,
        string firstFallback,
        string secondFallback,
        double angle = 90.0)
    {
        var first = colors.TryGetValue(firstKey, out var firstValue) ? firstValue : firstFallback;
        var second = colors.TryGetValue(secondKey, out var secondValue) ? secondValue : secondFallback;
        var brush = new LinearGradientBrush(ParseMediaColor(first), ParseMediaColor(second), angle);
        brush.Freeze();
        return brush;
    }

    private static Brush BrushFromMaster(string value, string fallback)
    {
        var selected = ColorPackGeneratorService.IsValidMasterColor(value) ? value : fallback;
        var brush = new SolidColorBrush(ParseMediaColor(selected));
        brush.Freeze();
        return brush;
    }

    private static Color ParseMediaColor(string value)
    {
        return (Color)ColorConverter.ConvertFromString(value)!;
    }

    private void NotifyPreviewProperties()
    {
        OnPropertyChanged(nameof(PreviewBackgroundBrush));
        OnPropertyChanged(nameof(PreviewSurfaceBrush));
        OnPropertyChanged(nameof(PreviewTopBarBrush));
        OnPropertyChanged(nameof(PreviewBottomBarBrush));
        OnPropertyChanged(nameof(PreviewFocusBrush));
        OnPropertyChanged(nameof(PreviewPlayBrush));
        OnPropertyChanged(nameof(PreviewTextBrush));
        OnPropertyChanged(nameof(PreviewSecondaryTextBrush));
        OnPropertyChanged(nameof(PreviewHighlightTextBrush));
        OnPropertyChanged(nameof(PreviewBorderBrush));
        OnPropertyChanged(nameof(PreviewAccentBrush));
        OnPropertyChanged(nameof(PreviewProgressBackgroundBrush));
        OnPropertyChanged(nameof(PreviewNotificationBrush));
        OnPropertyChanged(nameof(PreviewNotificationBorderBrush));
    }

    private string GetMasterColor(string key) => key switch
    {
        "Badges" => Badges,
        "ActiveButtons" => ActiveButtons,
        "OutgoingMessages" => OutgoingMessages,
        "PrimaryAccent" => PrimaryAccent,
        "SecondaryAccent" => SecondaryAccent,
        "Focus" => FocusColor,
        "ActionButtons" => ActionButtonsColor,
        "Progress" => ProgressColor,
        "Background" => BackgroundColor,
        "Bars" => BarsColor,
        "Menus" => MenusColor,
        "CinematicOverlay" => CinematicOverlayColor,
        "MenuHeader" => MenuHeaderColor,
        "Cards" => CardsColor,
        "Border" => BorderColor,
        "Notifications" => NotificationsColor,
        "PrimaryText" => PrimaryText,
        "SecondaryText" => SecondaryText,
        "HighlightText" => HighlightText,
        _ => ColorPackPalette.DefaultPrimaryAccent
    };

    private static string GetDefaultColor(string key) => key switch
    {
        "Badges" => "#21242A",
        "ActiveButtons" => "#21242A",
        "OutgoingMessages" => "#21242A",
        "PrimaryAccent" => ColorPackPalette.DefaultPrimaryAccent,
        "SecondaryAccent" => ColorPackPalette.DefaultSecondaryAccent,
        "Focus" => ColorPackPalette.DefaultFocus,
        "ActionButtons" => ColorPackPalette.DefaultActionButtons,
        "Progress" => ColorPackPalette.DefaultProgress,
        "Background" => ColorPackPalette.DefaultBackground,
        "Bars" => ColorPackPalette.DefaultBars,
        "Menus" => ColorPackPalette.DefaultMenus,
        "MenuHeader" => ColorPackPalette.DefaultMenuHeader,
        "Cards" => ColorPackPalette.DefaultCards,
        "Border" => ColorPackPalette.DefaultBorder,
        "Notifications" => ColorPackPalette.DefaultNotifications,
        "PrimaryText" => ColorPackPalette.DefaultPrimaryText,
        "SecondaryText" => ColorPackPalette.DefaultSecondaryText,
        "HighlightText" => ColorPackPalette.DefaultHighlightText,
        _ => ColorPackPalette.DefaultPrimaryAccent
    };

    private void SetMasterColor(string key, string value)
    {
        switch (key)
        {
            case "Badges": Badges = value; break;
            case "ActiveButtons": ActiveButtons = value; break;
            case "OutgoingMessages": OutgoingMessages = value; break;
            case "PrimaryAccent": PrimaryAccent = value; break;
            case "SecondaryAccent": SecondaryAccent = value; break;
            case "Focus": FocusColor = value; break;
            case "ActionButtons": ActionButtonsColor = value; break;
            case "Progress": ProgressColor = value; break;
            case "Background": BackgroundColor = value; break;
            case "Bars": BarsColor = value; break;
            case "Menus": MenusColor = value; break;
            case "CinematicOverlay": CinematicOverlayColor = value; break;
            case "MenuHeader": MenuHeaderColor = value; break;
            case "Cards": CardsColor = value; break;
            case "Border": BorderColor = value; break;
            case "Notifications": NotificationsColor = value; break;
            case "PrimaryText": PrimaryText = value; break;
            case "SecondaryText": SecondaryText = value; break;
            case "HighlightText": HighlightText = value; break;
        }
    }

    private void MetadataTextChanged(object sender, TextChangedEventArgs e)
    {
        MarkDirty();
    }

    private void EnsurePackId()
    {
        if (!string.IsNullOrWhiteSpace(currentPackId))
        {
            return;
        }

        currentPackId = CreatePackId(PackNameTextBox.Text);
        MarkDirty();
    }

    private static string CreatePackId(string packName)
    {
        var normalized = (packName ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var isAsciiLetter = character is >= 'a' and <= 'z';
            var isDigit = character is >= '0' and <= '9';
            if (isAsciiLetter || isDigit)
            {
                builder.Append(character);
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var slug = builder.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "custom-color-pack";
        }

        var suffix = Guid.NewGuid().ToString("N")[..8];
        return $"{slug}-{suffix}";
    }

    private static bool IsValidPackVersion(string version)
    {
        return Regex.IsMatch(
            version.Trim(),
            @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$");
    }

    private static string MakeSafeFileName(string value)
    {
        var result = string.IsNullOrWhiteSpace(value) ? "ColorPack" : value.Trim();
        foreach (var character in Path.GetInvalidFileNameChars())
        {
            result = result.Replace(character, '_');
        }

        return string.IsNullOrWhiteSpace(result) ? "ColorPack" : result;
    }

    private bool ConfirmDiscardChanges()
    {
        if (!isDirty)
        {
            return true;
        }

        var result = MessageBox.Show(
            OwnerWindow,
            Loc.Get("UnsavedChangesMessage"),
            Loc.Get("UnsavedChangesTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        return result == MessageBoxResult.Yes;
    }

    private void MarkDirty()
    {
        if (!suppressDirtyState)
        {
            isDirty = true;
        }
    }

    private void ShowError(string message, Exception exception)
    {
        StatusText = message + " " + exception.Message;
        MessageBox.Show(
            OwnerWindow,
            message + "\n\n" + exception.Message,
            "Aniki Pack Creator",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    public bool HasUnsavedChanges => isDirty;

    public bool ConfirmClose() => ConfirmDiscardChanges();
    public void Deactivate() { }
    public void OnHostClosed() { }

    private Window OwnerWindow => Window.GetWindow(this)
        ?? Application.Current.MainWindow
        ?? throw new InvalidOperationException("Aniki Pack Creator host window is not available.");

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
