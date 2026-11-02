using UnityEngine;

/// <summary>
/// One revolute joint of the robot.
/// Rotates its own Transform about a fixed local axis, so every link
/// parented under it is carried along. This is the whole kinematic chain:
/// six of these nested inside each other.
/// </summary>
public class RobotJoint : MonoBehaviour
{
    [Header("Identity")]
    public string jointName = "J1";

    [Header("Axis and limits")]
    [Tooltip("Axis of rotation in this joint's own local space.")]
    public Vector3 localAxis = Vector3.up;
    public float minAngle = -170f;
    public float maxAngle = 170f;

    [Tooltip("Degrees per second at 100 percent speed override.")]
    public float maxSpeed = 90f;

    [SerializeField] private float angle = 0f;

    private Quaternion restRotation;
    private bool initialised = false;

    /// <summary>True on the frame the joint was pushed against a limit.</summary>
    public bool HitLimitThisFrame { get; private set; }

    public float Angle { get { return angle; } }

    void Awake()
    {
        Init();
    }

    private void Init()
    {
        if (initialised) return;
        restRotation = transform.localRotation;
        initialised = true;
        Apply();
    }

    /// <summary>Set the joint angle directly, clamped to its limits.</summary>
    public void SetAngle(float value)
    {
        Init();
        float clamped = Mathf.Clamp(value, minAngle, maxAngle);
        HitLimitThisFrame = Mathf.Abs(clamped - value) > 0.0001f;
        angle = clamped;
        Apply();
    }

    /// <summary>
    /// Move the joint for one frame. direction is -1, 0 or +1.
    /// speedScale is the pendant speed override, 0 to 1.
    /// </summary>
    public void Jog(float direction, float speedScale, float deltaTime)
    {
        if (Mathf.Approximately(direction, 0f)) { HitLimitThisFrame = false; return; }
        SetAngle(angle + direction * maxSpeed * speedScale * deltaTime);
    }

    private void Apply()
    {
        transform.localRotation = restRotation * Quaternion.AngleAxis(angle, localAxis.normalized);
    }
}
