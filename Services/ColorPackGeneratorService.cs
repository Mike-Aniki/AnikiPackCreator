using AnikiVisualPackCreator.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AnikiVisualPackCreator.Services;

public static class ColorPackGeneratorService
{
    private static readonly Regex ColorRegex = new(
        "(?<prefix><Color\\s+x:Key=\"(?<key>[^\"]+)\">\\s*)(?<hex>#[0-9A-Fa-f]{8})(?<suffix>\\s*</Color>)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Rgb ReferencePrimaryAccent = ParseRgb(ColorPackPalette.DefaultPrimaryAccent);
    private static readonly Rgb ReferenceSecondaryAccent = ParseRgb(ColorPackPalette.DefaultSecondaryAccent);
    private static readonly Rgb ReferenceFocus = ParseRgb(ColorPackPalette.DefaultFocus);
    private static readonly Rgb ReferenceActionButtons = ParseRgb(ColorPackPalette.DefaultActionButtons);
    private static readonly Rgb ReferenceProgress = ParseRgb(ColorPackPalette.DefaultProgress);

    private static readonly Rgb ReferenceBackground = ParseRgb(ColorPackPalette.DefaultBackground);
    private static readonly Rgb ReferenceBars = ParseRgb(ColorPackPalette.DefaultBars);
    private static readonly Rgb ReferenceMenus = ParseRgb(ColorPackPalette.DefaultMenus);
    private static readonly Rgb ReferenceMenuHeader = ParseRgb(ColorPackPalette.DefaultMenuHeader);
    private static readonly Rgb ReferenceCards = ParseRgb(ColorPackPalette.DefaultCards);
    private static readonly Rgb ReferenceBorder = ParseRgb(ColorPackPalette.DefaultBorder);
    private static readonly Rgb ReferenceNotifications = ParseRgb(ColorPackPalette.DefaultNotifications);

    private static readonly Rgb ReferenceText = ParseRgb(ColorPackPalette.DefaultPrimaryText);
    private static readonly Rgb ReferenceSecondaryText = ParseRgb(ColorPackPalette.DefaultSecondaryText);
    private static readonly Rgb ReferenceHighlightText = ParseRgb(ColorPackPalette.DefaultHighlightText);

    private static readonly HashSet<string> PrimaryAccentKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accent", "GlyphColor", "GlassInnerLine_Mid",
        "CoverFlash_Transparent", "CoverFlash_VerySoft", "CoverFlash_Soft", "CoverFlash_Core", "CoverFlash_Tail",
        "BadgeTrophée"
    };

    private static readonly HashSet<string> SecondaryAccentKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "HltbAlt", "AccentCardStat", "AccentCardNews"
    };

    private static readonly HashSet<string> FocusKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "GlowFocusColor", "SelectionLightColor", "FocusPrimaryColor", "FocusSecondaryColor",
        "GameFocus_Left", "GameFocus_Right", "GameFocus_AnimatedContrast",
        "ButtonBackgroundFocus_Stop0", "ButtonBackgroundFocus_Stop1", "ButtonBackgroundFocus_Stop2", "ButtonBackgroundFocus_Stop3",
        "FocusFriendsCardBackground", "FocusFriendsCardBorder_Left", "FocusFriendsCardBorder_Right",
        "FocusBorderListNews_Left", "FocusBorderListNews_Right",
        "FocusCardNewsBorder_Left", "FocusCardNewsBorder_Right", "FocusCardNewsBackground",
        "FocusSuccessCardBorder_Left", "FocusSuccessCardBorder_Right", "FocusSuccessCardBackground"
    };

    private static readonly HashSet<string> ActionButtonKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "ButtonPlay_Top", "ButtonPlay_Bottom",
        "ButtonDetails_Top", "ButtonDetails_Bottom",
        "ButtonBackgroundNoFocus_Stop0", "ButtonBackgroundNoFocus_Stop1"
    };

    private static readonly HashSet<string> ProgressKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "ProgressLight", "ProgressDark", "ProgressBackground"
    };

    private static readonly HashSet<string> BarKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "TopBar_Top", "TopBar_Bottom",
        "BottomBar_Top", "BottomBar_Bottom",
        "ColorBottomBarOtherView", "GameListFrameBackground"
    };

    private static readonly HashSet<string> MenuKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "MenuBackground_Top", "MenuBackground_Bottom",
        "OverlayMenu_Top", "OverlayMenu_Mid", "OverlayMenu_Bottom",
        "ControlBackgroundColor"
    };

    private static readonly HashSet<string> MenuHeaderKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "MenuHeader_Top", "MenuHeader_Bottom"
    };

    private static readonly HashSet<string> NotificationKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "NotificationBackground", "NotificationBorder_Left", "NotificationBorder_Right"
    };

    private static readonly HashSet<string> HighlightTextKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "TextHighlight", "TextHighlightStatView", "TextHighlightNewsView",
        "TextHubPercentAchievement", "TextHubBannerCardTitle", "TextHubSuggestedGameBanner"
    };

    private static readonly HashSet<string> PrimaryTextKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "TextColor", "TextColorDark", "Hltb",
        "ButtonIconColorLight", "ButtonIconColorDark",
        "TextHub", "TextHubNameAchievement", "TextHubBannerCardBottom",
        "TextTitleStatView",
        "SuccessMainTextColorCard", "SuccessMainTextColorBadge"
    };

    private static readonly HashSet<string> SecondaryTextKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "TextSecondaryColor", "TextDetail", "TextAltDetail",
        "TextHubGameAchievement", "TextHubDateAchievement", "TextColorNewsDesc"
    };

    private static readonly HashSet<string> GeneralBorderKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "BorderBrushAH", "CardBorder", "AvatarBorder", "CardStatBorder",
        "MenuBorderPrimaryColor", "MenuBorderSecondaryColor",
        "SeparatorListGame", "Separator", "SeparatorBottomBar",
        "SeparatorTopBar_Left", "SeparatorTopBar_Mid", "SeparatorTopBar_Right",
        "ListBorder_Left", "ListBorder_Right", "ListDetailsBorder_Left", "ListDetailsBorder_Right",
        "HubCardBottomBorder", "HubCardBorder", "HubBannerBorder", "HubAchievementsBorder", "HubAchievementsIconBorder",
        "FriendsBannerBorder_Left", "FriendsBannerBorder_Right", "FriendsCardBorder", "FriendsCardBorderOffline",
        "BorderListNews", "CardNewsBorder",
        "SuccessBannerBorder_Left", "SuccessBannerBorder_Right", "SeparatorSuccess",
        "GlassInnerLine_Left", "GlassInnerLine_Right", "BadgeStatusFriends"
    };

    private static readonly HashSet<string> CardSurfaceKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "BackgroundAH", "BackgroundAHDark", "CardStat", "BackgroundItemEndPoint",
        "ListBackground", "ListDetailsBackground",
        "HubCardBottomBackground", "HubButtonBackgroundNoFocus", "HubBannerBackground",
        "HubSuggestedGameBannerBackground", "HubAchievementsBackground", "HubCardMaskImage", "HubAchievementsIconBackground",
        "CardStatBackground", "BannerStatsBackground",
        "FriendsBannerBackground", "FriendsCardBackground", "FriendsCardBackgroundOffline",
        "BackgroundListNews", "CardNewsBackground", "TabNewsBackgroundSelected", "TabNewsBackgroundFocus",
        "SuccessBannerBackground", "SuccessCardTopOverlay", "SuccessCardBottomOverlay",
        "SuccessDetailsCardTop", "SuccessDetailsCardBottom", "SuccessFriendsBackground"
    };

    public static string Generate(ColorPackPalette palette)
    {
        ValidatePalette(palette);
        var template = ColorPackTemplateService.LoadReferenceTemplate();
        var target = new FamilyTargets(palette);

        return ColorRegex.Replace(template, match =>
        {
            var key = match.Groups["key"].Value;
            var originalHex = match.Groups["hex"].Value;
            var original = ParseArgb(originalHex);
            var family = ResolveFamily(key, original.Rgb);
            var transformed = Transform(original, family, target);

            return match.Groups["prefix"].Value + ToArgbHex(transformed) + match.Groups["suffix"].Value;
        });
    }

    public static int CountTemplateColors()
    {
        return ColorRegex.Matches(ColorPackTemplateService.LoadReferenceTemplate()).Count;
    }

    public static void ValidatePalette(ColorPackPalette palette)
    {
        _ = ParseRgb(palette.PrimaryAccent);
        _ = ParseRgb(palette.SecondaryAccent);
        _ = ParseRgb(palette.Focus);
        _ = ParseRgb(palette.ActionButtons);
        _ = ParseRgb(palette.Progress);

        _ = ParseRgb(palette.Background);
        _ = ParseRgb(palette.Bars);
        _ = ParseRgb(palette.Menus);
        _ = ParseRgb(palette.MenuHeader);
        _ = ParseRgb(palette.Cards);
        _ = ParseRgb(palette.Border);
        _ = ParseRgb(palette.Notifications);

        _ = ParseRgb(palette.PrimaryText);
        _ = ParseRgb(palette.SecondaryText);
        _ = ParseRgb(palette.HighlightText);
    }

    public static bool IsValidMasterColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Regex.IsMatch(value.Trim(), @"^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant);
    }

    public static string NormalizeMasterColor(string value)
    {
        var text = value.Trim();
        if (!text.StartsWith('#'))
        {
            text = "#" + text;
        }

        if (!IsValidMasterColor(text))
        {
            throw new FormatException($"Invalid color: {value}");
        }

        return text.ToUpperInvariant();
    }

    private static ColorFamily ResolveFamily(string key, Rgb original)
    {
        if (string.Equals(key, "Transparent", StringComparison.OrdinalIgnoreCase))
        {
            return ColorFamily.FixedTransparent;
        }

        if (HighlightTextKeys.Contains(key)) return ColorFamily.HighlightText;
        if (PrimaryTextKeys.Contains(key)) return ColorFamily.PrimaryText;
        if (SecondaryTextKeys.Contains(key)) return ColorFamily.SecondaryText;

        if (FocusKeys.Contains(key)) return ColorFamily.Focus;
        if (ActionButtonKeys.Contains(key)) return ColorFamily.ActionButtons;
        if (ProgressKeys.Contains(key)) return ColorFamily.Progress;

        if (NotificationKeys.Contains(key)) return ColorFamily.Notifications;
        if (IsShadeLikeKey(key)) return ColorFamily.MenuHeader;
        if (MenuHeaderKeys.Contains(key)) return ColorFamily.Menus;
        if (MenuKeys.Contains(key)) return ColorFamily.Menus;
        if (BarKeys.Contains(key)) return ColorFamily.Bars;
        if (CardSurfaceKeys.Contains(key)) return ColorFamily.Cards;
        if (GeneralBorderKeys.Contains(key)) return ColorFamily.Border;

        if (SecondaryAccentKeys.Contains(key)) return ColorFamily.SecondaryAccent;
        if (PrimaryAccentKeys.Contains(key)) return ColorFamily.PrimaryAccent;

        if (IsAmbientBackgroundKey(key)) return ColorFamily.Background;

        return NearestSafeFamily(original);
    }

    private static bool IsAmbientBackgroundKey(string key)
    {
        return key.StartsWith("SecondaryViewBackground_", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsShadeLikeKey(string key)
    {
        return key.StartsWith("Shade", StringComparison.OrdinalIgnoreCase) ||
               key.StartsWith("GlassMain_", StringComparison.OrdinalIgnoreCase) ||
               key.StartsWith("GlassWindow_", StringComparison.OrdinalIgnoreCase);
    }

    private static ColorFamily NearestSafeFamily(Rgb color)
    {
        var candidates = new List<(ColorFamily Family, Rgb Anchor)>
        {
            (ColorFamily.PrimaryAccent, ReferencePrimaryAccent),
            (ColorFamily.SecondaryAccent, ReferenceSecondaryAccent),
            (ColorFamily.Background, ReferenceBackground),
            (ColorFamily.Bars, ReferenceBars),
            (ColorFamily.Menus, ReferenceMenus),
            (ColorFamily.Cards, ReferenceCards)
        };

        return candidates
            .OrderBy(item => ColorDistanceSquared(color, item.Anchor))
            .First().Family;
    }

    private static Argb Transform(Argb original, ColorFamily family, FamilyTargets target)
    {
        if (family == ColorFamily.FixedTransparent)
        {
            return original;
        }

        var (reference, selected) = family switch
        {
            ColorFamily.PrimaryAccent => (ReferencePrimaryAccent, target.PrimaryAccent),
            ColorFamily.SecondaryAccent => (ReferenceSecondaryAccent, target.SecondaryAccent),
            ColorFamily.Focus => (ReferenceFocus, target.Focus),
            ColorFamily.ActionButtons => (ReferenceActionButtons, target.ActionButtons),
            ColorFamily.Progress => (ReferenceProgress, target.Progress),

            ColorFamily.Background => (ReferenceBackground, target.Background),
            ColorFamily.Bars => (ReferenceBars, target.Bars),
            ColorFamily.Menus => (ReferenceMenus, target.Menus),
            ColorFamily.MenuHeader => (ReferenceMenuHeader, target.MenuHeader),
            ColorFamily.Cards => (ReferenceCards, target.Cards),
            ColorFamily.Border => (ReferenceBorder, target.Border),
            ColorFamily.Notifications => (ReferenceNotifications, target.Notifications),

            ColorFamily.PrimaryText => (ReferenceText, target.PrimaryText),
            ColorFamily.SecondaryText => (ReferenceSecondaryText, target.SecondaryText),
            ColorFamily.HighlightText => (ReferenceHighlightText, target.HighlightText),
            _ => (original.Rgb, original.Rgb)
        };

        var rgb = TransferColor(original.Rgb, reference, selected);
        return new Argb(original.A, rgb);
    }

    private static Rgb TransferColor(Rgb original, Rgb referenceAnchor, Rgb targetAnchor)
    {
        if (targetAnchor.Equals(referenceAnchor))
        {
            return original;
        }

        var sourceHsl = ToHsl(original);
        var referenceHsl = ToHsl(referenceAnchor);
        var targetHsl = ToHsl(targetAnchor);

        var saturationDelta = sourceHsl.S - referenceHsl.S;
        var lightnessDelta = sourceHsl.L - referenceHsl.L;

        var hue = targetHsl.H;
        var saturation = Clamp01(targetHsl.S + (saturationDelta * 0.65));
        var lightness = Clamp01(targetHsl.L + lightnessDelta);

        if (sourceHsl.S < 0.08 && targetHsl.S > 0.08)
        {
            saturation = Math.Min(targetHsl.S * 0.70, 0.45);
        }

        return FromHsl(new Hsl(hue, saturation, lightness));
    }

    private static double ColorDistanceSquared(Rgb left, Rgb right)
    {
        var dr = left.R - right.R;
        var dg = left.G - right.G;
        var db = left.B - right.B;
        return (dr * dr * 0.30) + (dg * dg * 0.59) + (db * db * 0.11);
    }

    private static Argb ParseArgb(string hex)
    {
        var text = hex.Trim().TrimStart('#');
        if (text.Length != 8)
        {
            throw new FormatException($"Invalid ARGB color: {hex}");
        }

        return new Argb(
            byte.Parse(text[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            new Rgb(
                byte.Parse(text.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(text.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(text.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)));
    }

    private static Rgb ParseRgb(string hex)
    {
        var text = hex.Trim().TrimStart('#');
        if (text.Length != 6)
        {
            throw new FormatException($"Invalid RGB color: {hex}");
        }

        return new Rgb(
            byte.Parse(text[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(text.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(text.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }

    private static string ToArgbHex(Argb color)
    {
        return $"#{color.A:X2}{color.Rgb.R:X2}{color.Rgb.G:X2}{color.Rgb.B:X2}";
    }

    private static Hsl ToHsl(Rgb rgb)
    {
        var r = rgb.R / 255.0;
        var g = rgb.G / 255.0;
        var b = rgb.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;
        var l = (max + min) / 2.0;

        if (delta < 0.000001)
        {
            return new Hsl(0, 0, l);
        }

        var s = l > 0.5
            ? delta / (2.0 - max - min)
            : delta / (max + min);

        double h;
        if (Math.Abs(max - r) < 0.000001)
        {
            h = ((g - b) / delta) + (g < b ? 6 : 0);
        }
        else if (Math.Abs(max - g) < 0.000001)
        {
            h = ((b - r) / delta) + 2;
        }
        else
        {
            h = ((r - g) / delta) + 4;
        }

        h /= 6.0;
        return new Hsl(h, s, l);
    }

    private static Rgb FromHsl(Hsl hsl)
    {
        if (hsl.S < 0.000001)
        {
            var value = ToByte(hsl.L);
            return new Rgb(value, value, value);
        }

        var q = hsl.L < 0.5
            ? hsl.L * (1.0 + hsl.S)
            : hsl.L + hsl.S - (hsl.L * hsl.S);
        var p = (2.0 * hsl.L) - q;

        var r = HueToRgb(p, q, hsl.H + (1.0 / 3.0));
        var g = HueToRgb(p, q, hsl.H);
        var b = HueToRgb(p, q, hsl.H - (1.0 / 3.0));
        return new Rgb(ToByte(r), ToByte(g), ToByte(b));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6.0) return p + ((q - p) * 6.0 * t);
        if (t < 1.0 / 2.0) return q;
        if (t < 2.0 / 3.0) return p + ((q - p) * ((2.0 / 3.0) - t) * 6.0);
        return p;
    }

    private static byte ToByte(double value)
    {
        return (byte)Math.Round(Clamp01(value) * 255.0);
    }

    private static double Clamp01(double value) => Math.Max(0.0, Math.Min(1.0, value));

    private enum ColorFamily
    {
        PrimaryAccent,
        SecondaryAccent,
        Focus,
        ActionButtons,
        Progress,
        Background,
        Bars,
        Menus,
        MenuHeader,
        Cards,
        Border,
        Notifications,
        PrimaryText,
        SecondaryText,
        HighlightText,
        FixedTransparent
    }

    private readonly record struct Rgb(byte R, byte G, byte B);
    private readonly record struct Argb(byte A, Rgb Rgb);
    private readonly record struct Hsl(double H, double S, double L);

    private sealed class FamilyTargets
    {
        public FamilyTargets(ColorPackPalette palette)
        {
            PrimaryAccent = ParseRgb(palette.PrimaryAccent);
            SecondaryAccent = ParseRgb(palette.SecondaryAccent);
            Focus = ParseRgb(palette.Focus);
            ActionButtons = ParseRgb(palette.ActionButtons);
            Progress = ParseRgb(palette.Progress);

            Background = ParseRgb(palette.Background);
            Bars = ParseRgb(palette.Bars);
            Menus = ParseRgb(palette.Menus);
            MenuHeader = ParseRgb(palette.MenuHeader);
            Cards = ParseRgb(palette.Cards);
            Border = ParseRgb(palette.Border);
            Notifications = ParseRgb(palette.Notifications);

            PrimaryText = ParseRgb(palette.PrimaryText);
            SecondaryText = ParseRgb(palette.SecondaryText);
            HighlightText = ParseRgb(palette.HighlightText);
        }

        public Rgb PrimaryAccent { get; }
        public Rgb SecondaryAccent { get; }
        public Rgb Focus { get; }
        public Rgb ActionButtons { get; }
        public Rgb Progress { get; }

        public Rgb Background { get; }
        public Rgb Bars { get; }
        public Rgb Menus { get; }
        public Rgb MenuHeader { get; }
        public Rgb Cards { get; }
        public Rgb Border { get; }
        public Rgb Notifications { get; }

        public Rgb PrimaryText { get; }
        public Rgb SecondaryText { get; }
        public Rgb HighlightText { get; }
    }
}
