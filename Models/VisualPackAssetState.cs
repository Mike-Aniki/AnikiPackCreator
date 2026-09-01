using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;

namespace AnikiVisualPackCreator.Models;

public sealed class VisualPackAssetState : INotifyPropertyChanged
{
    private string? sourcePath;
    private double zoom = 1.0;
    private double panX;
    private double panY;

    public VisualPackAssetState(VisualPackAssetDefinition definition)
    {
        Definition = definition;
    }

    public VisualPackAssetDefinition Definition { get; }
    public string FileName => Definition.FileName;
    public string DisplayName => Definition.DisplayName;
    public string DimensionText => Definition.DimensionText;
    public string Description => Definition.Description;

    public string? SourcePath
    {
        get => sourcePath;
        set
        {
            if (SetField(ref sourcePath, value))
            {
                OnPropertyChanged(nameof(IsReady));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(SourceFileName));
            }
        }
    }

    public bool IsReady => !string.IsNullOrWhiteSpace(SourcePath) && File.Exists(SourcePath);
    public string StatusText => IsReady ? Loc.Get("AssetReady") : Loc.Get("AssetMissing");
    public string SourceFileName => IsReady
        ? Path.GetFileName(SourcePath) ?? Loc.Get("NoSourceImageSelected")
        : Loc.Get("NoSourceImageSelected");

    public double Zoom
    {
        get => zoom;
        set => SetField(ref zoom, Math.Clamp(value, 0.75, 2.0));
    }

    public double PanX
    {
        get => panX;
        set => SetField(ref panX, Math.Clamp(value, -1.0, 1.0));
    }

    public double PanY
    {
        get => panY;
        set => SetField(ref panY, Math.Clamp(value, -1.0, 1.0));
    }

    public void ResetCrop()
    {
        Zoom = 1.0;
        PanX = 0.0;
        PanY = 0.0;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
