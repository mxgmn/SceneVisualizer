//Copyright (C) 2025 Maxim Gumin, The MIT License (MIT)

using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;

class Texture
{
    public float[] data;
    public int MX, MY;

    public Texture(string name, float darkening)
    {
        (int[] arrowBitmap, MX, MY) = ImageSharpHelper.LoadBitmap($"resources/{name}.png");
        data = new float[arrowBitmap.Length];
        for (int i = 0; i < arrowBitmap.Length; i++)
        {
            float a = ColorHelper.BytesFromInt(arrowBitmap[i]).Item1 / 255.0f;
            data[i] = a * (1.0f - darkening) + darkening;
        }
    }
}

enum OBJECT { OBJECT, WALL, FLOOR, DOOR, WINDOW };

class Cuboid
{
    public Vec center, color, size;
    public int direction;
    public string name;
    public OBJECT otype;
    int oindex;

    Vec min, max;
    float radiusSquared;

    Texture texture;
    Scene scene;
    float rhx, rhz;
    public (float, float)[] points;

    static Texture arrowTexture, doorTexture, windowTexture;
    public static Dictionary<string, OBJECT> typedict = new()
    {
        ["OBJECT"] = OBJECT.OBJECT,
        ["WALL"] = OBJECT.WALL,
        ["FLOOR"] = OBJECT.FLOOR,
        ["DOOR"] = OBJECT.DOOR,
        ["WINDOW"] = OBJECT.WINDOW,
    };
    //const float BANDWIDTH_MULTIPLIER = 0.1f; // 0.1f;
    const float MAX = 0.2f, MIN = 0.05f, BOUNDALPHA = 0.75f;//, STAIRMULT = 5.0f;

    static Cuboid()
    {
        arrowTexture = new Texture("arrow", 0.75f);
        doorTexture = new Texture("door", 0.6f);
        windowTexture = new Texture("window", 0.6f);
    }

    const float ZERO = 0.00001f, ALPHA = 0.98f, BETA = 0.5f * (1.0f - ALPHA);

    public Cuboid(JsonElement xobject, Scene scene)
    {
        this.scene = scene;

        name = xobject.GetProperty("name").GetString();
        center = new Vec(xobject.GetProperty("position").EnumerateArray().Select(x => x.GetSingle()).ToArray());
        size = new Vec(xobject.GetProperty("size").EnumerateArray().Select(x => x.GetSingle()).ToArray());

        bool typeSuccess = xobject.TryGetProperty("type", out JsonElement xtype);
        otype = typeSuccess ? typedict[xtype.GetString()] : OBJECT.OBJECT;

        direction = -1;
        if (xobject.TryGetProperty("facing", out JsonElement xdirection)) direction = Array.IndexOf(Scene.directions, xdirection.GetString());

        if (xobject.TryGetProperty("points", out JsonElement xpoints))
        {
            float[] pointArray = xpoints.EnumerateArray().Select(x => x.GetSingle()).ToArray();
            if (pointArray.Length % 2 != 0) throw new Exception("point array length should be even");
            points = new (float, float)[pointArray.Length / 2];
            for (int i = 0; i < points.Length; i++) points[i] = (pointArray[2 * i], pointArray[2 * i + 1]);
        }

        this.oindex = oindex;
        Init();
    }

    public Cuboid(string name, OBJECT otype, Vec size, Vec center, int direction, Vec color, Scene scene)
    {
        this.scene = scene;
        this.name = name;
        this.center = center;
        this.size = size;
        this.color = color;
        this.direction = direction;
        this.otype = otype;

        Init();
    }

    void Init()
    {
        if (otype == OBJECT.DOOR) texture = doorTexture;
        else if (otype == OBJECT.WINDOW) texture = windowTexture;
        else if (otype == OBJECT.FLOOR)
        {
            rhx = 1.0f / (scene.bandwidthMult * size.X);
            rhz = 1.0f / (scene.bandwidthMult * size.Z);
        }

        if (otype == OBJECT.WALL && direction == 1)
        {
            float FLOOR_THICKNESS = 0.5f * size.Y - center.Y;
            size.Y = FLOOR_THICKNESS;
            center.Y = -0.5f * FLOOR_THICKNESS;
        }

        min = center - 0.5f * size;
        max = center + 0.5f * size;

        radiusSquared = 0.25f * size.LengthSquared();
    }

    public Hit FirstHit(Ray ray)
    {
        Vec OP = center - ray.origin;
        float dot = Vec.DotProduct(OP, ray.vector);
        Vec perpend = OP - dot * ray.vector;
        if (perpend.LengthSquared() > radiusSquared) return null;

        float tmin = float.MaxValue;
        Vec normal = Vec.Zero;
        Vec c = color;

        if (ray.vector.X != 0.0f)
        {
            float t0 = (center.X + 0.5f * size.X - ray.origin.X) / ray.vector.X;
            if (t0 > ZERO)
            {
                Vec q0 = ray.Point(t0);
                if (q0.Y >= min.Y && q0.Y <= max.Y && q0.Z >= min.Z && q0.Z <= max.Z && t0 < tmin)
                {
                    tmin = t0;
                    normal = new Vec(1, 0, 0);
                    if (texture != null && direction == 0)
                    {
                        float u = (q0.Z - min.Z) / size.Z;
                        float v = (q0.Y - min.Y) / size.Y;
                        int tx = (int)((u * ALPHA + BETA) * texture.MX);
                        int ty = (int)((v * ALPHA + BETA) * texture.MY);
                        c *= texture.data[tx + ty * texture.MX];
                    }
                }
            }

            float t1 = (center.X - 0.5f * size.X - ray.origin.X) / ray.vector.X;
            if (t1 > ZERO)
            {
                Vec q1 = ray.Point(t1);
                if (q1.Y >= min.Y && q1.Y <= max.Y && q1.Z >= min.Z && q1.Z <= max.Z && t1 < tmin)
                {
                    tmin = t1;
                    normal = new Vec(-1, 0, 0);
                    if (texture != null && direction == 2)
                    {
                        float u = (q1.Z - min.Z) / size.Z;
                        float v = (q1.Y - min.Y) / size.Y;
                        int tx = (int)((u * ALPHA + BETA) * texture.MX);
                        int ty = (int)((v * ALPHA + BETA) * texture.MY);
                        c *= texture.data[tx + ty * texture.MX];
                    }
                }
            }
        }
        if (ray.vector.Y != 0.0f)
        {
            float t2 = (center.Y + 0.5f * size.Y - ray.origin.Y) / ray.vector.Y;
            if (t2 > ZERO) //up
            {
                Vec q2 = ray.Point(t2);
                if (q2.X >= min.X && q2.X <= max.X && q2.Z >= min.Z && q2.Z <= max.Z && t2 < tmin)
                {
                    tmin = t2;
                    normal = new Vec(0, 1, 0);
                    if (direction >= 0)
                    {
                        float u = (q2.X - min.X) / size.X;
                        float v = (q2.Z - min.Z) / size.Z;

                        if (direction == 1) (u, v) = (1.0f - v, u);
                        else if (direction == 2) u = 1.0f - u;
                        else if (direction == 3) (u, v) = (v, u);

                        int tx = (int)((u * ALPHA + BETA) * arrowTexture.MX);
                        int ty = (int)((v * ALPHA + BETA) * arrowTexture.MY);

                        c *= arrowTexture.data[tx + ty * arrowTexture.MX];
                    }
                }
                if (scene.hasPoints && otype == OBJECT.FLOOR)
                {
                    Vec sumBoundary = Vec.Zero, sumInner = Vec.Zero;
                    int numBoundary = 0, numInner = 0;
                    for (int k = 0; k < scene.cuboids.Length; k++)
                    {
                        Cuboid cuboid = scene.cuboids[k];
                        if (cuboid.points == null) continue;
                        float f = cuboid.Function(q2.X, q2.Z, rhx, rhz);
                        float abs = MathF.Abs(f - MIN);
                        (float dx, float dz) = cuboid.Gradient(q2.X, q2.Z, rhx, rhz);
                        float dl = 0.02f * MathF.Sqrt(dx * dx + dz * dz);
                        if (abs <= dl)
                        {
                            sumBoundary += ((1.0f - BOUNDALPHA) * color + BOUNDALPHA * cuboid.color).Square();
                            numBoundary++;
                        }
                        else if (numBoundary == 0 && f > MAX)
                        {
                            sumInner += cuboid.color.Square();
                            numInner++;
                        }
                        else if (numBoundary == 0 && f > MIN)
                        {
                            //float value = MathF.Floor(STAIRMULT * f / MAX) / STAIRMULT;
                            sumInner += ((f / MAX) * (cuboid.color - color) + color).Square();
                            numInner++;
                        }                        
                    }
                    if (numBoundary > 0) c = ((1.0f / numBoundary) * sumBoundary).Sqrt();
                    else if (numInner > 0) c = ((1.0f / numInner) * sumInner).Sqrt();
                }
            }

            //don't draw the bottom face
            /*float t3 = (center.Y - 0.5f * size.Y - ray.origin.Y) / ray.vector.Y;
            if (t3 > ZERO) //down
            {
                Vec q3 = ray.Point(t3);
                if (q3.X >= min.X && q3.X <= max.X && q3.Z >= min.Z && q3.Z <= max.Z && t3 < tmin)
                {
                    tmin = t3;
                    normal = new Vec(0, -1, 0);
                }
            }*/
        }
        if (ray.vector.Z != 0.0f)
        {
            float t4 = (center.Z + 0.5f * size.Z - ray.origin.Z) / ray.vector.Z;
            if (t4 > ZERO)
            {
                Vec q4 = ray.Point(t4);
                if (q4.X >= min.X && q4.X <= max.X && q4.Y >= min.Y && q4.Y <= max.Y && t4 < tmin)
                {
                    tmin = t4;
                    normal = new Vec(0, 0, 1);
                    if (texture != null && (direction == 1 || direction == 3))
                    {
                        float u = (q4.X - min.X) / size.X;
                        float v = (q4.Y - min.Y) / size.Y;
                        int tx = (int)((u * ALPHA + BETA) * texture.MX);
                        int ty = (int)((v * ALPHA + BETA) * texture.MY);
                        c *= texture.data[tx + ty * texture.MX];
                    }
                }
            }

            float t5 = (center.Z - 0.5f * size.Z - ray.origin.Z) / ray.vector.Z;
            if (t5 > ZERO)
            {
                Vec q5 = ray.Point(t5);
                if (q5.X >= min.X && q5.X <= max.X && q5.Y >= min.Y && q5.Y <= max.Y && t5 < tmin)
                {
                    tmin = t5;
                    normal = new Vec(0, 0, -1);
                }
            }
        }
        
        return tmin < float.MaxValue ? new Hit(ray, tmin, normal, c) : null;
    }

    public float DistanceField(Vec p)
    {
        Vec q = (p - center).Abs() - 0.5f * size;
        return q.ReLU().Length() + MathF.Min(MathF.Max(q.X, MathF.Max(q.Y, q.Z)), 0.0f);
    }

    float Function(float x, float z, float rhx, float rhz)
    {
        float result = 0.0f;
        for (int i = 0; i < points.Length; i++)
        {
            (float xi, float zi) = points[i];
            result += Helper.StandardNormal2D((x - xi) * rhx, (z - zi) * rhz);
        }
        return rhx * rhz * result / points.Length;
    }
    
    (float, float) Gradient(float x, float z, float rhx, float rhz)
    {
        float resultX = 0.0f, resultZ = 0.0f;
        for (int i = 0; i < points.Length; i++)
        {
            (float xi, float zi) = points[i];
            float snormal = Helper.StandardNormal2D((x - xi) * rhx, (z - zi) * rhz);
            resultX -= (x - xi) * snormal;
            resultZ -= (z - zi) * snormal;
        }
        float common = rhx * rhz / points.Length;
        return (common * rhx * resultX, common * rhz * resultZ);
    }
}
