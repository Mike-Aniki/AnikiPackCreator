using AnikiVisualPackCreator.Models;
using AnikiVisualPackCreator.Services;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AnikiVisualPackCreator;

public partial class LoginPackWindow : UserControl
{
    private string? currentProjectPath;
    private string? currentPackId;
    private string? videoSourcePath;
    private LoginPackMediaInfo? currentMediaInfo;
    private bool isDirty;
    private bool suppressDirtyState;

    public LoginPackWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => InitializeDefaultState();
    }

    private void InitializeDefaultState()
    {
        if (!string.IsNullOrWhiteSpace(PackNameTextBox.Text))
        {
            RefreshUi();
            return;
        }

        suppressDirtyState = true;
        PackNameTextBox.Text = Loc.Get("DefaultLoginPackName");
        VersionTextBox.Text = Loc.Get("DefaultVersion");
        DescriptionTextBox.Text = string.Empty;
        AuthorTextBox.Text = string.Empty;
        suppressDirtyState = false;
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
        PackNameTextBox.Text = Loc.Get("DefaultLoginPackName");
        AuthorTextBox.Text = string.Empty;
        VersionTextBox.Text = Loc.Get("DefaultVersion");
        DescriptionTextBox.Text = string.Empty;
        videoSourcePath = null;
        currentMediaInfo = null;
        StopPreview();
        suppressDirtyState = false;
        isDirty = false;
        SetStatus(Loc.Get("LoginStatusNewProject"));
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
            Title = Loc.Get("DialogOpenLoginProject"),
            Filter = Loc.Get("FilterOpenLoginProject")
        };

        if (dialog.ShowDialog(OwnerWindow) != true)
        {
            return;
        }

        try
        {
            suppressDirtyState = true;
            var document = LoginPackProjectService.Load(dialog.FileName);
            currentProjectPath = dialog.FileName;
            currentPackId = string.IsNullOrWhiteSpace(document.PackId) ? null : document.PackId;
            PackNameTextBox.Text = string.IsNullOrWhiteSpace(document.PackName) ? Loc.Get("DefaultLoginPackName") : document.PackName;
            AuthorTextBox.Text = document.Author ?? string.Empty;
            VersionTextBox.Text = string.IsNullOrWhiteSpace(document.Version) ? Loc.Get("DefaultVersion") : document.Version;
            DescriptionTextBox.Text = document.Description ?? string.Empty;

            videoSourcePath = null;
            currentMediaInfo = null;
            StopPreview();

            if (!string.IsNullOrWhiteSpace(document.VideoSourcePath) && File.Exists(document.VideoSourcePath))
            {
                LoadVideo(document.VideoSourcePath, updateStatus: false, markDirty: false);
            }
            else if (!string.IsNullOrWhiteSpace(document.VideoSourcePath))
            {
                SetStatus(Loc.Format("LoginStatusVideoMissingOnOpen", Path.GetFileName(document.VideoSourcePath)));
            }

            suppressDirtyState = false;
            isDirty = false;
            if (string.IsNullOrWhiteSpace(StatusTextBlock.Text) ||
                string.Equals(StatusTextBlock.Text, Loc.Get("LoginStatusSelect"), StringComparison.Ordinal))
            {
                SetStatus(Loc.Format("StatusProjectOpened", Path.GetFileName(dialog.FileName)));
            }

            RefreshUi();
        }
        catch (Exception exception)
        {
            suppressDirtyState = false;
            ShowError(Loc.Get("ErrorLoginProjectOpen"), exception);
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
                Title = Loc.Get("DialogSaveLoginProject"),
                Filter = Loc.Get("FilterSaveLoginProject"),
                DefaultExt = ".alpc",
                AddExtension = true,
                FileName = MakeSafeFileName(PackNameTextBox.Text) + ".alpc"
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
            LoginPackProjectService.Save(
                path,
                currentPackId!,
                PackNameTextBox.Text,
                AuthorTextBox.Text,
                VersionTextBox.Text,
                DescriptionTextBox.Text,
                videoSourcePath);

            currentProjectPath = path;
            isDirty = false;
            SetStatus(Loc.Format("StatusProjectSaved", Path.GetFileName(path)));
            return true;
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorLoginProjectSave"), exception);
            return false;
        }
    }

    private void SelectVideoClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = Loc.Get("DialogSelectLoginVideo"),
            Filter = Loc.Get("FilterMp4Files")
        };

        if (dialog.ShowDialog(OwnerWindow) != true)
        {
            return;
        }

        try
        {
            LoadVideo(dialog.FileName, updateStatus: true, markDirty: true);
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorUnsupportedLoginVideo"), exception);
        }
    }

    private void ClearVideoClick(object sender, RoutedEventArgs e)
    {
        videoSourcePath = null;
        currentMediaInfo = null;
        StopPreview();
        MarkDirty();
        RefreshUi();
        SetStatus(Loc.Get("LoginStatusCleared"));
    }

    private void RestartPreviewClick(object sender, RoutedEventArgs e)
    {
        if (!HasAssignedVideo())
        {
            return;
        }

        StartPreview();
        SetStatus(Loc.Get("LoginStatusPreviewing"));
    }

    private void ExportZipClick(object sender, RoutedEventArgs e)
    {
        if (!ValidateProject())
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = Loc.Get("DialogExportLoginPack"),
            Filter = Loc.Get("FilterLoginPackZip"),
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
            SetStatus(Loc.Get("LoginStatusGeneratingPack"));
            EnsurePackId();

            LoginPackExportService.Export(
                dialog.FileName,
                currentPackId!,
                PackNameTextBox.Text,
                AuthorTextBox.Text,
                VersionTextBox.Text,
                DescriptionTextBox.Text,
                videoSourcePath!,
                (_, name) => SetStatus(Loc.Format("LoginStatusGeneratingItem", name)));

            SetStatus(Loc.Format("LoginStatusPackExported", Path.GetFileName(dialog.FileName)));
            MessageBox.Show(
                OwnerWindow,
                Loc.Format("LoginExportSuccessMessage", dialog.FileName),
                Loc.Get("ExportCompleteTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorLoginPackExport"), exception);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }


    private void ShareCommunityPackClick(object sender, RoutedEventArgs e)
    {
        const string submissionUrl = "https://github.com/Mike-Aniki/AnikiCommunityPacks/issues/new?template=login-pack-submission.yml";

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
            errors.Add(Loc.Get("LoginValidationEnterPackName"));
        }

        if (string.IsNullOrWhiteSpace(VersionTextBox.Text))
        {
            errors.Add(Loc.Get("LoginValidationEnterVersion"));
        }
        else if (!IsValidPackVersion(VersionTextBox.Text))
        {
            errors.Add(Loc.Get("ValidationInvalidVersion"));
        }

        if (!HasAssignedVideo())
        {
            errors.Add(Loc.Get("LoginValidationSelectVideo"));
        }
        else
        {
            try
            {
                currentMediaInfo = LoginPackExportService.ValidateVideoFile(videoSourcePath!);
            }
            catch (Exception exception)
            {
                errors.Add(exception.Message);
            }
        }

        RefreshUi();

        if (errors.Count == 0)
        {
            SetStatus(Loc.Get("LoginStatusProjectValid"));
            return true;
        }

        SetStatus(Loc.Get("StatusValidationFailed"));
        MessageBox.Show(
            OwnerWindow,
            string.Join("\n\n", errors),
            Loc.Get("LoginValidationIncompleteTitle"),
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return false;
    }

    private void VideoDropBorderDragOver(object sender, DragEventArgs e)
    {
        e.Effects = HasSupportedDroppedVideo(e.Data)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void VideoDropBorderDrop(object sender, DragEventArgs e)
    {
        if (!HasSupportedDroppedVideo(e.Data))
        {
            return;
        }

        var files = e.Data.GetData(DataFormats.FileDrop) as string[];
        var path = files![0];
        try
        {
            LoadVideo(path, updateStatus: true, markDirty: true);
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorUnsupportedLoginVideo"), exception);
        }
    }

    private void PreviewPlayerMediaEnded(object sender, RoutedEventArgs e)
    {
        try
        {
            PreviewPlayer.Position = TimeSpan.Zero;
            PreviewPlayer.Play();
        }
        catch
        {
        }
    }

    private void PreviewPlayerMediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        SetStatus(Loc.Format("ErrorLoginPreview", e.ErrorException?.Message ?? string.Empty));
    }

    private void MetadataTextChanged(object sender, TextChangedEventArgs e)
    {
        MarkDirty();
    }

    private void LoadVideo(string path, bool updateStatus, bool markDirty)
    {
        var info = LoginPackExportService.ValidateVideoFile(path);
        videoSourcePath = path;
        currentMediaInfo = info;

        if (markDirty)
        {
            MarkDirty();
        }

        RefreshUi();
        StartPreview();

        if (updateStatus)
        {
            SetStatus(Loc.Format("LoginStatusLoaded", info.FileName));
        }
    }

    private void StartPreview()
    {
        if (!HasAssignedVideo())
        {
            return;
        }

        try
        {
            PreviewPlayer.Stop();
            PreviewPlayer.Source = new Uri(videoSourcePath!, UriKind.Absolute);
            PreviewPlayer.IsMuted = true;
            PreviewPlayer.Position = TimeSpan.Zero;
            PreviewPlayer.Play();
        }
        catch (Exception exception)
        {
            SetStatus(Loc.Format("ErrorLoginPreview", exception.Message));
        }
    }

    private void StopPreview()
    {
        try
        {
            PreviewPlayer.Stop();
            PreviewPlayer.Source = null;
        }
        catch
        {
        }
    }

    private void RefreshUi()
    {
        var hasVideo = HasAssignedVideo();
        PreviewPlaceholder.Visibility = hasVideo ? Visibility.Collapsed : Visibility.Visible;
        PreviewPlayer.Visibility = hasVideo ? Visibility.Visible : Visibility.Collapsed;
        ClearVideoButton.IsEnabled = hasVideo;
        RestartPreviewButton.IsEnabled = hasVideo;

        if (!hasVideo || currentMediaInfo is null)
        {
            SelectedFileTextBlock.Text = Loc.Get("LoginPreviewStatusEmpty");
            SelectedFileInfoTextBlock.Text = Loc.Get("LoginDropHint");
            VideoCodecValueTextBlock.Text = "—";
            ResolutionValueTextBlock.Text = "—";
            DurationValueTextBlock.Text = "—";
            SizeValueTextBlock.Text = "—";
            AudioValueTextBlock.Text = "—";
            CompatibilityValueTextBlock.Text = "—";
            CompatibilityValueTextBlock.Foreground = Brushes.White;
            return;
        }

        SelectedFileTextBlock.Text = currentMediaInfo.FileName;
        SelectedFileInfoTextBlock.Text = string.Format(
            "{0}  •  {1}",
            currentMediaInfo.VideoCodecDisplay,
            Mp4InspectorService.FormatBytes(currentMediaInfo.FileSizeBytes));
        VideoCodecValueTextBlock.Text = currentMediaInfo.VideoCodecDisplay;
        ResolutionValueTextBlock.Text = currentMediaInfo.Width > 0 && currentMediaInfo.Height > 0
            ? $"{currentMediaInfo.Width} × {currentMediaInfo.Height}"
            : "—";
        DurationValueTextBlock.Text = Mp4InspectorService.FormatDuration(currentMediaInfo.Duration);
        SizeValueTextBlock.Text = Mp4InspectorService.FormatBytes(currentMediaInfo.FileSizeBytes);
        AudioValueTextBlock.Text = currentMediaInfo.HasAudioTrack
            ? string.IsNullOrWhiteSpace(currentMediaInfo.AudioCodecDisplay)
                ? Loc.Get("LoginSummaryAudioPresent")
                : string.Format(Loc.Get("LoginSummaryAudioPresentWithCodec"), currentMediaInfo.AudioCodecDisplay)
            : Loc.Get("LoginSummaryAudioNone");
        CompatibilityValueTextBlock.Text = Loc.Get("LoginSummaryCompatibilityOk");
        CompatibilityValueTextBlock.Foreground = BrushFromHex("#FFB7F0CB");
    }

    private bool HasAssignedVideo()
    {
        return !string.IsNullOrWhiteSpace(videoSourcePath) && File.Exists(videoSourcePath);
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
            slug = "custom-login-video-pack";
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
        var result = string.IsNullOrWhiteSpace(value) ? "LoginVideoPack" : value.Trim();
        foreach (var character in Path.GetInvalidFileNameChars())
        {
            result = result.Replace(character, '_');
        }

        return string.IsNullOrWhiteSpace(result) ? "LoginVideoPack" : result;
    }

    private static bool HasSupportedDroppedVideo(IDataObject data)
    {
        if (!data.GetDataPresent(DataFormats.FileDrop))
        {
            return false;
        }

        var files = data.GetData(DataFormats.FileDrop) as string[];
        return files is { Length: > 0 }
               && File.Exists(files[0])
               && string.Equals(Path.GetExtension(files[0]), ".mp4", StringComparison.OrdinalIgnoreCase);
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

    private static Brush BrushFromHex(string hex)
    {
        var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex)!;
        return new System.Windows.Media.SolidColorBrush(color);
    }

    public bool HasUnsavedChanges => isDirty;

    public bool ConfirmClose()
    {
        return ConfirmDiscardChanges();
    }

    public void Activate()
    {
        if (HasAssignedVideo())
        {
            StartPreview();
        }
    }

    public void Deactivate()
    {
        StopPreview();
    }

    public void OnHostClosed()
    {
        StopPreview();
    }

    private Window OwnerWindow => Window.GetWindow(this)
        ?? Application.Current.MainWindow
        ?? throw new InvalidOperationException("Aniki Pack Creator host window is not available.");
}
