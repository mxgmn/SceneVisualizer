using System;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

static class ImageSharpHelper
{
    public static (int[] bitmap, int width, int height) LoadBitmap(string filename)
    {
        using var image = Image.Load<Bgra32>(filename);
        int width = image.Width, height = image.Height;
        int[] result = new int[width * height];
        image.CopyPixelDataTo(MemoryMarshal.Cast<int, Bgra32>(result));
        return (result, width, height);
    }

    unsafe public static void SaveBitmap(this int[] data, int width, int height, string filename)
    {
        if (width <= 0 || height <= 0 || data.Length != width * height) throw new Exception($"ERROR: wrong image width * height = {width} * {height}");
        /*fixed (int* pData = data)
        {
            using var image = Image.WrapMemory<Bgra32>(pData, width, height);
            image.SaveAsPng(filename);
        }*/
        Span<Bgra32> pixelSpan = MemoryMarshal.Cast<int, Bgra32>(data);
        using var image = Image.LoadPixelData<Bgra32>(pixelSpan.ToArray(), width, height);
        image.SaveAsPng(filename);
    }

    /*public static void SaveFloatBitmap(this float[] data, int width, int height, string filename)
    {
        int[] bitmap = new int[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            byte r = (byte)(data[i] * 255.0f);
            bitmap[i] = (255 << 24) + (r << 16) + (r << 8) + r;
        }
        SaveBitmap(bitmap, width, height, filename);
    }*/
}

