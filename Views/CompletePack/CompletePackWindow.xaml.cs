using AnikiVisualPackCreator.Models;
using AnikiVisualPackCreator.Services;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AnikiVisualPackCreator;

public partial class CompletePackWindow : UserControl
{
    private string? currentProjectPath;
    private string? currentPackId;
    private PackArchiveInfo? visualPack;
    private PackArchiveInfo? loginPack;
    private PackArchiveInfo? soundPack;
    private PackArchiveInfo? colorPack;
    private bool isDirty;
    private bool suppressDirtyState;

    public CompletePackWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => InitializeDefaultState();
    }

    private void InitializeDefaultState()
    {
        if (string.IsNullOrWhiteSpace(PackNameTextBox.Text))
        {
            suppressDirtyState = true;
            PackNameTextBox.Text = Loc.Get("DefaultCompletePackName");
            VersionTextBox.Text = Loc.Get("DefaultVersion");
            suppressDirtyState = false;
        }

        RefreshUi();
    }

    private void NewProjectClick(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardChanges())
        {
            return;
        }

        suppressDirtyState = true;
        currentProjectPath = null;
        currentPackId = null;
        PackNameTextBox.Text = Loc.Get("DefaultCompletePackName");
        AuthorTextBox.Text = string.Empty;
        VersionTextBox.Text = Loc.Get("DefaultVersion");
        DescriptionTextBox.Text = string.Empty;
        visualPack = null;
        loginPack = null;
        soundPack = null;
        colorPack = null;
        suppressDirtyState = false;
        isDirty = false;
        SetStatus(Loc.Get("CompleteStatusNewProject"));
        RefreshUi();
    }

    private void OpenProjectClick(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardChanges())
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = Loc.Get("DialogOpenCompleteProject"),
            Filter = Loc.Get("FilterOpenCompleteProject")
        };

        if (dialog.ShowDialog(OwnerWindow) != true)
        {
            return;
        }

        try
        {
            suppressDirtyState = true;
            var document = CompletePackProjectService.Load(dialog.FileName);
            currentProjectPath = dialog.FileName;
            currentPackId = string.IsNullOrWhiteSpace(document.PackId) ? null : document.PackId;
            PackNameTextBox.Text = string.IsNullOrWhiteSpace(document.PackName) ? Loc.Get("DefaultCompletePackName") : document.PackName;
            AuthorTextBox.Text = document.Author ?? string.Empty;
            VersionTextBox.Text = string.IsNullOrWhiteSpace(document.Version) ? Loc.Get("DefaultVersion") : document.Version;
            DescriptionTextBox.Text = document.Description ?? string.Empty;

            visualPack = TryLoadSavedPack(document.VisualPackPath, PackArchiveKind.Visual);
            loginPack = TryLoadSavedPack(document.LoginPackPath, PackArchiveKind.Login);
            soundPack = TryLoadSavedPack(document.SoundPackPath, PackArchiveKind.Sound);
            colorPack = TryLoadSavedPack(document.ColorPackPath, PackArchiveKind.Color);

            suppressDirtyState = false;
            isDirty = false;
            SetStatus(Loc.Format("StatusProjectOpened", Path.GetFileName(dialog.FileName)));
            RefreshUi();
        }
        catch (Exception exception)
        {
            suppressDirtyState = false;
            ShowError(Loc.Get("ErrorCompleteProjectOpen"), exception);
        }
    }

    private PackArchiveInfo? TryLoadSavedPack(string? path, PackArchiveKind kind)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            return PackArchiveInspectorService.Inspect(path, kind);
        }
        catch
        {
            return null;
        }
    }

    private void SaveProjectClick(object sender, RoutedEventArgs e)
    {
        SaveProject(forceChoosePath: false);
    }

    private bool SaveProject(bool forceChoosePath)
    {
        var path = forceChoosePath ? null : currentProjectPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            var dialog = new SaveFileDialog
            {
                Title = Loc.Get("DialogSaveCompleteProject"),
                Filter = Loc.Get("FilterSaveCompleteProject"),
                DefaultExt = ".acmp",
                AddExtension = true,
                FileName = MakeSafeFileName(PackNameTextBox.Text) + ".acmp"
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
            CompletePackProjectService.Save(
                path,
                currentPackId!,
                PackNameTextBox.Text,
                AuthorTextBox.Text,
                VersionTextBox.Text,
                DescriptionTextBox.Text,
                visualPack?.SourcePath,
                loginPack?.SourcePath,
                soundPack?.SourcePath,
                colorPack?.SourcePath);

            currentProjectPath = path;
            isDirty = false;
            SetStatus(Loc.Format("StatusProjectSaved", Path.GetFileName(path)));
            return true;
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorCompleteProjectSave"), exception);
            return false;
        }
    }

    private void ChooseVisualPackClick(object sender, RoutedEventArgs e) => ChoosePack(PackArchiveKind.Visual);
    private void ChooseLoginPackClick(object sender, RoutedEventArgs e) => ChoosePack(PackArchiveKind.Login);
    private void ChooseSoundPackClick(object sender, RoutedEventArgs e) => ChoosePack(PackArchiveKind.Sound);
    private void ChooseColorPackClick(object sender, RoutedEventArgs e) => ChoosePack(PackArchiveKind.Color);

    private void ClearVisualPackClick(object sender, RoutedEventArgs e) => ClearPack(PackArchiveKind.Visual);
    private void ClearLoginPackClick(object sender, RoutedEventArgs e) => ClearPack(PackArchiveKind.Login);
    private void ClearSoundPackClick(object sender, RoutedEventArgs e) => ClearPack(PackArchiveKind.Sound);
    private void ClearColorPackClick(object sender, RoutedEventArgs e) => ClearPack(PackArchiveKind.Color);

    private void ChoosePack(PackArchiveKind kind)
    {
        var dialog = new OpenFileDialog
        {
            Title = Loc.Format("DialogSelectCompletePack", GetKindDisplayName(kind)),
            Filter = Loc.Get("FilterCompletePackZip")
        };

        if (dialog.ShowDialog(OwnerWindow) != true)
        {
            return;
        }

        try
        {
            var info = PackArchiveInspectorService.Inspect(dialog.FileName, kind);
            SetPack(kind, info);
            MarkDirty();
            SetStatus(Loc.Format("CompleteStatusLoaded", info.Name, GetKindDisplayName(kind)));
            RefreshUi();
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorCompletePackSelect"), exception);
        }
    }

    private void ClearPack(PackArchiveKind kind)
    {
        SetPack(kind, null);
        MarkDirty();
        SetStatus(Loc.Format("CompleteStatusCleared", GetKindDisplayName(kind)));
        RefreshUi();
    }

    private void SetPack(PackArchiveKind kind, PackArchiveInfo? info)
    {
        switch (kind)
        {
            case PackArchiveKind.Visual:
                visualPack = info;
                break;
            case PackArchiveKind.Login:
                loginPack = info;
                break;
            case PackArchiveKind.Sound:
                soundPack = info;
                break;
            case PackArchiveKind.Color:
                colorPack = info;
                break;
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
            Title = Loc.Get("DialogExportCompletePack"),
            Filter = Loc.Get("FilterCompletePackZip"),
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
            Mouse.OverrideCursor = Cursors.Wait;
            EnsurePackId();
            SetStatus(Loc.Get("CompleteStatusGeneratingPack"));

            CompletePackExportService.Export(
                dialog.FileName,
                currentPackId!,
                PackNameTextBox.Text,
                AuthorTextBox.Text,
                VersionTextBox.Text,
                DescriptionTextBox.Text,
                visualPack!,
                loginPack!,
                soundPack,
                colorPack,
                (_, name) => SetStatus(Loc.Format("CompleteStatusGeneratingItem", name)));

            SetStatus(Loc.Format("CompleteStatusPackExported", Path.GetFileName(dialog.FileName)));
            MessageBox.Show(
                OwnerWindow,
                Loc.Format("CompleteExportSuccessMessage", dialog.FileName),
                Loc.Get("ExportCompleteTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorCompletePackExport"), exception);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }


    private void ShareCommunityPackClick(object sender, RoutedEventArgs e)
    {
        const string submissionUrl = "https://github.com/Mike-Aniki/AnikiCommunityPacks/issues/new?template=complete-pack-submission.yml";

        try
        {
            Process.Start(new ProcessStartInfo(submissionUrl)
            {
                UseShellExecute = true
            });
            SetStatus(Loc.Get("StatusCommunitySubmissionOpened"));
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
            errors.Add(Loc.Get("CompleteValidationEnterPackName"));
        }

        if (string.IsNullOrWhiteSpace(VersionTextBox.Text))
        {
            errors.Add(Loc.Get("CompleteValidationEnterVersion"));
        }
        else if (!IsValidPackVersion(VersionTextBox.Text))
        {
            errors.Add(Loc.Get("ValidationInvalidVersion"));
        }

        if (visualPack is null)
        {
            errors.Add(Loc.Get("CompleteValidationVisualRequired"));
        }

        if (loginPack is null)
        {
            errors.Add(Loc.Get("CompleteValidationLoginRequired"));
        }

        foreach (var pack in new[] { visualPack, loginPack, soundPack, colorPack }.Where(item => item is not null))
        {
            try
            {
                PackArchiveInspectorService.Inspect(pack!.SourcePath, pack.Kind);
            }
            catch (Exception exception)
            {
                errors.Add($"{GetKindDisplayName(pack!.Kind)}: {exception.Message}");
            }
        }

        if (errors.Count == 0)
        {
            SetStatus(Loc.Get("CompleteStatusProjectValid"));
            return true;
        }

        SetStatus(Loc.Get("StatusValidationFailed"));
        MessageBox.Show(
            OwnerWindow,
            string.Join("\n\n", errors),
            Loc.Get("CompleteValidationIncompleteTitle"),
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return false;
    }

    private void MetadataTextChanged(object sender, TextChangedEventArgs e)
    {
        MarkDirty();
    }

    private void RefreshUi()
    {
        SetSlotText(visualPack, VisualNameText, VisualMetaText, required: true);
        SetSlotText(loginPack, LoginNameText, LoginMetaText, required: true);
        SetSlotText(soundPack, SoundNameText, SoundMetaText, required: false);
        SetSlotText(colorPack, ColorNameText, ColorMetaText, required: false);

        VisualSummaryText.Text = BuildSummaryLine(Loc.Get("CompleteVisualPack"), visualPack, required: true);
        LoginSummaryText.Text = BuildSummaryLine(Loc.Get("CompleteLoginPack"), loginPack, required: true);
        SoundSummaryText.Text = BuildSummaryLine(Loc.Get("CompleteSoundPack"), soundPack, required: false);
        ColorSummaryText.Text = BuildSummaryLine(Loc.Get("CompleteColorPack"), colorPack, required: false);
    }

    private static void SetSlotText(PackArchiveInfo? info, TextBlock nameText, TextBlock metaText, bool required)
    {
        if (info is null)
        {
            nameText.Text = Loc.Get("CompleteNoPackSelected");
            metaText.Text = Loc.Get(required ? "CompleteChooseZipHint" : "CompleteOptionalZipHint");
            return;
        }

        nameText.Text = info.Name;
        var author = string.IsNullOrWhiteSpace(info.Author) ? Loc.Get("CompleteUnknownAuthor") : info.Author;
        metaText.Text = $"{author}  •  v{info.Version}  •  {FormatBytes(info.FileSizeBytes)}";
    }

    private static string BuildSummaryLine(string label, PackArchiveInfo? info, bool required)
    {
        if (info is not null)
        {
            return $"✓ {label} — {info.Name}";
        }

        return required
            ? $"✕ {label} — {Loc.Get("CompleteSummaryMissingRequired")}"
            : $"— {label} — {Loc.Get("CompleteSummaryOptionalNotIncluded")}";
    }

    private string GetKindDisplayName(PackArchiveKind kind)
    {
        return kind switch
        {
            PackArchiveKind.Visual => Loc.Get("CompleteVisualPack"),
            PackArchiveKind.Login => Loc.Get("CompleteLoginPack"),
            PackArchiveKind.Sound => Loc.Get("CompleteSoundPack"),
            PackArchiveKind.Color => Loc.Get("CompleteColorPack"),
            _ => kind.ToString()
        };
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
            .Normalize(System.Text.NormalizationForm.FormD);
        var builder = new System.Text.StringBuilder();

        foreach (var character in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) ==
                System.Globalization.UnicodeCategory.NonSpacingMark)
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
            slug = "custom-complete-pack";
        }

        return $"{slug}-{Guid.NewGuid().ToString("N")[..8]}";
    }

    private static bool IsValidPackVersion(string version)
    {
        return Regex.IsMatch(
            version.Trim(),
            @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$");
    }

    private static string MakeSafeFileName(string value)
    {
        var result = string.IsNullOrWhiteSpace(value) ? "CompletePack" : value.Trim();
        foreach (var character in Path.GetInvalidFileNameChars())
        {
            result = result.Replace(character, '_');
        }

        return string.IsNullOrWhiteSpace(result) ? "CompletePack" : result;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024L * 1024L)
        {
            return $"{bytes / 1024d:0.0} KB";
        }

        return $"{bytes / (1024d * 1024d):0.0} MB";
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

    private void SetStatus(string text)
    {
        StatusTextBlock.Text = text;
    }

    private void ShowError(string message, Exception exception)
    {
        SetStatus(message + " " + exception.Message);
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
}
