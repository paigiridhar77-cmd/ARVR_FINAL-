using UnityEngine;

/// <summary>
/// Goes on every robot link. Reports when that link touches anything
/// tagged as cell hardware.
///
/// IMPORTANT Unity detail: the arm is moved by script, not by physics,
/// so normal collision events will NOT fire. Every link therefore needs
///   - a Collider with Is Trigger ticked
///   - a Rigidbody with Is Kinematic ticked
/// Trigger events do fire for a kinematic rigidbody against a static
/// collider, which is why it is set up this way. If you forget the
/// Rigidbody you will get no collisions at all and think the script is
/// broken.
/// </summary>
public class LinkCollisionReporter : MonoBehaviour
{
    public SessionLogger logger;
    public string obstacleTag = "CellObstacle";

    void Start()
    {
        if (logger == null) logger = FindFirstObjectByType<SessionLogger>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (logger == null) return;
        if (other.CompareTag(obstacleTag))
        {
            logger.ReportCollision(gameObject.name, other.gameObject.name);
        }
    }
}
