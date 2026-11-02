using UnityEngine;

/// <summary>
/// Sits on the tool at the end of the arm. It does not simulate fingers.
/// Closing parents whatever part is inside the gripper volume to the tool,
/// opening unparents it and lets physics take over again. This is exactly
/// what industrial simulation packages do and it is far more stable than
/// trying to grip with friction.
///
/// Needs: a Box Collider with Is Trigger ticked, and a Rigidbody with
/// Is Kinematic ticked (trigger events need a rigidbody on one side).
/// </summary>
public class Gripper : MonoBehaviour
{
    [Tooltip("Empty child transform where a picked part is parented.")]
    public Transform attachPoint;

    public string partTag = "Part";

    private GameObject held;
    private GameObject candidate;

    public bool HasPart { get { return held != null; } }

    /// <summary>
    /// True when a part is inside the grip volume, so pressing close would
    /// actually pick something up. PickAssist uses this to tell you when you
    /// are low enough.
    /// </summary>
    public bool PartInRange { get { return candidate != null; } }

    void Reset()
    {
        attachPoint = transform;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(partTag) && held == null)
        {
            candidate = other.gameObject;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (candidate == other.gameObject)
        {
            candidate = null;
        }
    }

    /// <summary>Returns true if a part was actually picked up.</summary>
    public bool Close()
    {
        if (held != null || candidate == null) return false;

        held = candidate;
        Rigidbody rb = held.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        held.transform.SetParent(attachPoint != null ? attachPoint : transform, true);
        return true;
    }

    public void Open()
    {
        if (held == null) return;

        held.transform.SetParent(null, true);
        Rigidbody rb = held.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        held = null;
        candidate = null;
    }
}
