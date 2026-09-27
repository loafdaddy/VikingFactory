using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace VikingFactory
{
    /// <summary>
    /// Loads an 8-bit RGBA PNG. The game build cannot reference Unity's image module.
    /// </summary>
    internal static class PngIcon
    {
        public static bool TryLoad(string path, out Texture2D texture)
        {
            texture = null;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;
            try
            {
                var bytes = File.ReadAllBytes(path);
                int width;
                int height;
                var pixels = Decode(bytes, out width, out height);
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.SetPixels32(pixels);
                texture.Apply();
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                return true;
            }
            catch (Exception)
            {
                texture = null;
                return false;
            }
        }

        private static Color32[] Decode(byte[] png, out int width, out int height)
        {
            if (png.Length < 8 || png[0] != 137 || png[1] != 80 || png[2] != 78 || png[3] != 71)
                throw new InvalidDataException("Not a PNG.");
            width = 0;
            height = 0;
            byte bitDepth = 0;
            byte colorType = 0;
            byte interlace = 0;
            var compressed = new MemoryStream();
            var offset = 8;
            while (offset + 8 <= png.Length)
            {
                var length = ReadInt(png, offset);
                var kind = System.Text.Encoding.ASCII.GetString(png, offset + 4, 4);
                offset += 8;
                if (offset + length + 4 > png.Length)
                    throw new InvalidDataException("Truncated PNG.");
                if (kind == "IHDR")
                {
                    width = ReadInt(png, offset);
                    height = ReadInt(png, offset + 4);
                    bitDepth = png[offset + 8];
                    colorType = png[offset + 9];
                    interlace = png[offset + 12];
                }
                else if (kind == "IDAT")
                {
                    compressed.Write(png, offset, length);
                }
                else if (kind == "IEND")
                {
                    break;
                }
                offset += length + 4;
            }

            if (width <= 0 || height <= 0 || bitDepth != 8 || colorType != 6 || interlace != 0)
                throw new InvalidDataException("Only 8-bit RGBA icons are supported.");

            var inflated = Inflate(compressed.ToArray());
            var stride = width * 4;
            if (inflated.Length < height * (stride + 1))
                throw new InvalidDataException("PNG pixel data is short.");

            var raw = new byte[height * stride];
            var source = 0;
            for (var y = 0; y < height; y++)
            {
                var filter = inflated[source++];
                var row = y * stride;
                for (var x = 0; x < stride; x++)
                {
                    var value = inflated[source++];
                    var left = x >= 4 ? raw[row + x - 4] : (byte)0;
                    var up = y > 0 ? raw[row - stride + x] : (byte)0;
                    var upLeft = y > 0 && x >= 4 ? raw[row - stride + x - 4] : (byte)0;
                    raw[row + x] = (byte)(value + Recon(filter, left, up, upLeft));
                }
            }

            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                var src = (height - 1 - y) * stride;
                var dst = y * width;
                for (var x = 0; x < width; x++)
                {
                    pixels[dst + x] = new Color32(raw[src], raw[src + 1], raw[src + 2], raw[src + 3]);
                    src += 4;
                }
            }
            return pixels;
        }

        private static byte[] Inflate(byte[] zlib)
        {
            if (zlib.Length < 6)
                throw new InvalidDataException("PNG compression stream is short.");
            using (var input = new MemoryStream(zlib, 2, zlib.Length - 6))
            using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                deflate.CopyTo(output);
                return output.ToArray();
            }
        }

        private static int Recon(byte filter, byte left, byte up, byte upLeft)
        {
            switch (filter)
            {
                case 0:
                    return 0;
                case 1:
                    return left;
                case 2:
                    return up;
                case 3:
                    return (left + up) / 2;
                case 4:
                    var p = left + up - upLeft;
                    var pa = Math.Abs(p - left);
                    var pb = Math.Abs(p - up);
                    var pc = Math.Abs(p - upLeft);
                    if (pa <= pb && pa <= pc)
                        return left;
                    return pb <= pc ? up : upLeft;
                default:
                    throw new InvalidDataException("Unknown PNG filter.");
            }
        }

        private static int ReadInt(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
        }
    }
}
