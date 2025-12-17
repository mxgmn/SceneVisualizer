#if false
using System.Drawing;
using System.Drawing.Imaging;

static class BitmapHelper
{
    public static (int[] bitmap, int width, int height) LoadBitmap(string filename)
    {
        Bitmap bitmap = new(filename);
        int width = bitmap.Width, height = bitmap.Height;
        var bits = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        int[] result = new int[bitmap.Width * bitmap.Height];
        System.Runtime.InteropServices.Marshal.Copy(bits.Scan0, result, 0, result.Length);
        bitmap.UnlockBits(bits);
        bitmap.Dispose();
        return (result, width, height);
    }

    public static void SaveBitmap(this int[] data, int width, int height, string filename)
    {
        Bitmap result = new(width, height);
        var bits = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        System.Runtime.InteropServices.Marshal.Copy(data, 0, bits.Scan0, data.Length);
        result.UnlockBits(bits);
        result.Save(filename);
    }
    public static void SaveBitmap(this int[] data, int width, int height, string filename, int SCALE)
    {
        int[] result = new int[data.Length * SCALE * SCALE];
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                int d = data[x + y * width];
                for (int dy = 0; dy < SCALE; dy++) for (int dx = 0; dx < SCALE; dx++)
                        result[x * SCALE + dx + (y * SCALE + dy) * width * SCALE] = d;
            }
        SaveBitmap(result, width * SCALE, height * SCALE, filename);
    }
}
#endif
