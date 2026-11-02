using UnityEngine;

/// <summary>
/// Procedural textures. These exist so the cell looks like a workshop without
/// downloading a single asset: the wire mesh for the fencing, the hazard
/// striping on the floor, the rubber belt and the painted concrete are all
/// generated from code and saved into the project as real texture assets.
/// </summary>
public static class TextureKit
{
    // deterministic value noise, so the project looks the same on every machine
    private static float Hash(int x, int y, int seed)
    {
        int h = x * 374761393 + y * 668265263 + seed * 1274126177;
        h = (h ^ (h >> 13)) * 1274126177;
        return ((h ^ (h >> 16)) & 0x7fffffff) / (float)0x7fffffff;
    }

    private static float Noise(float x, float y, int seed)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
        float xf = x - xi, yf = y - yi;
        float u = xf * xf * (3f - 2f * xf);
        float v = yf * yf * (3f - 2f * yf);
        float a = Hash(xi, yi, seed), b = Hash(xi + 1, yi, seed);
        float c = Hash(xi, yi + 1, seed), d = Hash(xi + 1, yi + 1, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }

    private static float Fbm(float x, float y, int seed, int octaves = 4)
    {
        float sum = 0f, amp = 0.5f, freq = 1f;
        for (int i = 0; i < octaves; i++)
        {
            sum += Noise(x * freq, y * freq, seed + i * 17) * amp;
            amp *= 0.5f; freq *= 2f;
        }
        return sum;
    }

    private static Texture2D New(int size, string name)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, true);
        t.name = name;
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Bilinear;
        t.anisoLevel = 8;
        return t;
    }

    /// <summary>
    /// Welded wire mesh for the safety fencing: opaque square wires with fully
    /// transparent holes. Used with alpha clipping so you can genuinely see
    /// through the fence into the cell.
    /// </summary>
    public static Texture2D WireMesh(int size = 512, int cells = 10, float wire = 0.035f)
    {
        var t = New(size, "WireMeshTex");
        var px = new Color32[size * size];
        float cell = 1f / cells;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x / (float)size) % cell / cell;
                float v = (y / (float)size) % cell / cell;
                float du = Mathf.Min(u, 1f - u);
                float dv = Mathf.Min(v, 1f - v);
                bool onWire = du < wire || dv < wire;

                if (!onWire)
                {
                    px[y * size + x] = new Color32(0, 0, 0, 0);
                }
                else
                {
                    // round the wire a little so it catches light instead of
                    // reading as a flat grid of lines
                    float d = Mathf.Min(du, dv) / wire;
                    float shade = 0.55f + 0.45f * Mathf.Sqrt(Mathf.Clamp01(1f - d * d));
                    byte g = (byte)(shade * 190f);
                    px[y * size + x] = new Color32(g, (byte)(g * 0.99f), (byte)(g * 0.95f), 255);
                }
            }
        t.SetPixels32(px); t.Apply(true);
        return t;
    }

    /// <summary>Painted concrete floor with a faint speckle and wear.</summary>
    public static Texture2D Concrete(int size = 512, Color tint = default(Color))
    {
        if (tint == default(Color)) tint = new Color(0.46f, 0.47f, 0.49f);
        var t = New(size, "FloorTex");
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float f = x / (float)size * 9f, g = y / (float)size * 9f;
                float n = Fbm(f, g, 3, 5);
                float speck = Hash(x, y, 91) > 0.985f ? 0.14f : 0f;
                float v = Mathf.Clamp01(0.82f + (n - 0.5f) * 0.42f + speck);
                px[y * size + x] = new Color(tint.r * v, tint.g * v, tint.b * v, 1f);
            }
        t.SetPixels32(px); t.Apply(true);
        return t;
    }

    /// <summary>Diagonal hazard striping for the floor keep-out zone.</summary>
    public static Texture2D HazardStripes(int size = 256, int stripes = 6)
    {
        var t = New(size, "HazardTex");
        var px = new Color32[size * size];
        Color yellow = new Color(0.90f, 0.70f, 0.08f);
        Color black = new Color(0.10f, 0.10f, 0.11f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float s = ((x + y) / (float)size * stripes) % 1f;
                Color c = s < 0.5f ? yellow : black;
                float wear = 0.88f + Fbm(x / 28f, y / 28f, 7, 3) * 0.24f;
                px[y * size + x] = new Color(c.r * wear, c.g * wear, c.b * wear, 1f);
            }
        t.SetPixels32(px); t.Apply(true);
        return t;
    }

    /// <summary>Rubber conveyor belt: dark, with fine lateral tread lines.</summary>
    public static Texture2D BeltTread(int size = 256, int lines = 28)
    {
        var t = New(size, "BeltTex");
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float v = ((y / (float)size) * lines) % 1f;
                float groove = v < 0.14f ? 0.62f : 1f;
                float grain = 0.93f + Fbm(x / 16f, y / 16f, 11, 3) * 0.14f;
                float b = 0.17f * groove * grain;
                px[y * size + x] = new Color(b, b * 1.02f, b * 1.06f, 1f);
            }
        t.SetPixels32(px); t.Apply(true);
        return t;
    }

    /// <summary>Brushed metal for guards, frames and the fixture plate.</summary>
    public static Texture2D BrushedMetal(int size = 256)
    {
        var t = New(size, "BrushedTex");
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float streak = Noise(x / 1.6f, y / 48f, 23);
                float v = 0.70f + (streak - 0.5f) * 0.20f;
                px[y * size + x] = new Color(v, v * 1.005f, v * 1.02f, 1f);
            }
        t.SetPixels32(px); t.Apply(true);
        return t;
    }

    /// <summary>Slightly weathered paint for the robot castings.</summary>
    public static Texture2D PaintWear(int size = 256, Color baseColor = default(Color))
    {
        if (baseColor == default(Color)) baseColor = new Color(0.91f, 0.64f, 0.09f);
        var t = New(size, "PaintTex");
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = Fbm(x / 34f, y / 34f, 31, 4);
                float v = 0.94f + (n - 0.5f) * 0.12f;
                px[y * size + x] = new Color(baseColor.r * v, baseColor.g * v, baseColor.b * v, 1f);
            }
        t.SetPixels32(px); t.Apply(true);
        return t;
    }
}
