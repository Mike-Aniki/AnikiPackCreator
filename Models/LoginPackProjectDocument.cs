namespace AnikiVisualPackCreator.Models;

public sealed class LoginPackProjectDocument
{
    public int FormatVersion { get; set; } = 1;
    public string PackId { get; set; } = string.Empty;
    public string PackName { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = string.Empty;
    public string? VideoSourcePath { get; set; }
}
