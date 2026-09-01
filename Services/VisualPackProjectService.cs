using AnikiVisualPackCreator.Models;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using System.IO;
using System.Text.Json;

namespace AnikiVisualPackCreator.Services;

public static class VisualPackProjectService
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
        IEnumerable<VisualPackAssetState> assets)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory;
        Directory.CreateDirectory(projectDirectory);

        var document = new VisualPackProjectDocument
        {
            PackId = packId?.Trim() ?? string.Empty,
            PackName = packName?.Trim() ?? string.Empty,
            Author = author?.Trim() ?? string.Empty,
            Version = string.IsNullOrWhiteSpace(version) ? "1.0.0" : version.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Assets = assets.Select(asset => new VisualPackAssetProjectState
            {
                FileName = asset.FileName,
                SourcePath = MakePortablePath(projectDirectory, asset.SourcePath),
                Zoom = asset.Zoom,
                PanX = asset.PanX,
                PanY = asset.PanY
            }).ToList()
        };

        File.WriteAllText(projectPath, JsonSerializer.Serialize(document, JsonOptions));
    }

    public static VisualPackProjectDocument Load(string projectPath)
    {
        var json = File.ReadAllText(projectPath);
        var document = JsonSerializer.Deserialize<VisualPackProjectDocument>(json, JsonOptions)
            ?? throw new InvalidDataException(Loc.Get("ServiceProjectEmptyInvalid"));

        if (document.FormatVersion != 1)
        {
            throw new InvalidDataException(Loc.Format("ServiceUnsupportedFormatVersion", document.FormatVersion));
        }

        // Backward compatibility with projects created before Community Pack metadata existed.
        document.PackId ??= string.Empty;
        document.PackName ??= string.Empty;
        document.Author ??= string.Empty;
        document.Version = string.IsNullOrWhiteSpace(document.Version) ? "1.0.0" : document.Version.Trim();
        document.Description ??= string.Empty;
        document.Assets ??= [];

        var projectDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory;
        foreach (var asset in document.Assets)
        {
            if (!string.IsNullOrWhiteSpace(asset.SourcePath) && !Path.IsPathRooted(asset.SourcePath))
            {
                asset.SourcePath = Path.GetFullPath(Path.Combine(projectDirectory, asset.SourcePath));
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
