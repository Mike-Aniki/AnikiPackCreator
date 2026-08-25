using Loc = AnikiVisualPackCreator.Localization.LocalizationService;

namespace AnikiVisualPackCreator.Models;

public sealed class VisualPackAssetDefinition
{
    public VisualPackAssetDefinition(
        string fileName,
        string displayName,
        int width,
        int height,
        string description,
        string? previewOverlayResource = null)
    {
        FileName = fileName;
        DisplayName = displayName;
        Width = width;
        Height = height;
        Description = description;
        PreviewOverlayResource = previewOverlayResource;
    }

    public string FileName { get; }
    public string DisplayName { get; }
    public int Width { get; }
    public int Height { get; }
    public string Description { get; }
    public string? PreviewOverlayResource { get; }
    public string DimensionText => $"{Width} × {Height}";

    public static IReadOnlyList<VisualPackAssetDefinition> All { get; } =
    [
        new("MainBackground.jpg", Loc.Get("AssetMainName"), 1920, 1080, Loc.Get("AssetMainDescription")),
        new(
            "Welcome.jpg",
            Loc.Get("AssetHubName"),
            1920,
            1080,
            Loc.Get("AssetHubDescription"),
            "Assets/PreviewOverlays/HubView.png"),
        new(
            "StatView.jpg",
            Loc.Get("AssetProfileName"),
            1920,
            1080,
            Loc.Get("AssetProfileDescription"),
            "Assets/PreviewOverlays/ProfileView.png"),
        new(
            "FriendsView.jpg",
            Loc.Get("AssetFriendsName"),
            1920,
            1080,
            Loc.Get("AssetFriendsDescription"),
            "Assets/PreviewOverlays/FriendsView.png"),
        new("AchievementsView.jpg", Loc.Get("AssetAchievementsName"), 1920, 1080, Loc.Get("AssetAchievementsDescription")),
        new(
            "MediaView.jpg",
            Loc.Get("AssetCapturesName"),
            1920,
            1080,
            Loc.Get("AssetCapturesDescription"),
            "Assets/PreviewOverlays/CapturesView.png"),
        new(
            "StoreView.jpg",
            Loc.Get("AssetStoreName"),
            1920,
            1080,
            Loc.Get("AssetStoreDescription"),
            "Assets/PreviewOverlays/StoreView.png"),
        new("MainMenu.jpg", Loc.Get("AssetMainMenuName"), 531, 986, Loc.Get("AssetMainMenuDescription")),
        new("SettingsBackground.jpg", Loc.Get("AssetSettingsMenuName"), 487, 1080, Loc.Get("AssetSettingsMenuDescription")),
        new("FrameSettingsBackground.jpg", Loc.Get("AssetSettingsWindowName"), 1247, 900, Loc.Get("AssetSettingsWindowDescription")),
        new("MessageBox.jpg", Loc.Get("AssetMessageBoxName"), 830, 429, Loc.Get("AssetMessageBoxDescription")),
        new("GameMenu.jpg", Loc.Get("AssetGameMenuName"), 470, 655, Loc.Get("AssetGameMenuDescription")),
        new("ItemMenu.jpg", Loc.Get("AssetItemMenuName"), 503, 818, Loc.Get("AssetItemMenuDescription")),
        new(
            "Login.jpg",
            Loc.Get("AssetLoginName"),
            857,
            238,
            Loc.Get("AssetLoginDescription"),
            "Assets/PreviewOverlays/LoginView.png")
    ];
}
