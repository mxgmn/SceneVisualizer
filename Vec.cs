//вполне можно было учитывать и встроенный вектор. Нужно замерить, какой быстрее!

using System;

struct Vec
{
    public float X, Y, Z;

    public static Vec Zero = new(0.0f, 0.0f, 0.0f);
    public static Vec WHITE = new(1.0f, 1.0f, 1.0f);
    public static Vec GREEN = new(0.0f, 1.0f, 0.0f);

    public Vec(float X, float Y, float Z) { this.X = X; this.Y = Y; this.Z = Z; }
    public Vec(float[] a)
    {
        X = a[0];
        Y = a[1];
        Z = a[2];
    }
    
    public static Vec Gray(float A) => new(A, A, A);
    public static Vec VecFromInt(int i)
    {
        var (r, g, b) = ColorHelper.BytesFromInt(i);
        return new Vec(r / 255.0f, g / 255.0f, b / 255.0f);
    }

    //public Vec(Random random)
    //{
    //    X = random.NextSingle();
    //    Y = random.NextSingle();
    //    Z = random.NextSingle();
    //}
    public Vec(string color)
    {
        int c = Convert.ToInt32(color, 16);
        X = ((c & 0xff0000) >> 16) / 255.0f;
        Y = ((c & 0xff00) >> 8) / 255.0f;
        Z = (c & 0xff) / 255.0f;
    }

    public static Vec operator +(Vec v1, Vec v2) => new(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
    public static Vec operator -(Vec v1, Vec v2) => new(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
    //public static Vec operator *(Vec v1, Vec v2) => new(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z);
    public static Vec operator *(float a, Vec v) => new(a * v.X, a * v.Y, a * v.Z);
    public static Vec operator *(Vec v, float a) => new(a * v.X, a * v.Y, a * v.Z);
    //public static Vec operator /(Vec v, float a) => new(v.X / a, v.Y / a, v.Z / a);
    public static Vec operator -(Vec v) => new(-v.X, -v.Y, -v.Z);

    //public static Vec Min(Vec v1, Vec v2) => new(MathF.Min(v1.X, v2.X), MathF.Min(v1.Y, v2.Y), MathF.Min(v1.Z, v2.Z));
    //public static Vec Max(Vec v1, Vec v2) => new(MathF.Max(v1.X, v2.X), MathF.Max(v1.Y, v2.Y), MathF.Max(v1.Z, v2.Z));
    public static float DotProduct(Vec v1, Vec v2) => v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
    public static Vec CrossProduct(Vec v1, Vec v2) => new(v1.Y * v2.Z - v1.Z * v2.Y, v1.Z * v2.X - v1.X * v2.Z, v1.X * v2.Y - v1.Y * v2.X);

    public readonly float LengthSquared() => X * X + Y * Y + Z * Z;
    public readonly float Length() => MathF.Sqrt(X * X + Y * Y + Z * Z);
    //public readonly float Volume() => X * Y * Z;
    public readonly Vec ReLU() => new(MathF.Max(X, 0.0f), MathF.Max(Y, 0.0f), MathF.Max(Z, 0.0f));
    public readonly Vec Abs() => new(MathF.Abs(X), MathF.Abs(Y), MathF.Abs(Z));
    public readonly Vec Square() => new(X * X, Y * Y, Z * Z);
    public readonly Vec Sqrt() => new(MathF.Sqrt(X), MathF.Sqrt(Y), MathF.Sqrt(Z));
    public readonly Vec Normalized()
    {
        float length = Length();
        return new Vec(X / length, Y / length, Z / length);
    }
    public void Normalize()
    {
        float length = Length();
        X /= length;
        Y /= length;
        Z /= length;
    }
    public readonly Vec GammaCorrect(float GAMMA)
    {
        float x = MathF.Pow(X, GAMMA);
        float y = MathF.Pow(Y, GAMMA);
        float z = MathF.Pow(Z, GAMMA);
        return new Vec(x, y, z);
    }

    public readonly Vec Randomize(float r, Random random)
    {
        float x = X * MathF.Exp(r * (2.0f * random.NextSingle() - 1.0f));
        float y = Y * MathF.Exp(r * (2.0f * random.NextSingle() - 1.0f));
        float z = Z * MathF.Exp(r * (2.0f * random.NextSingle() - 1.0f));
        return new Vec(Math.Clamp(x, 0.0f, 1.0f), Math.Clamp(y, 0.0f, 1.0f), Math.Clamp(z, 0.0f, 1.0f));
    }

    public readonly int ToIntColor()
    {
        byte r = (byte)(X * 255.0f);
        byte g = (byte)(Y * 255.0f);
        byte b = (byte)(Z * 255.0f);
        return (255 << 24) + (r << 16) + (g << 8) + b;
    }

    public override readonly string ToString() => $"({X:0.00}, {Y:0.00}, {Z:0.00})";
    public readonly string ToFreeString() => $"({X:#.####}, {Y:#.####}, {Z:#.####})";
}

class Zec
{
    public int x, y, z;

    public Zec(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
    public Zec(int[] a)
    {
        x = a[0];
        y = a[1];
        z = a[2];
    }

    static string Spaces(int x)
    {
        string result = "";
        if (x <= 9 && x >= 0) result += "   ";
        else if (x <= 99 && x >= -9) result += "  ";
        else if (x <= 999 && x >= -99) result += " ";
        return result + (x == 0 ? " " : x);
    }

    public string PrettyString() => x == 0 && y == 0 && z == 0 ? "" : "  " + Spaces(x) + " " + Spaces(y) + " " + Spaces(z);
}
