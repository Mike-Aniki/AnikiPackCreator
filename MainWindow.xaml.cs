using AnikiVisualPackCreator.Models;
using AnikiVisualPackCreator.Services;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using Microsoft.Win32;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AnikiVisualPackCreator;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaCaptionColor = 35;

    private readonly DispatcherTimer previewTimer;
    private string? cachedSourcePath;
    private BitmapSource? cachedSourceBitmap;
    private VisualPackAssetState? selectedAsset;
    private BitmapSource? previewBitmap;
    private BitmapSource? previewOverlayBitmap;
    private bool showUiPreview = true;
    private string statusText = Loc.Get("StatusSelectOrDrop");
    private double exportProgress;
    private string? currentProjectPath;
    private string currentPackId = string.Empty;
    private bool isDraggingPreview;
    private Point dragStartPoint;
    private double dragStartPanX;
    private double dragStartPanY;
    private bool suppressDirtyState = true;
    private bool isDirty;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ApplyDarkTitleBar();

        Assets = new ObservableCollection<VisualPackAssetState>(
            VisualPackAssetDefinition.All.Select(definition => new VisualPackAssetState(definition)));

        foreach (var asset in Assets)
        {
            asset.PropertyChanged += AssetPropertyChanged;
        }

        previewTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(70)
        };
        previewTimer.Tick += (_, _) =>
        {
            previewTimer.Stop();
            UpdatePreview();
        };

        DataContext = this;
        SelectedAsset = Assets.FirstOrDefault();
        suppressDirtyState = false;
        isDirty = false;
        Loaded += (_, _) =>
        {
            UpdatePreviewViewportSize();
            UpdatePreview();
        };
    }

    private void ApplyDarkTitleBar()
    {
        try
        {
            var windowHandle = new WindowInteropHelper(this).Handle;
            var enabled = 1;
            var result = DwmSetWindowAttribute(
                windowHandle,
                DwmwaUseImmersiveDarkMode,
                ref enabled,
                sizeof(int));

            if (result != 0)
            {
                DwmSetWindowAttribute(
                    windowHandle,
                    DwmwaUseImmersiveDarkModeBefore20H1,
                    ref enabled,
                    sizeof(int));
            }

            var captionColor = 0x00221B15;
            DwmSetWindowAttribute(
                windowHandle,
                DwmwaCaptionColor,
                ref captionColor,
                sizeof(int));
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize);

    public ObservableCollection<VisualPackAssetState> Assets { get; }

    public VisualPackAssetState? SelectedAsset
    {
        get => selectedAsset;
        set
        {
            if (ReferenceEquals(selectedAsset, value))
            {
                return;
            }

            selectedAsset = value;
            OnPropertyChanged();
            UpdatePreviewOverlay();
            UpdatePreviewViewportSize();
            QueuePreviewUpdate();
        }
    }

    public BitmapSource? PreviewBitmap
    {
        get => previewBitmap;
        private set
        {
            previewBitmap = value;
            OnPropertyChanged();
        }
    }

    public BitmapSource? PreviewOverlayBitmap
    {
        get => previewOverlayBitmap;
        private set
        {
            previewOverlayBitmap = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasPreviewOverlay));
        }
    }

    public bool HasPreviewOverlay => PreviewOverlayBitmap is not null;

    public bool ShowUiPreview
    {
        get => showUiPreview;
        set
        {
            if (showUiPreview == value)
            {
                return;
            }

            showUiPreview = value;
            OnPropertyChanged();
            QueuePreviewUpdate();
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SelectImageClick(object sender, RoutedEventArgs e)
    {
        if (SelectedAsset is null)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = Loc.Format("DialogSelectSourceImage", SelectedAsset.DisplayName),
            Filter = Loc.Get("FilterImageFiles"),
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            SetSelectedSource(dialog.FileName);
        }
    }

    private void ClearImageClick(object sender, RoutedEventArgs e)
    {
        if (SelectedAsset is null)
        {
            return;
        }

        SelectedAsset.SourcePath = null;
        SelectedAsset.ResetCrop();
        ClearPreviewSourceCache();
        PreviewBitmap = null;
        StatusText = Loc.Format("StatusImageCleared", SelectedAsset.DisplayName);
    }

    private void FillMissingClick(object sender, RoutedEventArgs e)
    {
        if (SelectedAsset is null || !SelectedAsset.IsReady)
        {
            MessageBox.Show(
                this,
                Loc.Get("FillMissingSelectSource"),
                "Aniki Pack Creator",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var filledCount = 0;
        foreach (var asset in Assets.Where(asset => !asset.IsReady))
        {
            asset.SourcePath = SelectedAsset.SourcePath;
            asset.ResetCrop();
            filledCount++;
        }

        StatusText = filledCount == 0
            ? Loc.Get("StatusNoMissingSlots")
            : Loc.Format("StatusFilledMissingSlots", filledCount, SelectedAsset.SourceFileName);
    }

    private void ResetCropClick(object sender, RoutedEventArgs e)
    {
        SelectedAsset?.ResetCrop();
    }

    private void PreviewHostSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdatePreviewViewportSize();
    }

    private void PreviewImageMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (SelectedAsset is null || !SelectedAsset.IsReady)
        {
            return;
        }

        isDraggingPreview = true;
        dragStartPoint = e.GetPosition(PreviewViewport);
        dragStartPanX = SelectedAsset.PanX;
        dragStartPanY = SelectedAsset.PanY;
        PreviewViewport.CaptureMouse();
        e.Handled = true;
    }

    private void PreviewImageMouseMove(object sender, MouseEventArgs e)
    {
        if (!isDraggingPreview || SelectedAsset is null)
        {
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            isDraggingPreview = false;
            PreviewViewport.ReleaseMouseCapture();
            return;
        }

        var point = e.GetPosition(PreviewViewport);
        var width = Math.Max(1.0, PreviewViewport.ActualWidth);
        var height = Math.Max(1.0, PreviewViewport.ActualHeight);
        SelectedAsset.PanX = dragStartPanX + (point.X - dragStartPoint.X) / (width / 2.0);
        SelectedAsset.PanY = dragStartPanY + (point.Y - dragStartPoint.Y) / (height / 2.0);
        e.Handled = true;
    }

    private void PreviewImageMouseUp(object sender, MouseButtonEventArgs e)
    {
        isDraggingPreview = false;
        PreviewViewport.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void PreviewImageMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (SelectedAsset is null || !SelectedAsset.IsReady)
        {
            return;
        }

        var factor = e.Delta > 0 ? 1.10 : 1.0 / 1.10;
        SelectedAsset.Zoom *= factor;
        e.Handled = true;
    }

    private void PreviewImageDragOver(object sender, DragEventArgs e)
    {
        e.Effects = HasSupportedDroppedImage(e.Data) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void PreviewImageDrop(object sender, DragEventArgs e)
    {
        if (!HasSupportedDroppedImage(e.Data))
        {
            return;
        }

        var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        SetSelectedSource(files[0]);
        e.Handled = true;
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
            PackNameTextBox.Text = Loc.Get("DefaultPackName");
            AuthorTextBox.Text = string.Empty;
            VersionTextBox.Text = Loc.Get("DefaultVersion");
            DescriptionTextBox.Text = string.Empty;
            foreach (var asset in Assets)
            {
                asset.SourcePath = null;
                asset.ResetCrop();
            }

            currentProjectPath = null;
            ClearPreviewSourceCache();
            PreviewBitmap = null;
            SelectedAsset = Assets.FirstOrDefault();
            isDirty = false;
            StatusText = Loc.Get("StatusNewProject");
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
            Title = Loc.Get("DialogOpenProject"),
            Filter = Loc.Get("FilterOpenProject"),
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var document = VisualPackProjectService.Load(dialog.FileName);
            suppressDirtyState = true;
            var upgradedLegacyProject = string.IsNullOrWhiteSpace(document.PackId);
            currentPackId = upgradedLegacyProject
                ? CreatePackId(document.PackName)
                : document.PackId.Trim();
            PackNameTextBox.Text = document.PackName;
            AuthorTextBox.Text = document.Author;
            VersionTextBox.Text = string.IsNullOrWhiteSpace(document.Version) ? Loc.Get("DefaultVersion") : document.Version;
            DescriptionTextBox.Text = document.Description ?? string.Empty;

            foreach (var asset in Assets)
            {
                var saved = document.Assets.FirstOrDefault(item =>
                    string.Equals(item.FileName, asset.FileName, StringComparison.OrdinalIgnoreCase));

                asset.SourcePath = saved?.SourcePath;
                asset.Zoom = saved?.Zoom ?? 1.0;
                asset.PanX = saved?.PanX ?? 0.0;
                asset.PanY = saved?.PanY ?? 0.0;
            }

            currentProjectPath = dialog.FileName;
            ClearPreviewSourceCache();
            isDirty = upgradedLegacyProject;
            StatusText = upgradedLegacyProject
                ? Loc.Format("StatusLegacyProjectUpgraded", Path.GetFileName(dialog.FileName))
                : Loc.Format("StatusProjectOpened", Path.GetFileName(dialog.FileName));
            QueuePreviewUpdate();
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorProjectOpen"), exception);
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
                Title = Loc.Get("DialogSaveProject"),
                Filter = Loc.Get("FilterSaveProject"),
                DefaultExt = ".avpc",
                AddExtension = true,
                FileName = MakeSafeFileName(PackNameTextBox.Text) + ".avpc"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return false;
            }

            path = dialog.FileName;
        }

        try
        {
            EnsurePackId();
            VisualPackProjectService.Save(
                path,
                currentPackId,
                PackNameTextBox.Text,
                AuthorTextBox.Text,
                VersionTextBox.Text,
                DescriptionTextBox.Text,
                Assets);
            currentProjectPath = path;
            isDirty = false;
            StatusText = Loc.Format("StatusProjectSaved", Path.GetFileName(path));
            return true;
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorProjectSave"), exception);
            return false;
        }
    }

    private void ExportZipClick(object sender, RoutedEventArgs e)
    {
        if (!ValidateProject(false))
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = Loc.Get("DialogExportPack"),
            Filter = Loc.Get("FilterVisualPackZip"),
            DefaultExt = ".zip",
            AddExtension = true,
            FileName = MakeSafeFileName(PackNameTextBox.Text) + ".zip"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            ExportProgress = 0;
            StatusText = Loc.Get("StatusGeneratingPack");

            EnsurePackId();
            VisualPackExportService.Export(
                dialog.FileName,
                currentPackId,
                PackNameTextBox.Text,
                AuthorTextBox.Text,
                VersionTextBox.Text,
                DescriptionTextBox.Text,
                Assets,
                (step, name) =>
                {
                    ExportProgress = step;
                    StatusText = Loc.Format("StatusGeneratingItem", name);
                    Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
                });

            ExportProgress = Assets.Count;
            StatusText = Loc.Format("StatusPackExported", Path.GetFileName(dialog.FileName));
            MessageBox.Show(
                this,
                Loc.Format("ExportSuccessMessage", dialog.FileName),
                Loc.Get("ExportCompleteTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorPackExport"), exception);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void MetadataTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        MarkDirty();
    }

    private void ShareCommunityPackClick(object sender, RoutedEventArgs e)
    {
        const string submissionUrl = "https://github.com/Mike-Aniki/AnikiCommunityVisualPacks/issues/new?template=visual-pack-submission.yml";

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

    private void AssetPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        MarkDirty();

        if (ReferenceEquals(sender, SelectedAsset))
        {
            QueuePreviewUpdate();
        }
    }

    private void SetSelectedSource(string path)
    {
        if (SelectedAsset is null)
        {
            return;
        }

        try
        {
            var bitmap = ImageRenderService.LoadBitmap(path);
            cachedSourcePath = path;
            cachedSourceBitmap = bitmap;
            SelectedAsset.SourcePath = path;
            SelectedAsset.ResetCrop();
            StatusText = Loc.Format("StatusLoadedForAsset", Path.GetFileName(path), SelectedAsset.DisplayName);
            UpdatePreview();
        }
        catch (Exception exception)
        {
            ShowError(Loc.Get("ErrorUnsupportedImage"), exception);
        }
    }

    private void QueuePreviewUpdate()
    {
        if (!IsLoaded)
        {
            return;
        }

        previewTimer.Stop();
        previewTimer.Start();
    }

    private void UpdatePreview()
    {
        previewTimer.Stop();

        if (SelectedAsset is null || !SelectedAsset.IsReady)
        {
            PreviewBitmap = null;
            return;
        }

        try
        {
            var path = SelectedAsset.SourcePath!;
            var source = cachedSourceBitmap;
            if (source is null || !string.Equals(cachedSourcePath, path, StringComparison.OrdinalIgnoreCase))
            {
                source = ImageRenderService.LoadBitmap(path);
                cachedSourcePath = path;
                cachedSourceBitmap = source;
            }

            const int previewWidth = 720;
            var previewHeight = Math.Max(
                1,
                (int)Math.Round(previewWidth * SelectedAsset.Definition.Height / (double)SelectedAsset.Definition.Width));
            PreviewBitmap = ImageRenderService.RenderThemePreview(
                source,
                previewWidth,
                previewHeight,
                SelectedAsset,
                ShowUiPreview);
        }
        catch (Exception exception)
        {
            PreviewBitmap = null;
            StatusText = Loc.Format("StatusPreviewError", exception.Message);
        }
    }

    private void UpdatePreviewViewportSize()
    {
        if (!IsLoaded || SelectedAsset is null || PreviewHost.ActualWidth <= 0 || PreviewHost.ActualHeight <= 0)
        {
            return;
        }

        var availableWidth = Math.Max(100.0, PreviewHost.ActualWidth - 30.0);
        var availableHeight = Math.Max(100.0, PreviewHost.ActualHeight - 30.0);
        var targetRatio = SelectedAsset.Definition.Width / (double)SelectedAsset.Definition.Height;
        var availableRatio = availableWidth / availableHeight;

        if (availableRatio > targetRatio)
        {
            PreviewViewport.Height = availableHeight;
            PreviewViewport.Width = availableHeight * targetRatio;
        }
        else
        {
            PreviewViewport.Width = availableWidth;
            PreviewViewport.Height = availableWidth / targetRatio;
        }
    }

    private void UpdatePreviewOverlay()
    {
        PreviewOverlayBitmap = null;

        var resourcePath = SelectedAsset?.Definition.PreviewOverlayResource;
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(
                "pack://application:,,,/" + resourcePath.TrimStart('/'),
                UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            PreviewOverlayBitmap = bitmap;
        }
        catch (Exception exception)
        {
            StatusText = Loc.Format("StatusUiPreviewError", exception.Message);
        }
    }

    private bool ValidateProject(bool showSuccessMessage)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(PackNameTextBox.Text))
        {
            errors.Add(Loc.Get("ValidationEnterPackName"));
        }

        if (string.IsNullOrWhiteSpace(VersionTextBox.Text))
        {
            errors.Add(Loc.Get("ValidationEnterVersion"));
        }
        else if (!IsValidPackVersion(VersionTextBox.Text))
        {
            errors.Add(Loc.Get("ValidationInvalidVersion"));
        }

        var missing = Assets.Where(asset => !asset.IsReady).ToList();
        if (missing.Count > 0)
        {
            errors.Add(Loc.Format("ValidationMissingImages", string.Join(", ", missing.Select(asset => asset.FileName))));
        }

        if (errors.Count == 0)
        {
            StatusText = Loc.Format("StatusProjectValid", Assets.Count);
            if (showSuccessMessage)
            {
                MessageBox.Show(
                    this,
                    Loc.Get("ValidationReadyMessage"),
                    Loc.Get("ValidationCompleteTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            return true;
        }

        StatusText = Loc.Get("StatusValidationFailed");
        MessageBox.Show(
            this,
            string.Join("\n\n", errors),
            Loc.Get("ValidationIncompleteTitle"),
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        return false;
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
            slug = "custom-visual-pack";
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

    private bool ConfirmDiscardChanges()
    {
        if (!isDirty)
        {
            return true;
        }

        var result = MessageBox.Show(
            this,
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

    private void ClearPreviewSourceCache()
    {
        cachedSourcePath = null;
        cachedSourceBitmap = null;
    }

    private static bool HasSupportedDroppedImage(IDataObject data)
    {
        if (!data.GetDataPresent(DataFormats.FileDrop))
        {
            return false;
        }

        var files = data.GetData(DataFormats.FileDrop) as string[];
        if (files is null || files.Length == 0 || !File.Exists(files[0]))
        {
            return false;
        }

        var extension = Path.GetExtension(files[0]);
        return new[] { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tif", ".tiff" }
            .Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static string MakeSafeFileName(string value)
    {
        var result = string.IsNullOrWhiteSpace(value) ? "VisualPack" : value.Trim();
        foreach (var character in Path.GetInvalidFileNameChars())
        {
            result = result.Replace(character, '_');
        }

        return string.IsNullOrWhiteSpace(result) ? "VisualPack" : result;
    }

    private void ShowError(string message, Exception exception)
    {
        StatusText = message + " " + exception.Message;
        MessageBox.Show(
            this,
            message + "\n\n" + exception.Message,
            "Aniki Pack Creator",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!ConfirmDiscardChanges())
        {
            e.Cancel = true;
        }

        base.OnClosing(e);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
