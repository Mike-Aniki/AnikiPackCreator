using AnikiVisualPackCreator.Models;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using System.Text.Json;

namespace AnikiVisualPackCreator.Services;

public static class LoginPackProjectService
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
        string? videoSourcePath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory;
        Directory.CreateDirectory(projectDirectory);

        var document = new LoginPackProjectDocument
        {
            PackId = packId?.Trim() ?? string.Empty,
            PackName = packName?.Trim() ?? string.Empty,
            Author = author?.Trim() ?? string.Empty,
            Version = string.IsNullOrWhiteSpace(version) ? "1.0.0" : version.Trim(),
            Description = description?.Trim() ?? string.Empty,
            VideoSourcePath = MakePortablePath(projectDirectory, videoSourcePath)
        };

        File.WriteAllText(projectPath, JsonSerializer.Serialize(document, JsonOptions));
    }

    public static LoginPackProjectDocument Load(string projectPath)
    {
        var json = File.ReadAllText(projectPath);
        var document = JsonSerializer.Deserialize<LoginPackProjectDocument>(json, JsonOptions)
            ?? throw new InvalidDataException(Loc.Get("ServiceLoginProjectEmptyInvalid"));

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
        if (!string.IsNullOrWhiteSpace(document.VideoSourcePath) && !Path.IsPathRooted(document.VideoSourcePath))
        {
            document.VideoSourcePath = Path.GetFullPath(Path.Combine(projectDirectory, document.VideoSourcePath));
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
