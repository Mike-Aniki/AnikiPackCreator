using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
namespace AnikiVisualPackCreator.Services;
public static class ExpertColorService
{
    static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2006/xaml";
    static readonly HashSet<string> Types = new() { "Color", "SolidColorBrush", "LinearGradientBrush", "RadialGradientBrush" };
    public static Dictionary<string, XElement> Resources(string xaml) =>
        XDocument.Parse(xaml).Root!.Elements().Where(e => e.Attribute(X+"Key") != null)
        .ToDictionary(e => (string)e.Attribute(X+"Key")!, e => new XElement(e));
    public static string Apply(string xaml, Dictionary<string,string>? overrides)
    {
        var doc=XDocument.Parse(xaml,LoadOptions.PreserveWhitespace);
        if(overrides==null) return xaml;
        foreach(var item in overrides)
        {
            var original=doc.Root!.Elements().SingleOrDefault(e=>(string?)e.Attribute(X+"Key")==item.Key)
                ?? throw new InvalidDataException("Ressource inconnue : "+item.Key);
            var replacement=XElement.Parse(item.Value);
            Validate(original,replacement,item.Key);
            original.ReplaceWith(replacement);
        }
        return doc.ToString(SaveOptions.DisableFormatting);
    }
    public static void Validate(XElement original,XElement value,string key)
    {
        if(!Types.Contains(original.Name.LocalName)||!Types.Contains(value.Name.LocalName)||
           value.Name.Namespace!=original.Name.Namespace||(string?)value.Attribute(X+"Key")!=key||
           (original.Name.LocalName=="Color")!=(value.Name.LocalName=="Color"))
            throw new InvalidDataException("Type de ressource incorrect : "+key);
        foreach(var child in value.Elements().Where(e=>e.Name.LocalName!="GradientStop"))
            if(!original.Elements().Any(e=>XNode.DeepEquals(e,child)))throw new InvalidDataException("La transformation du thème doit être conservée.");
        foreach(var e in new[]{value}.Concat(value.Elements().Where(e=>e.Name.LocalName=="GradientStop")))
        {
            if(e!=value && e.Name.LocalName!="GradientStop")throw new InvalidDataException("Élément non autorisé.");
            foreach(var a in e.Attributes().Where(a=>!a.IsNamespaceDeclaration))
            {
                if(a.Name==X+"Key" && e==value)continue;
                if(a.Name.Namespace!=XNamespace.None)throw new InvalidDataException("Attribut non autorisé.");
                switch(a.Name.LocalName)
                {
                    case "Color": CheckColor(a.Value);break;
                    case "MappingMode": if(a.Value!="RelativeToBoundingBox" && a.Value!="Absolute")throw new InvalidDataException("Mode de dégradé incorrect.");break;
                    case "Offset":case "Opacity":case "RadiusX":case "RadiusY":
                        if(!double.TryParse(a.Value,NumberStyles.Float,CultureInfo.InvariantCulture,out var n)||!double.IsFinite(n)||n<0||n>1)
                            throw new InvalidDataException("Position/opacité : valeur entre 0 et 1 requise.");break;
                    case "StartPoint":case "EndPoint":case "Center":case "GradientOrigin":
                        var parts=a.Value.Split(',');
                        if(parts.Length!=2||parts.Any(p=>!double.TryParse(p,NumberStyles.Float,CultureInfo.InvariantCulture,out var v)||!double.IsFinite(v)))
                            throw new InvalidDataException("Point incorrect.");break;
                    default:throw new InvalidDataException("Attribut non autorisé : "+a.Name);
                }
            }
        }
        if(value.Name.LocalName=="Color")CheckColor(value.Value);
        if(value.Name.LocalName.EndsWith("GradientBrush") && value.Elements().Count(e=>e.Name.LocalName=="GradientStop")<2)
            throw new InvalidDataException("Un dégradé nécessite au moins deux points.");
    }
    static void CheckColor(string color)
    {
        if(!Regex.IsMatch(color,@"^#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$"))
            throw new InvalidDataException("Couleur attendue : #RRGGBB ou #AARRGGBB.");
    }
    public static void RemoveAffectedOverrides(string before,string after,Dictionary<string,string> overrides)
    {
        var old=Resources(before);var next=Resources(after);
        var changed=next.Keys.Where(k=>!old.TryGetValue(k,out var e)||!XNode.DeepEquals(e,next[k])).ToHashSet();
        bool added;
        do {
            added=false;
            foreach(var item in next)
                if(!changed.Contains(item.Key) && Regex.Matches(item.Value.ToString(),@"\{(?:DynamicResource|StaticResource) ([^}]+)\}")
                    .Any(m=>changed.Contains(m.Groups[1].Value)))added|=changed.Add(item.Key);
        }while(added);
        foreach(var key in changed)overrides.Remove(key);
    }
    public static XElement Resolve(XElement element,Dictionary<string,XElement> resources)
    {
        var copy=new XElement(element);
        foreach(var a in copy.DescendantsAndSelf().Attributes().Where(a=>a.Name.LocalName=="Color").ToList())
        {
            var m=Regex.Match(a.Value,@"^\{(?:DynamicResource|StaticResource) ([^}]+)\}$");
            if(m.Success && resources.TryGetValue(m.Groups[1].Value,out var color))a.Value=color.Value.Trim();
            else if(m.Success && m.Groups[1].Value == "PageDotsFocus")a.Value="#E6FFFFFF";
            else if(m.Success && m.Groups[1].Value == "PageDotsBackground")a.Value="#66FFFFFF";
        }
        return copy;
    }
}


