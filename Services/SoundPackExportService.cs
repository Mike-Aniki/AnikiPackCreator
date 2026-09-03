using AnikiVisualPackCreator.Models;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace AnikiVisualPackCreator.Services;

public static class SoundPackExportService
{
    public static void Export(
        string destinationZipPath,
        string packId,
        string packName,
        string author,
        string version,
        string description,
        IReadOnlyCollection<SoundPackSoundState> sounds,
        Action<int, string>? progress = null)
    {
        var selected = sounds.Where(sound => sound.IsReady).ToList();
        if (selected.Count == 0)
        {
            throw new InvalidOperationException(Loc.Get("ServiceSoundAtLeastOneRequired"));
        }

        if (string.IsNullOrWhiteSpace(packName))
        {
            throw new InvalidOperationException(Loc.Get("ServiceSoundPackNameRequired"));
        }

        if (string.IsNullOrWhiteSpace(packId))
        {
            throw new InvalidOperationException(Loc.Get("ServiceSoundPackIdRequired"));
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new InvalidOperationException(Loc.Get("ServiceSoundPackVersionRequired"));
        }

        var destinationDirectory = Path.GetDirectoryName(destinationZipPath);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        var temporaryZipPath = destinationZipPath + ".tmp-" + Guid.NewGuid().ToString("N");

        try
        {
            using (var file = new FileStream(temporaryZipPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Create))
            {
                var index = 0;
                foreach (var sound in selected)
                {
                    index++;
                    progress?.Invoke(index, sound.DisplayName);

                    ValidateAudioFile(sound);
                    var entryPath = sound.TargetPath.Replace('\\', '/');
                    var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
                    using var input = new FileStream(sound.SourcePath!, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var output = entry.Open();
                    input.CopyTo(output);
                }

                var creatorVersion = typeof(SoundPackExportService).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
                var manifestEntry = archive.CreateEntry("soundpack.json", CompressionLevel.Optimal);
                using var manifestStream = manifestEntry.Open();
                JsonSerializer.Serialize(manifestStream, new
                {
                    formatVersion = 1,
                    type = "soundPack",
                    id = packId.Trim(),
                    name = packName.Trim(),
                    author = author?.Trim() ?? string.Empty,
                    version = version.Trim(),
                    description = description?.Trim() ?? string.Empty,
                    createdWith = "Aniki Pack Creator",
                    creatorVersion,
                    sounds = selected.Select(sound => new
                    {
                        key = sound.Key,
                        target = sound.TargetPath.Replace('\\', '/'),
                        kind = sound.IsMusic ? "music" : "sound",
                        format = sound.Format == SoundPackAudioFormat.Mp3 ? "mp3" : "wav"
                    }).ToArray()
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            ValidateGeneratedZip(temporaryZipPath, selected);
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

    public static void ValidateAudioFile(SoundPackSoundState sound)
    {
        if (sound is null)
        {
            throw new ArgumentNullException(nameof(sound));
        }

        if (sound.Format == SoundPackAudioFormat.Mp3)
        {
            ValidateMp3File(sound.SourcePath!);
            return;
        }

        ValidateWaveFile(sound.SourcePath!);
    }

    private static void ValidateGeneratedZip(string zipPath, IReadOnlyCollection<SoundPackSoundState> selected)
    {
        using var archive = ZipFile.OpenRead(zipPath);

        var manifestEntry = archive.Entries.SingleOrDefault(item =>
            string.Equals(item.FullName, "soundpack.json", StringComparison.OrdinalIgnoreCase));
        if (manifestEntry is null)
        {
            throw new InvalidDataException(Loc.Format("ServiceGeneratedZipMissingFile", "soundpack.json"));
        }

        foreach (var sound in selected)
        {
            var expected = sound.TargetPath.Replace('\\', '/');
            var entry = archive.Entries.SingleOrDefault(item =>
                string.Equals(item.FullName, expected, StringComparison.OrdinalIgnoreCase));
            if (entry is null)
            {
                throw new InvalidDataException(Loc.Format("ServiceGeneratedZipMissingFile", expected));
            }

            using var stream = entry.Open();
            if (sound.Format == SoundPackAudioFormat.Mp3)
            {
                ValidateMp3Stream(stream, expected);
            }
            else
            {
                ValidateWaveStream(stream, expected);
            }
        }

        using var manifestStream = manifestEntry.Open();
        using var document = JsonDocument.Parse(manifestStream);
        var root = document.RootElement;
        if (!root.TryGetProperty("id", out var id) || string.IsNullOrWhiteSpace(id.GetString()) ||
            !root.TryGetProperty("version", out var version) || string.IsNullOrWhiteSpace(version.GetString()) ||
            !root.TryGetProperty("sounds", out var sounds) || sounds.ValueKind != JsonValueKind.Array || sounds.GetArrayLength() == 0)
        {
            throw new InvalidDataException(Loc.Get("ServiceGeneratedSoundManifestInvalid"));
        }
    }

    public static void ValidateWaveFile(string path)
    {
        ValidateExistingFile(path);

        if (!string.Equals(Path.GetExtension(path), ".wav", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(Loc.Get("ServiceSoundWaveOnly"));
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        ValidateWaveStream(stream, Path.GetFileName(path));
    }

    public static void ValidateMp3File(string path)
    {
        ValidateExistingFile(path);

        if (!string.Equals(Path.GetExtension(path), ".mp3", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(Loc.Get("ServiceMusicMp3Only"));
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        ValidateMp3Stream(stream, Path.GetFileName(path));
    }

    private static void ValidateExistingFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw new FileNotFoundException(Loc.Get("ServiceSoundFileMissing"), path);
        }
    }

    private static void ValidateWaveStream(Stream stream, string displayName)
    {
        Span<byte> header = stackalloc byte[12];
        var read = stream.Read(header);
        if (read < 12 ||
            header[0] != (byte)'R' || header[1] != (byte)'I' || header[2] != (byte)'F' || header[3] != (byte)'F' ||
            header[8] != (byte)'W' || header[9] != (byte)'A' || header[10] != (byte)'V' || header[11] != (byte)'E')
        {
            throw new InvalidDataException(Loc.Format("ServiceSoundInvalidWave", displayName));
        }
    }

    private static void ValidateMp3Stream(Stream stream, string displayName)
    {
        var header = new byte[4096];
        var read = stream.Read(header, 0, header.Length);
        if (read < 2)
        {
            throw new InvalidDataException(Loc.Format("ServiceMusicInvalidMp3", displayName));
        }

        if (read >= 3 && header[0] == (byte)'I' && header[1] == (byte)'D' && header[2] == (byte)'3')
        {
            return;
        }

        for (var index = 0; index < read - 1; index++)
        {
            if (header[index] == 0xFF && (header[index + 1] & 0xE0) == 0xE0)
            {
                return;
            }
        }

        throw new InvalidDataException(Loc.Format("ServiceMusicInvalidMp3", displayName));
    }
}
