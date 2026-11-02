using UnityEngine;

/// <summary>
/// An invisible trigger box sitting just above the conveyor. When the part
/// is released inside it and stops moving, the task counts as complete.
/// Waiting for it to settle stops a part that merely flies through on the
/// way past from scoring a false pass.
/// </summary>
public class TargetZone : MonoBehaviour
{
    public SessionLogger logger;
    public string partTag = "Part";

    [Tooltip("Part must be inside the zone and this slow to count.")]
    public float settleSpeed = 0.05f;

    [Tooltip("And stay that way for this long.")]
    public float settleTime = 0.5f;

    private float insideSince = -1f;

    void Start()
    {
        if (logger == null) logger = FindFirstObjectByType<SessionLogger>();
    }

    void OnTriggerStay(Collider other)
    {
        if (logger == null || logger.TaskComplete) return;
        if (!other.CompareTag(partTag)) return;

        Rigidbody rb = other.attachedRigidbody;
        bool settled = rb == null || (rb.linearVelocity.magnitude < settleSpeed && !rb.isKinematic);

        if (!settled)
        {
            insideSince = -1f;
            return;
        }

        if (insideSince < 0f) insideSince = Time.time;

        if (Time.time - insideSince >= settleTime)
        {
            logger.ReportTaskComplete();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(partTag)) insideSince = -1f;
    }
}
