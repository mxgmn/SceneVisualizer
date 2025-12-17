//погодите, но для AO мне нужна именно случайная точка в сфере! Или тут важно, что поверхность ловит больше света именно с нормальных направлений?
//думаю, что важно: для солнца нормаль важна - значит, она должна быть важна и для неба.

using System;

static class Helper
{
    public static string Shorten(this string s, char c, int occurrence)
    {
        int currentIndex = -1;
        for (int i = 0; i < occurrence; i++)
        {
            currentIndex = s.IndexOf(c, currentIndex + 1);
            if (currentIndex == -1) return s;
        }
        return s[..currentIndex];
    }

    public static string Shorten(this string s)
    {
        if (s.Contains(' ')) return Shorten(s, ' ', 3);
        else if (s.Contains('_')) return Shorten(s, '_', 1);
        else return s;        
    }

    /*static float goldenAngle = MathF.PI * (3.0f - MathF.Sqrt(5.0f));
    public static Vec RandomInUnitSphere(int i, int totalSamples)
    {
        float y = 1.0f - (i / (float)(totalSamples - 1)) * 2.0f;
        float radius = MathF.Sqrt(1.0f - y * y);
        float theta = goldenAngle * i;
        float x = MathF.Cos(theta) * radius;
        float z = MathF.Sin(theta) * radius;
        return new Vec(x, y, z);
    }

    public static Vec RandomInUnitBall(int i, int totalSamples, Random random)
    {
        float y = 1.0f - (i / (float)(totalSamples - 1)) * 2.0f;
        float radius = MathF.Sqrt(1.0f - y * y);

        float theta = goldenAngle * i;

        float x = MathF.Cos(theta) * radius;
        float z = MathF.Sin(theta) * radius;

        float R = MathF.Pow(random.NextSingle(), 1.0f / 3.0f);
        return new Vec(R * x, R * y, R * z);
    }*/

    public static Vec RandomInUnitBall(Random random)
    {
        Vec v;
        do v = 2.0f * new Vec(random.NextSingle(), random.NextSingle(), random.NextSingle()) - new Vec(1.0f, 1.0f, 1.0f);
        while (v.LengthSquared() >= 1.0);
        return v;
    }

    /*public static void Denoise(this float[] occlusion, int[] indices, int RX, int RY, float[] averages, int RADIUS)
    {
        for (int y = 0; y < RY; y++) for (int x = 0; x < RX; x++)
            {
                float sum = 0.0f;
                int i = x + y * RX;
                int index = indices[i];
                if (index < 0)
                {
                    averages[i] = 1.0f;
                    continue;
                }
                int summands = 0;
                for (int dy = -RADIUS; dy <= RADIUS; dy++)
                {
                    int ydy = y + dy;
                    if (ydy < 0 || ydy >= RY) continue;
                    for (int dx = -RADIUS; dx <= RADIUS; dx++)
                    {
                        int xdx = x + dx;
                        if (xdx < 0 || xdx >= RX) continue;
                        int idi = xdx + ydy * RX;
                        if (indices[idi] == index)
                        {
                            sum += occlusion[idi];
                            summands++;
                        }
                    }
                }
                averages[i] = sum / summands;
            }
        Array.Copy(averages, occlusion, averages.Length);
    }*/

    //static float NORMMULT = 1.0f / MathF.Sqrt(2.0f * MathF.PI);
    //public static float StandardNormal(float x) => NORMMULT * MathF.Exp(-0.5f * x * x);
    static float NORMMULT2D = 0.5f / MathF.PI;
    public static float StandardNormal2D(float x, float y) => NORMMULT2D * MathF.Exp(-0.5f * (x * x + y * y));
}
