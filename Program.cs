//Copyright (C) 2025 Maxim Gumin, The MIT License (MIT)

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        int panelwidth = 540; //540
        int ao = 0;
        string font = "8x15";
        float distance = 11.5f; //11.5f;
        bool uniqueColors = true;
        int super = 1; //1
        bool ortho = false; //false;
        bool names = true;
        bool allnames = false;
        bool savemask = false;

        //string s = "folder=examples/atiss/bed filename=room size=1200x1200 panelwidth=0 ao=-1 font=10x20 distance=11.5 unique=true super=1";
        //string s = "folder=examples/ours-2/bedroom filename=bedroom size=1200x1200 panelwidth=0 ao=-1 font=10x20 distance=12 unique=true super=1";
        //args = s.Split(' ');

        if (args.Length == 0)
        {
            var folder = Directory.CreateDirectory("output");
            foreach (var file in folder.GetFiles()) file.Delete();
            Stopwatch sw = Stopwatch.StartNew();

            Random random = new();
            XElement xroot = XDocument.Load("scenes.xml").Root;
            foreach (XElement xelem in xroot.Elements())
            {
                string name = xelem.Attribute("name").Value;
                if (xelem.Name == "scene")
                {
                    Scene scene = new($"examples/{name}.json", uniqueColors);
                    string filename = $"{name}-{random.Next(1000)}";
                    string bitmapFilename = $"output/{filename}.png";
                    string maskFilename = $"output/{filename}-mask.png";
                    (int[] bitmap, int[] mask, int BX, int BY) = scene.TakeScreenshot(1200, 1200, panelwidth, -1, font, distance, super, ortho, names, allnames); //1200
                    ImageSharpHelper.SaveBitmap(bitmap, BX, BY, bitmapFilename);
                    if (savemask) ImageSharpHelper.SaveBitmap(mask, BX, BY, maskFilename);
                    Console.WriteLine($"saved to {bitmapFilename}");
                }
                else if (xelem.Name == "folder")
                {
                    string[] filepaths = Directory.EnumerateFiles($"examples\\{name}").ToArray();
                    Parallel.ForEach(filepaths, filepath =>
                    {
                        string basename = Path.GetFileNameWithoutExtension(filepath);
                        Scene scene = new(filepath, uniqueColors);
                        string filename = $"{basename}-{random.Next(1000)}";
                        string bitmapFilename = $"output/{filename}.png";
                        string maskFilename = $"output/{filename}-mask.png";
                        (int[] bitmap, int[] mask, int BX, int BY) = scene.TakeScreenshot(1200, 1200, panelwidth, -1, font, distance, super, ortho, names, allnames);
                        ImageSharpHelper.SaveBitmap(bitmap, BX, BY, bitmapFilename);
                        if (savemask) ImageSharpHelper.SaveBitmap(mask, BX, BY, maskFilename);
                        Console.WriteLine($"saved to {bitmapFilename}");
                    });
                }
                else throw new Exception($"wrong xml tag {xelem.Name}");
            }

            Console.WriteLine($"visuzalizer took {sw.ElapsedMilliseconds} milleseconds");
        }
        else
        {
            Dictionary<string, string> dict = [];
            foreach (string arg in args)
            {
                string[] split = arg.Split('=');
                dict.Add(split[0], split[1]);
            }

            string folder = dict["folder"];
            string namehead = null;

            if (dict.ContainsKey("filename")) namehead = dict["filename"];
            if (dict.TryGetValue("font", out string value)) font = value;
            if (dict.ContainsKey("panelwidth")) panelwidth = int.Parse(dict["panelwidth"]);
            if (dict.ContainsKey("ao")) ao = int.Parse(dict["ao"]);
            if (dict.ContainsKey("distance")) distance = float.Parse(dict["distance"]);
            if (dict.ContainsKey("unique")) uniqueColors = bool.Parse(dict["unique"]);
            if (dict.ContainsKey("super")) super = int.Parse(dict["super"]);
            if (dict.ContainsKey("ortho")) ortho = bool.Parse(dict["ortho"]);
            if (dict.ContainsKey("names")) names = bool.Parse(dict["names"]);
            if (dict.ContainsKey("allnames")) allnames = bool.Parse(dict["allnames"]);
            if (dict.ContainsKey("mask")) savemask = bool.Parse(dict["mask"]);

            int RX = 800, RY = 800;
            if (dict.ContainsKey("size"))
            {
                string sizestring = dict["size"];
                string[] split = sizestring.Split('x');
                RX = int.Parse(split[0]);
                RY = int.Parse(split[1]);
            }

            Func<string, bool> f = namehead == null ? s => Path.GetFileName(s).EndsWith(".json") : s => Path.GetFileName(s).StartsWith(namehead) && Path.GetFileName(s).EndsWith(".json");
            string[] filenames = Directory.EnumerateFiles(folder).Where(f).ToArray();
            Parallel.ForEach(filenames, filename =>
            {
                Scene scene = new(filename, uniqueColors);
                string noextension = Path.GetFileNameWithoutExtension(filename);
                string bitmapFilename = $"{folder}{noextension}.png";
                string maskFilename = $"{folder}{noextension}-mask.png";
                (int[] bitmap, int[] mask, int BX, int BY) = scene.TakeScreenshot(RX, RY, panelwidth, ao, font, distance, super, ortho, names, allnames);
                ImageSharpHelper.SaveBitmap(bitmap, BX, BY, bitmapFilename);
                if (savemask) ImageSharpHelper.SaveBitmap(mask, BX, BY, maskFilename);
                Console.WriteLine($"saved to {bitmapFilename}");
            });
        }
    }
}

/*
Как сделать AO: много маленьких солнц, равномерно распределённых по сфере, для каждого применяется cos.

Логика упростится, если hit.t = MAXVALUE считать промахнувшимся лучом, как я это сделал в шейдере.

Можно сделать GPU-версию визуализатора. Хорошо тем, что код не будет фрагментирован.
Интересно, будет ли она работать быстрее параллельной версии.

Есть вариант просто жадно находить положение как можно ближе с центром объекта, которое не пересекается с другими лейблами.

В идеале визуализатор должен выбирать шрифт в зависимости от размера кубоида.
*/
