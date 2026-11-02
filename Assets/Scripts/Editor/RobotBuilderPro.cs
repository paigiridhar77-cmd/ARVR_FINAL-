#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds a properly modelled six axis industrial arm, proportioned on the
/// FANUC LR Mate 200iD (about 0.72 m reach).
///
/// The important thing about this builder is what it does NOT change. The
/// component layout is identical to the first version: a RobotArm on the root,
/// a RobotJoint on each of J1 to J6 with the same axes, limits and speeds, and
/// collider objects still named Link1 to Link6 so the Console still prints
/// "Link3 hit MachineTable" exactly as before. Every existing script, every
/// taught program and every logged result stays valid. Only the geometry is
/// different.
///
/// Visuals and collision are kept separate on purpose. The detailed castings
/// are display only, with no colliders at all, while collision uses a handful
/// of simple boxes. That is how real simulation packages do it: swept volumes
/// for physics, pretty meshes for the eye. It also keeps the collision
/// behaviour exactly as it was when the test results were recorded.
///
///   Tools > Robot Trainer > 1. Build Detailed Robot
/// </summary>
public static class RobotBuilderPro
{
    // link lengths, metres
    const float H_BASE = 0.145f;   // floor to the J1 mounting face
    const float H_J1 = 0.185f;     // J1 face up to the shoulder pivot
    const float L_UPPER = 0.330f;  // J2 -> J3
    const float L_FORE = 0.260f;   // J3 -> J5
    const float L_WRIST = 0.085f;  // J5 -> tool flange

    // Fabricated pedestal the robot stands on. This is not decoration.
    // The wrist centre is only L_UPPER + L_FORE = 0.590 m from the shoulder,
    // and a top-down pick needs the wrist directly above the part by the whole
    // tool length. Bolted flat to the floor the shoulder sits at 0.330 m, the
    // part sits at 0.760 m, and the wrist would have to reach 0.680 m: 90 mm
    // more than the arm has. Raising the shoulder to 0.730 m brings that to
    // 0.514 m, 87 percent of reach, which still leaves the arm a visible bend.
    // A pedestal this size is also what a real LR Mate 200iD machine tending
    // cell uses, so it reads correctly as well as working.
    const float RISER = 0.400f;

    struct JointSpec
    {
        public string name; public Vector3 offset; public Vector3 axis;
        public float min, max, speed;
        public JointSpec(string n, Vector3 o, Vector3 a, float mn, float mx, float sp)
        { name = n; offset = o; axis = a; min = mn; max = mx; speed = sp; }
    }

    // Axes, limits and speeds are unchanged from the first build, so the
    // report's figures stay true. Only J1's height above the floor changes,
    // because the whole robot now stands on the pedestal.
    static readonly JointSpec[] Specs = {
        new JointSpec("J1", new Vector3(0, RISER + H_BASE, 0), Vector3.up, -170f, 170f, 110f),
        new JointSpec("J2", new Vector3(0, H_J1, 0),    Vector3.right, -100f, 100f, 100f),
        new JointSpec("J3", new Vector3(0, L_UPPER, 0), Vector3.right, -120f, 120f, 110f),
        new JointSpec("J4", new Vector3(0, L_FORE, 0),  Vector3.up,    -180f, 180f, 180f),
        new JointSpec("J5", Vector3.zero,               Vector3.right, -110f, 110f, 180f),
        new JointSpec("J6", new Vector3(0, L_WRIST, 0), Vector3.up,    -360f, 360f, 250f),
    };

    [MenuItem("Tools/Robot Trainer/1. Build Detailed Robot", false, 10)]
    public static void Build()
    {
        var pal = AssetForge.BuildPalette();
        TagKit.EnsureTag("Part");
        TagKit.EnsureTag("CellObstacle");

        var old = GameObject.Find("Robot");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("Robot");
        Undo.RegisterCreatedObjectUndo(root, "Build Detailed Robot");
        root.transform.position = Vector3.zero;

        BuildPedestal(root.transform, pal);

        Transform parent = root.transform;
        var joints = new RobotJoint[Specs.Length];

        for (int i = 0; i < Specs.Length; i++)
        {
            JointSpec s = Specs[i];
            var go = new GameObject(s.name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = s.offset;

            var rj = go.AddComponent<RobotJoint>();
            rj.jointName = s.name;
            rj.localAxis = s.axis;
            rj.minAngle = s.min;
            rj.maxAngle = s.max;
            rj.maxSpeed = s.speed;
            joints[i] = rj;

            BuildLinkVisual(i, go.transform, pal);
            BuildLinkCollider(i, go.transform);

            parent = go.transform;
        }

        // tool flange and gripper hang off J6
        var tool = new GameObject("Tool");
        tool.transform.SetParent(parent, false);
        tool.transform.localPosition = new Vector3(0, 0.030f, 0);
        BuildGripper(tool.transform, pal);

        var arm = root.AddComponent<RobotArm>();
        arm.joints = joints;

        Selection.activeGameObject = root;
        Debug.Log("Detailed robot built. Reach is about 0.72 m. " +
                  "Visual meshes carry no colliders; collision uses Link1 to Link6.");
    }

    // ------------------------------------------------------------- pedestal

    static void BuildPedestal(Transform root, AssetForge.Palette pal)
    {
        // ---- the fabricated steel pedestal, floor up to RISER
        Piece(root, "Riser_FootPlate",
            MeshKit.BeveledBox(new Vector3(0.420f, 0.022f, 0.420f), 0.004f, "riser_foot"),
            pal.cabinet, new Vector3(0, 0.011f, 0), Quaternion.identity);

        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? -1 : 1) * 0.168f;
            float z = (i < 2 ? -1 : 1) * 0.168f;
            Piece(root, "Riser_Anchor" + i,
                MeshKit.Lathe(new[] {
                    MeshKit.P(0.016f, 0f), MeshKit.P(0.016f, 0.011f), MeshKit.P(0.012f, 0.015f)
                }, 6, true, true, "anchor"),
                pal.robotDark, new Vector3(x, 0.022f, z), Quaternion.identity);
        }

        float colH = RISER - 0.045f;                      // between the two plates
        Piece(root, "Riser_Column",
            MeshKit.BeveledBox(new Vector3(0.200f, colH, 0.200f), 0.008f, "riser_col"),
            pal.cabinet, new Vector3(0, 0.022f + colH * 0.5f, 0), Quaternion.identity);

        // gusset ribs on all four faces, which is what makes it read as
        // fabricated steel rather than a grey box
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f;
            var rot = Quaternion.Euler(0, a, 0);
            Piece(root, "Riser_Gusset" + i,
                MeshKit.BeveledBox(new Vector3(0.012f, colH * 0.62f, 0.085f), 0.003f, "gusset"),
                pal.cabinet,
                rot * new Vector3(0.106f, 0.022f + colH * 0.31f, 0f), rot);
        }

        Piece(root, "Riser_TopPlate",
            MeshKit.BeveledBox(new Vector3(0.320f, 0.023f, 0.320f), 0.004f, "riser_top"),
            pal.steel, new Vector3(0, RISER - 0.0115f, 0), Quaternion.identity);

        // ---- the robot's own cast base, now sitting on top of the pedestal
        Piece(root, "Base_MountPlate",
            MeshKit.BeveledBox(new Vector3(0.30f, 0.018f, 0.30f), 0.004f, "base_plate"),
            pal.steel, new Vector3(0, RISER + 0.009f, 0), Quaternion.identity);

        // four foot bolts, the small detail that sells a machine base
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? -1 : 1) * 0.118f;
            float z = (i < 2 ? -1 : 1) * 0.118f;
            Piece(root, "Base_Bolt" + i,
                MeshKit.Lathe(new[] {
                    MeshKit.P(0.013f, 0f), MeshKit.P(0.013f, 0.009f), MeshKit.P(0.010f, 0.012f)
                }, 6, true, true, "bolt"),
                pal.robotDark, new Vector3(x, RISER + 0.018f, z), Quaternion.identity);
        }

        Piece(root, "Base_Casting",
            MeshKit.Lathe(new[] {
                MeshKit.P(0.105f, 0.018f), MeshKit.P(0.105f, 0.036f), MeshKit.P(0.092f, 0.048f),
                MeshKit.P(0.090f, 0.105f), MeshKit.P(0.100f, 0.118f), MeshKit.P(0.100f, 0.132f),
                MeshKit.P(0.082f, 0.145f)
            }, 40, true, true, "base_casting"),
            pal.robotDark, new Vector3(0, RISER, 0), Quaternion.identity);
    }

    // --------------------------------------------------------- link visuals

    static void BuildLinkVisual(int i, Transform jointT, AssetForge.Palette pal)
    {
        Quaternion toX = Quaternion.Euler(0, 0, 90);   // lathe axis Y -> X

        switch (i)
        {
            case 0: // J1 turret and shoulder cheeks
                Piece(jointT, "Vis_J1_Turret",
                    MeshKit.Lathe(new[] {
                        MeshKit.P(0.082f, 0f), MeshKit.P(0.086f, 0.022f), MeshKit.P(0.086f, 0.118f),
                        MeshKit.P(0.078f, 0.146f), MeshKit.P(0.070f, 0.172f)
                    }, 36, true, true, "j1_turret"),
                    pal.robotYellow, Vector3.zero, Quaternion.identity);

                for (int s = 0; s < 2; s++)
                {
                    float sx = s == 0 ? -1f : 1f;
                    Piece(jointT, "Vis_J1_Cheek" + s,
                        MeshKit.BeveledBox(new Vector3(0.052f, 0.150f, 0.104f), 0.016f, "j1_cheek"),
                        pal.robotYellow, new Vector3(sx * 0.070f, 0.108f, 0f), Quaternion.identity);
                }
                break;

            case 1: // J2 upper arm
                Piece(jointT, "Vis_J2_Arm",
                    MeshKit.BeveledBox(new Vector3(0.096f, L_UPPER * 0.80f, 0.110f), 0.022f, "j2_arm"),
                    pal.robotYellow, new Vector3(0, L_UPPER * 0.47f, 0), Quaternion.identity);

                Piece(jointT, "Vis_J2_Motor",
                    Barrel(0.074f, 0.064f, 0.062f, 28, "j2_motor"),
                    pal.robotYellow, Vector3.zero, toX);

                Piece(jointT, "Vis_J2_Conduit",
                    MeshKit.BeveledBox(new Vector3(0.034f, L_UPPER * 0.62f, 0.030f), 0.008f, "conduit"),
                    pal.robotDark, new Vector3(0, L_UPPER * 0.46f, -0.064f), Quaternion.identity);

                Piece(jointT, "Vis_J2_ElbowBoss",
                    Barrel(0.060f, 0.052f, 0.050f, 26, "j3_boss"),
                    pal.robotYellow, new Vector3(0, L_UPPER, 0), toX);
                break;

            case 2: // J3 forearm
                Piece(jointT, "Vis_J3_Forearm",
                    MeshKit.BeveledBox(new Vector3(0.080f, L_FORE * 0.78f, 0.090f), 0.020f, "j3_fore"),
                    pal.robotYellow, new Vector3(0, L_FORE * 0.44f, 0), Quaternion.identity);

                Piece(jointT, "Vis_J3_MotorCan",
                    MeshKit.Lathe(new[] {
                        MeshKit.P(0.050f, 0f), MeshKit.P(0.054f, 0.012f), MeshKit.P(0.054f, 0.086f),
                        MeshKit.P(0.046f, 0.100f)
                    }, 26, true, true, "j4_can"),
                    pal.robotYellow, new Vector3(0, L_FORE * 0.78f - 0.030f, 0), Quaternion.identity);
                break;

            case 3: // J4 roll housing
                Piece(jointT, "Vis_J4_Housing",
                    MeshKit.Lathe(new[] {
                        MeshKit.P(0.044f, -0.034f), MeshKit.P(0.048f, -0.024f),
                        MeshKit.P(0.048f, 0.006f), MeshKit.P(0.042f, 0.016f)
                    }, 26, true, true, "j4_house"),
                    pal.robotGrey, Vector3.zero, Quaternion.identity);
                break;

            case 4: // J5 wrist cluster
                Piece(jointT, "Vis_J5_Wrist",
                    MeshKit.BeveledBox(new Vector3(0.064f, 0.068f, 0.072f), 0.016f, "j5_wrist"),
                    pal.robotYellow, new Vector3(0, 0.030f, 0), Quaternion.identity);

                Piece(jointT, "Vis_J5_Pivot",
                    Barrel(0.042f, 0.040f, 0.038f, 22, "j5_pivot"),
                    pal.robotGrey, new Vector3(0, 0.030f, 0), toX);
                break;

            case 5: // J6 flange
                Piece(jointT, "Vis_J6_Flange",
                    MeshKit.Lathe(new[] {
                        MeshKit.P(0.030f, 0f), MeshKit.P(0.033f, 0.006f),
                        MeshKit.P(0.033f, 0.022f), MeshKit.P(0.026f, 0.030f)
                    }, 28, true, true, "flange"),
                    pal.robotGrey, Vector3.zero, Quaternion.identity);

                // bolt circle on the flange face
                for (int b = 0; b < 6; b++)
                {
                    float a = b / 6f * Mathf.PI * 2f;
                    Piece(jointT, "Vis_J6_Bolt" + b,
                        MeshKit.Lathe(new[] {
                            MeshKit.P(0.0045f, 0f), MeshKit.P(0.0045f, 0.004f)
                        }, 6, true, true, "fbolt"),
                        pal.robotDark,
                        new Vector3(Mathf.Cos(a) * 0.022f, 0.030f, Mathf.Sin(a) * 0.022f),
                        Quaternion.identity);
                }
                break;
        }
    }

    /// <summary>Symmetric barrel centred on its own origin, for cross-axis motors.</summary>
    static Mesh Barrel(float rMax, float rEnd, float halfLen, int seg, string name)
    {
        return MeshKit.Lathe(new[] {
            MeshKit.P(rEnd * 0.86f, -halfLen),
            MeshKit.P(rEnd,         -halfLen * 0.84f),
            MeshKit.P(rMax,         -halfLen * 0.52f),
            MeshKit.P(rMax,          halfLen * 0.52f),
            MeshKit.P(rEnd,          halfLen * 0.84f),
            MeshKit.P(rEnd * 0.86f,  halfLen)
        }, seg, true, true, name);
    }

    // -------------------------------------------------------- link colliders

    static void BuildLinkCollider(int i, Transform jointT)
    {
        // one simple box per link, matching the swept volume of the casting
        Vector3 size, centre;
        switch (i)
        {
            case 0: size = new Vector3(0.21f, 0.18f, 0.17f); centre = new Vector3(0, 0.090f, 0); break;
            case 1: size = new Vector3(0.11f, L_UPPER, 0.13f); centre = new Vector3(0, L_UPPER * 0.50f, -0.01f); break;
            case 2: size = new Vector3(0.10f, L_FORE, 0.11f); centre = new Vector3(0, L_FORE * 0.48f, 0); break;
            case 3: size = new Vector3(0.09f, 0.06f, 0.09f); centre = new Vector3(0, -0.010f, 0); break;
            case 4: size = new Vector3(0.09f, 0.09f, 0.09f); centre = new Vector3(0, 0.032f, 0); break;
            default: size = new Vector3(0.07f, 0.045f, 0.07f); centre = new Vector3(0, 0.016f, 0); break;
        }

        var go = new GameObject("Link" + (i + 1));
        go.transform.SetParent(jointT, false);
        go.transform.localPosition = centre;

        var bc = go.AddComponent<BoxCollider>();
        bc.size = size;
        bc.isTrigger = true;

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        go.AddComponent<LinkCollisionReporter>();
    }

    // -------------------------------------------------------------- gripper

    static void BuildGripper(Transform tool, AssetForge.Palette pal)
    {
        var grip = new GameObject("Gripper");
        grip.transform.SetParent(tool, false);
        grip.transform.localPosition = Vector3.zero;

        // Local +Y points AWAY from the flange, so everything here is built
        // outward from zero: housing first, then the fingers past it. Getting
        // this the wrong way round puts the fingers inside the wrist casting
        // and the grip volume behind the flange, where a part can never be.
        //
        // The finger gap has to clear the part, which is a 0.062 m cube, so
        // the pads sit at +/-0.0425 leaving a 0.073 m opening.

        Piece(grip.transform, "Vis_Grip_Body",
            MeshKit.BeveledBox(new Vector3(0.115f, 0.048f, 0.068f), 0.008f, "grip_body"),
            pal.robotDark, new Vector3(0, 0.024f, 0), Quaternion.identity);

        // pneumatic actuator, lying across the back of the housing
        Piece(grip.transform, "Vis_Grip_Cylinder",
            MeshKit.Lathe(new[] {
                MeshKit.P(0.018f, 0f), MeshKit.P(0.020f, 0.004f),
                MeshKit.P(0.020f, 0.062f), MeshKit.P(0.016f, 0.066f)
            }, 20, true, true, "grip_cyl"),
            pal.steel, new Vector3(0.033f, 0.026f, -0.046f), Quaternion.Euler(0, 0, 90));

        for (int s = 0; s < 2; s++)
        {
            float sx = s == 0 ? -1f : 1f;

            // finger, running from the face of the housing outward
            Piece(grip.transform, "Vis_Grip_Finger" + s,
                MeshKit.BeveledBox(new Vector3(0.014f, 0.062f, 0.040f), 0.003f, "finger"),
                pal.steel, new Vector3(sx * 0.050f, 0.079f, 0f), Quaternion.identity);

            // rubber gripping pad on the inner face, near the tip
            Piece(grip.transform, "Vis_Grip_Pad" + s,
                MeshKit.BeveledBox(new Vector3(0.012f, 0.030f, 0.044f), 0.002f, "pad"),
                pal.blackRubber, new Vector3(sx * 0.0425f, 0.092f, 0f), Quaternion.identity);
        }

        // grip detection volume, sitting in the opening between the pads
        var bc = grip.AddComponent<BoxCollider>();
        bc.center = new Vector3(0, 0.070f, 0);
        bc.size = new Vector3(0.080f, 0.075f, 0.080f);
        bc.isTrigger = true;

        var rb = grip.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var attach = new GameObject("AttachPoint");
        attach.transform.SetParent(grip.transform, false);
        attach.transform.localPosition = new Vector3(0, 0.070f, 0);

        var g = grip.AddComponent<Gripper>();
        g.attachPoint = attach.transform;
        g.partTag = "Part";
    }

    // ----------------------------------------------------------------- util

    static GameObject Piece(Transform parent, string name, Mesh mesh, Material mat,
                            Vector3 localPos, Quaternion localRot)
    {
        mesh = AssetForge.SaveMesh(mesh, name + "_" + mesh.name);
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        return go;
    }
}

/// <summary>Adds the tags the cell needs if the project does not have them yet.</summary>
public static class TagKit
{
    public static void EnsureTag(string tag)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;
        var so = new SerializedObject(assets[0]);
        var tags = so.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        so.ApplyModifiedProperties();
    }
}
#endif
