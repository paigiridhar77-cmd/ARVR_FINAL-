#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the work cell: a belt conveyor with drum pulleys and a moving belt, a
/// machine table carrying a fixture with locating pins and toggle clamps, a
/// fenced enclosure in welded wire mesh, a control cabinet, a safety beacon and
/// a concrete floor with hazard striping.
///
/// Everything the robot can hit is tagged CellObstacle, exactly as before, so
/// the collision logging and the recorded test results still hold.
///
///   Tools > Robot Trainer > 2. Build Detailed Cell
/// </summary>
public static class CellBuilderPro
{
    // ---- cell layout, metres, robot standing at the origin ----
    static readonly Vector3 TablePos = new Vector3(0.46f, 0f, 0.08f);
    // 0.60 m, not the 0.70 m first used. With the robot's shoulder at
    // 0.730 m a part sitting on a 0.70 m table puts the taught approach
    // point 100 mm above it at 95 percent of the arm's wrist reach, so the
    // arm is almost dead straight there and small jogs behave strangely.
    // At 0.60 m every taught point stays under 88 percent.
    const float TableTop = 0.60f;

    static readonly Vector3 ConvPos = new Vector3(-0.38f, 0f, 0.34f);
    const float ConvTop = 0.42f;
    const float ConvLen = 1.05f;      // along Z
    const float ConvWidth = 0.30f;

    const float FenceMinX = -1.15f, FenceMaxX = 1.15f;
    const float FenceMinZ = -0.95f, FenceMaxZ = 1.25f;
    const float FenceH = 1.60f;

    [MenuItem("Tools/Robot Trainer/2. Build Detailed Cell", false, 11)]
    public static void Build()
    {
        var pal = AssetForge.BuildPalette();
        TagKit.EnsureTag("Part");
        TagKit.EnsureTag("CellObstacle");

        var old = GameObject.Find("WorkCell");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("WorkCell");
        Undo.RegisterCreatedObjectUndo(root, "Build Detailed Cell");

        BuildFloor(root.transform, pal);
        BuildMachineTable(root.transform, pal);
        BuildFixture(root.transform, pal);
        BuildConveyor(root.transform, pal);
        BuildFencing(root.transform, pal);
        BuildCabinet(root.transform, pal);
        BuildBeacon(root.transform, pal);
        BuildPartAndZone(root.transform, pal);

        Selection.activeGameObject = root;
        Debug.Log("Detailed cell built. Conveyor belt and beacon animate in play mode.");
    }

    // ----------------------------------------------------------------- floor

    static void BuildFloor(Transform root, AssetForge.Palette pal)
    {
        var floor = new GameObject("Floor");
        floor.transform.SetParent(root, false);

        Piece(floor.transform, "Floor_Concrete",
            MeshKit.Plane(8f, 8f, new Vector2(8, 8), "floor"),
            pal.floor, Vector3.zero, Quaternion.identity, null);

        var col = floor.AddComponent<BoxCollider>();
        col.size = new Vector3(8f, 0.04f, 8f);
        col.center = new Vector3(0, -0.02f, 0);

        // hazard striping marking the robot's keep-out ring
        var ring = new GameObject("Floor_HazardRing");
        ring.transform.SetParent(floor.transform, false);
        float inner = 0.52f, band = 0.12f;
        for (int s = 0; s < 4; s++)
        {
            bool alongX = (s % 2 == 0);
            float sign = (s < 2) ? -1f : 1f;
            Vector3 size = alongX ? new Vector3(inner * 2f + band * 2f, 1f, band)
                                  : new Vector3(band, 1f, inner * 2f + band * 2f);
            Vector3 pos = alongX ? new Vector3(0, 0.002f, sign * (inner + band * 0.5f))
                                 : new Vector3(sign * (inner + band * 0.5f), 0.002f, 0);
            Piece(ring.transform, "Hazard" + s,
                MeshKit.Plane(size.x, size.z, new Vector2(size.x * 4f, size.z * 4f), "hz"),
                pal.hazard, pos, Quaternion.identity, null);
        }
    }

    // --------------------------------------------------------- machine table

    static void BuildMachineTable(Transform root, AssetForge.Palette pal)
    {
        var t = new GameObject("MachineTable");
        t.transform.SetParent(root, false);
        t.transform.position = TablePos;
        t.tag = "CellObstacle";

        float w = 0.46f, d = 0.42f, legR = 0.022f;

        Piece(t.transform, "Table_Top",
            MeshKit.BeveledBox(new Vector3(w, 0.040f, d), 0.006f, "table_top"),
            pal.steel, new Vector3(0, TableTop - 0.020f, 0), Quaternion.identity, null);

        Piece(t.transform, "Table_Apron",
            MeshKit.BeveledBox(new Vector3(w - 0.06f, 0.075f, d - 0.06f), 0.006f, "apron"),
            pal.cabinet, new Vector3(0, TableTop - 0.078f, 0), Quaternion.identity, null);

        for (int i = 0; i < 4; i++)
        {
            float sx = (i % 2 == 0 ? -1f : 1f) * (w * 0.5f - 0.045f);
            float sz = (i < 2 ? -1f : 1f) * (d * 0.5f - 0.045f);
            Piece(t.transform, "Table_Leg" + i,
                MeshKit.Lathe(new[] {
                    MeshKit.P(legR * 1.5f, 0f), MeshKit.P(legR * 1.5f, 0.012f),
                    MeshKit.P(legR, 0.022f), MeshKit.P(legR, TableTop - 0.040f)
                }, 14, true, true, "leg"),
                pal.cabinet, new Vector3(sx, 0, sz), Quaternion.identity, null);
        }

        var bc = t.AddComponent<BoxCollider>();
        bc.center = new Vector3(0, TableTop * 0.5f, 0);
        bc.size = new Vector3(w, TableTop, d);
    }

    // --------------------------------------------------------------- fixture

    static void BuildFixture(Transform root, AssetForge.Palette pal)
    {
        var f = new GameObject("Fixture");
        f.transform.SetParent(root, false);
        f.transform.position = TablePos + new Vector3(0, TableTop, 0);
        f.tag = "CellObstacle";

        Piece(f.transform, "Fix_BasePlate",
            MeshKit.BeveledBox(new Vector3(0.24f, 0.024f, 0.22f), 0.004f, "fix_plate"),
            pal.steel, new Vector3(0, 0.012f, 0), Quaternion.identity, null);

        // machined nest the part drops into
        Piece(f.transform, "Fix_NestFront",
            MeshKit.BeveledBox(new Vector3(0.10f, 0.022f, 0.016f), 0.002f, "nest"),
            pal.cabinet, new Vector3(0, 0.035f, -0.040f), Quaternion.identity, null);
        Piece(f.transform, "Fix_NestBack",
            MeshKit.BeveledBox(new Vector3(0.10f, 0.022f, 0.016f), 0.002f, "nest"),
            pal.cabinet, new Vector3(0, 0.035f, 0.040f), Quaternion.identity, null);

        // two tapered locating pins
        for (int s = 0; s < 2; s++)
        {
            float sx = s == 0 ? -0.082f : 0.082f;
            Piece(f.transform, "Fix_Pin" + s,
                MeshKit.Lathe(new[] {
                    MeshKit.P(0.013f, 0f), MeshKit.P(0.013f, 0.010f),
                    MeshKit.P(0.0085f, 0.014f), MeshKit.P(0.0085f, 0.042f),
                    MeshKit.P(0.0055f, 0.050f)
                }, 18, true, true, "pin"),
                pal.steel, new Vector3(sx, 0.024f, 0), Quaternion.identity, null);
        }

        // two toggle clamps: pedestal, pivot, arm, handle
        for (int s = 0; s < 2; s++)
        {
            float sz = s == 0 ? -0.082f : 0.082f;
            float dir = s == 0 ? -1f : 1f;
            var clamp = new GameObject("Fix_Clamp" + s);
            clamp.transform.SetParent(f.transform, false);
            clamp.transform.localPosition = new Vector3(0.0f, 0.024f, sz);

            Piece(clamp.transform, "Clamp_Base",
                MeshKit.BeveledBox(new Vector3(0.034f, 0.030f, 0.026f), 0.004f, "cl_base"),
                pal.cabinet, new Vector3(0, 0.015f, 0), Quaternion.identity, null);

            Piece(clamp.transform, "Clamp_Pivot",
                MeshKit.Lathe(new[] { MeshKit.P(0.007f, -0.018f), MeshKit.P(0.007f, 0.018f) },
                    12, true, true, "cl_piv"),
                pal.steel, new Vector3(0, 0.034f, 0), Quaternion.Euler(0, 0, 90), null);

            Piece(clamp.transform, "Clamp_Arm",
                MeshKit.BeveledBox(new Vector3(0.012f, 0.012f, 0.072f), 0.002f, "cl_arm"),
                pal.steel, new Vector3(0, 0.036f, -dir * 0.030f),
                Quaternion.Euler(dir * 16f, 0, 0), null);

            Piece(clamp.transform, "Clamp_Handle",
                MeshKit.BeveledBox(new Vector3(0.010f, 0.044f, 0.012f), 0.002f, "cl_h"),
                pal.plainRed, new Vector3(0, 0.058f, dir * 0.030f),
                Quaternion.Euler(dir * 28f, 0, 0), null);
        }

        var bc = f.AddComponent<BoxCollider>();
        bc.center = new Vector3(0, 0.030f, 0);
        bc.size = new Vector3(0.24f, 0.060f, 0.22f);
    }

    // -------------------------------------------------------------- conveyor

    static void BuildConveyor(Transform root, AssetForge.Palette pal)
    {
        var c = new GameObject("Conveyor");
        c.transform.SetParent(root, false);
        c.transform.position = ConvPos;
        c.tag = "CellObstacle";

        float halfW = ConvWidth * 0.5f;
        float frameT = 0.045f;
        float sideX = halfW + 0.028f;
        var rollers = new List<Transform>();

        // side frames
        for (int s = 0; s < 2; s++)
        {
            float sx = s == 0 ? -sideX : sideX;
            Piece(c.transform, "Conv_Side" + s,
                MeshKit.BeveledBox(new Vector3(0.040f, frameT * 2.2f, ConvLen), 0.005f, "conv_side"),
                pal.frame, new Vector3(sx, ConvTop - 0.045f, 0), Quaternion.identity, null);

            // guide rail along the top edge
            Piece(c.transform, "Conv_Rail" + s,
                MeshKit.BeveledBox(new Vector3(0.012f, 0.055f, ConvLen - 0.04f), 0.002f, "conv_rail"),
                pal.steel, new Vector3(sx, ConvTop + 0.028f, 0), Quaternion.identity, null);
        }

        // belt surface
        var belt = Piece(c.transform, "Conv_Belt",
            MeshKit.Plane(ConvWidth, ConvLen - 0.06f, new Vector2(1f, 8f), "belt"),
            pal.belt, new Vector3(0, ConvTop, 0), Quaternion.identity, null);

        // drum pulleys at both ends, axis along X
        for (int s = 0; s < 2; s++)
        {
            float sz = s == 0 ? -(ConvLen * 0.5f - 0.03f) : (ConvLen * 0.5f - 0.03f);
            var drum = Piece(c.transform, "Conv_Drum" + s,
                MeshKit.Lathe(new[] {
                    MeshKit.P(0.030f, -halfW - 0.006f), MeshKit.P(0.048f, -halfW + 0.004f),
                    MeshKit.P(0.048f,  halfW - 0.004f), MeshKit.P(0.030f,  halfW + 0.006f)
                }, 24, true, true, "drum"),
                pal.robotDark, new Vector3(0, ConvTop - 0.048f, sz), Quaternion.Euler(0, 0, 90), null);
            rollers.Add(drum.transform);
        }

        // support rollers visible between the frames
        int n = 7;
        for (int i = 0; i < n; i++)
        {
            float t = (i + 0.5f) / n;
            float sz = Mathf.Lerp(-ConvLen * 0.40f, ConvLen * 0.40f, t);
            var r = Piece(c.transform, "Conv_Roller" + i,
                MeshKit.Lathe(new[] {
                    MeshKit.P(0.020f, -halfW + 0.004f), MeshKit.P(0.024f, -halfW + 0.012f),
                    MeshKit.P(0.024f,  halfW - 0.012f), MeshKit.P(0.020f,  halfW - 0.004f)
                }, 16, true, true, "roller"),
                pal.steel, new Vector3(0, ConvTop - 0.062f, sz), Quaternion.Euler(0, 0, 90), null);
            rollers.Add(r.transform);
        }

        // legs
        for (int i = 0; i < 4; i++)
        {
            float sx = (i % 2 == 0 ? -1f : 1f) * (sideX - 0.01f);
            float sz = (i < 2 ? -1f : 1f) * (ConvLen * 0.5f - 0.10f);
            Piece(c.transform, "Conv_Leg" + i,
                MeshKit.BeveledBox(new Vector3(0.034f, ConvTop - 0.070f, 0.034f), 0.004f, "conv_leg"),
                pal.cabinet, new Vector3(sx, (ConvTop - 0.070f) * 0.5f, sz), Quaternion.identity, null);
            Piece(c.transform, "Conv_Foot" + i,
                MeshKit.BeveledBox(new Vector3(0.060f, 0.012f, 0.060f), 0.002f, "foot"),
                pal.robotDark, new Vector3(sx, 0.006f, sz), Quaternion.identity, null);
        }

        // drive motor and gearbox at the far end
        Piece(c.transform, "Conv_Gearbox",
            MeshKit.BeveledBox(new Vector3(0.075f, 0.090f, 0.090f), 0.010f, "gearbox"),
            pal.cabinet, new Vector3(sideX + 0.055f, ConvTop - 0.055f, ConvLen * 0.5f - 0.08f),
            Quaternion.identity, null);
        Piece(c.transform, "Conv_Motor",
            MeshKit.Lathe(new[] {
                MeshKit.P(0.030f, 0f), MeshKit.P(0.034f, 0.010f),
                MeshKit.P(0.034f, 0.090f), MeshKit.P(0.026f, 0.102f)
            }, 20, true, true, "motor"),
            pal.robotDark, new Vector3(sideX + 0.055f, ConvTop - 0.055f, ConvLen * 0.5f + 0.02f),
            Quaternion.Euler(-90, 0, 0), null);

        var bc = c.AddComponent<BoxCollider>();
        bc.center = new Vector3(0, ConvTop * 0.5f, 0);
        bc.size = new Vector3(ConvWidth + 0.10f, ConvTop, ConvLen);

        var anim = c.AddComponent<ConveyorAnimator>();
        anim.beltRenderer = belt.GetComponent<Renderer>();
        anim.rollers = rollers.ToArray();
        anim.speed = 0.22f;
        anim.tilesPerMetre = 8f;
        anim.rollerRadius = 0.048f;
    }

    // --------------------------------------------------------------- fencing

    static void BuildFencing(Transform root, AssetForge.Palette pal)
    {
        var fence = new GameObject("SafetyFencing");
        fence.transform.SetParent(root, false);
        fence.tag = "CellObstacle";

        // three full sides; the fourth is left open as the operator doorway
        Run(fence.transform, pal, new Vector3(FenceMinX, 0, FenceMinZ), new Vector3(FenceMinX, 0, FenceMaxZ), "West");
        Run(fence.transform, pal, new Vector3(FenceMinX, 0, FenceMaxZ), new Vector3(FenceMaxX, 0, FenceMaxZ), "North");
        Run(fence.transform, pal, new Vector3(FenceMaxX, 0, FenceMaxZ), new Vector3(FenceMaxX, 0, FenceMinZ), "East");
        // south wall, with a gap in the middle for the doorway
        Run(fence.transform, pal, new Vector3(FenceMaxX, 0, FenceMinZ), new Vector3(0.42f, 0, FenceMinZ), "SouthA");
        Run(fence.transform, pal, new Vector3(-0.42f, 0, FenceMinZ), new Vector3(FenceMinX, 0, FenceMinZ), "SouthB");

        // light curtain posts either side of the doorway
        for (int s = 0; s < 2; s++)
        {
            float sx = s == 0 ? -0.42f : 0.42f;
            Piece(fence.transform, "LightCurtain" + s,
                MeshKit.BeveledBox(new Vector3(0.055f, 1.25f, 0.055f), 0.006f, "lc"),
                pal.plainRed, new Vector3(sx, 0.625f, FenceMinZ), Quaternion.identity, null);
        }
    }

    static void Run(Transform parent, AssetForge.Palette pal, Vector3 a, Vector3 b, string name)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.01f) return;
        Vector3 dir = d / len;
        Vector3 mid = (a + b) * 0.5f;
        Vector3 normal = Vector3.Cross(Vector3.up, dir).normalized;
        Quaternion rot = Quaternion.LookRotation(normal, Vector3.up);

        var run = new GameObject("Fence_" + name);
        run.transform.SetParent(parent, false);
        run.transform.position = mid;
        run.transform.rotation = rot;

        // posts every ~1.1 m, including both ends
        int bays = Mathf.Max(1, Mathf.RoundToInt(len / 1.10f));
        float bayLen = len / bays;

        for (int i = 0; i <= bays; i++)
        {
            float x = -len * 0.5f + i * bayLen;
            Piece(run.transform, name + "_Post" + i,
                MeshKit.Tube(0.026f, 0.019f, FenceH, 14, name + "_post"),
                pal.frame, new Vector3(x, 0f, 0f), Quaternion.identity, null);
            Piece(run.transform, name + "_PostFoot" + i,
                MeshKit.BeveledBox(new Vector3(0.085f, 0.014f, 0.085f), 0.002f, name + "_pfoot"),
                pal.cabinet, new Vector3(x, 0.007f, 0f), Quaternion.identity, null);
        }

        for (int i = 0; i < bays; i++)
        {
            float cx = -len * 0.5f + (i + 0.5f) * bayLen;

            // the mesh panel itself, alpha clipped so you can see through it
            float panelW = bayLen - 0.06f;
            float panelH = FenceH - 0.18f;
            var tiling = new Vector2(panelW * 5.2f, panelH * 5.2f);
            Piece(run.transform, name + "_MeshPanel" + i,
                MeshKit.Panel(panelW, panelH, tiling, name + "_panel" + i),
                pal.mesh, new Vector3(cx, 0.09f + panelH * 0.5f, 0f), Quaternion.identity, null);

            // frame around the panel
            Piece(run.transform, name + "_RailTop" + i,
                MeshKit.BeveledBox(new Vector3(bayLen - 0.05f, 0.032f, 0.026f), 0.003f, name + "_rail"),
                pal.frame, new Vector3(cx, FenceH - 0.075f, 0f), Quaternion.identity, null);
            Piece(run.transform, name + "_RailBottom" + i,
                MeshKit.BeveledBox(new Vector3(bayLen - 0.05f, 0.032f, 0.026f), 0.003f, name + "_rail"),
                pal.frame, new Vector3(cx, 0.085f, 0f), Quaternion.identity, null);
        }

        var bc = run.AddComponent<BoxCollider>();
        bc.center = new Vector3(0, FenceH * 0.5f, 0);
        bc.size = new Vector3(len, FenceH, 0.06f);
    }

    // --------------------------------------------------------------- cabinet

    static void BuildCabinet(Transform root, AssetForge.Palette pal)
    {
        var cab = new GameObject("ControlCabinet");
        cab.transform.SetParent(root, false);
        cab.transform.position = new Vector3(0.80f, 0f, -0.60f);
        cab.transform.rotation = Quaternion.Euler(0, -28f, 0);
        cab.tag = "CellObstacle";

        float w = 0.46f, h = 1.05f, d = 0.34f;

        Piece(cab.transform, "Cab_Body",
            MeshKit.BeveledBox(new Vector3(w, h, d), 0.010f, "cab"),
            pal.cabinet, new Vector3(0, h * 0.5f + 0.05f, 0), Quaternion.identity, null);
        Piece(cab.transform, "Cab_Plinth",
            MeshKit.BeveledBox(new Vector3(w - 0.03f, 0.05f, d - 0.03f), 0.004f, "plinth"),
            pal.robotDark, new Vector3(0, 0.025f, 0), Quaternion.identity, null);

        // door seam and handle
        Piece(cab.transform, "Cab_DoorSeam",
            MeshKit.BeveledBox(new Vector3(0.006f, h - 0.10f, 0.004f), 0.001f, "seam"),
            pal.robotDark, new Vector3(0, h * 0.5f + 0.05f, d * 0.5f + 0.001f), Quaternion.identity, null);
        Piece(cab.transform, "Cab_Handle",
            MeshKit.BeveledBox(new Vector3(0.022f, 0.110f, 0.018f), 0.003f, "handle"),
            pal.steel, new Vector3(0.10f, h * 0.52f, d * 0.5f + 0.012f), Quaternion.identity, null);

        // vents
        for (int i = 0; i < 5; i++)
            Piece(cab.transform, "Cab_Vent" + i,
                MeshKit.BeveledBox(new Vector3(w - 0.14f, 0.008f, 0.004f), 0.001f, "vent"),
                pal.robotDark, new Vector3(-0.06f, 0.26f + i * 0.020f, d * 0.5f + 0.002f),
                Quaternion.identity, null);

        // power lamp and estop
        Piece(cab.transform, "Cab_Lamp",
            MeshKit.Lathe(new[] { MeshKit.P(0.016f, 0f), MeshKit.P(0.016f, 0.010f), MeshKit.P(0.011f, 0.016f) },
                14, true, true, "lamp"),
            pal.warnGreen, new Vector3(-0.12f, h * 0.80f, d * 0.5f + 0.004f),
            Quaternion.Euler(90, 0, 0), null);

        Piece(cab.transform, "Cab_EStop",
            MeshKit.Lathe(new[] {
                MeshKit.P(0.020f, 0f), MeshKit.P(0.026f, 0.008f),
                MeshKit.P(0.026f, 0.020f), MeshKit.P(0.021f, 0.027f)
            }, 18, true, true, "estop"),
            pal.warnRed, new Vector3(-0.04f, h * 0.80f, d * 0.5f + 0.004f),
            Quaternion.Euler(90, 0, 0), null);

        var bc = cab.AddComponent<BoxCollider>();
        bc.center = new Vector3(0, h * 0.5f + 0.05f, 0);
        bc.size = new Vector3(w, h + 0.1f, d);
    }

    // ---------------------------------------------------------------- beacon

    static void BuildBeacon(Transform root, AssetForge.Palette pal)
    {
        var post = new GameObject("BeaconPost");
        post.transform.SetParent(root, false);
        post.transform.position = new Vector3(-1.02f, 0f, -0.70f);
        post.tag = "CellObstacle";

        Piece(post.transform, "Beacon_Post",
            MeshKit.Tube(0.024f, 0.017f, 1.45f, 14, "bpost"),
            pal.cabinet, Vector3.zero, Quaternion.identity, null);
        Piece(post.transform, "Beacon_Foot",
            MeshKit.BeveledBox(new Vector3(0.12f, 0.016f, 0.12f), 0.003f, "bfoot"),
            pal.robotDark, new Vector3(0, 0.008f, 0), Quaternion.identity, null);

        var head = new GameObject("Beacon_Head");
        head.transform.SetParent(post.transform, false);
        head.transform.localPosition = new Vector3(0, 1.45f, 0);

        Piece(head.transform, "Beacon_Collar",
            MeshKit.Lathe(new[] { MeshKit.P(0.042f, 0f), MeshKit.P(0.042f, 0.020f) }, 18, true, true, "collar"),
            pal.robotDark, Vector3.zero, Quaternion.identity, null);

        var dome = Piece(head.transform, "Beacon_Dome",
            MeshKit.Lathe(new[] {
                MeshKit.P(0.040f, 0.020f), MeshKit.P(0.042f, 0.040f),
                MeshKit.P(0.036f, 0.070f), MeshKit.P(0.020f, 0.090f), MeshKit.P(0.004f, 0.098f)
            }, 20, true, true, "dome"),
            pal.warnRed, Vector3.zero, Quaternion.identity, null);

        var lightGo = new GameObject("Beacon_Light");
        lightGo.transform.SetParent(head.transform, false);
        lightGo.transform.localPosition = new Vector3(0, 0.060f, 0);
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.45f, 0.05f);
        l.range = 3.2f;
        l.intensity = 1.2f;
        l.shadows = LightShadows.None;

        var b = head.AddComponent<BeaconLight>();
        b.domeRenderer = dome.GetComponent<Renderer>();
        b.domeLight = l;
    }

    // ------------------------------------------------------- part and target

    static void BuildPartAndZone(Transform root, AssetForge.Palette pal)
    {
        var oldPart = GameObject.Find("Part");
        if (oldPart != null) Undo.DestroyObjectImmediate(oldPart);
        var oldZone = GameObject.Find("TargetZone");
        if (oldZone != null) Undo.DestroyObjectImmediate(oldZone);

        // the workpiece: a small machined block with a chamfer
        var part = new GameObject("Part");
        part.tag = "Part";
        part.transform.position = TablePos + new Vector3(0, TableTop + 0.060f, 0);

        var vis = Piece(part.transform, "Part_Visual",
            MeshKit.BeveledBox(new Vector3(0.062f, 0.048f, 0.062f), 0.006f, "part"),
            pal.partBlue, Vector3.zero, Quaternion.identity, null);

        var pc = part.AddComponent<BoxCollider>();
        pc.size = new Vector3(0.062f, 0.048f, 0.062f);
        var prb = part.AddComponent<Rigidbody>();
        prb.mass = 0.35f;
        prb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Undo.RegisterCreatedObjectUndo(part, "Part");

        // target zone sitting just above the belt
        var zone = new GameObject("TargetZone");
        zone.transform.position = ConvPos + new Vector3(0, ConvTop + 0.07f, -0.18f);
        var zc = zone.AddComponent<BoxCollider>();
        zc.isTrigger = true;
        zc.size = new Vector3(0.26f, 0.18f, 0.40f);
        zone.AddComponent<TargetZone>();
        Undo.RegisterCreatedObjectUndo(zone, "TargetZone");
    }

    // ------------------------------------------------------------------ util

    static GameObject Piece(Transform parent, string name, Mesh mesh, Material mat,
                            Vector3 localPos, Quaternion localRot, string tag)
    {
        mesh = AssetForge.SaveMesh(mesh, name + "_" + mesh.name);
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        if (!string.IsNullOrEmpty(tag)) go.tag = tag;
        return go;
    }
}
#endif
