namespace AnikiVisualPackCreator.Models;

public enum PackArchiveKind
{
    Visual,
    Login,
    Sound,
    Color
}

public sealed class PackArchiveInfo
{
    public PackArchiveKind Kind { get; init; }
    public string SourcePath { get; init; } = string.Empty;
    public string ManifestName { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
}
