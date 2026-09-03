using AnikiVisualPackCreator.Models;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using System.Text;

namespace AnikiVisualPackCreator.Services;

public static class Mp4InspectorService
{
    public const long MaxFileSizeBytes = 50L * 1024L * 1024L;

    public static LoginPackMediaInfo Inspect(string path)
    {
        ValidateExistingFile(path);

        if (!string.Equals(Path.GetExtension(path), ".mp4", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(Loc.Get("ServiceLoginMp4Only"));
        }

        var fileInfo = new FileInfo(path);
        if (fileInfo.Length >= MaxFileSizeBytes)
        {
            throw new InvalidDataException(Loc.Format("ServiceLoginFileTooLarge", FormatBytes(MaxFileSizeBytes)));
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

        var context = new ParseContext();
        ParseBoxes(reader, stream.Length, context, currentTrack: null);

        if (!context.HasFtyp)
        {
            throw new InvalidDataException(Loc.Get("ServiceLoginMp4Only"));
        }

        var videoTrack = context.Tracks.FirstOrDefault(track =>
            string.Equals(track.HandlerType, "vide", StringComparison.OrdinalIgnoreCase));
        if (videoTrack is null)
        {
            throw new InvalidDataException(Loc.Get("ServiceLoginVideoTrackMissing"));
        }

        var videoCodecDisplay = GetVideoCodecDisplayName(videoTrack.Codec);
        if (string.IsNullOrWhiteSpace(videoCodecDisplay))
        {
            throw new InvalidDataException(Loc.Format(
                "ServiceLoginUnsupportedVideoCodec",
                string.IsNullOrWhiteSpace(videoTrack.Codec) ? "unknown" : videoTrack.Codec));
        }

        var audioTrack = context.Tracks.FirstOrDefault(track =>
            string.Equals(track.HandlerType, "soun", StringComparison.OrdinalIgnoreCase));

        return new LoginPackMediaInfo
        {
            FileName = Path.GetFileName(path) ?? string.Empty,
            FileSizeBytes = fileInfo.Length,
            Width = videoTrack.Width,
            Height = videoTrack.Height,
            Duration = GetDuration(videoTrack),
            VideoCodecId = videoTrack.Codec ?? string.Empty,
            VideoCodecDisplay = videoCodecDisplay,
            HasAudioTrack = audioTrack is not null,
            AudioCodecId = audioTrack?.Codec ?? string.Empty,
            AudioCodecDisplay = GetAudioCodecDisplayName(audioTrack?.Codec)
        };
    }

    public static string GetVideoCodecDisplayName(string? codec)
    {
        return codec?.ToLowerInvariant() switch
        {
            "avc1" or "avc3" => "H.264 / AVC",
            "hvc1" or "hev1" => "H.265 / HEVC",
            _ => string.Empty
        };
    }

    public static string GetAudioCodecDisplayName(string? codec)
    {
        return codec?.ToLowerInvariant() switch
        {
            "mp4a" => "AAC",
            "ac-3" => "AC-3",
            "ec-3" => "E-AC-3",
            "alac" => "ALAC",
            _ when string.IsNullOrWhiteSpace(codec) => string.Empty,
            _ => codec!.ToUpperInvariant()
        };
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024L * 1024L)
        {
            return $"{bytes / 1024d:0.0} KB";
        }

        return $"{bytes / (1024d * 1024d):0.0} MB";
    }

    public static string FormatDuration(TimeSpan? duration)
    {
        if (duration is null || duration.Value <= TimeSpan.Zero)
        {
            return "—";
        }

        var value = duration.Value;
        return value.TotalHours >= 1
            ? value.ToString(@"h\:mm\:ss")
            : value.ToString(@"m\:ss");
    }

    private static void ValidateExistingFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw new FileNotFoundException(Loc.Get("ServiceLoginVideoFileMissing"), path);
        }
    }

    private static TimeSpan? GetDuration(TrackInfo track)
    {
        if (track.TimeScale <= 0 || track.DurationUnits <= 0)
        {
            return null;
        }

        try
        {
            var seconds = track.DurationUnits / (double)track.TimeScale;
            return TimeSpan.FromSeconds(seconds);
        }
        catch
        {
            return null;
        }
    }

    private static void ParseBoxes(BinaryReader reader, long endPosition, ParseContext context, TrackInfo? currentTrack)
    {
        var stream = reader.BaseStream;
        while (stream.Position + 8 <= endPosition)
        {
            var boxStart = stream.Position;
            var size32 = ReadUInt32BigEndian(reader);
            var type = ReadFourCc(reader);
            var headerSize = 8L;
            long boxSize;

            if (size32 == 1)
            {
                boxSize = checked((long)ReadUInt64BigEndian(reader));
                headerSize = 16;
            }
            else if (size32 == 0)
            {
                boxSize = endPosition - boxStart;
            }
            else
            {
                boxSize = size32;
            }

            if (boxSize < headerSize)
            {
                throw new InvalidDataException($"Invalid MP4 box size for '{type}'.");
            }

            var boxEnd = boxStart + boxSize;
            if (boxEnd > endPosition || boxEnd < boxStart)
            {
                throw new InvalidDataException($"Invalid MP4 box bounds for '{type}'.");
            }

            switch (type)
            {
                case "ftyp":
                    context.HasFtyp = true;
                    break;

                case "moov":
                case "mdia":
                case "minf":
                case "stbl":
                    ParseBoxes(reader, boxEnd, context, currentTrack);
                    break;

                case "trak":
                    var track = new TrackInfo();
                    ParseBoxes(reader, boxEnd, context, track);
                    context.Tracks.Add(track);
                    break;

                case "hdlr" when currentTrack is not null:
                    ParseHandler(reader, boxEnd, currentTrack);
                    break;

                case "mdhd" when currentTrack is not null:
                    ParseMediaHeader(reader, boxEnd, currentTrack);
                    break;

                case "stsd" when currentTrack is not null:
                    ParseSampleDescription(reader, boxEnd, currentTrack);
                    break;

                case "tkhd" when currentTrack is not null:
                    ParseTrackHeader(reader, boxEnd, currentTrack);
                    break;
            }

            stream.Position = boxEnd;
        }
    }

    private static void ParseHandler(BinaryReader reader, long boxEnd, TrackInfo track)
    {
        var payloadLength = checked((int)(boxEnd - reader.BaseStream.Position));
        if (payloadLength < 12)
        {
            return;
        }

        var data = reader.ReadBytes(payloadLength);
        track.HandlerType = ReadFourCc(data, 8);
    }

    private static void ParseMediaHeader(BinaryReader reader, long boxEnd, TrackInfo track)
    {
        var payloadLength = checked((int)(boxEnd - reader.BaseStream.Position));
        if (payloadLength < 24)
        {
            return;
        }

        var data = reader.ReadBytes(payloadLength);
        var version = data[0];

        if (version == 1)
        {
            if (payloadLength < 32)
            {
                return;
            }

            track.TimeScale = ReadUInt32BigEndian(data, 20);
            track.DurationUnits = ReadUInt64BigEndian(data, 24);
        }
        else
        {
            track.TimeScale = ReadUInt32BigEndian(data, 12);
            track.DurationUnits = ReadUInt32BigEndian(data, 16);
        }
    }

    private static void ParseTrackHeader(BinaryReader reader, long boxEnd, TrackInfo track)
    {
        var payloadLength = checked((int)(boxEnd - reader.BaseStream.Position));
        if (payloadLength < 8)
        {
            return;
        }

        var data = reader.ReadBytes(payloadLength);
        if (payloadLength < 8)
        {
            return;
        }

        var widthFixed = ReadUInt32BigEndian(data, payloadLength - 8);
        var heightFixed = ReadUInt32BigEndian(data, payloadLength - 4);
        track.Width = (int)(widthFixed >> 16);
        track.Height = (int)(heightFixed >> 16);
    }

    private static void ParseSampleDescription(BinaryReader reader, long boxEnd, TrackInfo track)
    {
        var payloadLength = checked((int)(boxEnd - reader.BaseStream.Position));
        if (payloadLength < 16)
        {
            return;
        }

        var data = reader.ReadBytes(payloadLength);
        var entryCount = ReadUInt32BigEndian(data, 4);
        if (entryCount == 0 || payloadLength < 16)
        {
            return;
        }

        track.Codec = ReadFourCc(data, 12);
    }

    private static uint ReadUInt32BigEndian(BinaryReader reader)
    {
        Span<byte> buffer = stackalloc byte[4];
        var read = reader.Read(buffer);
        if (read != 4)
        {
            throw new EndOfStreamException();
        }

        return ReadUInt32BigEndian(buffer);
    }

    private static uint ReadUInt32BigEndian(ReadOnlySpan<byte> data)
    {
        return ((uint)data[0] << 24) | ((uint)data[1] << 16) | ((uint)data[2] << 8) | data[3];
    }

    private static uint ReadUInt32BigEndian(byte[] data, int offset)
    {
        return ReadUInt32BigEndian(data.AsSpan(offset, 4));
    }

    private static ulong ReadUInt64BigEndian(BinaryReader reader)
    {
        Span<byte> buffer = stackalloc byte[8];
        var read = reader.Read(buffer);
        if (read != 8)
        {
            throw new EndOfStreamException();
        }

        return ReadUInt64BigEndian(buffer);
    }

    private static ulong ReadUInt64BigEndian(ReadOnlySpan<byte> data)
    {
        return ((ulong)data[0] << 56) |
               ((ulong)data[1] << 48) |
               ((ulong)data[2] << 40) |
               ((ulong)data[3] << 32) |
               ((ulong)data[4] << 24) |
               ((ulong)data[5] << 16) |
               ((ulong)data[6] << 8) |
               data[7];
    }

    private static ulong ReadUInt64BigEndian(byte[] data, int offset)
    {
        return ReadUInt64BigEndian(data.AsSpan(offset, 8));
    }

    private static string ReadFourCc(BinaryReader reader)
    {
        Span<byte> buffer = stackalloc byte[4];
        var read = reader.Read(buffer);
        if (read != 4)
        {
            throw new EndOfStreamException();
        }

        return ReadFourCc(buffer);
    }

    private static string ReadFourCc(ReadOnlySpan<byte> data)
    {
        return Encoding.ASCII.GetString(data);
    }

    private static string ReadFourCc(byte[] data, int offset)
    {
        return ReadFourCc(data.AsSpan(offset, 4));
    }

    private sealed class ParseContext
    {
        public bool HasFtyp { get; set; }
        public List<TrackInfo> Tracks { get; } = [];
    }

    private sealed class TrackInfo
    {
        public string HandlerType { get; set; } = string.Empty;
        public string? Codec { get; set; }
        public uint TimeScale { get; set; }
        public ulong DurationUnits { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
