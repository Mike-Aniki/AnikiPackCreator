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
        var scale = fillScale * Math.Clamp(state.Zoom, 1.0, 3.0);
        var renderedWidth = sourceWidth * scale;
        var renderedHeight = sourceHeight * scale;
        var overflowX = Math.Max(0.0, renderedWidth - outputWidth);
        var overflowY = Math.Max(0.0, renderedHeight - outputHeight);
        var imageX = -overflowX / 2.0 + Math.Clamp(state.PanX, -1.0, 1.0) * overflowX / 2.0;
        var imageY = -overflowY / 2.0 + Math.Clamp(state.PanY, -1.0, 1.0) * overflowY / 2.0;

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

        // ZipArchiveEntry streams are not seekable, while WPF bitmap encoders
        // require a seekable destination. Encode in memory, then copy the JPEG.
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
