using System.Reflection;

namespace AnikiVisualPackCreator.Services;

public static class ColorPackTemplateService
{
    private const string ResourceName = "AnikiPackCreator.GoldenGraphiteTemplate.xaml";

    public static string LoadReferenceTemplate()
    {
        var assembly = typeof(ColorPackTemplateService).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The embedded 3.GoldenGraphite.xaml template could not be opened.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
