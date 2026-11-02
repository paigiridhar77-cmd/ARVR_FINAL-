#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates the project's meshes, textures and materials and saves them as real
/// assets under Assets/Generated. Saving them matters: a mesh or material that
/// only lives in memory is lost the moment the scene reloads, which is why the
/// builders here write everything to disk first and then reference it.
/// </summary>
public static class AssetForge
{
    public const string Root = "Assets/Generated";
    public const string MeshDir = Root + "/Meshes";
    public const string TexDir = Root + "/Textures";
    public const string MatDir = Root + "/Materials";

    public static void EnsureFolders()
    {
        Directory.CreateDirectory(MeshDir);
        Directory.CreateDirectory(TexDir);
        Directory.CreateDirectory(MatDir);
        AssetDatabase.Refresh();
    }

    // ------------------------------------------------------------- shaders

    public static Shader LitShader()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Diffuse");
        return s;
    }

    // --------------------------------------------------------------- saving

    public static Mesh SaveMesh(Mesh m, string fileName)
    {
        string path = MeshDir + "/" + fileName + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            existing.Clear();
            existing.SetVertices(new System.Collections.Generic.List<Vector3>(m.vertices));
            existing.SetUVs(0, new System.Collections.Generic.List<Vector2>(m.uv));
            existing.SetTriangles(m.triangles, 0);
            existing.RecalculateNormals();
            existing.RecalculateTangents();
            existing.RecalculateBounds();
            EditorUtility.SetDirty(existing);
            return existing;
        }
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    public static Texture2D SaveTexture(Texture2D t, string fileName)
    {
        string path = TexDir + "/" + fileName + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;
        AssetDatabase.CreateAsset(t, path);
        return t;
    }

    public static Material SaveMaterial(Material m, string fileName)
    {
        string path = MatDir + "/" + fileName + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.shader = m.shader;
            existing.CopyPropertiesFromMaterial(m);
            EditorUtility.SetDirty(existing);
            return existing;
        }
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // ------------------------------------------------------------ materials

    public static Material Make(string fileName, Color baseColor,
                                float metallic, float smoothness,
                                Texture2D map = null, Vector2 tiling = default(Vector2))
    {
        var m = new Material(LitShader());
        m.name = fileName;
        SetColor(m, baseColor);
        SetFloatIfHas(m, "_Metallic", metallic);
        SetFloatIfHas(m, "_Smoothness", smoothness);
        SetFloatIfHas(m, "_Glossiness", smoothness);   // built-in Standard fallback
        if (map != null)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", map);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", map);
            if (tiling == default(Vector2)) tiling = Vector2.one;
            if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", tiling);
            if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", tiling);
        }
        return SaveMaterial(m, fileName);
    }

    /// <summary>Material with alpha clipping, for the wire mesh fence panels.</summary>
    public static Material MakeCutout(string fileName, Color baseColor,
                                      float metallic, float smoothness,
                                      Texture2D map, Vector2 tiling)
    {
        var m = new Material(LitShader());
        m.name = fileName;
        SetColor(m, baseColor);
        SetFloatIfHas(m, "_Metallic", metallic);
        SetFloatIfHas(m, "_Smoothness", smoothness);
        SetFloatIfHas(m, "_Glossiness", smoothness);

        if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", map); m.SetTextureScale("_BaseMap", tiling); }
        if (m.HasProperty("_MainTex")) { m.SetTexture("_MainTex", map); m.SetTextureScale("_MainTex", tiling); }

        SetFloatIfHas(m, "_AlphaClip", 1f);
        SetFloatIfHas(m, "_Cutoff", 0.5f);
        SetFloatIfHas(m, "_Mode", 1f);                 // Standard shader cutout
        m.EnableKeyword("_ALPHATEST_ON");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;

        // the fence is a thin panel, so it must render from both sides
        SetFloatIfHas(m, "_Cull", 0f);

        return SaveMaterial(m, fileName);
    }

    public static Material MakeEmissive(string fileName, Color baseColor, Color emission)
    {
        var m = new Material(LitShader());
        m.name = fileName;
        SetColor(m, baseColor);
        SetFloatIfHas(m, "_Metallic", 0f);
        SetFloatIfHas(m, "_Smoothness", 0.7f);
        if (m.HasProperty("_EmissionColor"))
        {
            m.SetColor("_EmissionColor", emission);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        return SaveMaterial(m, fileName);
    }

    private static void SetColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }

    private static void SetFloatIfHas(Material m, string prop, float v)
    {
        if (m.HasProperty(prop)) m.SetFloat(prop, v);
    }

    // ------------------------------------------------- the project's palette

    public class Palette
    {
        public Material robotYellow, robotDark, robotGrey, steel, blackRubber;
        public Material floor, hazard, belt, mesh, frame, warnRed, warnGreen, partBlue, cabinet, plainRed;
    }

    public static Palette BuildPalette()
    {
        EnsureFolders();

        var paint = SaveTexture(TextureKit.PaintWear(256, new Color(0.93f, 0.66f, 0.10f)), "PaintTex");
        var brushed = SaveTexture(TextureKit.BrushedMetal(256), "BrushedTex");
        var concrete = SaveTexture(TextureKit.Concrete(512), "FloorTex");
        var hazardTex = SaveTexture(TextureKit.HazardStripes(256, 6), "HazardTex");
        var beltTex = SaveTexture(TextureKit.BeltTread(256, 28), "BeltTex");
        var meshTex = SaveTexture(TextureKit.WireMesh(512, 10, 0.035f), "WireMeshTex");

        var p = new Palette();
        p.robotYellow = Make("RobotYellow", new Color(1f, 1f, 1f), 0.15f, 0.62f, paint, Vector2.one);
        p.robotDark = Make("RobotDark", new Color(0.17f, 0.17f, 0.19f), 0.35f, 0.55f);
        p.robotGrey = Make("RobotGrey", new Color(0.58f, 0.59f, 0.62f), 0.70f, 0.60f);
        p.steel = Make("Steel", new Color(0.78f, 0.79f, 0.82f), 0.85f, 0.70f, brushed, new Vector2(2, 2));
        p.blackRubber = Make("BlackRubber", new Color(0.09f, 0.09f, 0.10f), 0.0f, 0.22f);
        p.floor = Make("FloorConcrete", Color.white, 0.0f, 0.18f, concrete, new Vector2(10, 10));
        p.hazard = Make("HazardPaint", Color.white, 0.0f, 0.30f, hazardTex, new Vector2(3, 1));
        p.belt = Make("ConveyorBelt", Color.white, 0.0f, 0.30f, beltTex, new Vector2(1, 8));
        p.mesh = MakeCutout("FenceMesh", new Color(0.72f, 0.74f, 0.77f), 0.80f, 0.55f, meshTex, new Vector2(6, 4));
        p.frame = Make("FenceFrame", new Color(0.86f, 0.70f, 0.12f), 0.45f, 0.45f);
        p.warnRed = MakeEmissive("BeaconRed", new Color(0.55f, 0.05f, 0.05f), new Color(2.2f, 0.18f, 0.12f));
        p.warnGreen = MakeEmissive("LampGreen", new Color(0.05f, 0.4f, 0.12f), new Color(0.15f, 1.6f, 0.3f));
        p.partBlue = Make("PartBlue", new Color(0.16f, 0.42f, 0.78f), 0.30f, 0.65f);
        p.cabinet = Make("CabinetGrey", new Color(0.33f, 0.35f, 0.38f), 0.45f, 0.42f);
        p.plainRed = Make("SafetyRed", new Color(0.62f, 0.09f, 0.09f), 0.25f, 0.45f);

        AssetDatabase.SaveAssets();
        return p;
    }
}
#endif
