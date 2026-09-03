namespace AnikiVisualPackCreator.Models;

public sealed class LoginPackMediaInfo
{
    public string FileName { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public TimeSpan? Duration { get; init; }
    public string VideoCodecId { get; init; } = string.Empty;
    public string VideoCodecDisplay { get; init; } = string.Empty;
    public bool HasAudioTrack { get; init; }
    public string AudioCodecId { get; init; } = string.Empty;
    public string AudioCodecDisplay { get; init; } = string.Empty;
}
