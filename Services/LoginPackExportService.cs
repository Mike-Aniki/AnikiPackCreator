using AnikiVisualPackCreator.Models;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using System.IO.Compression;
using System.Text.Json;

namespace AnikiVisualPackCreator.Services;

public static class LoginPackExportService
{
    private const string VideoEntryName = "Login.mp4";

    public static void Export(
        string destinationZipPath,
        string packId,
        string packName,
        string author,
        string version,
        string description,
        string videoSourcePath,
        Action<int, string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(packName))
        {
            throw new InvalidOperationException(Loc.Get("ServiceLoginPackNameRequired"));
        }

        if (string.IsNullOrWhiteSpace(packId))
        {
            throw new InvalidOperationException(Loc.Get("ServiceLoginPackIdRequired"));
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new InvalidOperationException(Loc.Get("ServiceLoginPackVersionRequired"));
        }

        if (string.IsNullOrWhiteSpace(videoSourcePath))
        {
            throw new InvalidOperationException(Loc.Get("ServiceLoginVideoRequired"));
        }

        var info = ValidateVideoFile(videoSourcePath);
        var destinationDirectory = Path.GetDirectoryName(destinationZipPath);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        var temporaryZipPath = Path.Combine(destinationDirectory ?? Environment.CurrentDirectory, $"{Guid.NewGuid():N}.tmp.zip");

        try
        {
            if (File.Exists(temporaryZipPath))
            {
                File.Delete(temporaryZipPath);
            }

            using (var archive = ZipFile.Open(temporaryZipPath, ZipArchiveMode.Create))
            {
                progress?.Invoke(1, info.FileName);

                var videoEntry = archive.CreateEntry(VideoEntryName, CompressionLevel.NoCompression);
                using (var input = new FileStream(videoSourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var output = videoEntry.Open())
                {
                    input.CopyTo(output);
                }

                var creatorVersion = typeof(LoginPackExportService).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
                var manifestEntry = archive.CreateEntry("loginpack.json", CompressionLevel.Optimal);
                using var manifestStream = manifestEntry.Open();
                JsonSerializer.Serialize(manifestStream, new
                {
                    formatVersion = 1,
                    type = "loginPack",
                    id = packId.Trim(),
                    name = packName.Trim(),
                    author = author?.Trim() ?? string.Empty,
                    version = version.Trim(),
                    description = description?.Trim() ?? string.Empty,
                    createdWith = "Aniki Pack Creator",
                    creatorVersion,
                    video = new
                    {
                        target = VideoEntryName,
                        container = "mp4",
                        codec = info.VideoCodecId,
                        codecDisplay = info.VideoCodecDisplay,
                        width = info.Width,
                        height = info.Height,
                        duration = info.Duration?.ToString() ?? string.Empty,
                        hasAudioTrack = info.HasAudioTrack,
                        audioCodec = info.AudioCodecId,
                        audioMutedInTheme = true
                    }
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            ValidateGeneratedZip(temporaryZipPath);
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

    public static LoginPackMediaInfo ValidateVideoFile(string path)
    {
        return Mp4InspectorService.Inspect(path);
    }

    private static void ValidateGeneratedZip(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);

        var manifestEntry = archive.Entries.SingleOrDefault(item =>
            string.Equals(item.FullName, "loginpack.json", StringComparison.OrdinalIgnoreCase));
        if (manifestEntry is null)
        {
            throw new InvalidDataException(Loc.Format("ServiceGeneratedZipMissingFile", "loginpack.json"));
        }

        var videoEntry = archive.Entries.SingleOrDefault(item =>
            string.Equals(item.FullName, VideoEntryName, StringComparison.OrdinalIgnoreCase));
        if (videoEntry is null)
        {
            throw new InvalidDataException(Loc.Format("ServiceGeneratedZipMissingFile", VideoEntryName));
        }

        using var manifestStream = manifestEntry.Open();
        using var document = JsonDocument.Parse(manifestStream);
        var root = document.RootElement;
        if (!root.TryGetProperty("id", out var id) || string.IsNullOrWhiteSpace(id.GetString()) ||
            !root.TryGetProperty("version", out var version) || string.IsNullOrWhiteSpace(version.GetString()) ||
            !root.TryGetProperty("video", out var video) || video.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException(Loc.Get("ServiceGeneratedLoginManifestInvalid"));
        }
    }
}
