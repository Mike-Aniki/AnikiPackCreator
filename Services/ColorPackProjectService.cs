using AnikiVisualPackCreator.Models;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using System.Text.Json;

namespace AnikiVisualPackCreator.Services;

public static class ColorPackProjectService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void Save(string path, ColorPackProjectDocument document)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(document, JsonOptions));
    }

    public static ColorPackProjectDocument Load(string path)
    {
        var document = JsonSerializer.Deserialize<ColorPackProjectDocument>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException(Loc.Get("ServiceColorProjectEmptyInvalid"));

        if (document.FormatVersion != 1)
        {
            throw new InvalidDataException(Loc.Format("ServiceUnsupportedFormatVersion", document.FormatVersion));
        }

        document.PackId ??= string.Empty;
        document.PackName ??= string.Empty;
        document.Author ??= string.Empty;
        document.Version = string.IsNullOrWhiteSpace(document.Version) ? "1.0.0" : document.Version.Trim();
        document.Description ??= string.Empty;
        document.TemplateId = string.IsNullOrWhiteSpace(document.TemplateId)
            ? ColorPackPalette.ReferenceTemplateId
            : document.TemplateId.Trim();

        if (!string.Equals(document.TemplateId, ColorPackPalette.ReferenceTemplateId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(Loc.Format("ServiceColorUnsupportedTemplate", document.TemplateId));
        }

        document.PrimaryAccent = NormalizeOrDefault(document.PrimaryAccent, ColorPackPalette.DefaultPrimaryAccent);
        document.SecondaryAccent = NormalizeOrDefault(document.SecondaryAccent, ColorPackPalette.DefaultSecondaryAccent);
        document.Background = NormalizeOrDefault(document.Background, ColorPackPalette.DefaultBackground);
        document.Border = NormalizeOrDefault(document.Border, ColorPackPalette.DefaultBorder);
        document.PrimaryText = NormalizeOrDefault(document.PrimaryText, ColorPackPalette.DefaultPrimaryText);
        document.SecondaryText = NormalizeOrDefault(document.SecondaryText, ColorPackPalette.DefaultSecondaryText);

        document.Focus = NormalizeOrFallback(document.Focus, document.SecondaryAccent);
        document.ActionButtons = NormalizeOrFallback(document.ActionButtons, document.Background);
        document.Progress = NormalizeOrFallback(document.Progress, document.PrimaryAccent);
        document.Bars = NormalizeOrFallback(document.Bars, document.Background);
        document.Menus = NormalizeOrFallback(document.Menus, document.Background);
        document.MenuHeader = NormalizeOrFallback(document.MenuHeader, document.Background);
        document.Cards = NormalizeOrFallback(document.Cards, document.Background);
        document.Notifications = NormalizeOrFallback(document.Notifications, document.Background);

        document.HighlightText = NormalizeOrFallback(document.HighlightText, document.PrimaryText);

        return document;
    }

    private static string NormalizeOrDefault(string? value, string fallback)
    {
        return ColorPackGeneratorService.NormalizeMasterColor(
            string.IsNullOrWhiteSpace(value) ? fallback : value);
    }

    private static string NormalizeOrFallback(string? value, string fallback)
    {
        return ColorPackGeneratorService.NormalizeMasterColor(
            string.IsNullOrWhiteSpace(value) ? fallback : value);
    }
}
