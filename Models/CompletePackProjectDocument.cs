namespace AnikiVisualPackCreator.Models;

public sealed class CompletePackProjectDocument
{
    public int FormatVersion { get; set; } = 1;
    public string PackId { get; set; } = string.Empty;
    public string PackName { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = string.Empty;
    public string? VisualPackPath { get; set; }
    public string? LoginPackPath { get; set; }
    public string? SoundPackPath { get; set; }
    public string? ColorPackPath { get; set; }
}
