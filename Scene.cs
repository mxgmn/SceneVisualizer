//Copyright (C) 2025 Maxim Gumin, The MIT License (MIT)

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;

class Scene
{
    public Vec sceneSize;
    public Cuboid[] cuboids;
    Relation[] relations;
    string name;
    int seed;
    public Random random;

    int numberOfGroups = 0, numberOfConstants = -1;
    int degreesOfFreedom = -1;
    float degreesPerObject = -1.0f;
    float fillPercentage = -1.0f;
    float minScore = 0.0f, sumScore = 0.0f;
    public float bandwidthMult = 0.1f;
    public bool hasPoints;

    static readonly Vec toSun;
    static readonly Vec[] palette;
    static readonly int ONWHITE = ColorHelper.IntFromRGB(200, 200, 200);
    public static readonly string[] directions = ["EAST", "NORTH", "WEST", "SOUTH"];
    const string formatString = $"{{0:0.}}";
    static Scene()
    {
        palette = ImageSharpHelper.LoadBitmap("resources/palette.png").bitmap.Select(Vec.VecFromInt).ToArray();

        const float sunrise = 0.55f * 0.5f * MathF.PI, sunangle = 0.25f * 0.5f * MathF.PI;
        toSun = new Vec(MathF.Cos(sunrise) * MathF.Cos(sunangle), MathF.Sin(sunrise), MathF.Cos(sunrise) * MathF.Sin(sunangle));
    }

    public Scene(string inputFilename, bool uniqueColors)
    {
        random = new();
        string text = File.ReadAllText(inputFilename);
        JsonElement root = JsonDocument.Parse(text).RootElement;
        name = root.GetProperty("name").GetString();
        bool interior = true;
        if (root.TryGetProperty("interior", out JsonElement xinterior)) interior = xinterior.GetBoolean();

        float[] sceneSizeArray = root.GetProperty("size").EnumerateArray().Select(x => x.GetSingle()).ToArray();
        sceneSize = new(sceneSizeArray);

        if (root.TryGetProperty("degrees_of_freedom", out var xdegrees)) degreesOfFreedom = xdegrees.GetInt32();
        if (root.TryGetProperty("degrees_of_freedom_per_object", out var xdpo)) degreesPerObject = xdpo.GetSingle();
        if (root.TryGetProperty("fill_percentage", out var xFillPercentage)) fillPercentage = xFillPercentage.GetSingle();
        if (root.TryGetProperty("min_score", out var xMinScore)) minScore = xMinScore.GetSingle();
        if (root.TryGetProperty("sum_score", out var xSumScore)) sumScore = xSumScore.GetSingle();
        if (root.TryGetProperty("bandwidth", out var xBandwidth)) bandwidthMult = xBandwidth.GetSingle();
        if (root.TryGetProperty("number_of_constants", out var xconstants)) numberOfConstants = xconstants.GetInt32();

        List<Cuboid> objectList = [];
        List<Relation> relationList = [];

        bool success = root.TryGetProperty("relations", out var xrelations);
        if (success) foreach (JsonElement xrelation in xrelations.EnumerateArray()) relationList.Add(new Relation(xrelation));
        relations = relationList.ToArray();

        Dictionary<int, Vec> groupColors = [];
        Dictionary<string, Vec> nameColors = [];

        Vec paletteColor()
        {
            Vec result = palette[numberOfGroups % palette.Length];
            numberOfGroups++;
            return result;
        };

        bool hasFloor = false;
        JsonElement[] xobjects = root.GetProperty("objects").EnumerateArray().ToArray();
        for (int i = 0; i < xobjects.Length; i++)
        {
            JsonElement xobject = xobjects[i];
            Cuboid o = new(xobject, this);
            if (o.otype == OBJECT.FLOOR) hasFloor = true;
            if (o.points != null) hasPoints = true;

            int group = -1;
            if (xobject.TryGetProperty("group", out JsonElement xgroup)) group = xgroup.GetInt32();

            //if (o.otype == OBJECT.FLOOR) o.color = Vec.Gray(0.4f);
            //else if (o.otype == OBJECT.WALL) o.color = Vec.Gray(0.9f);
            if (uniqueColors) //используется только эта ветвь
            {
                string colorstr = null;
                if (xobject.TryGetProperty("color", out var xcolor)) colorstr = xcolor.GetString();
                const float A = 0.5f;

                if (o.otype == OBJECT.FLOOR)
                {
                    if (colorstr == null) o.color = Vec.Gray(0.4f);
                    else o.color = A * new Vec(colorstr) + (1f - A) * Vec.Gray(0.4f);
                }
                else if (o.otype == OBJECT.WALL)
                {
                    if (colorstr == null) o.color = Vec.Gray(0.9f);
                    else o.color = A * new Vec(colorstr) + (1f - A) * Vec.Gray(0.9f);
                }
                else if (colorstr != null) o.color = new Vec(colorstr);
                else if (group < 0) o.color = paletteColor();
                else if (groupColors.ContainsKey(group)) o.color = groupColors[group];
                else
                {
                    o.color = paletteColor();
                    groupColors.Add(group, o.color);
                }
            }
            else
            {
                if (nameColors.ContainsKey(o.name) && group < 0) o.color = nameColors[o.name];
                else
                {
                    o.color = paletteColor();
                    nameColors.TryAdd(o.name, o.color);
                }
            }

            if (interior || o.otype != OBJECT.WALL) objectList.Add(o);
        }

        if (!hasFloor)
        {
            Cuboid floor = new("FLOOR", OBJECT.FLOOR, new Vec(sceneSize.X, 0.1f, sceneSize.Z), new Vec(0, -0.05f, 0), -1, Vec.Gray(0.4f), this);
            objectList.Add(floor);
        }
        cuboids = objectList.ToArray();
    }

    public (Hit hit, int cuboidIndex) FirstHit(Ray ray)
    {
        float min = float.MaxValue;
        Hit argmin = null;
        int cuboidIndex = -1;
        for (int i = 0; i < cuboids.Length; i++)
        {
            Hit hit = cuboids[i].FirstHit(ray);
            if (hit != null && hit.t < min)
            {
                min = hit.t;
                argmin = hit;
                cuboidIndex = i;
            }
        }
        return (argmin, cuboidIndex);
    }

    float Field(Vec p)
    {
        float result = 1000000.0f;
        for (int i = 0; i < cuboids.Length; i++)
        {
            float value = cuboids[i].DistanceField(p);
            if (value < result) result = value;
        }
        return result;
    }

    float Occlusion(Vec pos, Vec normal, int runs)
    {
        if (runs == 0) return 1.0f;
        else if (runs < 0)
        {
            const float height = 0.08f, bound = 0.5f;
            float linear = (1.0f - bound) * Field(pos + height * normal) / height + bound;
            return MathF.Min(MathF.Max(linear, bound), 1.0f);
        }

        Random rayrandom = new(seed);
        int numberOfSkies = 0;
        for (int r = 0; r < runs; r++)
        {
            Vec bounce = normal + Helper.RandomInUnitBall(rayrandom);
            bounce.Normalize();
            Ray bounced = new(pos, bounce);
            if (FirstHit(bounced).hit == null) numberOfSkies++;
        }
        return (float)numberOfSkies / runs;
    }

    public (Vec color, int cuboidIndex) Color(Ray ray, int ao)
    {
        float sunIntensity = ao > 0 ? 0.25f : 0.5f;
        float skyIntensity = 1.0f - sunIntensity;

        (Hit hit, int cuboidIndex) = FirstHit(ray);
        if (hit != null)
        {
            float cos = Math.Max(Vec.DotProduct(hit.normal, toSun), 0.0f);
            Vec pos = ray.Point(hit.t);
            if (FirstHit(new Ray(pos + 0.001f * hit.normal, toSun)).hit != null) cos *= 0.075f; //прибавили тут небольшую нормаль, давайте посмотрим
            return (hit.color * (cos * sunIntensity + Occlusion(pos, hit.normal, ao) * skyIntensity), cuboidIndex);
        }
        return (Vec.WHITE, -1); //если у нас тут есть -1, то значение null можно уже не использовать для hit
    }

    public (int[] bitmap, int[] mask, int BX, int BY) TakeScreenshot(int RX, int RY, int TX, int ao, string fontname, float distance, int super, bool ortho, bool names, bool allnames)
    {
        //Vec size = cuboids[^1].size;
        float average = 0.5f * (sceneSize.X + sceneSize.Z);
        Camera camera;
        if (ortho) camera = new(average * distance / 6.0f, 0, 0, 1.0f);
        else camera = new(average * distance / 6.0f, 0.6f, 0.1f, 1.0f); //average * 11.0f

        int BX = RX + TX, BY = RY;

        int[] bitmap = new int[BX * BY];
        int[] mask = new int[BX * BY];
        //int almost = ColorHelper.IntFromRGB(255, 238, 238);
        //Console.WriteLine($"almost = {almost}");
        for (int i = 0; i < bitmap.Length; i++) //это нужно сделать через intrinsic fill, как в MJ
        {
            bitmap[i] = -1;
            mask[i] = -4370;
        }

        Random random = new();
        seed = random.Next(1000);
        if (super == 1)
        {
            for (int y = 0; y < RY; y++)
            {
                float v = (y + 0.5f) / RY; //эта штука пробегает от 0 до 1
                for (int x = 0; x < RX; x++)
                {
                    float u = (x + 0.5f) / RX;
                    (Vec color, int cuboidIndex) = Color(camera.CameraRay(u, v), ao);
                    int i = x + (RY - y - 1) * BX;
                    bitmap[i] = color.GammaCorrect(0.75f).ToIntColor();
                    if (cuboidIndex >= 0)
                    {
                        byte b = (byte)(cuboidIndex * 31 % 256);
                        mask[i] = ColorHelper.IntFromRGB(b, b, b); //cuboid.color.ToIntColor();
                    }
                }
            }
        }
        else
        {
            float ISUPER = 1.0f / super;
            float ISUPER2 = 1.0f / (super * super);
            Vec acc;
            for (int y = 0; y < RY; y++) for (int x = 0; x < RX; x++)
                {
                    acc = Vec.Zero;
                    int cuboidIndex = -1;
                    for (int dy = 0; dy < super; dy++) for (int dx = 0; dx < super; dx++)
                        {
                            float u = (x + dx * ISUPER) / RX;
                            float v = (y + dy * ISUPER) / RY;
                            (Vec color, cuboidIndex) = Color(camera.CameraRay(u, v), ao);
                            acc += color.GammaCorrect(0.75f);
                        }
                    int i = x + (RY - y - 1) * BX;
                    bitmap[i] = (acc * ISUPER2).ToIntColor();
                    if (cuboidIndex >= 0)
                    {
                        byte b = (byte)(cuboidIndex * 31 % 256);
                        mask[i] = ColorHelper.IntFromRGB(b, b, b);
                    }
                }
        }

        Writer big = new("Tamzen10x20", bitmap, BX, BY);
        if (names)
        {
            List<Rectangle> rectangles = [];

            for (int i = 0; i < cuboids.Length; i++)
            {
                Cuboid o = cuboids[i];
                if (o.otype == OBJECT.FLOOR || o.otype == OBJECT.WALL) continue;
                (float u, float v) = camera.Projection(o.center);
                float x = u * RX - o.name.Length * big.FX / 2.0f;
                float y = (1 - v) * RY - 1 - big.FY / 2.0f;

                Rectangle rect = big.Rect(o.name, (int)(x + 0.5f), (int)(y + 0.5f));
                if (allnames || !rect.Overlaps(rectangles))
                {
                    big.DrawString(o.name, (int)(x + 0.5f), (int)(y + 0.5f), ColorHelper.ALMOSTWHITE, true, ONWHITE);
                    rectangles.Add(rect);
                }
            }
        }
        big.DrawString(name, (RX - name.Length * big.FX) / 2, big.FY, ColorHelper.BLACK, true);
        if (minScore != 0.0f || sumScore != 0.0f)
        {
            string s = $"min_score = {minScore}, sum_score = {sumScore}";
            big.DrawString(s, (RX - s.Length * big.FX) / 2, RY - 3 * big.FY / 2, ColorHelper.BLACK, false);
        }

        Writer writer = new($"Tamzen{fontname}", bitmap, BX, BY);
        writer.Set(RX, writer.FY);
        float loss = relations.Where(r => r.active).Select(r => r.loss).Sum();
        float repelloss = relations.Where(r => r.active && (r.name == "repel" || r.name == "repelWall")).Select(r => r.loss).Sum();
        writer.WriteLine($"total loss = {string.Format(formatString, loss)}", ColorHelper.BLACK, loss > 0.00001);
        if (repelloss > 0.0f) writer.WriteLine($"repel loss = {repelloss:0.0000}", ColorHelper.BLACK, true);
        writer.WriteLine($"{cuboids.Where(c => c.otype != OBJECT.FLOOR && c.otype != OBJECT.WALL).Count()} objects", ColorHelper.BLACK, false);
        writer.WriteLine($"{relations.Length} errors", ColorHelper.BLACK, false);
        writer.WriteLine($"{numberOfConstants} constants", ColorHelper.BLACK, false);
        //writer.WriteLine($"degrees of freedom = {degreesOfFreedom}", ColorHelper.BLACK, false);
        //writer.WriteLine($"degrees of freedom per object = {degreesPerObject}", ColorHelper.BLACK, false);
        if (fillPercentage >= 0.0) writer.WriteLine($"fill percentage = {fillPercentage}", ColorHelper.BLACK, false);
        writer.WriteLine("", -1);
        
        WriteRelations(writer);
        return (bitmap, mask, BX, BY);
    }

    void WriteRelations(Writer writer)
    {
        int maxlength = -1;
        foreach (Relation relation in relations)
        {
            if (writer.GLOBALY + (writer.cy + 1) * writer.FY >= writer.BY)
            {
                writer.Set(writer.GLOBALX + writer.FX * (maxlength + 5), writer.GLOBALY);
                maxlength = -1;
            }

            int length = WriteRelation(writer, relation);
            if (length > maxlength) maxlength = length;
        }
    }

    int WriteRelation(Writer writer, Relation relation)
    {
        //bool bold = relation.active && relation.loss >= 0.0001f;
        bool bold = relation.active && relation.loss >= 10f;
        string prefix = (relation.loss >= 10.0f || relation.loss < 0) ? "" : " ";

        int length = 0;
        string loss = relation.active ? prefix + string.Format(formatString, relation.loss) : "       ";
        string force = relation.force == null ? "" : relation.force.PrettyString();
        int BLACK = relation.name == "repel" || relation.name == "repelWall" ? ColorHelper.PURPLE : (relation.active ? ColorHelper.BLACK : ColorHelper.GRAY);

        length += writer.Write($"{loss}{force}  {relation.name}(", BLACK, bold, -1);
        for (int i = 0; i < relation.args.Length; i++)
        {
            if (relation.name == "noOverlap" && relation.args.Length >= 4 && relation.args[2] == 0.0 && relation.args[3] == 0 && i >= 2) break;
            if (i > 0) length += writer.Write(", ", BLACK, bold);

            char c = relation.arity[i];
            if (c == 'o')
            {
                int cuboidIndex = relation.args[i];
                if (cuboidIndex < cuboids.Length)
                {
                    Cuboid o = cuboids[cuboidIndex];
                    length += writer.Write(o.name, o.color.ToIntColor(), true);
                }
                else length += writer.Write("?", ColorHelper.RED, true);
            }
            else if (c == 'n') length += writer.Write(string.Format(formatString, relation.args[i] / 100.0f), BLACK, bold);
            else if (c == 'd')
            {
                string direction = directions[relation.args[i]];
                length += writer.Write(direction, BLACK, bold);
            }
        }
        writer.WriteLine(")", BLACK, bold);
        return length;
    }
}
