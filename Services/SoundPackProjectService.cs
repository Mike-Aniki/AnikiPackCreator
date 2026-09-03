using AnikiVisualPackCreator.Models;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using System.IO;
using System.Text.Json;

namespace AnikiVisualPackCreator.Services;

public static class SoundPackProjectService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static void Save(
        string projectPath,
        string packId,
        string packName,
        string author,
        string version,
        string description,
        IEnumerable<SoundPackSoundState> sounds)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory;
        Directory.CreateDirectory(projectDirectory);

        var document = new SoundPackProjectDocument
        {
            PackId = packId?.Trim() ?? string.Empty,
            PackName = packName?.Trim() ?? string.Empty,
            Author = author?.Trim() ?? string.Empty,
            Version = string.IsNullOrWhiteSpace(version) ? "1.0.0" : version.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Sounds = sounds.Select(sound => new SoundPackSoundProjectState
            {
                Key = sound.Key,
                SourcePath = MakePortablePath(projectDirectory, sound.SourcePath)
            }).ToList()
        };

        File.WriteAllText(projectPath, JsonSerializer.Serialize(document, JsonOptions));
    }

    public static SoundPackProjectDocument Load(string projectPath)
    {
        var json = File.ReadAllText(projectPath);
        var document = JsonSerializer.Deserialize<SoundPackProjectDocument>(json, JsonOptions)
            ?? throw new InvalidDataException(Loc.Get("ServiceSoundProjectEmptyInvalid"));

        if (document.FormatVersion != 1)
        {
            throw new InvalidDataException(Loc.Format("ServiceUnsupportedFormatVersion", document.FormatVersion));
        }

        document.PackId ??= string.Empty;
        document.PackName ??= string.Empty;
        document.Author ??= string.Empty;
        document.Version = string.IsNullOrWhiteSpace(document.Version) ? "1.0.0" : document.Version.Trim();
        document.Description ??= string.Empty;
        document.Sounds ??= [];

        var projectDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory;
        foreach (var sound in document.Sounds)
        {
            if (!string.IsNullOrWhiteSpace(sound.SourcePath) && !Path.IsPathRooted(sound.SourcePath))
            {
                sound.SourcePath = Path.GetFullPath(Path.Combine(projectDirectory, sound.SourcePath));
            }
        }

        return document;
    }

    private static string? MakePortablePath(string projectDirectory, string? sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return null;
        }

        try
        {
            return Path.GetRelativePath(projectDirectory, sourcePath);
        }
        catch
        {
            return sourcePath;
        }
    }
}
