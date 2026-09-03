using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;

namespace AnikiVisualPackCreator.Models;

public sealed class SoundPackSoundState : INotifyPropertyChanged
{
    private string? sourcePath;

    public SoundPackSoundState(SoundPackSoundDefinition definition)
    {
        Definition = definition;
    }

    public SoundPackSoundDefinition Definition { get; }
    public string Key => Definition.Key;
    public string TargetPath => Definition.TargetPath;
    public string DisplayName => Definition.DisplayName;
    public string SectionName => Definition.SectionName;
    public string CategoryName => Definition.CategoryName;
    public string ContextName => string.IsNullOrWhiteSpace(CategoryName) ? SectionName : CategoryName;
    public SoundPackAudioFormat Format => Definition.Format;
    public bool IsMusic => Definition.IsMusic;
    public string SelectButtonText => Definition.SelectButtonText;
    public string DropHintText => Definition.DropHintText;
    public string FormatDescription => Definition.FormatDescription;

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
                OnPropertyChanged(nameof(SourceInfoText));
            }
        }
    }

    public bool IsReady => !string.IsNullOrWhiteSpace(SourcePath) && File.Exists(SourcePath);
    public string StatusText => IsReady ? Loc.Get("SoundAssigned") : Loc.Get("SoundOptional");
    public string SourceFileName => IsReady
        ? Path.GetFileName(SourcePath) ?? Definition.NoFileSelectedText
        : Definition.NoFileSelectedText;

    public string SourceInfoText
    {
        get
        {
            if (!IsReady)
            {
                return Definition.NoFileSelectedText;
            }

            try
            {
                var size = new FileInfo(SourcePath!).Length;
                return $"{SourceFileName}  •  {FormatBytes(size)}";
            }
            catch
            {
                return SourceFileName;
            }
        }
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

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024L * 1024L)
        {
            return $"{bytes / 1024d:0.0} KB";
        }

        return $"{bytes / (1024d * 1024d):0.0} MB";
    }
}
