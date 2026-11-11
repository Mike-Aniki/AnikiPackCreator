using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using TextBox=System.Windows.Controls.TextBox;
namespace AnikiVisualPackCreator;
public sealed class ColorStudioControl : UserControl
{
    readonly Grid square=new(){Height=145,ClipToBounds=true};
    readonly Border hueLayer=new();
    readonly Canvas cursorLayer=new(){IsHitTestVisible=false};
    readonly Ellipse cursor=new(){Width=13,Height=13,Stroke=Brushes.White,StrokeThickness=2,Fill=Brushes.Transparent};
    readonly Slider hue=new(){Minimum=0,Maximum=360};
    readonly Slider alpha=new(){Minimum=0,Maximum=100};
    readonly TextBox hex=new(){MinWidth=110};
    readonly Border swatch=new(){Width=50,Height=38,CornerRadius=new CornerRadius(7),Margin=new Thickness(0,0,12,0)};
    readonly TextBlock alphaValue=new(){Width=45,VerticalAlignment=VerticalAlignment.Center};
    double h,s,v;byte a;bool syncing;
    public event Action<Color>? ColorChanged;
    public ColorStudioControl(Color initial)
    {
        var root=new Grid{Margin=new Thickness(0,10,0,10)};
        root.RowDefinitions.Add(new(){Height=GridLength.Auto});
        root.RowDefinitions.Add(new(){Height=GridLength.Auto});
        root.Children.Add(square);square.Margin=new Thickness(0,0,0,12);
        square.Children.Add(hueLayer);
        square.Children.Add(new Border{Background=new LinearGradientBrush(Colors.White,Colors.Transparent,0)});
        square.Children.Add(new Border{Background=new LinearGradientBrush(Colors.Transparent,Colors.Black,90)});
        cursorLayer.Children.Add(cursor);square.Children.Add(cursorLayer);
        square.MouseLeftButtonDown+=(_,e)=>{square.CaptureMouse();Move(e.GetPosition(square));};
        square.MouseMove+=(_,e)=>{if(square.IsMouseCaptured)Move(e.GetPosition(square));};
        square.MouseLeftButtonUp+=(_,_)=>square.ReleaseMouseCapture();
        square.SizeChanged+=(_,_)=>Draw();
        var right=new StackPanel();Grid.SetRow(right,1);root.Children.Add(right);
        var top=new DockPanel();top.Children.Add(swatch);top.Children.Add(hex);right.Children.Add(top);
        right.Children.Add(new TextBlock{Text="Teinte",Margin=new Thickness(0,10,0,2)});
        var spectrum=new LinearGradientBrush{StartPoint=new Point(0,0.5),EndPoint=new Point(1,0.5)};
        for(int i=0;i<=6;i++)spectrum.GradientStops.Add(new GradientStop(Hsv(i*60,1,1,255),i/6.0));
        right.Children.Add(new Border{Height=5,CornerRadius=new CornerRadius(2),Background=spectrum,Margin=new Thickness(0,3,0,0)});
        right.Children.Add(hue);
        var opacity=new DockPanel{Margin=new Thickness(0,8,0,0)};DockPanel.SetDock(alphaValue,Dock.Right);opacity.Children.Add(alphaValue);opacity.Children.Add(new TextBlock{Text="Opacité",VerticalAlignment=VerticalAlignment.Center});right.Children.Add(opacity);right.Children.Add(alpha);
        Content=root;
        Set(initial);
        hue.ValueChanged+=(_,_)=>{if(!syncing){h=hue.Value;Commit();}};
        alpha.ValueChanged+=(_,_)=>{if(!syncing){a=(byte)Math.Round(alpha.Value/100.0*255,MidpointRounding.AwayFromZero);Commit();}};
        hex.TextChanged+=(_,_)=>{if(syncing)return;try{var text=hex.Text.Trim();if(!System.Text.RegularExpressions.Regex.IsMatch(text,@"^#[0-9a-fA-F]{6}$"))return;var color=(Color)ColorConverter.ConvertFromString(text);color.A=a;Set(color);ColorChanged?.Invoke(color);}catch{}};
    }
    void Move(Point point){s=Math.Clamp(point.X/Math.Max(1,square.ActualWidth),0,1);v=1-Math.Clamp(point.Y/Math.Max(1,square.ActualHeight),0,1);Commit();}
    void Set(Color color)
    {
        a=color.A;double r=color.R/255.0,g=color.G/255.0,b=color.B/255.0;var max=Math.Max(r,Math.Max(g,b));var min=Math.Min(r,Math.Min(g,b));var d=max-min;v=max;s=max==0?0:d/max;
        h=d==0?0:max==r?60*((g-b)/d%6):max==g?60*((b-r)/d+2):60*((r-g)/d+4);if(h<0)h+=360;Draw();
    }
    static Color Hsv(double h,double s,double v,byte a)
    {
        var c=v*s;var x=c*(1-Math.Abs(h/60%2-1));var m=v-c;
        var rgb=h<60?(c,x,0d):h<120?(x,c,0d):h<180?(0d,c,x):h<240?(0d,x,c):h<300?(x,0d,c):(c,0d,x);
        return Color.FromArgb(a,(byte)Math.Round((rgb.Item1+m)*255),(byte)Math.Round((rgb.Item2+m)*255),(byte)Math.Round((rgb.Item3+m)*255));
    }
    void Commit(){Draw();ColorChanged?.Invoke(Hsv(h,s,v,a));}
    void Draw()
    {
        syncing=true;var color=Hsv(h,s,v,a);hueLayer.Background=new SolidColorBrush(Hsv(h,1,1,255));swatch.Background=new SolidColorBrush(color);hex.Text=$"#{color.R:X2}{color.G:X2}{color.B:X2}";hue.Value=h;alpha.Value=a/2.55;alphaValue.Text=$"{a/2.55:0} %";Canvas.SetLeft(cursor,s*square.ActualWidth-6);Canvas.SetTop(cursor,(1-v)*square.ActualHeight-6);syncing=false;
    }
}

