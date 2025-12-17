using System.Collections.Generic;

class Rectangle
{
    public float x, y, sizex, sizey;

    public Rectangle(float x, float y, float sizex, float sizey)
    {
        this.x = x;
        this.y = y;
        this.sizex = sizex;
        this.sizey = sizey;
    }

    public bool Overlaps(Rectangle r) => 
        r.x - 0.5f * r.sizex < x + 0.5f * r.sizex && r.x + 0.5f * r.sizex > x - 0.5f * sizex && r.y - 0.5f * r.sizey < y + 0.5f * r.sizey && r.y + 0.5f * r.sizey > y - 0.5f * sizey;
    public bool Overlaps(List<Rectangle> list)
    {
        for (int i = 0; i < list.Count; i++) if (Overlaps(list[i])) return true;
        return false;
    }
}

class Writer
{
    int[] bitmap;
    public int BX, BY;
    public int GLOBALX, GLOBALY;

    public int cx, cy;

    int[] font;// r, fontb;
    public readonly int FX, FY;

    static readonly char[] legend = "ABCDEFGHIJKLMNOPQRSTUVWXYZλ12345abcdefghijklmnopqrstuvwxyz 67890{}[]()<>$*-+=/#_%^@\\&|~?'\"`!,.;:".ToCharArray();
    public static Dictionary<char, byte> map;
    static Writer()
    {
        map = new Dictionary<char, byte>();
        for (int i = 0; i < legend.Length; i++) map.Add(legend[i], (byte)i);
    }

    public Writer(string name, int[] bitmap, int BX, int BY)
    {
        this.bitmap = bitmap;
        this.BX = BX;
        this.BY = BY;

        (font, int FWIDTH, int FHEIGHT) = ImageSharpHelper.LoadBitmap($"resources/{name}.png");
        //(fontr, int FWIDTH, int FHEIGHT) = BitmapHelper.LoadBitmap($"resources/{name}r.png");
        //fontb = BitmapHelper.LoadBitmap($"resources/{name}b.png").bitmap;
        FX = FWIDTH / 32;
        FY = FHEIGHT / 6;
    }

    public void Set(int GLOBALX, int GLOBALY)
    {
        this.GLOBALX = GLOBALX;
        this.GLOBALY = GLOBALY;
        cx = 0;
        cy = 0;
    }

    public int Write(string s, int color, bool bold = false, int background = 0)
    {
        DrawString(s, GLOBALX + cx * FX, GLOBALY + cy * FY, color, bold, background);
        cx += s.Length;
        return s.Length;
    }

    public void WriteLine(string s, int color, bool bold = false, int background = 0)
    {
        Write(s, color, bold, background);
        cx = 0;
        cy++;
    }

    public Rectangle Rect(string text, int x, int y) => new(x + 0.5f * text.Length * FX, y + 0.5f * FY, 0.9f * text.Length * FX, 0.5f * FY);
    public void DrawString(string text, int x, int y, int color, bool bold = false, int background = 0)
    {
        if (y + FY > BY) return;
        //int[] font = bold ? fontb : fontr;
        int boldshift = bold ? 3 * FY : 0;

        for (int n = 0; n < text.Length; n++)
        {
            if (x + FX * n >= BX) continue;
            //byte c = map[text[n]];
            bool success = map.TryGetValue(text[n], out byte c);
            if (!success) c = 0;
            int fx = c % 32, fy = c / 32;
            for (int dy = 0; dy < FY; dy++) for (int dx = 0; dx < FX; dx++)
                {
                    int f = font[fx * FX + dx + (fy * FY + dy + boldshift) * FX * 32];
                    int xdx = x + FX * n + dx;
                    int ydy = y + dy;
                    if (xdx < 0 || xdx >= BX || ydy < 0 || ydy >= BY) return;

                    int idi = xdx + ydy * BX;
                    int b = bitmap[idi];
                    if (f != -1) bitmap[idi] = color;
                    else if (background == -1) bitmap[idi] = -1;
                    else if (background != 0 && b == -1) bitmap[idi] = background;
                }
        }
    }
}
