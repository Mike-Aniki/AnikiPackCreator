using Image = System.Windows.Controls.Image;
using Panel = System.Windows.Controls.Panel; using TextBox = System.Windows.Controls.TextBox;
using ListBox = System.Windows.Controls.ListBox;
using System.Globalization;
using System.Xml.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Markup;
using AnikiVisualPackCreator.Services;
namespace AnikiVisualPackCreator;
public sealed class ExpertPaletteWindow : Window
{
    readonly Image themeImage = new(){Stretch=Stretch.Uniform};
    readonly TextBlock sceneCaption = new(){TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSlateGray};
    readonly TextBlock customizationCount = new(){Foreground=Brushes.LightSkyBlue,VerticalAlignment=VerticalAlignment.Center};
    readonly Dictionary<string,string> originalOverrides;
    ColorPackWindow? previewRenderer;
    System.Windows.Threading.DispatcherTimer? previewTimer;
    readonly Dictionary<string,Button> sceneButtons = new();
    string activeScene = "Bibliothèque";
    bool compareOriginal;
    string selectedZone = "Toutes les zones";
    bool showTechnical;
    string searchText = "";
    readonly string automatic;
    readonly HashSet<string> recent;
    readonly Dictionary<string,XElement> baseline;
    readonly ListBox list=new(){Background=new SolidColorBrush(Color.FromRgb(21,27,34)),Foreground=Brushes.White,BorderBrush=new SolidColorBrush(Color.FromRgb(45,65,75))};
    readonly StackPanel fields=new();
    readonly Border sample=new(){Height=70,Margin=new Thickness(0,8,0,8),CornerRadius=new CornerRadius(8)};
    readonly TextBlock info=new(){TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8)};
    readonly TextBlock error=new(){Foreground=Brushes.Salmon,TextWrapping=TextWrapping.Wrap};
    XElement? edited;
    XElement? selectedStop;
    Canvas? stopTrack;
    WrapPanel? stopChips;
    string? key;
    bool loading;
    public Dictionary<string,string> Result {get;}
    static readonly XNamespace X="http://schemas.microsoft.com/winfx/2006/xaml";
    static readonly XNamespace P="http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    public ExpertPaletteWindow(string automatic,Dictionary<string,string> overrides, HashSet<string>? recentlyChanged = null)
    {
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/AnikiPackCreator;component/Views/ColorPack/PaletteEditorStyles.xaml") });
        recent = recentlyChanged ?? new();
        this.automatic=automatic;baseline=ExpertColorService.Resources(automatic);Result=new(overrides);originalOverrides=new(overrides);
        Title="Aniki · Atelier couleurs";Width=1560;Height=940;MinWidth=1150;MinHeight=760;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        Background=new SolidColorBrush(Color.FromRgb(13,17,23));Foreground=Brushes.White;
        var layout=new DockPanel{Margin=new Thickness(20),Background=Background};Content=layout;
        var footer=new WrapPanel{Margin=new Thickness(0,16,0,0)};DockPanel.SetDock(footer,Dock.Bottom);layout.Children.Add(footer);
        Button AddButton(Panel panel,string label,Action action){var b=new Button{Content=label,Margin=new Thickness(0,0,8,6),Padding=new Thickness(10,6,10,6)};b.Click+=(_,_)=>action();panel.Children.Add(b);return b;}
        AddButton(footer,"Enregistrer ces modifications",()=>{if(!string.IsNullOrEmpty(error.Text))return;try{ExpertColorService.Apply(automatic,Result);DialogResult=true;}catch(Exception ex){error.Text=ex.Message;}});
        AddButton(footer,"Annuler",()=>DialogResult=false);footer.Children.Add(customizationCount);
        var header=new DockPanel{Margin=new Thickness(0,0,0,20)};
        var badge=new Border{Background=new SolidColorBrush(Color.FromRgb(25,52,61)),CornerRadius=new CornerRadius(9),Padding=new Thickness(12,6,12,6),HorizontalAlignment=System.Windows.HorizontalAlignment.Right};
        badge.Child=new TextBlock{Text="MODE EXPERT",Foreground=Brushes.Turquoise,FontSize=11,FontWeight=FontWeights.Bold};DockPanel.SetDock(badge,Dock.Right);header.Children.Add(badge);
        var titles=new StackPanel();titles.Children.Add(new TextBlock{Text="Atelier couleurs",FontSize=27,FontWeight=FontWeights.SemiBold});
        titles.Children.Add(new TextBlock{Text="Choisissez un élément. Créez sa couleur. Voyez le résultat.",Foreground=Brushes.LightSlateGray,Margin=new Thickness(0,5,0,0),FontSize=13});
        header.Children.Add(titles);DockPanel.SetDock(header,Dock.Top);layout.Children.Add(header);
        var columns=new Grid();columns.ColumnDefinitions.Add(new(){Width=new GridLength(235)});columns.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});columns.ColumnDefinitions.Add(new(){Width=new GridLength(390)});layout.Children.Add(columns);
        var left=new DockPanel{Margin=new Thickness(0,0,16,0)};columns.Children.Add(left);
        var navigation=new StackPanel();DockPanel.SetDock(navigation,Dock.Top);left.Children.Add(navigation);
        navigation.Children.Add(new TextBlock{Text="ZONE DU THÈME",Foreground=Brushes.LightSkyBlue,Margin=new Thickness(0,0,0,8)});
        var zones=new System.Windows.Controls.ComboBox{MinHeight=34,ItemsSource=new[]{"Toutes les zones","Navigation","Accueil","Panneaux","Boutons et sélection","Textes et icônes","Social","Succès","Actualités","Progression","Fonds et effets"},SelectedIndex=0,Margin=new Thickness(0,0,0,12)};
        navigation.Children.Add(zones);
        zones.SelectionChanged+=(_,_)=>{selectedZone=zones.SelectedItem?.ToString()??"Toutes les zones";Populate(searchText);list.SelectedIndex=0;};
        var search=new TextBox{Margin=new Thickness(0,0,0,8),ToolTip="Rechercher un élément : barre, message, fond…"};
        navigation.Children.Add(new TextBlock{Text="Rechercher un élément",Margin=new Thickness(0,0,0,4)});navigation.Children.Add(search);
        var technical=new System.Windows.Controls.CheckBox{Content="Ressources techniques",FontSize=11,Margin=new Thickness(0,0,0,12)};
        technical.Checked+=(_,_)=>{showTechnical=true;Populate(searchText);};technical.Unchecked+=(_,_)=>{showTechnical=false;Populate(searchText);};
        navigation.Children.Add(technical);left.Children.Add(list);
        var center=new DockPanel{Margin=new Thickness(0,0,18,0)};
        Grid.SetColumn(center,1);columns.Children.Add(center);
        var sceneBar=new WrapPanel{Margin=new Thickness(0,0,0,12)};
        foreach(var name in new[]{"Bibliothèque","Accueil","Panneaux","Fiche de jeu","Social"})
        {
            var tab=new Button{Content=name,Padding=new Thickness(9,6,9,6),FontSize=11,Margin=new Thickness(0,0,5,5)};
            sceneButtons[name]=tab;tab.Click+=(_,_)=>{activeScene=name;QueueThemePreview();};sceneBar.Children.Add(tab);
        }
        DockPanel.SetDock(sceneBar,Dock.Top);center.Children.Add(sceneBar);
        var previewNotes=new StackPanel{Margin=new Thickness(0,15,0,0)};
        var compare=new System.Windows.Controls.CheckBox{Content="Comparer avec l’ouverture de l’atelier",Margin=new Thickness(0,0,0,12)};
        compare.Checked+=(_,_)=>{compareOriginal=true;QueueThemePreview();};compare.Unchecked+=(_,_)=>{compareOriginal=false;QueueThemePreview();};
        previewNotes.Children.Add(compare);previewNotes.Children.Add(sceneCaption);
        previewNotes.Children.Add(new TextBlock{Text="L’aperçu utilise les scènes du Creator. Les ressources partagées modifient plusieurs éléments du thème.",Foreground=Brushes.SlateGray,FontSize=11,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,10,0,0)});
        DockPanel.SetDock(previewNotes,Dock.Bottom);center.Children.Add(previewNotes);
        var previewCard=new Border{CornerRadius=new CornerRadius(14),Background=new SolidColorBrush(Color.FromRgb(9,14,20)),BorderBrush=new SolidColorBrush(Color.FromRgb(38,53,68)),BorderThickness=new Thickness(1),Padding=new Thickness(8),Child=themeImage};
        center.Children.Add(previewCard);
        var right=new StackPanel{Margin=new Thickness(14)};
        var inspector=new Border{Background=new SolidColorBrush(Color.FromRgb(20,28,38)),CornerRadius=new CornerRadius(14),BorderBrush=new SolidColorBrush(Color.FromRgb(38,53,68)),BorderThickness=new Thickness(1)};
        inspector.Child=new ScrollViewer{Content=right,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
        Grid.SetColumn(inspector,2);columns.Children.Add(inspector);
        right.Children.Add(new TextBlock{Text="PERSONNALISER L’ÉLÉMENT",Foreground=Brushes.LightSlateGray,FontSize=10,Margin=new Thickness(0,0,0,8)});
        right.Children.Add(info);sample.Height=45;right.Children.Add(sample);
        var actions=new WrapPanel{Margin=new Thickness(0,8,0,8)};right.Children.Add(actions);
        AddButton(actions,"Unie",()=>ConvertBrush(false));
        AddButton(actions,"Dégradé",()=>ConvertBrush(true));
        AddButton(actions,"+ Couleur",()=>{if(edited?.Name.LocalName.EndsWith("GradientBrush")==true){selectedStop=new XElement(P+"GradientStop",new XAttribute("Color","#FFFFFFFF"),new XAttribute("Offset","0.5"));edited.Add(selectedStop);BuildFields();Save();}});
        AddButton(actions,"Rétablir",()=>{if(key!=null){Result.Remove(key);LoadSelected();}});
        right.Children.Add(fields);right.Children.Add(error);
        search.TextChanged+=(_,_)=>{searchText=search.Text;Populate(searchText);};
        list.SelectionChanged+=(_,_)=>LoadSelected();
        list.DisplayMemberPath="Label"; list.SelectedValuePath="Key";
        Populate("");
        list.SelectedIndex=0;
        Closed+=(_,_)=>{previewTimer?.Stop();previewRenderer=null;};
    }
    static readonly Dictionary<string,string> Labels = new()
    {
        ["TopBar"] = "Barre supérieure",
        ["BottomBar"] = "Barre inférieure",
        ["SecondaryViewBackground"] = "Fond des pages",
        ["BackgroundMenu"] = "Fond des panneaux",
        ["HeaderMenu"] = "En-tête des panneaux",
        ["OverlayMenu_Mid"] = "Cartes et rangées · fond",
        ["OverlayMenu"] = "Panneaux secondaires · dégradé",
        ["MenuBorderBrush"] = "Contour des panneaux",
        ["ButtonPlayColor"] = "Bouton Jouer",
        ["ButtonDetailsColor"] = "Bouton Détails",
        ["FocusGameBorderBrush"] = "Contour du jeu sélectionné",
        ["AnimatedFocusGameBorderBrush"] = "Reflet animé de la sélection",
        ["ButtonBackgroundNoFocus"] = "Boutons au repos",
        ["ButtonBackgroundFocus"] = "Boutons sélectionnés",
        ["BadgeBackgroundBrush"] = "Compteurs de messages",
        ["ActiveButtonBackgroundBrush"] = "Boutons cochés et activés",
        ["OutgoingMessageBackgroundBrush"] = "Mes bulles de conversation",
        ["TextBrush"] = "Texte principal",
        ["TextAltBrush"] = "Texte secondaire",
        ["TextDetailBrush"] = "Descriptions et informations",
        ["TextHighlightBrush"] = "Textes mis en avant",
        ["ProgressBrush"] = "Progression remplie",
        ["ProgressBackgroundBrush"] = "Fond des barres de progression",
        ["HubBannerBackgroundBrush"] = "Bannière du profil · fond",
        ["HubBannerBorderBrush"] = "Bannière du profil · contour",
        ["HubCardBorderBrush"] = "Cartes de l’accueil · contour",
        ["HubCardBottomBackgroundBrush"] = "Cartes de l’accueil · zone du titre",
        ["HubSuggestedGameBannerBackgroundBrush"] = "Jeu recommandé · fond",
        ["HubAchievementsBackgroundBrush"] = "Succès de l’accueil · fond",
        ["FriendsBannerBackgroundBrush"] = "Profil d’un ami · fond",
        ["CardNewsBackgroundBrush"] = "Carte d’actualité · fond",
        ["CardNewsBorderBrush"] = "Carte d’actualité · contour",
        ["SuccessMainCardBackground"] = "Carte de succès · fond",
        ["NotificationBackgroundBrush"] = "Notification · fond",
        ["NotificationBorder"] = "Notification · contour",
        ["CinematicOverlayTintBrush"] = "Voile cinématique",
        ["ShadeBackground"] = "Voile sur le fond de la bibliothèque",
        ["ShadeBackgroundDetails"] = "Voile de la fiche de jeu",
        ["ShadeBottom"] = "Assombrissement en bas de page",
        ["GlassMainBrush"] = "Reflets de la navigation",
        ["SocialVoiceVolumeBrush"] = "Volume de la conversation",
        ["SocialVoiceBorderBrush"] = "Conversation vocale · contour",
        ["PageDotsBackgroundBrush"] = "Pages · indicateurs inactifs",
        ["PageDotsFocusBrush"] = "Page active",
        ["AccentBrush"] = "Petits accents décoratifs",
    };
    static string Group(string key)
    {
        if(key is "TopBar" or "BottomBar" or "PageDotsBackgroundBrush" or "PageDotsFocusBrush")return "Navigation";
        if(key is "BadgeBackgroundBrush" or "OutgoingMessageBackgroundBrush")return "Social";
        if(key.Contains("Text")||key.Contains("Glyph"))return "Textes et icônes";
        if(key.Contains("TopBar")||key.Contains("BottomBar"))return "Navigation";
        if(key.Contains("Success")||key.Contains("Achievement")||key.Contains("Troph"))return "Succès";
        if(key.Contains("Friends")||key.Contains("Social")||key.Contains("Message"))return "Social";
        if(key.Contains("News"))return "Actualités";
        if(key.Contains("Hub"))return "Accueil";
        if(key.Contains("Progress"))return "Progression";
        if(key.Contains("Menu")||key.Contains("Overlay"))return "Panneaux";
        if(key.Contains("Button")||key.Contains("Focus")||key.Contains("Selection"))return "Boutons et sélection";
        return "Fonds et effets";
    }
    static string Friendly(string key)
    {
        if (Labels.TryGetValue(key, out var label)) return label;
        var name=System.Text.RegularExpressions.Regex.Replace(key,"([a-z])([A-Z])","$1 $2").Replace("_"," ");
        foreach(var pair in new[]{("Background","Fond"),("Border","Bordure"),("Brush",""),("Color",""),("Top","Haut"),("Bottom","Bas"),("Left","Gauche"),("Right","Droite"),("Mid","Milieu"),("Focus","Sélection"),("Text","Texte"),("Button","Bouton"),("Menu","Panneau"),("Progress","Progression"),("Light","Clair"),("Dark","Sombre")})
            name=name.Replace(pair.Item1,pair.Item2);
        return name.Trim();
    }

    void Populate(string search)
    {
        list.ItemsSource=baseline.Where(p=>new[]{"Color","SolidColorBrush","LinearGradientBrush","RadialGradientBrush"}.Contains(p.Value.Name.LocalName)
            && (showTechnical || Labels.ContainsKey(p.Key))
            && (selectedZone=="Toutes les zones" || Group(p.Key)==selectedZone)
            && (Friendly(p.Key).Contains(search,StringComparison.OrdinalIgnoreCase)||p.Key.Contains(search,StringComparison.OrdinalIgnoreCase)))
            .OrderBy(p=>Group(p.Key)).ThenBy(p=>Friendly(p.Key)).Select(p=>new {Key=p.Key,Label=Friendly(p.Key)+(Result.ContainsKey(p.Key)?"  ★":recent.Contains(p.Key)?"  ●":"")}).ToList();
    }
    void LoadSelected()
    {
        if(list.SelectedValue is not string selected)return;
        key=selected;
        selectedStop=null;
        var all=ExpertColorService.Resources(ExpertColorService.Apply(automatic,Result));
        edited=ExpertColorService.Resolve(all[key],all);
        activeScene = SceneFor(key);
        BuildFields();
        QueueThemePreview();
    }
    void ConvertBrush(bool gradient)
    {
        if(edited==null||key==null||edited.Name.LocalName=="Color")return;
        edited=new XElement(P+(gradient?"LinearGradientBrush":"SolidColorBrush"),new XAttribute(X+"Key",key));
        if(gradient){edited.SetAttributeValue("StartPoint","0,0");edited.SetAttributeValue("EndPoint","0,1");edited.Add(new XElement(P+"GradientStop",new XAttribute("Color","#FF21242A"),new XAttribute("Offset","0")),new XElement(P+"GradientStop",new XAttribute("Color","#FF596070"),new XAttribute("Offset","1")));}
        else edited.SetAttributeValue("Color","#FF21242A");
        BuildFields();Save();
    }
    void Field(string label,string value,Action<string> set,bool color=false)
    {
        var row=new DockPanel{Margin=new Thickness(0,3,0,3)};
        var caption=new TextBlock{Text=label,Width=135,VerticalAlignment=VerticalAlignment.Center};row.Children.Add(caption);
        var box=new TextBox{Text=value,MinWidth=145};
        if(color){
            var pick=new Button{Content="Choisir…",Margin=new Thickness(6,0,0,0)};
            DockPanel.SetDock(pick,Dock.Right);row.Children.Add(pick);
            pick.Click+=(_,_)=>{using var dialog=new System.Windows.Forms.ColorDialog{FullOpen=true};try{var c=(Color)ColorConverter.ConvertFromString(box.Text);dialog.Color=System.Drawing.Color.FromArgb(c.R,c.G,c.B);}catch{}if(dialog.ShowDialog()==System.Windows.Forms.DialogResult.OK){var c=dialog.Color;var alpha=box.Text.Length==9?box.Text.Substring(1,2):"FF";box.Text=$"#{alpha}{c.R:X2}{c.G:X2}{c.B:X2}";}};
        }
        row.Children.Add(box);fields.Children.Add(row);
        box.TextChanged+=(_,_)=>{set(box.Text);Save();};
    }
    void Percent(string label,double current,Action<double> update)
    {
        var row=new Grid{Margin=new Thickness(0,5,0,9)};
        row.ColumnDefinitions.Add(new(){Width=new GridLength(140)});row.ColumnDefinitions.Add(new());row.ColumnDefinitions.Add(new(){Width=new GridLength(50)});
        row.Children.Add(new TextBlock{Text=label,VerticalAlignment=VerticalAlignment.Center});
        var slider=new System.Windows.Controls.Slider{Foreground=Brushes.Turquoise,Minimum=0,Maximum=100,Value=current*100,TickFrequency=1,IsSnapToTickEnabled=true,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(8,0,12,0)};
        Grid.SetColumn(slider,1);row.Children.Add(slider);
        var number=new TextBlock{Text=$"{current*100:0} %",VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(number,2);row.Children.Add(number);
        slider.ValueChanged+=(_,_)=>{number.Text=$"{slider.Value:0} %";update(slider.Value/100);Save();};fields.Children.Add(row);
    }
    void ColorChoice(string label,string value,Action<string> update)
    {
        fields.Children.Add(new TextBlock{Text=label,FontSize=14,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,10,0,0)});
        Color color;
        try{color=(Color)ColorConverter.ConvertFromString(value);}catch{color=Colors.White;}
        var picker=new ColorStudioControl(color);
        picker.ColorChanged+=c=>{update(c.ToString());Save();};
        fields.Children.Add(picker);
    }
    void GradientStrip(XElement[] stops)
    {
        fields.Children.Add(new TextBlock{Text="Déplacez les repères pour répartir les couleurs. Cliquez sur une pastille pour la modifier.",Foreground=Brushes.LightSlateGray,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,6,0,10)});
        stopTrack=new Canvas{Height=54,Margin=new Thickness(10,0,10,16)};
        fields.Children.Add(stopTrack);
        void Render()
        {
            if(stopTrack==null)return;
            stopTrack.Children.Clear();
            foreach(var stop in stops)
            {
                var thumb=new System.Windows.Controls.Primitives.Thumb{Width=20,Height=26,Tag=stop,Cursor=System.Windows.Input.Cursors.SizeWE,
                    Template=(ControlTemplate)XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Thumb'><Border Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='3' CornerRadius='5'/></ControlTemplate>"),
                    BorderBrush=ReferenceEquals(stop,selectedStop)?Brushes.Turquoise:Brushes.White};
                Canvas.SetTop(thumb,34);
                thumb.DragStarted+=(_,_)=>selectedStop=stop;
                thumb.DragDelta+=(_,e)=>{var width=Math.Max(1,stopTrack.ActualWidth-20);var offset=double.Parse((string?)stop.Attribute("Offset")??"0",CultureInfo.InvariantCulture);
                    stop.SetAttributeValue("Offset",Math.Clamp(offset+e.HorizontalChange/width,0,1).ToString(CultureInfo.InvariantCulture));Save();};
                thumb.DragCompleted+=(_,_)=>BuildFields();
                stopTrack.Children.Add(thumb);
            }
            UpdateStrip();
        }
        stopTrack.SizeChanged+=(_,_)=>Render();
        Render();
        var chips=new WrapPanel{Margin=new Thickness(0,0,0,10)}; stopChips=chips;
        foreach(var stop in stops)
        {
            var color=(Color)ColorConverter.ConvertFromString((string?)stop.Attribute("Color")??"#FFFFFFFF");
            var label=new StackPanel{Orientation=System.Windows.Controls.Orientation.Horizontal};
            label.Children.Add(new Border{Width=22,Height=22,CornerRadius=new CornerRadius(11),Background=new SolidColorBrush(color),BorderBrush=Brushes.SlateGray,BorderThickness=new Thickness(1),Margin=new Thickness(0,0,8,0)});
            label.Children.Add(new TextBlock{Text=$"{double.Parse((string?)stop.Attribute("Offset")??"0",CultureInfo.InvariantCulture)*100:0} %",VerticalAlignment=VerticalAlignment.Center});
            var chip=new Button{Tag=stop,Content=label,Margin=new Thickness(0,0,8,5),BorderBrush=ReferenceEquals(stop,selectedStop)?Brushes.Turquoise:Brushes.SlateGray};
            chip.Click+=(_,_)=>{selectedStop=stop;BuildFields();};chips.Children.Add(chip);
        }
        fields.Children.Add(chips);
    }
    void UpdateStrip()
    {
        if(stopTrack==null||edited==null)return;
        try{
            var gradient=new LinearGradientBrush{StartPoint=new Point(0,0.5),EndPoint=new Point(1,0.5)};
            foreach(var stop in edited.Elements().Where(e=>e.Name.LocalName=="GradientStop"))
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString((string)stop.Attribute("Color")!),double.Parse((string)stop.Attribute("Offset")!,CultureInfo.InvariantCulture)));
            stopTrack.Background=gradient;
            if(stopChips!=null)
                foreach(var button in stopChips.Children.OfType<Button>())
                    if(button.Tag is XElement point && button.Content is StackPanel content)
                    {
                        ((Border)content.Children[0]).Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString((string)point.Attribute("Color")!));
                        ((TextBlock)content.Children[1]).Text=$"{double.Parse((string)point.Attribute("Offset")!,CultureInfo.InvariantCulture)*100:0} %";
                    }

            foreach(var thumb in stopTrack.Children.OfType<System.Windows.Controls.Primitives.Thumb>())
                if(thumb.Tag is XElement stop){thumb.Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString((string)stop.Attribute("Color")!));Canvas.SetLeft(thumb,double.Parse((string)stop.Attribute("Offset")!,CultureInfo.InvariantCulture)*Math.Max(0,stopTrack.ActualWidth-20));}
        }catch{}
    }
    void BuildFields()
    {
        if(edited==null||key==null)return;
        loading=true;stopTrack=null;stopChips=null;fields.Children.Clear();error.Text="";
        info.FontSize=19;info.FontWeight=FontWeights.SemiBold;
        info.Text=Friendly(key)+(Result.ContainsKey(key)?" · Personnalisé":"");
        info.ToolTip=key;
        if(edited.Name.LocalName=="Color")ColorChoice("Couleur",edited.Value,v=>edited.Value=v);
        else if(edited.Name.LocalName=="SolidColorBrush")ColorChoice("Couleur",(string?)edited.Attribute("Color")??"#FFFFFFFF",v=>edited.SetAttributeValue("Color",v));
        else
        {
            if(edited.Name.LocalName=="LinearGradientBrush")
            {
                fields.Children.Add(new TextBlock{Text="Sens du dégradé",Margin=new Thickness(0,10,0,6)});
                var direction=new System.Windows.Controls.ComboBox{MinHeight=34,ItemsSource=new[]{"De haut en bas","De gauche à droite","En diagonale ↘","En diagonale ↗","Direction personnalisée"},Margin=new Thickness(0,0,0,10)};
                var start=(string?)edited.Attribute("StartPoint");var end=(string?)edited.Attribute("EndPoint");
                direction.SelectedIndex=(start,end) switch {("0,0","0,1")=>0,("0,0","1,0")=>1,("0,0","1,1")=>2,("0,1","1,0")=>3,_=>4};
                direction.SelectionChanged+=(_,_)=>{if(direction.SelectedIndex<4){var points=new[]{("0,0","0,1"),("0,0","1,0"),("0,0","1,1"),("0,1","1,0")}[direction.SelectedIndex];edited.SetAttributeValue("StartPoint",points.Item1);edited.SetAttributeValue("EndPoint",points.Item2);Save();}};
                fields.Children.Add(direction);
            }
            var stops=edited.Elements().Where(e=>e.Name.LocalName=="GradientStop").ToArray();
            if(selectedStop==null||!stops.Contains(selectedStop))selectedStop=stops.First();
            GradientStrip(stops);
            var active=selectedStop;
            ColorChoice("Couleur sélectionnée",(string?)active.Attribute("Color")??"#FFFFFFFF",v=>active.SetAttributeValue("Color",v));
            Percent("Position",double.Parse((string?)active.Attribute("Offset")??"0",CultureInfo.InvariantCulture),v=>active.SetAttributeValue("Offset",v.ToString(CultureInfo.InvariantCulture)));
            var remove=new Button{Content="Retirer la couleur sélectionnée",IsEnabled=stops.Length>2,HorizontalAlignment=System.Windows.HorizontalAlignment.Left,Margin=new Thickness(0,4,0,10)};
            remove.Click+=(_,_)=>{active.Remove();selectedStop=null;BuildFields();Save();};fields.Children.Add(remove);

        }
        if(edited.Name.LocalName!="Color")Percent("Opacité globale",double.Parse((string?)edited.Attribute("Opacity")??"1",CultureInfo.InvariantCulture),v=>edited.SetAttributeValue("Opacity",v.ToString(CultureInfo.InvariantCulture)));
        var technical=new Expander{Header="Réglages de précision · codes et coordonnées",Margin=new Thickness(0,16,0,0)};
        var details=new StackPanel();technical.Content=details;fields.Children.Add(technical);
        details.Children.Add(new TextBlock{Text="Ressource : "+key,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8)});
        foreach(var e in edited.DescendantsAndSelf())
        {
            var attributes=e.Attributes().Where(a=>new[]{"Color","StartPoint","EndPoint","Center","GradientOrigin","RadiusX","RadiusY"}.Contains(a.Name.LocalName)).ToArray();
            foreach(var attr in attributes)
            {
                var box=new TextBox{Text=attr.Value,Margin=new Thickness(0,3,0,3),ToolTip=attr.Name.LocalName};
                box.TextChanged+=(_,_)=>{attr.Value=box.Text;Save();};details.Children.Add(box);
            }
        }
        loading=false;Preview();
    }
    static string SceneFor(string resource)
    {
        if(resource.Contains("Message")||resource.Contains("Voice")||resource.Contains("BadgeBackground")||resource.Contains("ActiveButton"))return "Social";
        if(resource.Contains("Hub"))return "Accueil";
        if(resource.Contains("Menu")||resource.Contains("Overlay"))return "Panneaux";
        if(resource.Contains("Success")||resource.Contains("Progress")||resource.Contains("Details"))return "Fiche de jeu";
        return "Bibliothèque";
    }
    void QueueThemePreview()
    {
        foreach(var tab in sceneButtons)
        {
            tab.Value.BorderBrush=tab.Key==activeScene?Brushes.Turquoise:new SolidColorBrush(Color.FromRgb(50,75,88));
            tab.Value.Background=new SolidColorBrush(tab.Key==activeScene?Color.FromRgb(29,68,78):Color.FromRgb(23,34,44));
        }
        customizationCount.Text=$"  {Result.Count} élément(s) personnalisé(s)";
        previewTimer ??= new System.Windows.Threading.DispatcherTimer { Interval=TimeSpan.FromMilliseconds(80) };
        previewTimer.Stop();
        previewTimer.Tick -= RenderThemePreview;
        previewTimer.Tick += RenderThemePreview;
        previewTimer.Start();
    }
    void RenderThemePreview(object? sender,EventArgs e)
    {
        previewTimer?.Stop();
        try
        {
            previewRenderer ??= new ColorPackWindow();
            var xaml=ExpertColorService.Apply(automatic,compareOriginal?originalOverrides:Result);
            themeImage.Source=previewRenderer.RenderExpertScene(xaml,activeScene);
            sceneCaption.Text=(compareOriginal?"AVANT · ":"APERÇU EN DIRECT · ")+activeScene+"\n"+(key==null?"":Friendly(key));
        }
        catch(Exception ex){error.Text="Aperçu indisponible : "+ex.Message;}
    }
    void Preview()
    {
        if(edited==null)return;
        UpdateStrip();
        try{
            var copy=new XElement(edited);copy.Attribute(X+"Key")?.Remove();
            sample.Background=edited.Name.LocalName=="Color"?new SolidColorBrush((Color)ColorConverter.ConvertFromString(edited.Value)):(Brush)XamlReader.Parse(copy.ToString());
        }catch{}
    }
    void Save()
    {
        if(loading||edited==null||key==null)return;
        try {ExpertColorService.Validate(baseline[key],edited,key);Result[key]=edited.ToString();error.Text="";info.Text=Friendly(key)+" · Personnalisé";Preview();QueueThemePreview();}
        catch(Exception ex){error.Text=ex.Message;}
    }
}




