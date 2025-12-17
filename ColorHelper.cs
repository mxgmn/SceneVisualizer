static class ColorHelper
{
    public static int IntFromRGB(byte r, byte g, byte b) => (255 << 24) + (r << 16) + (g << 8) + b;

    //public static int IntFromString(this string hex) => (255 << 24) + Convert.ToInt32(hex, 16);

    public static (byte, byte, byte) BytesFromInt(int i)
    {
        byte r = (byte)((i & 0xff0000) >> 16);
        byte g = (byte)((i & 0xff00) >> 8);
        byte b = (byte)(i & 0xff);
        return (r, g, b);
    }

    public const int BLACK = 255 << 24;
    public const int ALMOSTWHITE = (255 << 24) + (254 << 16) + (254 << 8) + 254;
    public const int RED = (255 << 24) + (255 << 16);
    public const int GRAY = (255 << 24) + (194 << 16) + (195 << 8) + 199;
    //public const int PURPLE = (255 << 24) + (75 << 16) + (0 << 8) + 130;
    public const int PURPLE = (255 << 24) + (171 << 16) + (39 << 8) + 227;
}
