namespace AnikiVisualPackCreator.Models;

public sealed class SoundPackProjectDocument
{
    public int FormatVersion { get; set; } = 1;
    public string PackId { get; set; } = string.Empty;
    public string PackName { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = string.Empty;
    public List<SoundPackSoundProjectState> Sounds { get; set; } = [];
}

public sealed class SoundPackSoundProjectState
{
    public string Key { get; set; } = string.Empty;
    public string? SourcePath { get; set; }
}
