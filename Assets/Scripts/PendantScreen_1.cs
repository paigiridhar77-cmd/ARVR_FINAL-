using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Draws the pendant screen. Put this on the world space Canvas that sits
/// on the pendant model and drag in the two text fields.
///
/// If the "using TMPro" line errors, install TextMeshPro from
/// Window > Package Manager, or use Window > TextMeshPro > Import TMP
/// Essential Resources.
/// </summary>
public class PendantScreen : MonoBehaviour
{
    public TeachPendant pendant;
    public SessionLogger logger;

    [Tooltip("Top line: joint, angle, speed, deadman state.")]
    public TMP_Text statusText;

    [Tooltip("Body: the taught program listing.")]
    public TMP_Text programText;

    [Tooltip("Bottom line: collisions and cycle time.")]
    public TMP_Text scoreText;

    private readonly StringBuilder sb = new StringBuilder();

    void Start()
    {
        if (pendant == null) pendant = FindFirstObjectByType<TeachPendant>();
        if (logger == null) logger = FindFirstObjectByType<SessionLogger>();
    }

    void Update()
    {
        if (pendant == null) return;

        if (statusText != null) statusText.text = pendant.StatusLine;

        if (programText != null)
        {
            sb.Length = 0;
            if (pendant.program.Count == 0)
            {
                sb.Append("no points taught");
            }
            else
            {
                for (int i = 0; i < pendant.program.Count; i++)
                {
                    ProgramPoint p = pendant.program[i];
                    sb.Append("P").Append(i + 1).Append("  J1 ")
                      .Append(p.angles[0].ToString("F0"))
                      .Append("  J2 ").Append(p.angles[1].ToString("F0"))
                      .Append("  J3 ").Append(p.angles[2].ToString("F0"));
                    if (p.action != GripperAction.None) sb.Append("   [").Append(p.action).Append("]");
                    sb.AppendLine();
                }
            }
            programText.text = sb.ToString();
        }

        if (scoreText != null && logger != null)
        {
            scoreText.text = string.Format(
                "collisions {0}   limits {1}   cycle {2:F1} s   {3}",
                logger.Collisions, logger.LimitWarnings, logger.CycleTime,
                logger.TaskComplete ? "TASK DONE" : "task not done");
        }
    }
}
