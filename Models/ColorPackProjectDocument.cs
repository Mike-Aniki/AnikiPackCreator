namespace AnikiVisualPackCreator.Models;

public sealed class ColorPackProjectDocument
{
    public int FormatVersion { get; set; } = 1;
    public string PackId { get; set; } = string.Empty;
    public string PackName { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = string.Empty;
    public string TemplateId { get; set; } = ColorPackPalette.ReferenceTemplateId;

    public string PrimaryAccent { get; set; } = ColorPackPalette.DefaultPrimaryAccent;
    public string SecondaryAccent { get; set; } = ColorPackPalette.DefaultSecondaryAccent;

    public string Focus { get; set; } = string.Empty;
    public string ActionButtons { get; set; } = string.Empty;
    public string Progress { get; set; } = string.Empty;

    public string Background { get; set; } = ColorPackPalette.DefaultBackground;
    public string Bars { get; set; } = string.Empty;
    public string Menus { get; set; } = string.Empty;
    public string MenuHeader { get; set; } = string.Empty;
    public string Cards { get; set; } = string.Empty;
    public string Border { get; set; } = ColorPackPalette.DefaultBorder;
    public string Notifications { get; set; } = string.Empty;

    public string PrimaryText { get; set; } = ColorPackPalette.DefaultPrimaryText;
    public string SecondaryText { get; set; } = ColorPackPalette.DefaultSecondaryText;
    public string HighlightText { get; set; } = string.Empty;
}

public sealed class ColorPackPalette
{
    public const string ReferenceTemplateId = "3.GoldenGraphite";

    public const string DefaultPrimaryAccent = "#E2C15A";
    public const string DefaultSecondaryAccent = "#FFF0C8";
    public const string DefaultFocus = "#FFF0C8";
    public const string DefaultActionButtons = "#1A1D22";
    public const string DefaultProgress = "#E2C15A";

    public const string DefaultBackground = "#1A1D22";
    public const string DefaultBars = "#1B1E24";
    public const string DefaultMenus = "#1B1E24";
    public const string DefaultMenuHeader = "#21242A";
    public const string DefaultCards = "#21242A";
    public const string DefaultBorder = "#21242A";
    public const string DefaultNotifications = "#161A20";

    public const string DefaultPrimaryText = "#F5F7FA";
    public const string DefaultSecondaryText = "#D6DAE0";
    public const string DefaultHighlightText = "#FFF0C8";

    public string PrimaryAccent { get; init; } = DefaultPrimaryAccent;
    public string SecondaryAccent { get; init; } = DefaultSecondaryAccent;
    public string Focus { get; init; } = DefaultFocus;
    public string ActionButtons { get; init; } = DefaultActionButtons;
    public string Progress { get; init; } = DefaultProgress;

    public string Background { get; init; } = DefaultBackground;
    public string Bars { get; init; } = DefaultBars;
    public string Menus { get; init; } = DefaultMenus;
    public string MenuHeader { get; init; } = DefaultMenuHeader;
    public string Cards { get; init; } = DefaultCards;
    public string Border { get; init; } = DefaultBorder;
    public string Notifications { get; init; } = DefaultNotifications;

    public string PrimaryText { get; init; } = DefaultPrimaryText;
    public string SecondaryText { get; init; } = DefaultSecondaryText;
    public string HighlightText { get; init; } = DefaultHighlightText;
}
