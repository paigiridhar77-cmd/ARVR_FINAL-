using UnityEngine;

/// <summary>
/// Shows, as numbers in the corner of the Game view, where the gripper is
/// relative to whatever it is aiming at: the part while the hand is empty, the
/// drop zone over the conveyor once the part is held.
///
/// This exists because judging "is the gripper directly above the part" by eye
/// in a perspective view is close to impossible. A real teach pendant shows the
/// tool position for the same reason, so this is not a crutch, it is what the
/// machine would give you.
///
/// Goes on the TrainerSystem object. Press G to hide it before taking
/// screenshots for the report.
/// </summary>
public class PickAssist : MonoBehaviour
{
    public Gripper gripper;
    public Transform part;
    public Transform dropZone;

    [Tooltip("Within this many millimetres horizontally the gripper counts as lined up.")]
    public float alignToleranceMm = 25f;

    public bool show = true;
    public KeyCode toggleKey = KeyCode.G;

    private Transform grip;         // the AttachPoint, the point between the pads
    private GUIStyle box, head, big, small;

    void Start()
    {
        if (gripper == null) gripper = FindFirstObjectByType<Gripper>();
        if (gripper != null)
            grip = gripper.attachPoint != null ? gripper.attachPoint : gripper.transform;

        if (part == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Part");
            if (p == null) p = GameObject.Find("Part");
            if (p != null) part = p.transform;
        }

        if (dropZone == null)
        {
            GameObject z = GameObject.Find("TargetZone");
            if (z != null) dropZone = z.transform;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) show = !show;
    }

    void BuildStyles()
    {
        box = new GUIStyle(GUI.skin.box);
        box.normal.background = Texture2D.whiteTexture;
        box.padding = new RectOffset(14, 14, 10, 12);

        head = new GUIStyle(GUI.skin.label);
        head.fontSize = 13;
        head.fontStyle = FontStyle.Bold;
        head.normal.textColor = new Color(0.82f, 0.86f, 0.92f);

        big = new GUIStyle(GUI.skin.label);
        big.fontSize = 20;
        big.fontStyle = FontStyle.Bold;

        small = new GUIStyle(GUI.skin.label);
        small.fontSize = 13;
        small.normal.textColor = new Color(0.78f, 0.81f, 0.86f);
    }

    void OnGUI()
    {
        if (!show || grip == null) return;
        if (box == null) BuildStyles();

        bool holding = gripper != null && gripper.HasPart;
        Transform target = holding ? dropZone : part;
        string what = holding ? "DROP ZONE" : "PART";
        if (target == null) return;

        Vector3 d = target.position - grip.position;
        float across = new Vector2(d.x, d.z).magnitude * 1000f;   // mm
        float height = -d.y * 1000f;                              // mm, + means above
        bool lined = across <= alignToleranceMm;
        bool inRange = gripper != null && (holding || gripper.PartInRange);

        // verdict
        string verdict;
        Color tint;
        if (inRange && !holding)      { verdict = "IN THE GRIP, PRESS C"; tint = new Color(0.35f, 0.85f, 0.40f); }
        else if (holding && lined && Mathf.Abs(height) < 90f)
                                      { verdict = "OVER THE ZONE, PRESS O"; tint = new Color(0.35f, 0.85f, 0.40f); }
        else if (!lined)              { verdict = "NOT LINED UP"; tint = new Color(0.95f, 0.62f, 0.25f); }
        else if (height > 0f)         { verdict = "LINED UP, GO DOWN"; tint = new Color(0.45f, 0.75f, 0.98f); }
        else                          { verdict = "LINED UP, GO UP"; tint = new Color(0.45f, 0.75f, 0.98f); }

        var prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.07f, 0.08f, 0.10f, 0.88f);
        GUILayout.BeginArea(new Rect(14, 14, 290, 182), box);
        GUI.backgroundColor = prev;

        GUILayout.Label("GRIPPER TO " + what, head);
        GUILayout.Space(4);

        big.normal.textColor = lined ? new Color(0.55f, 0.90f, 0.58f)
                                     : new Color(0.98f, 0.72f, 0.38f);
        GUILayout.Label("across  " + across.ToString("0") + " mm", big);

        big.normal.textColor = new Color(0.86f, 0.89f, 0.94f);
        string updown = height >= 0f ? "above" : "below";
        GUILayout.Label("height  " + Mathf.Abs(height).ToString("0") + " mm " + updown, big);

        GUILayout.Space(2);
        GUILayout.Label("X " + (d.x * 1000f).ToString("+0;-0") +
                        "    Z " + (d.z * 1000f).ToString("+0;-0") + "   mm to go", small);

        GUILayout.Space(6);
        var vs = new GUIStyle(small);
        vs.fontSize = 14;
        vs.fontStyle = FontStyle.Bold;
        vs.normal.textColor = tint;
        GUILayout.Label(verdict, vs);

        GUILayout.Label("G hides this panel", small);
        GUILayout.EndArea();
    }
}
