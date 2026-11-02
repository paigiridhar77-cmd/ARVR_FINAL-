using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural mesh builder. Everything in the cell is generated from these few
/// primitives, so the project needs no downloaded models and nothing can go
/// missing when the project is moved between machines.
///
/// Lathe      revolves a 2D profile around the Y axis. Used for the robot
///            castings, motor barrels, rollers, pins and posts.
/// BeveledBox a cube with its edges chamfered. A plain cube reads as a
///            programmer placeholder; a 3 mm chamfer catches the light along
///            every edge and immediately reads as a machined part.
/// Tube       hollow cylinder, for fence posts and roller shells.
/// Plane      a flat quad with proper UVs, for the belt and the mesh panels.
/// </summary>
public static class MeshKit
{
    // ---------------------------------------------------------------- lathe

    /// <summary>
    /// Revolve a profile around the Y axis. Profile points are (radius, height).
    /// Points are taken in order, so the profile is read bottom to top.
    /// </summary>
    public static Mesh Lathe(Vector2[] profile, int segments = 32,
                             bool capBottom = true, bool capTop = true,
                             string name = "lathe")
    {
        int n = profile.Length;
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        // one extra column so the UV seam closes cleanly
        int cols = segments + 1;

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                float t = (float)j / segments;
                float a = t * Mathf.PI * 2f;
                float r = profile[i].x;
                verts.Add(new Vector3(Mathf.Cos(a) * r, profile[i].y, Mathf.Sin(a) * r));
                uvs.Add(new Vector2(t, (float)i / Mathf.Max(1, n - 1)));
            }
        }

        for (int i = 0; i < n - 1; i++)
        {
            int a0 = i * cols;
            int b0 = (i + 1) * cols;
            for (int j = 0; j < segments; j++)
            {
                tris.Add(a0 + j); tris.Add(b0 + j); tris.Add(b0 + j + 1);
                tris.Add(a0 + j); tris.Add(b0 + j + 1); tris.Add(a0 + j + 1);
            }
        }

        if (capBottom && profile[0].x > 0.0001f)
            AddCap(verts, uvs, tris, profile[0], segments, true);
        if (capTop && profile[n - 1].x > 0.0001f)
            AddCap(verts, uvs, tris, profile[n - 1], segments, false);

        return Finish(verts, uvs, tris, name);
    }

    private static void AddCap(List<Vector3> verts, List<Vector2> uvs, List<int> tris,
                               Vector2 rim, int segments, bool facingDown)
    {
        int centre = verts.Count;
        verts.Add(new Vector3(0f, rim.y, 0f));
        uvs.Add(new Vector2(0.5f, 0.5f));

        int ringStart = verts.Count;
        for (int j = 0; j <= segments; j++)
        {
            float a = (float)j / segments * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(a) * rim.x, rim.y, Mathf.Sin(a) * rim.x));
            uvs.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
        }

        for (int j = 0; j < segments; j++)
        {
            if (facingDown)
            {
                tris.Add(centre); tris.Add(ringStart + j + 1); tris.Add(ringStart + j);
            }
            else
            {
                tris.Add(centre); tris.Add(ringStart + j); tris.Add(ringStart + j + 1);
            }
        }
    }

    // ----------------------------------------------------------- beveled box

    /// <summary>
    /// A box whose edges are chamfered by <paramref name="bevel"/>.
    /// Built as a 4x4x4 lattice of corner points with the 8 true corners and the
    /// 12 edge strips removed, which leaves 6 faces plus the chamfer bands.
    /// </summary>
    public static Mesh BeveledBox(Vector3 size, float bevel, string name = "box")
    {
        float hx = size.x * 0.5f, hy = size.y * 0.5f, hz = size.z * 0.5f;
        float b = Mathf.Min(bevel, hx * 0.49f, hy * 0.49f, hz * 0.49f);
        if (b <= 0.0001f) return PlainBox(size, name);

        float[] xs = { -hx, -hx + b, hx - b, hx };
        float[] ys = { -hy, -hy + b, hy - b, hy };
        float[] zs = { -hz, -hz + b, hz - b, hz };

        var index = new Dictionary<int, int>();
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();

        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                for (int k = 0; k < 4; k++)
                {
                    int extreme = 0;
                    if (i == 0 || i == 3) extreme++;
                    if (j == 0 || j == 3) extreme++;
                    if (k == 0 || k == 3) extreme++;
                    if (extreme > 1) continue;      // drops corners and edges
                    index[Key(i, j, k)] = verts.Count;
                    verts.Add(new Vector3(xs[i], ys[j], zs[k]));
                    uvs.Add(new Vector2((xs[i] + hx) / size.x, (ys[j] + hy) / size.y));
                }

        var tris = new List<int>();
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                for (int k = 0; k < 4; k++)
                {
                    TryQuad(index, tris, verts, i, j, k, 1, 0, 0, 0, 1, 0);  // XY
                    TryQuad(index, tris, verts, i, j, k, 0, 1, 0, 0, 0, 1);  // YZ
                    TryQuad(index, tris, verts, i, j, k, 1, 0, 0, 0, 0, 1);  // XZ
                }

        return Finish(verts, uvs, tris, name);
    }

    private static int Key(int i, int j, int k) { return (i * 4 + j) * 4 + k; }

    private static void TryQuad(Dictionary<int, int> index, List<int> tris, List<Vector3> verts,
                                int i, int j, int k,
                                int ax, int ay, int az, int bx, int by, int bz)
    {
        int a = Key(i, j, k);
        int b = Key(i + ax, j + ay, k + az);
        int c = Key(i + ax + bx, j + ay + by, k + az + bz);
        int d = Key(i + bx, j + by, k + bz);
        if (i + ax + bx > 3 || j + ay + by > 3 || k + az + bz > 3) return;
        int ia, ib, ic, id;
        if (!index.TryGetValue(a, out ia)) return;
        if (!index.TryGetValue(b, out ib)) return;
        if (!index.TryGetValue(c, out ic)) return;
        if (!index.TryGetValue(d, out id)) return;

        // wind the quad so its normal points away from the box centre
        Vector3 mid = (verts[ia] + verts[ib] + verts[ic] + verts[id]) * 0.25f;
        Vector3 nrm = Vector3.Cross(verts[ib] - verts[ia], verts[ic] - verts[ia]);
        if (Vector3.Dot(nrm, mid) < 0f)
        {
            tris.Add(ia); tris.Add(ic); tris.Add(ib);
            tris.Add(ia); tris.Add(id); tris.Add(ic);
        }
        else
        {
            tris.Add(ia); tris.Add(ib); tris.Add(ic);
            tris.Add(ia); tris.Add(ic); tris.Add(id);
        }
    }

    public static Mesh PlainBox(Vector3 size, string name = "box")
    {
        float hx = size.x * 0.5f, hy = size.y * 0.5f, hz = size.z * 0.5f;
        Vector3[] c = {
            new Vector3(-hx,-hy,-hz), new Vector3( hx,-hy,-hz),
            new Vector3( hx, hy,-hz), new Vector3(-hx, hy,-hz),
            new Vector3(-hx,-hy, hz), new Vector3( hx,-hy, hz),
            new Vector3( hx, hy, hz), new Vector3(-hx, hy, hz)
        };
        int[,] faces = {
            {0,3,2,1},{4,5,6,7},{0,1,5,4},{1,2,6,5},{2,3,7,6},{3,0,4,7}
        };
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        for (int f = 0; f < 6; f++)
        {
            int s = verts.Count;
            for (int v = 0; v < 4; v++) verts.Add(c[faces[f, v]]);
            uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
            tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
            tris.Add(s); tris.Add(s + 2); tris.Add(s + 3);
        }
        return Finish(verts, uvs, tris, name);
    }

    // ------------------------------------------------------------------ tube

    public static Mesh Tube(float outerRadius, float innerRadius, float height,
                            int segments = 24, string name = "tube")
    {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        int cols = segments + 1;

        float[] radii = { outerRadius, outerRadius, innerRadius, innerRadius };
        float[] hs = { 0f, height, height, 0f };
        for (int ring = 0; ring < 4; ring++)
            for (int j = 0; j < cols; j++)
            {
                float t = (float)j / segments;
                float a = t * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * radii[ring], hs[ring], Mathf.Sin(a) * radii[ring]));
                uvs.Add(new Vector2(t, ring * 0.33f));
            }

        for (int ring = 0; ring < 3; ring++)
        {
            int a0 = ring * cols, b0 = (ring + 1) * cols;
            for (int j = 0; j < segments; j++)
            {
                tris.Add(a0 + j); tris.Add(b0 + j); tris.Add(b0 + j + 1);
                tris.Add(a0 + j); tris.Add(b0 + j + 1); tris.Add(a0 + j + 1);
            }
        }
        // close the loop from the inner-bottom ring back to the outer-bottom ring
        int last = 3 * cols;
        for (int j = 0; j < segments; j++)
        {
            tris.Add(last + j); tris.Add(j); tris.Add(j + 1);
            tris.Add(last + j); tris.Add(j + 1); tris.Add(last + j + 1);
        }
        return Finish(verts, uvs, tris, name);
    }

    // ----------------------------------------------------------------- plane

    /// <summary>Flat quad in the XZ plane, with UVs tiled by uvScale.</summary>
    public static Mesh Plane(float sizeX, float sizeZ, Vector2 uvScale, string name = "plane")
    {
        float hx = sizeX * 0.5f, hz = sizeZ * 0.5f;
        var verts = new List<Vector3> {
            new Vector3(-hx,0,-hz), new Vector3(hx,0,-hz),
            new Vector3(hx,0,hz),   new Vector3(-hx,0,hz)
        };
        var uvs = new List<Vector2> {
            new Vector2(0,0), new Vector2(uvScale.x,0),
            new Vector2(uvScale.x,uvScale.y), new Vector2(0,uvScale.y)
        };
        var tris = new List<int> { 0, 2, 1, 0, 3, 2 };
        return Finish(verts, uvs, tris, name);
    }

    /// <summary>Flat quad standing in the XY plane, used for fence panels.</summary>
    public static Mesh Panel(float sizeX, float sizeY, Vector2 uvScale, string name = "panel")
    {
        float hx = sizeX * 0.5f, hy = sizeY * 0.5f;
        var verts = new List<Vector3> {
            new Vector3(-hx,-hy,0), new Vector3(hx,-hy,0),
            new Vector3(hx,hy,0),   new Vector3(-hx,hy,0)
        };
        var uvs = new List<Vector2> {
            new Vector2(0,0), new Vector2(uvScale.x,0),
            new Vector2(uvScale.x,uvScale.y), new Vector2(0,uvScale.y)
        };
        var tris = new List<int> { 0, 1, 2, 0, 2, 3 };
        return Finish(verts, uvs, tris, name);
    }

    // ----------------------------------------------------------------- utils

    private static Mesh Finish(List<Vector3> verts, List<Vector2> uvs, List<int> tris, string name)
    {
        var m = new Mesh();
        m.name = name;
        if (verts.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateTangents();
        m.RecalculateBounds();
        return m;
    }

    /// <summary>Shorthand for a profile point.</summary>
    public static Vector2 P(float radius, float height) { return new Vector2(radius, height); }
}
