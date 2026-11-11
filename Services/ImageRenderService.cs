using FlowDirection = System.Windows.FlowDirection; using FontFamily = System.Windows.Media.FontFamily; using Pen = System.Windows.Media.Pen;
using AnikiVisualPackCreator.Models;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AnikiVisualPackCreator.Services;

public static class ImageRenderService
{
    private const double SoftAnikiSaturation = 0.25;
    private const double SoftAnikiBrightness = 0.98;
    private const double SoftAnikiContrast = 1.04;

    public static BitmapSource LoadBitmap(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var decoder = BitmapDecoder.Create(
            stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);

        var frame = decoder.Frames[0];
        frame.Freeze();
        return frame;
    }

    public static BitmapSource Render(BitmapSource source, int outputWidth, int outputHeight, VisualPackAssetState state)
    {
        if (outputWidth <= 0 || outputHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(outputWidth));
        }

        var sourceWidth = Math.Max(1, source.PixelWidth);
        var sourceHeight = Math.Max(1, source.PixelHeight);
        var fillScale = Math.Max(outputWidth / (double)sourceWidth, outputHeight / (double)sourceHeight);
        var scale = fillScale * Math.Clamp(state.Zoom, 0.75, 2.0);
        var renderedWidth = sourceWidth * scale;
        var renderedHeight = sourceHeight * scale;

        var centeredX = (outputWidth - renderedWidth) / 2.0;
        var centeredY = (outputHeight - renderedHeight) / 2.0;
        var panTravelX = Math.Abs(outputWidth - renderedWidth) / 2.0;
        var panTravelY = Math.Abs(outputHeight - renderedHeight) / 2.0;
        var imageX = centeredX + Math.Clamp(state.PanX, -1.0, 1.0) * panTravelX;
        var imageY = centeredY + Math.Clamp(state.PanY, -1.0, 1.0) * panTravelY;

        var visual = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);

        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(Brushes.Black, null, new Rect(0, 0, outputWidth, outputHeight));
            context.DrawImage(source, new Rect(imageX, imageY, renderedWidth, renderedHeight));
        }

        var rendered = new RenderTargetBitmap(
            outputWidth,
            outputHeight,
            96,
            96,
            PixelFormats.Pbgra32);
        rendered.Render(visual);
        rendered.Freeze();

        if (string.Equals(state.FileName, "MainBackground.jpg", StringComparison.OrdinalIgnoreCase))
        {
            return rendered;
        }

        return ApplyColorAdjustments(
            rendered,
            SoftAnikiSaturation,
            SoftAnikiBrightness,
            SoftAnikiContrast);
    }

    public static BitmapSource RenderThemePreview(
        BitmapSource source,
        int outputWidth,
        int outputHeight,
        VisualPackAssetState state,
        bool applyThemeEffect)
    {
        var renderedImage = Render(source, outputWidth, outputHeight, state);
        var fileName = state.FileName;

        if (!applyThemeEffect)
        {
            return renderedImage;
        }

        if (fileName.Equals("Login.jpg", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("HomeProfileCard.jpg", StringComparison.OrdinalIgnoreCase))
            return RenderBannerPreview(renderedImage, outputWidth, outputHeight, fileName);

        if (string.Equals(fileName, "MainBackground.jpg", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fileName, "Login.jpg", StringComparison.OrdinalIgnoreCase))
        {
            return renderedImage;
        }

        var background = IsMenuPreview(fileName)
            ? CreateDefaultOverlayMenuBrush()
            : CreateDefaultSecondaryViewBackgroundBrush();

        var imageOpacity = GetThemeImageOpacity(fileName);
        var opacityMask = CreateThemeOpacityMask(fileName);
        var bounds = new Rect(0, 0, outputWidth, outputHeight);

        var visual = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);

        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(background, null, bounds);

            if (opacityMask is not null)
            {
                context.PushOpacityMask(opacityMask);
            }

            context.PushOpacity(imageOpacity);
            context.DrawImage(renderedImage, bounds);
            context.Pop();

            if (opacityMask is not null)
            {
                context.Pop();
            }
        }

        var preview = new RenderTargetBitmap(
            outputWidth,
            outputHeight,
            96,
            96,
            PixelFormats.Pbgra32);
        preview.Render(visual);
        preview.Freeze();
        return preview;
    }

    private static BitmapSource RenderBannerPreview(BitmapSource image, int width, int height, string fileName)
    {
        var login = fileName.Equals("Login.jpg", StringComparison.OrdinalIgnoreCase);
        var designWidth = login ? 550.0 : 1100.0;
        var designHeight = login ? 200.0 : 300.0;
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(width/designWidth, height/designHeight));
            var bounds = new Rect(0,0,designWidth,designHeight);
            dc.PushClip(new RectangleGeometry(bounds,login?18:19,login?18:19));
            dc.DrawImage(image,bounds);
            var shade = new LinearGradientBrush { StartPoint=new Point(0,0),EndPoint=new Point(1,0) };
            foreach(var pair in new[]{(0.0,0xD8),(.38,0xB0),(.72,0x50),(1.0,0)})
                shade.GradientStops.Add(new GradientStop(Color.FromArgb((byte)pair.Item2,0,0,0),pair.Item1));
            dc.DrawRectangle(shade,null,bounds);
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(login?(byte)77:(byte)61,26,29,34)),null,bounds);
            void Text(string value,double size,double x,double y,Brush? color=null,bool bold=false)
            {
                var text=new FormattedText(value,System.Globalization.CultureInfo.CurrentUICulture,FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"),FontStyles.Normal,bold?FontWeights.SemiBold:FontWeights.Normal,FontStretches.Normal),
                    size,color??Brushes.White,1);
                dc.DrawText(text,new Point(x,y));
            }
            if(login)
            {
                var fr=System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName=="fr";
                // Current theme: avatar on the left, identity in the middle,
                // sign-in action on the right. Welcome belongs outside the banner.
                var avatar = new Point(80,100);
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(24,42,62)),
                    new Pen(new SolidColorBrush(Color.FromRgb(73,149,202)),2),avatar,51,51);
                dc.PushClip(new EllipseGeometry(avatar,48,48));
                var silhouette = new SolidColorBrush(Color.FromRgb(166,190,214));
                dc.DrawEllipse(silhouette,null,new Point(80,86),16,19);
                dc.DrawEllipse(silhouette,null,new Point(80,135),34,29);
                dc.Pop();
                Text(fr?"Joueur":"Player",28,157,67,null,true);
                Text(fr?"Code d’accès":"Access code",13,157,111,
                    new SolidColorBrush(Color.FromRgb(239,171,68)));
                Text("›",32,510,67,new SolidColorBrush(Color.FromRgb(239,171,68)));
                Text(fr?"Connexion":"Sign in",14,470,116);
            }
            else
            {
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(33,43,56)),new Pen(Brushes.LightGray,2),new Point(95,105),51,51);
                Text("G",38,80,77,Brushes.White,true);
                Text("Player",34,175,48,null,true);
                Text("Fighting fan",20,177,95,Brushes.LightGray);
                Text("Gamerscore  ·  60",17,177,135,Brushes.LightGray);
                Text("Total play time  ·  1d 11h",17,177,168,Brushes.LightGray);
                Text("Top platform · Steam",15,177,219,Brushes.LightGray);
                var labels=new[]{"Total Games","Installed","Favorite","Monthly play time","Played this month","Completed"};
                var values=new[]{"225","201","2","15 mn","2","0"};
                for(int i=0;i<6;i++)
                {
                    var x=625+i%3*149;var y=38+i/3*112;
                    dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(100,35,39,46)),new Pen(new SolidColorBrush(Color.FromArgb(35,255,255,255)),1),new Rect(x,y,137,100),14,14);
                    Text(labels[i],12,x+13,y+12,Brushes.LightGray);Text(values[i],27,x+13,y+40,null,true);
                }
            }
            dc.Pop();
            dc.DrawRoundedRectangle(null,new Pen(new SolidColorBrush(Color.FromArgb(145,220,225,232)),2),bounds,18,18);
            dc.Pop();
        }
        var output=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);
        output.Render(visual);output.Freeze();return output;
    }

    private static bool IsMenuPreview(string fileName)
    {
        return fileName.Equals("MainMenu.jpg", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("SettingsBackground.jpg", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("FrameSettingsBackground.jpg", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("MessageBox.jpg", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("GameMenu.jpg", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("ItemMenu.jpg", StringComparison.OrdinalIgnoreCase);
    }

    private static double GetThemeImageOpacity(string fileName)
    {
        if (fileName.Equals("FriendsView.jpg", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("AchievementsView.jpg", StringComparison.OrdinalIgnoreCase))
        {
            return 0.20;
        }

        if (fileName.Equals("MediaView.jpg", StringComparison.OrdinalIgnoreCase))
        {
            return 0.25;
        }

        return 0.15;
    }

    private static Brush CreateDefaultOverlayMenuBrush()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x15, 0x1D, 0x26), 0.0));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x0E, 0x14, 0x1C), 0.4));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x05, 0x08, 0x0D), 1.0));
        brush.Freeze();
        return brush;
    }

    private static Brush CreateDefaultSecondaryViewBackgroundBrush()
    {
        var brush = new RadialGradientBrush
        {
            Center = new Point(0.5, 0.4),
            GradientOrigin = new Point(0.5, 0.4),
            RadiusX = 0.8,
            RadiusY = 0.9
        };
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x11, 0x18, 0x20), 0.0));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x0A, 0x10, 0x17), 0.7));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x05, 0x08, 0x0D), 1.0));
        brush.Freeze();
        return brush;
    }

    private static Brush? CreateThemeOpacityMask(string fileName)
    {
        if (fileName.Equals("Welcome.jpg", StringComparison.OrdinalIgnoreCase))
        {
            return CreateVerticalMask((0.0, 1.0), (0.90, 1.0), (1.0, 0.0));
        }

        if (fileName.Equals("StatView.jpg", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("MediaView.jpg", StringComparison.OrdinalIgnoreCase))
        {
            return CreateVerticalMask((0.0, 1.0), (0.30, 0.5), (0.60, 0.5), (1.0, 1.0));
        }

        if (fileName.Equals("FriendsView.jpg", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("StoreView.jpg", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("MainMenu.jpg", StringComparison.OrdinalIgnoreCase))
        {
            return CreateVerticalMask((0.0, 0.0), (0.10, 1.0), (0.90, 1.0), (1.0, 0.0));
        }

        if (fileName.Equals("AchievementsView.jpg", StringComparison.OrdinalIgnoreCase))
        {
            return CreateVerticalMask((0.0, 0.0), (0.15, 0.0), (0.35, 1.0), (0.85, 1.0), (1.0, 0.0));
        }

        return null;
    }

    private static Brush CreateVerticalMask(params (double Offset, double Opacity)[] stops)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };

        foreach (var (offset, opacity) in stops)
        {
            var alpha = (byte)Math.Clamp(Math.Round(opacity * 255.0), 0.0, 255.0);
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(alpha, 255, 255, 255), offset));
        }

        brush.Freeze();
        return brush;
    }

    public static void EncodeJpeg(BitmapSource bitmap, Stream destination, int quality = 92)
    {
        var encoder = new JpegBitmapEncoder
        {
            QualityLevel = Math.Clamp(quality, 1, 100)
        };
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        if (destination.CanSeek)
        {
            encoder.Save(destination);
            return;
        }

        using var buffer = new MemoryStream();
        encoder.Save(buffer);
        buffer.Position = 0;
        buffer.CopyTo(destination);
    }

    private static BitmapSource ApplyColorAdjustments(
        BitmapSource source,
        double saturation,
        double brightness,
        double contrast)
    {
        var width = source.PixelWidth;
        var height = source.PixelHeight;
        var stride = width * 4;
        var pixels = new byte[stride * height];
        source.CopyPixels(pixels, stride, 0);

        saturation = Math.Clamp(saturation, 0.0, 1.5);
        brightness = Math.Clamp(brightness, 0.5, 1.5);
        contrast = Math.Clamp(contrast, 0.5, 1.5);

        for (var index = 0; index < pixels.Length; index += 4)
        {
            double blue = pixels[index];
            double green = pixels[index + 1];
            double red = pixels[index + 2];
            var gray = red * 0.2126 + green * 0.7152 + blue * 0.0722;

            var adjustedRed = AdjustChannel(red, gray, saturation, brightness, contrast);
            var adjustedGreen = AdjustChannel(green, gray, saturation, brightness, contrast);
            var adjustedBlue = AdjustChannel(blue, gray, saturation, brightness, contrast);

            pixels[index] = (byte)adjustedBlue;
            pixels[index + 1] = (byte)adjustedGreen;
            pixels[index + 2] = (byte)adjustedRed;
            pixels[index + 3] = 255;
        }

        var result = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            stride);
        result.Freeze();
        return result;
    }

    private static int AdjustChannel(
        double channel,
        double gray,
        double saturation,
        double brightness,
        double contrast)
    {
        var adjusted = gray + (channel - gray) * saturation;
        adjusted = (adjusted - 127.5) * contrast + 127.5;
        adjusted *= brightness;
        return (int)Math.Clamp(Math.Round(adjusted), 0.0, 255.0);
    }
}

