using Loc = AnikiVisualPackCreator.Localization.LocalizationService;

namespace AnikiVisualPackCreator.Models;

public enum SoundPackAudioFormat
{
    Wav,
    Mp3
}

public sealed class SoundPackSoundDefinition
{
    public SoundPackSoundDefinition(
        string key,
        string targetPath,
        string displayNameKey,
        string sectionKey,
        string categoryKey,
        SoundPackAudioFormat format)
    {
        Key = key;
        TargetPath = targetPath;
        DisplayName = Loc.Get(displayNameKey);
        SectionName = Loc.Get(sectionKey);
        CategoryName = string.IsNullOrWhiteSpace(categoryKey) ? string.Empty : Loc.Get(categoryKey);
        Format = format;
    }

    public string Key { get; }
    public string TargetPath { get; }
    public string DisplayName { get; }
    public string SectionName { get; }
    public string CategoryName { get; }
    public SoundPackAudioFormat Format { get; }
    public bool IsMusic => Format == SoundPackAudioFormat.Mp3;

    public string SelectButtonText => Loc.Get(IsMusic ? "SelectMusic" : "SelectSound");
    public string DropHintText => Loc.Get(IsMusic ? "MusicDropHint" : "SoundDropHint");
    public string FormatDescription => Loc.Get(IsMusic ? "SoundFormatDescriptionMp3" : "SoundFormatDescriptionWav");
    public string NoFileSelectedText => Loc.Get(IsMusic ? "NoMusicSelected" : "NoSoundSelected");

    public static IReadOnlyList<SoundPackSoundDefinition> All { get; } =
    [
        new("navigation", "audio/navigation.wav", "SoundName_Navigation", "SoundSection_ThemeSounds", "SoundCategory_Interface", SoundPackAudioFormat.Wav),
        new("activation", "audio/activation.wav", "SoundName_Activation", "SoundSection_ThemeSounds", "SoundCategory_Interface", SoundPackAudioFormat.Wav),
        new("change-display", "audio/ChangeDisplay.wav", "SoundName_ChangeDisplay", "SoundSection_ThemeSounds", "SoundCategory_Interface", SoundPackAudioFormat.Wav),
        new("enter-game-details", "audio/EnterGameDetails.wav", "SoundName_EnterGameDetails", "SoundSection_ThemeSounds", "SoundCategory_Interface", SoundPackAudioFormat.Wav),
        new("exit-game-details", "audio/ExitGameDetails.wav", "SoundName_ExitGameDetails", "SoundSection_ThemeSounds", "SoundCategory_Interface", SoundPackAudioFormat.Wav),
        new("home-hub-close", "audio/HomeHubClose.wav", "SoundName_HomeHubClose", "SoundSection_ThemeSounds", "SoundCategory_Interface", SoundPackAudioFormat.Wav),
        new("open-additional-view", "audio/OpenAdditionalView.wav", "SoundName_OpenAdditionalView", "SoundSection_ThemeSounds", "SoundCategory_Interface", SoundPackAudioFormat.Wav),
        new("notification", "audio/Noti.wav", "SoundName_Notification", "SoundSection_ThemeSounds", "SoundCategory_Interface", SoundPackAudioFormat.Wav),
        new("session-summary", "audio/SessionSummary.wav", "SoundName_SessionSummary", "SoundSection_ThemeSounds", "SoundCategory_Interface", SoundPackAudioFormat.Wav),
        new("warning", "audio/Warning.wav", "SoundName_Warning", "SoundSection_ThemeSounds", "SoundCategory_Interface", SoundPackAudioFormat.Wav),

        new("application-stopped", "audio/Events/ApplicationStopped.wav", "SoundName_ApplicationStopped", "SoundSection_ThemeSounds", "SoundCategory_Events", SoundPackAudioFormat.Wav),
        new("game-installed", "audio/Events/GameInstalled.wav", "SoundName_GameInstalled", "SoundSection_ThemeSounds", "SoundCategory_Events", SoundPackAudioFormat.Wav),
        new("game-starting", "audio/Events/GameStarting.wav", "SoundName_GameStarting", "SoundSection_ThemeSounds", "SoundCategory_Events", SoundPackAudioFormat.Wav),
        new("game-started", "audio/Events/GameStarted.wav", "SoundName_GameStarted", "SoundSection_ThemeSounds", "SoundCategory_Events", SoundPackAudioFormat.Wav),
        new("game-stopped", "audio/Events/GameStopped.wav", "SoundName_GameStopped", "SoundSection_ThemeSounds", "SoundCategory_Events", SoundPackAudioFormat.Wav),
        new("game-uninstalled", "audio/Events/GameUninstalled.wav", "SoundName_GameUninstalled", "SoundSection_ThemeSounds", "SoundCategory_Events", SoundPackAudioFormat.Wav),
        new("library-updated", "audio/Events/LibraryUpdated.wav", "SoundName_LibraryUpdated", "SoundSection_ThemeSounds", "SoundCategory_Events", SoundPackAudioFormat.Wav),

        new("login-music", "audio/LoginOST.mp3", "SoundName_LoginMusic", "SoundSection_AmbientMusic", "", SoundPackAudioFormat.Mp3),
        new("hub-music", "audio/HubOST.mp3", "SoundName_HubMusic", "SoundSection_AmbientMusic", "", SoundPackAudioFormat.Mp3),
        new("secondary-views-music", "audio/SecondaryViewsOST.mp3", "SoundName_SecondaryViewsMusic", "SoundSection_AmbientMusic", "", SoundPackAudioFormat.Mp3)
    ];
}
