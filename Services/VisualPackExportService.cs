using AnikiVisualPackCreator.Models;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Windows.Media.Imaging;

namespace AnikiVisualPackCreator.Services;

public static class VisualPackExportService
{
    public static void Export(
        string destinationZipPath,
        string packName,
        string author,
        IReadOnlyCollection<VisualPackAssetState> assets,
        Action<int, string>? progress = null)
    {
        var missing = assets.Where(asset => !asset.IsReady).Select(asset => asset.FileName).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(Loc.Format("ServiceMissingSourceImages", string.Join("\n", missing)));
        }

        if (string.IsNullOrWhiteSpace(packName))
        {
            throw new InvalidOperationException(Loc.Get("ServicePackNameRequired"));
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
                foreach (var asset in assets)
                {
                    index++;
                    progress?.Invoke(index, asset.DisplayName);

                    var source = ImageRenderService.LoadBitmap(asset.SourcePath!);
                    var output = ImageRenderService.Render(
                        source,
                        asset.Definition.Width,
                        asset.Definition.Height,
                        asset);

                    var entry = archive.CreateEntry(asset.FileName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    ImageRenderService.EncodeJpeg(output, entryStream);
                }

                var manifestEntry = archive.CreateEntry("visualpack.json", CompressionLevel.Optimal);
                using var manifestStream = manifestEntry.Open();
                JsonSerializer.Serialize(manifestStream, new
                {
                    formatVersion = 1,
                    id = CreatePackId(packName),
                    name = packName.Trim(),
                    author = author?.Trim() ?? string.Empty,
                    builtInSeed = false
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

    private static void ValidateGeneratedZip(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);

        foreach (var definition in VisualPackAssetDefinition.All)
        {
            var entry = archive.Entries.SingleOrDefault(item =>
                string.Equals(item.FullName, definition.FileName, StringComparison.OrdinalIgnoreCase));

            if (entry is null)
            {
                throw new InvalidDataException(Loc.Format("ServiceGeneratedZipMissingFile", definition.FileName));
            }

            using var stream = entry.Open();
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            buffer.Position = 0;
            var decoder = BitmapDecoder.Create(
                buffer,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];

            if (frame.PixelWidth != definition.Width || frame.PixelHeight != definition.Height)
            {
                throw new InvalidDataException(Loc.Format(
                    "ServiceGeneratedInvalidDimensions",
                    definition.FileName,
                    frame.PixelWidth,
                    frame.PixelHeight,
                    definition.Width,
                    definition.Height));
            }
        }

        if (!archive.Entries.Any(item =>
                string.Equals(item.FullName, "visualpack.json", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidDataException(Loc.Format("ServiceGeneratedZipMissingFile", "visualpack.json"));
        }
    }

    private static string CreatePackId(string packName)
    {
        var characters = packName
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();
        var value = new string(characters);

        while (value.Contains("--", StringComparison.Ordinal))
        {
            value = value.Replace("--", "-", StringComparison.Ordinal);
        }

        value = value.Trim('-');
        return string.IsNullOrWhiteSpace(value) ? "custom-visual-pack" : value;
    }
}
