namespace AnikiVisualPackCreator.Models;

public sealed class VisualPackProjectDocument
{
    public int FormatVersion { get; set; } = 1;
    public string PackId { get; set; } = string.Empty;
    public string PackName { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = string.Empty;
    public List<VisualPackAssetProjectState> Assets { get; set; } = [];
}

public sealed class VisualPackAssetProjectState
{
    public string FileName { get; set; } = string.Empty;
    public string? SourcePath { get; set; }
    public double Zoom { get; set; } = 1.0;
    public double PanX { get; set; }
    public double PanY { get; set; }
}
