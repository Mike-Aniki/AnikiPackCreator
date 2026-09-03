using AnikiVisualPackCreator.Models;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using System.IO.Compression;
using System.Text.Json;

namespace AnikiVisualPackCreator.Services;

public static class PackArchiveInspectorService
{
    public static PackArchiveInfo Inspect(string zipPath, PackArchiveKind expectedKind)
    {
        if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
        {
            throw new FileNotFoundException(Loc.Get("ServiceCompletePackFileMissing"), zipPath);
        }

        if (!string.Equals(Path.GetExtension(zipPath), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(Loc.Get("ServiceCompleteZipOnly"));
        }

        var manifestName = GetManifestName(expectedKind);
        using var archive = ZipFile.OpenRead(zipPath);
        var manifest = archive.Entries.SingleOrDefault(entry =>
            string.Equals(entry.FullName, manifestName, StringComparison.OrdinalIgnoreCase));

        if (manifest is null)
        {
            throw new InvalidDataException(Loc.Format("ServiceCompleteWrongPackType", GetDisplayKind(expectedKind), manifestName));
        }

        using var stream = manifest.Open();
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;

        var id = ReadRequiredString(root, "id", manifestName);
        var name = ReadRequiredString(root, "name", manifestName);
        var version = ReadRequiredString(root, "version", manifestName);
        var author = ReadOptionalString(root, "author");
        var description = ReadOptionalString(root, "description");

        if (root.TryGetProperty("type", out var typeProperty) && typeProperty.ValueKind == JsonValueKind.String)
        {
            var type = typeProperty.GetString() ?? string.Empty;
            var expectedType = GetExpectedType(expectedKind);
            if (!string.IsNullOrWhiteSpace(expectedType) &&
                !string.Equals(type, expectedType, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(Loc.Format("ServiceCompleteManifestTypeMismatch", expectedType, type));
            }
        }

        return new PackArchiveInfo
        {
            Kind = expectedKind,
            SourcePath = zipPath,
            ManifestName = manifestName,
            Id = id,
            Name = name,
            Author = author,
            Version = version,
            Description = description,
            FileSizeBytes = new FileInfo(zipPath).Length
        };
    }

    public static string GetManifestName(PackArchiveKind kind)
    {
        return kind switch
        {
            PackArchiveKind.Visual => "visualpack.json",
            PackArchiveKind.Login => "loginpack.json",
            PackArchiveKind.Sound => "soundpack.json",
            PackArchiveKind.Color => "colorpack.json",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public static string GetBundleEntryName(PackArchiveKind kind)
    {
        return kind switch
        {
            PackArchiveKind.Visual => "packs/visual.zip",
            PackArchiveKind.Login => "packs/login.zip",
            PackArchiveKind.Sound => "packs/sound.zip",
            PackArchiveKind.Color => "packs/color.zip",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    private static string GetExpectedType(PackArchiveKind kind)
    {
        return kind switch
        {
            PackArchiveKind.Visual => string.Empty,
            PackArchiveKind.Login => "loginPack",
            PackArchiveKind.Sound => "soundPack",
            PackArchiveKind.Color => "colorPack",
            _ => string.Empty
        };
    }

    private static string GetDisplayKind(PackArchiveKind kind)
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

    private static string ReadRequiredString(JsonElement root, string propertyName, string manifestName)
    {
        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new InvalidDataException(Loc.Format("ServiceCompleteManifestMissingField", manifestName, propertyName));
        }

        return property.GetString()!.Trim();
    }

    private static string ReadOptionalString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return string.Empty;
        }

        return property.GetString()?.Trim() ?? string.Empty;
    }
}
