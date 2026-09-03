using AnikiVisualPackCreator.Models;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace AnikiVisualPackCreator.Services;

public static class ColorPackExportService
{
    public static void Export(
        string destinationZipPath,
        string packId,
        string packName,
        string author,
        string version,
        string description,
        ColorPackPalette palette)
    {
        if (string.IsNullOrWhiteSpace(packName))
        {
            throw new InvalidOperationException(Loc.Get("ServiceColorPackNameRequired"));
        }

        if (string.IsNullOrWhiteSpace(packId))
        {
            throw new InvalidOperationException(Loc.Get("ServiceColorPackIdRequired"));
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new InvalidOperationException(Loc.Get("ServiceColorPackVersionRequired"));
        }

        ColorPackGeneratorService.ValidatePalette(palette);
        var xaml = ColorPackGeneratorService.Generate(palette);
        ValidateGeneratedXaml(xaml);

        var directory = Path.GetDirectoryName(destinationZipPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = destinationZipPath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var file = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Create))
            {
                var xamlEntry = archive.CreateEntry("colors.xaml", CompressionLevel.Optimal);
                using (var stream = xamlEntry.Open())
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(xaml);
                }

                var manifestEntry = archive.CreateEntry("colorpack.json", CompressionLevel.Optimal);
                using var manifestStream = manifestEntry.Open();
                var creatorVersion = typeof(ColorPackExportService).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
                JsonSerializer.Serialize(manifestStream, new
                {
                    formatVersion = 1,
                    type = "colorPack",
                    id = packId.Trim(),
                    name = packName.Trim(),
                    author = author?.Trim() ?? string.Empty,
                    version = version.Trim(),
                    description = description?.Trim() ?? string.Empty,
                    template = ColorPackPalette.ReferenceTemplateId,
                    resource = "colors.xaml",
                    createdWith = "Aniki Pack Creator",
                    creatorVersion,
                    masterColors = new
                    {
                        primaryAccent = palette.PrimaryAccent,
                        secondaryAccent = palette.SecondaryAccent,
                        focus = palette.Focus,
                        actionButtons = palette.ActionButtons,
                        progress = palette.Progress,
                        background = palette.Background,
                        bars = palette.Bars,
                        menus = palette.Menus,
                        menuHeader = palette.MenuHeader,
                        cards = palette.Cards,
                        border = palette.Border,
                        notifications = palette.Notifications,
                        primaryText = palette.PrimaryText,
                        secondaryText = palette.SecondaryText,
                        highlightText = palette.HighlightText
                    }
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            ValidateGeneratedZip(temporaryPath);
            File.Move(temporaryPath, destinationZipPath, true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
            catch
            {
            }
        }
    }

    private static void ValidateGeneratedXaml(string xaml)
    {
        var document = XDocument.Parse(xaml, LoadOptions.PreserveWhitespace);
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        var colors = document.Descendants(presentation + "Color")
            .Where(element => element.Attribute(x + "Key") != null)
            .ToList();

        if (colors.Count != ColorPackGeneratorService.CountTemplateColors())
        {
            throw new InvalidDataException(Loc.Format(
                "ServiceColorGeneratedColorCountInvalid",
                colors.Count,
                ColorPackGeneratorService.CountTemplateColors()));
        }
    }

    private static void ValidateGeneratedZip(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var manifest = archive.GetEntry("colorpack.json")
            ?? throw new InvalidDataException(Loc.Format("ServiceGeneratedZipMissingFile", "colorpack.json"));
        var colors = archive.GetEntry("colors.xaml")
            ?? throw new InvalidDataException(Loc.Format("ServiceGeneratedZipMissingFile", "colors.xaml"));

        using (var stream = colors.Open())
        using (var reader = new StreamReader(stream))
        {
            ValidateGeneratedXaml(reader.ReadToEnd());
        }

        using var manifestStream = manifest.Open();
        using var document = JsonDocument.Parse(manifestStream);
        var root = document.RootElement;
        if (!root.TryGetProperty("id", out var id) || string.IsNullOrWhiteSpace(id.GetString()) ||
            !root.TryGetProperty("version", out var version) || string.IsNullOrWhiteSpace(version.GetString()) ||
            !root.TryGetProperty("template", out var template) ||
            !string.Equals(template.GetString(), ColorPackPalette.ReferenceTemplateId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(Loc.Get("ServiceGeneratedColorManifestInvalid"));
        }
    }
}
