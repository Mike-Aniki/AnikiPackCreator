using AnikiVisualPackCreator.Models;
using AnikiVisualPackCreator.Services;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using Microsoft.Win32;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Threading;

namespace AnikiVisualPackCreator;

public partial class SoundPackWindow : UserControl, INotifyPropertyChanged
{

    private readonly MediaPlayer previewPlayer = new();
    private SoundPackSoundState? selectedSound;
    private string statusText = Loc.Get("SoundStatusSelect");
    private double exportProgress;
    private string? currentProjectPath;
    private string currentPackId = string.Empty;
    private bool suppressDirtyState = true;
    private bool isDirty;

    public SoundPackWindow()
    {
        InitializeComponent();
        Sounds = new ObservableCollection<SoundPackSoundState>(
            SoundPackSoundDefinition.All.Select(definition => new SoundPackSoundState(definition)));

        foreach (var sound in Sounds)
        {
            sound.PropertyChanged += SoundPropertyChanged;
        }

        SoundsView = CollectionViewSource.GetDefaultView(Sounds);
        SoundsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SoundPackSoundState.SectionName)));

        DataContext = this;
        SelectedSound = Sounds.FirstOrDefault();
        suppressDirtyState = false;
        isDirty = false;
    }

    public ObservableCollection<SoundPackSoundState> Sounds { get; }
    public ICollectionView SoundsView { get; }

    public SoundPackSoundState? SelectedSound
    {
        get => selectedSound;
        set
        {
            if (ReferenceEquals(selectedSound, value))
            {
                return;
            }

            StopPreview();
            selectedSound = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => statusText;
        private set
        {
            statusText = value;
            OnPropertyChanged();
        }
    }

    public double ExportProgress
    {
        get => exportProgress;
        private set
        {
            exportProgress = value;
            OnPropertyChanged();
        }
    }

    public string SelectedSoundCountText => Loc.Format(
        "SoundSelectedCountFormat",
        Sounds.Count(sound => sound.IsReady),
        Sounds.Count);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SelectSoundClick(object sender, RoutedEventArgs e)
    {
        if (SelectedSound is null)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = Loc.Format(SelectedSound.IsMusic ? "DialogSelectSourceMusic" : "DialogSelectSourceSound", SelectedSound.DisplayName),
            Filter = Loc.Get(SelectedSound.IsMusic ? "FilterMp3Files" : "FilterWaveFiles"),
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(OwnerWindow) == true)
        {
            SetSelectedSource(dialog.FileName);
        }
    }

    private void ClearSoundClick(object sender, RoutedEventArgs e)
    {
        if (SelectedSound is null)
        {
            return;
        }

        StopPreview();
        SelectedSound.SourcePath = null;
        StatusText = Loc.Format("SoundStatusCleared", SelectedSound.DisplayName);
    }

    private void PreviewSoundClick(object sender, RoutedEventArgs e)
    {
        if (SelectedSound?.IsReady != true)
        {
            return;
        }

        try
        {
            StopPreview();
            SoundPackExportService.ValidateAudioFile(SelectedSound);
            previewPlayer.Open(new Uri(Path.GetFullPath(SelectedSound.SourcePath!), UriKind.Absolute));
            previewPlayer.Volume = 1.0;
            previewPlayer.Play();
            StatusText = Loc.Format("SoundStatusPreviewing", SelectedSound.DisplayName);
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorSoundPreview"), exception);
        }
    }

    private void SoundDropAreaDragOver(object sender, DragEventArgs e)
    {
        e.Effects = HasSupportedDroppedAudio(e.Data, SelectedSound) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void SoundDropAreaDrop(object sender, DragEventArgs e)
    {
        if (!HasSupportedDroppedAudio(e.Data, SelectedSound))
        {
            return;
        }

        var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        SetSelectedSource(files[0]);
        e.Handled = true;
    }

    private void SetSelectedSource(string path)
    {
        if (SelectedSound is null)
        {
            return;
        }

        try
        {
            var previousPath = SelectedSound.SourcePath;
            SelectedSound.SourcePath = path;
            try
            {
                SoundPackExportService.ValidateAudioFile(SelectedSound);
            }
            catch
            {
                SelectedSound.SourcePath = previousPath;
                throw;
            }

            StopPreview();
            StatusText = Loc.Format("SoundStatusLoaded", Path.GetFileName(path), SelectedSound.DisplayName);
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get(SelectedSound.IsMusic ? "ErrorUnsupportedMusic" : "ErrorUnsupportedSound"), exception);
        }
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
            StopPreview();
            currentPackId = string.Empty;
            PackNameTextBox.Text = Loc.Get("DefaultSoundPackName");
            AuthorTextBox.Text = string.Empty;
            VersionTextBox.Text = Loc.Get("DefaultVersion");
            DescriptionTextBox.Text = string.Empty;

            foreach (var sound in Sounds)
            {
                sound.SourcePath = null;
            }

            currentProjectPath = null;
            SelectedSound = Sounds.FirstOrDefault();
            isDirty = false;
            StatusText = Loc.Get("SoundStatusNewProject");
            RefreshSummary();
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
            Title = Loc.Get("DialogOpenSoundProject"),
            Filter = Loc.Get("FilterOpenSoundProject"),
            CheckFileExists = true
        };

        if (dialog.ShowDialog(OwnerWindow) != true)
        {
            return;
        }

        try
        {
            var document = SoundPackProjectService.Load(dialog.FileName);
            suppressDirtyState = true;
            StopPreview();

            var upgradedLegacyProject = string.IsNullOrWhiteSpace(document.PackId);
            currentPackId = upgradedLegacyProject
                ? CreatePackId(document.PackName)
                : document.PackId.Trim();

            PackNameTextBox.Text = document.PackName;
            AuthorTextBox.Text = document.Author;
            VersionTextBox.Text = string.IsNullOrWhiteSpace(document.Version) ? Loc.Get("DefaultVersion") : document.Version;
            DescriptionTextBox.Text = document.Description ?? string.Empty;

            foreach (var sound in Sounds)
            {
                var saved = document.Sounds.FirstOrDefault(item =>
                    string.Equals(item.Key, sound.Key, StringComparison.OrdinalIgnoreCase));
                sound.SourcePath = saved?.SourcePath;
            }

            currentProjectPath = dialog.FileName;
            isDirty = upgradedLegacyProject;
            SelectedSound = Sounds.FirstOrDefault();
            StatusText = upgradedLegacyProject
                ? Loc.Format("SoundStatusLegacyProjectUpgraded", Path.GetFileName(dialog.FileName))
                : Loc.Format("StatusProjectOpened", Path.GetFileName(dialog.FileName));
            RefreshSummary();
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorSoundProjectOpen"), exception);
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
        var path = forceChoosePath ? null : currentProjectPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            var dialog = new SaveFileDialog
            {
                Title = Loc.Get("DialogSaveSoundProject"),
                Filter = Loc.Get("FilterSaveSoundProject"),
                DefaultExt = ".aspc",
                AddExtension = true,
                FileName = MakeSafeFileName(PackNameTextBox.Text) + ".aspc"
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
            SoundPackProjectService.Save(
                path,
                currentPackId,
                PackNameTextBox.Text,
                AuthorTextBox.Text,
                VersionTextBox.Text,
                DescriptionTextBox.Text,
                Sounds);

            currentProjectPath = path;
            isDirty = false;
            StatusText = Loc.Format("StatusProjectSaved", Path.GetFileName(path));
            return true;
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorSoundProjectSave"), exception);
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
            Title = Loc.Get("DialogExportSoundPack"),
            Filter = Loc.Get("FilterSoundPackZip"),
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
            ExportProgress = 0;
            StatusText = Loc.Get("SoundStatusGeneratingPack");
            EnsurePackId();

            SoundPackExportService.Export(
                dialog.FileName,
                currentPackId,
                PackNameTextBox.Text,
                AuthorTextBox.Text,
                VersionTextBox.Text,
                DescriptionTextBox.Text,
                Sounds,
                (step, name) =>
                {
                    ExportProgress = step;
                    StatusText = Loc.Format("SoundStatusGeneratingItem", name);
                    Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
                });

            ExportProgress = Sounds.Count(sound => sound.IsReady);
            StatusText = Loc.Format("SoundStatusPackExported", Path.GetFileName(dialog.FileName));
            MessageBox.Show(
                OwnerWindow,
                Loc.Format("SoundExportSuccessMessage", dialog.FileName),
                Loc.Get("ExportCompleteTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorSoundPackExport"), exception);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }


    private void ShareCommunityPackClick(object sender, RoutedEventArgs e)
    {
        const string submissionUrl = "https://github.com/Mike-Aniki/AnikiCommunityPacks/issues/new?template=sound-pack-submission.yml";

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
            errors.Add(Loc.Get("SoundValidationEnterPackName"));
        }

        if (string.IsNullOrWhiteSpace(VersionTextBox.Text))
        {
            errors.Add(Loc.Get("SoundValidationEnterVersion"));
        }
        else if (!IsValidPackVersion(VersionTextBox.Text))
        {
            errors.Add(Loc.Get("ValidationInvalidVersion"));
        }

        var selected = Sounds.Where(sound => sound.IsReady).ToList();
        if (selected.Count == 0)
        {
            errors.Add(Loc.Get("SoundValidationAtLeastOne"));
        }

        foreach (var sound in selected)
        {
            try
            {
                SoundPackExportService.ValidateAudioFile(sound);
            }
            catch (Exception exception)
            {
                errors.Add($"{sound.DisplayName}: {exception.Message}");
            }
        }

        if (errors.Count == 0)
        {
            StatusText = Loc.Format("SoundStatusProjectValid", selected.Count);
            return true;
        }

        StatusText = Loc.Get("StatusValidationFailed");
        MessageBox.Show(
            OwnerWindow,
            string.Join("\n\n", errors),
            Loc.Get("SoundValidationIncompleteTitle"),
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return false;
    }

    private void MetadataTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        MarkDirty();
    }

    private void SoundPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        MarkDirty();
        RefreshSummary();
    }

    private void RefreshSummary()
    {
        OnPropertyChanged(nameof(SelectedSoundCountText));
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
            slug = "custom-sound-pack";
        }

        var suffix = Guid.NewGuid().ToString("N")[..8];
        return $"{slug}-{suffix}";
    }

    private static bool IsValidPackVersion(string version)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            version.Trim(),
            @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$");
    }

    private static string MakeSafeFileName(string value)
    {
        var result = string.IsNullOrWhiteSpace(value) ? "SoundPack" : value.Trim();
        foreach (var character in Path.GetInvalidFileNameChars())
        {
            result = result.Replace(character, '_');
        }

        return string.IsNullOrWhiteSpace(result) ? "SoundPack" : result;
    }

    private static bool HasSupportedDroppedAudio(IDataObject data, SoundPackSoundState? selectedSound)
    {
        if (selectedSound is null || !data.GetDataPresent(DataFormats.FileDrop))
        {
            return false;
        }

        var files = data.GetData(DataFormats.FileDrop) as string[];
        if (files is not { Length: > 0 } || !File.Exists(files[0]))
        {
            return false;
        }

        var expectedExtension = selectedSound.IsMusic ? ".mp3" : ".wav";
        return string.Equals(Path.GetExtension(files[0]), expectedExtension, StringComparison.OrdinalIgnoreCase);
    }

    private void StopPreview()
    {
        try
        {
            previewPlayer.Stop();
            previewPlayer.Close();
        }
        catch
        {
        }
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

    public bool ConfirmClose()
    {
        return ConfirmDiscardChanges();
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
