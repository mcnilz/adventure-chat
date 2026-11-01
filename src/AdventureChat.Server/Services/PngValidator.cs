using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace AdventureChat.Server.Services;

internal static class PngValidator
{
    private const int MaxBytes = 64 * 1024 * 1024;
    private const int MaxDecodedBytes = 128 * 1024 * 1024;
    private static readonly uint[] CrcTable = CreateCrcTable();

    public static void Validate(string file, int width, int height)
    {
        using var stream = File.OpenRead(file);
        if (stream.Length > MaxBytes) throw new InvalidOperationException("PNG exceeds 64 MB.");
        Span<byte> signature = stackalloc byte[8];
        stream.ReadExactly(signature);
        if (!signature.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) throw new InvalidOperationException("Invalid PNG signature.");
        using var compressed = new MemoryStream();
        var header = new byte[8];
        var checksum = new byte[4];
        var sawHeader = false;
        var sawPalette = false;
        var sawData = false;
        var dataEnded = false;
        var ended = false;
        var bitsPerPixel = 0;
        var interlaced = false;
        var indexed = false;
        while (stream.Position < stream.Length)
        {
            stream.ReadExactly(header);
            var length = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0, 4));
            if (length > MaxBytes || length + 4L > stream.Length - stream.Position) throw new InvalidOperationException("Truncated PNG chunk.");
            var type = Encoding.ASCII.GetString(header, 4, 4);
            var data = new byte[(int)length];
            stream.ReadExactly(data);
            stream.ReadExactly(checksum);
            if (Crc(header.AsSpan(4, 4), data) != BinaryPrimitives.ReadUInt32BigEndian(checksum)) throw new InvalidOperationException("Invalid PNG checksum.");
            if (!sawHeader)
            {
                if (type != "IHDR" || data.Length != 13 || BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0, 4)) != width ||
                    BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4, 4)) != height || data[10] != 0 || data[11] != 0 || data[12] > 1)
                    throw new InvalidOperationException("Invalid PNG header or dimensions.");
                var depth = data[8];
                var channels = data[9] switch { 0 => 1, 2 => 3, 3 => 1, 4 => 2, 6 => 4, _ => 0 };
                var validDepth = data[9] switch
                {
                    0 => depth is 1 or 2 or 4 or 8 or 16,
                    2 or 4 or 6 => depth is 8 or 16,
                    3 => depth is 1 or 2 or 4 or 8,
                    _ => false
                };
                if (!validDepth) throw new InvalidOperationException("Invalid PNG colour format.");
                bitsPerPixel = depth * channels;
                indexed = data[9] == 3;
                interlaced = data[12] == 1;
                sawHeader = true;
            }
            else if (type == "IHDR") throw new InvalidOperationException("Duplicate PNG header.");
            if (sawData && type != "IDAT") dataEnded = true;
            if (type == "PLTE")
            {
                if (sawData || sawPalette || data.Length is 0 or > 768 || data.Length % 3 != 0) throw new InvalidOperationException("Invalid PNG palette.");
                sawPalette = true;
            }
            if (type == "IDAT")
            {
                if (dataEnded || (indexed && !sawPalette)) throw new InvalidOperationException("Invalid PNG image data order.");
                compressed.Write(data);
                sawData = true;
            }
            if (type == "IEND")
            {
                if (data.Length != 0 || stream.Position != stream.Length || !sawData) throw new InvalidOperationException("Invalid PNG end marker.");
                ended = true;
                break;
            }
            if (type is not ("IHDR" or "PLTE" or "IDAT" or "IEND") && (header[4] & 0x20) == 0)
                throw new InvalidOperationException("Unsupported critical PNG chunk.");
        }
        if (!ended) throw new InvalidOperationException("PNG end marker is missing.");
        compressed.Position = 0;
        using var decoder = new ZLibStream(compressed, CompressionMode.Decompress);
        var passes = interlaced
            ? new (int X, int Y, int Dx, int Dy)[] { (0, 0, 8, 8), (4, 0, 8, 8), (0, 4, 4, 8), (2, 0, 4, 4), (0, 2, 2, 4), (1, 0, 2, 2), (0, 1, 1, 2) }
            : [(0, 0, 1, 1)];
        var buffer = new byte[81920];
        long total = 0;
        foreach (var (x, y, dx, dy) in passes)
        {
            var passWidth = width <= x ? 0 : (width - x + dx - 1) / dx;
            var passHeight = height <= y ? 0 : (height - y + dy - 1) / dy;
            if (passWidth == 0 || passHeight == 0) continue;
            var rowBytes = ((long)passWidth * bitsPerPixel + 7) / 8;
            total += (rowBytes + 1) * passHeight;
            if (total > MaxDecodedBytes) throw new InvalidOperationException("Decoded PNG exceeds 128 MB.");
            for (var row = 0; row < passHeight; row++)
            {
                var filter = decoder.ReadByte();
                if (filter is < 0 or > 4) throw new InvalidOperationException("Invalid or truncated PNG scanline.");
                for (var remaining = rowBytes; remaining > 0;)
                {
                    var count = decoder.Read(buffer, 0, (int)Math.Min(remaining, buffer.Length));
                    if (count == 0) throw new InvalidOperationException("Truncated PNG image data.");
                    remaining -= count;
                }
            }
        }
        if (decoder.ReadByte() != -1) throw new InvalidOperationException("Excess PNG image data.");
    }

    private static uint Crc(ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        uint crc = uint.MaxValue;
        foreach (var value in type) crc = CrcTable[(crc ^ value) & 0xff] ^ (crc >> 8);
        foreach (var value in data) crc = CrcTable[(crc ^ value) & 0xff] ^ (crc >> 8);
        return ~crc;
    }

    private static uint[] CreateCrcTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            var value = i;
            for (var bit = 0; bit < 8; bit++) value = (value >> 1) ^ (0xedb88320u & (uint)-(int)(value & 1));
            table[i] = value;
        }
        return table;
    }
}
