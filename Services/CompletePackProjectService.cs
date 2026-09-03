using AnikiVisualPackCreator.Models;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using System.Text.Json;

namespace AnikiVisualPackCreator.Services;

public static class CompletePackProjectService
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
        string? visualPackPath,
        string? loginPackPath,
        string? soundPackPath,
        string? colorPackPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory;
        Directory.CreateDirectory(projectDirectory);

        var document = new CompletePackProjectDocument
        {
            PackId = packId?.Trim() ?? string.Empty,
            PackName = packName?.Trim() ?? string.Empty,
            Author = author?.Trim() ?? string.Empty,
            Version = string.IsNullOrWhiteSpace(version) ? "1.0.0" : version.Trim(),
            Description = description?.Trim() ?? string.Empty,
            VisualPackPath = MakePortablePath(projectDirectory, visualPackPath),
            LoginPackPath = MakePortablePath(projectDirectory, loginPackPath),
            SoundPackPath = MakePortablePath(projectDirectory, soundPackPath),
            ColorPackPath = MakePortablePath(projectDirectory, colorPackPath)
        };

        File.WriteAllText(projectPath, JsonSerializer.Serialize(document, JsonOptions));
    }

    public static CompletePackProjectDocument Load(string projectPath)
    {
        var json = File.ReadAllText(projectPath);
        var document = JsonSerializer.Deserialize<CompletePackProjectDocument>(json, JsonOptions)
            ?? throw new InvalidDataException(Loc.Get("ServiceCompleteProjectEmptyInvalid"));

        if (document.FormatVersion != 1)
        {
            throw new InvalidDataException(Loc.Format("ServiceUnsupportedFormatVersion", document.FormatVersion));
        }

        document.PackId ??= string.Empty;
        document.PackName ??= string.Empty;
        document.Author ??= string.Empty;
        document.Version = string.IsNullOrWhiteSpace(document.Version) ? "1.0.0" : document.Version.Trim();
        document.Description ??= string.Empty;

        var projectDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory;
        document.VisualPackPath = ResolvePortablePath(projectDirectory, document.VisualPackPath);
        document.LoginPackPath = ResolvePortablePath(projectDirectory, document.LoginPackPath);
        document.SoundPackPath = ResolvePortablePath(projectDirectory, document.SoundPackPath);
        document.ColorPackPath = ResolvePortablePath(projectDirectory, document.ColorPackPath);

        return document;
    }

    private static string? ResolvePortablePath(string projectDirectory, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Path.IsPathRooted(value)
            ? value
            : Path.GetFullPath(Path.Combine(projectDirectory, value));
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
