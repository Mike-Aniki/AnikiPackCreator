using AnikiVisualPackCreator.Models;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using System.IO.Compression;
using System.Text.Json;

namespace AnikiVisualPackCreator.Services;

public static class CompletePackExportService
{
    public static void Export(
        string destinationZipPath,
        string packId,
        string packName,
        string author,
        string version,
        string description,
        PackArchiveInfo visualPack,
        PackArchiveInfo loginPack,
        PackArchiveInfo? soundPack,
        PackArchiveInfo? colorPack,
        Action<int, string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(packName))
        {
            throw new InvalidOperationException(Loc.Get("ServiceCompletePackNameRequired"));
        }

        if (string.IsNullOrWhiteSpace(packId))
        {
            throw new InvalidOperationException(Loc.Get("ServiceCompletePackIdRequired"));
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new InvalidOperationException(Loc.Get("ServiceCompletePackVersionRequired"));
        }

        ValidateInfo(visualPack, PackArchiveKind.Visual);
        ValidateInfo(loginPack, PackArchiveKind.Login);
        if (soundPack is not null)
        {
            ValidateInfo(soundPack, PackArchiveKind.Sound);
        }

        if (colorPack is not null)
        {
            ValidateInfo(colorPack, PackArchiveKind.Color);
        }

        var destinationDirectory = Path.GetDirectoryName(destinationZipPath);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        var temporaryZipPath = Path.Combine(
            destinationDirectory ?? Environment.CurrentDirectory,
            $"{Guid.NewGuid():N}.tmp.zip");

        try
        {
            using (var archive = ZipFile.Open(temporaryZipPath, ZipArchiveMode.Create))
            {
                var items = new List<PackArchiveInfo> { visualPack, loginPack };
                if (soundPack is not null)
                {
                    items.Add(soundPack);
                }

                if (colorPack is not null)
                {
                    items.Add(colorPack);
                }

                var step = 0;
                foreach (var item in items)
                {
                    step++;
                    progress?.Invoke(step, item.Name);
                    AddExistingZip(archive, item);
                }

                var creatorVersion = typeof(CompletePackExportService).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
                var manifestEntry = archive.CreateEntry("completepack.json", CompressionLevel.Optimal);
                using var manifestStream = manifestEntry.Open();
                JsonSerializer.Serialize(manifestStream, new
                {
                    formatVersion = 1,
                    type = "completePack",
                    id = packId.Trim(),
                    name = packName.Trim(),
                    author = author?.Trim() ?? string.Empty,
                    version = version.Trim(),
                    description = description?.Trim() ?? string.Empty,
                    createdWith = "Aniki Pack Creator",
                    creatorVersion,
                    behavior = "installSeparatePacks",
                    packs = new
                    {
                        visual = ToManifestPack(visualPack),
                        login = ToManifestPack(loginPack),
                        sound = soundPack is null ? null : ToManifestPack(soundPack),
                        color = colorPack is null ? null : ToManifestPack(colorPack)
                    }
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            ValidateGeneratedZip(temporaryZipPath, soundPack is not null, colorPack is not null);
            File.Move(temporaryZipPath, destinationZipPath, true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryZipPath))
                {
                    File.Delete(temporaryZipPath);
                }
            }
            catch
            {
            }
        }
    }

    private static object ToManifestPack(PackArchiveInfo info)
    {
        return new
        {
            file = PackArchiveInspectorService.GetBundleEntryName(info.Kind),
            id = info.Id,
            name = info.Name,
            author = info.Author,
            version = info.Version,
            kind = info.Kind.ToString().ToLowerInvariant()
        };
    }

    private static void AddExistingZip(ZipArchive archive, PackArchiveInfo info)
    {
        var entry = archive.CreateEntry(
            PackArchiveInspectorService.GetBundleEntryName(info.Kind),
            CompressionLevel.NoCompression);
        using var input = new FileStream(info.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var output = entry.Open();
        input.CopyTo(output);
    }

    private static void ValidateInfo(PackArchiveInfo info, PackArchiveKind kind)
    {
        if (info.Kind != kind)
        {
            throw new InvalidDataException(Loc.Get("ServiceCompletePackTypeInvalid"));
        }

        PackArchiveInspectorService.Inspect(info.SourcePath, kind);
    }

    private static void ValidateGeneratedZip(string path, bool hasSound, bool hasColor)
    {
        using var archive = ZipFile.OpenRead(path);

        RequireEntry(archive, "completepack.json");
        RequireEntry(archive, "packs/visual.zip");
        RequireEntry(archive, "packs/login.zip");
        if (hasSound)
        {
            RequireEntry(archive, "packs/sound.zip");
        }

        if (hasColor)
        {
            RequireEntry(archive, "packs/color.zip");
        }

        using var manifestStream = archive.GetEntry("completepack.json")!.Open();
        using var document = JsonDocument.Parse(manifestStream);
        var root = document.RootElement;
        if (!root.TryGetProperty("type", out var type) ||
            !string.Equals(type.GetString(), "completePack", StringComparison.OrdinalIgnoreCase) ||
            !root.TryGetProperty("id", out var id) || string.IsNullOrWhiteSpace(id.GetString()) ||
            !root.TryGetProperty("version", out var version) || string.IsNullOrWhiteSpace(version.GetString()) ||
            !root.TryGetProperty("packs", out var packs) || packs.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException(Loc.Get("ServiceGeneratedCompleteManifestInvalid"));
        }
    }

    private static void RequireEntry(ZipArchive archive, string entryName)
    {
        if (archive.Entries.All(entry =>
                !string.Equals(entry.FullName, entryName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidDataException(Loc.Format("ServiceGeneratedZipMissingFile", entryName));
        }
    }
}
