#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// One menu item that builds the whole thing: assets, robot, cell, lighting,
/// camera and the TrainerSystem wiring. Use this first, then the individual
/// builders only if you want to rebuild one part.
///
///   Tools > Robot Trainer > 0. Build Everything
/// </summary>
public static class SceneBuilderPro
{
    [MenuItem("Tools/Robot Trainer/0. Build Everything", false, 0)]
    public static void BuildAll()
    {
        AssetForge.BuildPalette();
        RobotBuilderPro.Build();
        CellBuilderPro.Build();
        SetupLighting();
        SetupCamera();
        WireTrainerSystem();

        AssetDatabase.SaveAssets();
        Debug.Log("Scene built. Press Play, click the Game view, then hold Space and " +
                  "use 1 to 6 with the arrow keys to jog the arm.");
    }

    // -------------------------------------------------------------- lighting

    [MenuItem("Tools/Robot Trainer/3. Set Up Lighting", false, 12)]
    public static void SetupLighting()
    {
        // key light
        var sunGo = GameObject.Find("Directional Light");
        if (sunGo == null)
        {
            sunGo = new GameObject("Directional Light");
            sunGo.AddComponent<Light>();
            Undo.RegisterCreatedObjectUndo(sunGo, "Sun");
        }
        var sun = sunGo.GetComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.96f, 0.90f);
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.72f;
        sunGo.transform.rotation = Quaternion.Euler(48f, -38f, 0f);

        // cool fill from the opposite side, so the shadow sides are not black
        var fillGo = GameObject.Find("Fill Light");
        if (fillGo == null)
        {
            fillGo = new GameObject("Fill Light");
            fillGo.AddComponent<Light>();
            Undo.RegisterCreatedObjectUndo(fillGo, "Fill");
        }
        var fill = fillGo.GetComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.72f, 0.80f, 0.95f);
        fill.intensity = 0.45f;
        fill.shadows = LightShadows.None;
        fillGo.transform.rotation = Quaternion.Euler(26f, 152f, 0f);

        // ambient: a workshop is lit from a bright ceiling and a grey floor
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.60f, 0.68f);
        RenderSettings.ambientEquatorColor = new Color(0.42f, 0.44f, 0.47f);
        RenderSettings.ambientGroundColor = new Color(0.24f, 0.24f, 0.25f);
        RenderSettings.fog = false;

        Debug.Log("Lighting set: key, fill and trilight ambient.");
    }

    // ---------------------------------------------------------------- camera

    // The cell is small and ringed by nine fence posts, a control cabinet, a
    // beacon post, the conveyor and the machine table, so most camera angles
    // put something squarely in front of the arm. These two positions were
    // checked by tracing the sight line from the camera to the arm, the part
    // and the conveyor belt, and they are clear of all of it.
    //
    // Aim at a point a little above the robot base rather than at the origin,
    // or the shot sits too low and fills with floor.
    // Aim at the middle of the arm. The robot stands on a 0.40 m pedestal, so
    // this is higher than it looks: aiming lower fills the shot with floor.
    static readonly Vector3 LookTarget = new Vector3(0.05f, 0.95f, 0.15f);

    /// <summary>Whole cell in frame. Use this for the work cell figure.</summary>
    [MenuItem("Tools/Robot Trainer/4. Frame Camera (wide)", false, 13)]
    public static void SetupCamera()
    {
        PlaceCamera(new Vector3(1.95f, 1.80f, 3.44f), 55f);
        Debug.Log("Camera framed wide on the whole work cell.");
    }

    /// <summary>
    /// Close on the arm, about 2 m out, for the jogging and pick and place
    /// figures. The arm fills roughly 40 percent of the frame height here
    /// against 24 percent on the wide shot.
    /// </summary>
    [MenuItem("Tools/Robot Trainer/4b. Frame Camera (close on robot)", false, 14)]
    public static void SetupCameraClose()
    {
        PlaceCamera(new Vector3(-1.68f, 1.30f, -0.85f), 55f);
        Debug.Log("Camera framed close on the robot.");
    }

    /// <summary>
    /// Tight on the fixture, with a narrower lens so there is less perspective
    /// distortion. This is the view for checking whether the gripper is lined
    /// up over the part, together with the PickAssist readout.
    /// </summary>
    [MenuItem("Tools/Robot Trainer/4c. Frame Camera (over the fixture)", false, 15)]
    public static void SetupCameraFixture()
    {
        PlaceCamera(new Vector3(0.82f, 0.95f, 1.07f), 40f, new Vector3(0.46f, 0.68f, 0.08f));
        Debug.Log("Camera framed on the fixture. Watch the PickAssist numbers, not the picture.");
    }

    static void PlaceCamera(Vector3 pos, float fov)
    {
        PlaceCamera(pos, fov, LookTarget);
    }

    static void PlaceCamera(Vector3 pos, float fov, Vector3 target)
    {
        var camGo = Camera.main != null ? Camera.main.gameObject : GameObject.Find("Main Camera");
        if (camGo == null)
        {
            camGo = new GameObject("Main Camera");
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.tag = "MainCamera";
            Undo.RegisterCreatedObjectUndo(camGo, "Camera");
        }

        // clear any parent first, or an inherited transform moves the camera
        // somewhere else entirely. LookAt derives the rotation from the
        // position, so the two can never drift out of step the way a hand
        // written Euler angle can.
        camGo.transform.SetParent(null, true);
        camGo.transform.position = pos;
        camGo.transform.LookAt(target);

        var cam = camGo.GetComponent<Camera>();
        cam.fieldOfView = fov;
        cam.nearClipPlane = 0.03f;
        cam.farClipPlane = 60f;
        cam.enabled = true;

        // a second enabled camera left in the scene can win and render
        // something else entirely, so switch any other one off
        foreach (var other in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (other != cam) other.enabled = false;

        Undo.RegisterFullObjectHierarchyUndo(camGo, "Frame Camera");
        EditorUtility.SetDirty(camGo);
    }

    // ---------------------------------------------------------------- wiring

    [MenuItem("Tools/Robot Trainer/5. Wire Trainer System", false, 16)]
    public static void WireTrainerSystem()
    {
        var sysGo = GameObject.Find("TrainerSystem");
        if (sysGo == null)
        {
            sysGo = new GameObject("TrainerSystem");
            Undo.RegisterCreatedObjectUndo(sysGo, "TrainerSystem");
        }
        sysGo.transform.position = Vector3.zero;

        var pendant = sysGo.GetComponent<TeachPendant>();
        if (pendant == null) pendant = sysGo.AddComponent<TeachPendant>();

        var logger = sysGo.GetComponent<SessionLogger>();
        if (logger == null) logger = sysGo.AddComponent<SessionLogger>();

        var input = sysGo.GetComponent<DesktopPendantInput>();
        if (input == null) input = sysGo.AddComponent<DesktopPendantInput>();

        var assist = sysGo.GetComponent<PickAssist>();
        if (assist == null) assist = sysGo.AddComponent<PickAssist>();

        var robot = GameObject.Find("Robot");
        if (robot != null)
        {
            pendant.arm = robot.GetComponent<RobotArm>();
            pendant.gripper = robot.GetComponentInChildren<Gripper>();
        }
        pendant.logger = logger;
        input.pendant = pendant;

        if (robot != null) assist.gripper = robot.GetComponentInChildren<Gripper>();
        var partGo = GameObject.Find("Part");
        if (partGo != null) assist.part = partGo.transform;

        var zone = GameObject.Find("TargetZone");
        if (zone != null)
        {
            var tz = zone.GetComponent<TargetZone>();
            if (tz != null) tz.logger = logger;
            assist.dropZone = zone.transform;
        }

        foreach (var rep in Object.FindObjectsByType<LinkCollisionReporter>(FindObjectsSortMode.None))
            rep.logger = logger;

        EditorUtility.SetDirty(sysGo);
        Debug.Log("TrainerSystem wired: arm, gripper, logger, target zone and link reporters.");
    }
}
#endif
