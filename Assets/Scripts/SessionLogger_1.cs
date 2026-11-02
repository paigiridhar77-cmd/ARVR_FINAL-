using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Counts collisions, limit warnings and cycle time, and writes two files:
///
///   summary.csv   one row per session, appended. This is what you plot.
///   events_*.csv  the full timeline of one session.
///
/// On Windows:  C:\Users\YOU\AppData\LocalLow\DefaultCompany\RobotTrainer\
/// On macOS:    ~/Library/Application Support/DefaultCompany/RobotTrainer/
///
/// The exact folder is printed to the Console when you save.
///
/// TO SAVE WITHOUT THE KEYBOARD: in play mode, right click the
/// "Session Logger (Script)" header in the Inspector and choose
/// "Save session now". This works even when function keys are taken
/// over by the operating system, which is the usual situation on a Mac.
/// </summary>
public class SessionLogger : MonoBehaviour
{
    [Header("Fill these in before each participant runs")]
    public string participantId = "P01";

    [Tooltip("vr_trained or sheet_only. Keep the spelling identical every time.")]
    public string condition = "vr_trained";

    public int attempt = 1;

    [Header("Live counters, read only")]
    public int Collisions;
    public int LimitWarnings;
    public float CycleTime;
    public int PointsTaught;
    public float PathLengthDeg;
    public bool TaskComplete;

    [Tooltip("Same link touching the same object again within this many seconds is not counted twice.")]
    public float collisionCooldown = 0.5f;

    private float sessionStart;
    private readonly List<string> events = new List<string>();
    private readonly Dictionary<string, float> lastHit = new Dictionary<string, float>();
    private readonly Dictionary<string, float> lastLimit = new Dictionary<string, float>();

    void Start()
    {
        sessionStart = Time.time;
        Log("session_start", participantId + " " + condition + " attempt " + attempt);
    }

    public float SessionSeconds { get { return Time.time - sessionStart; } }

    public void ReportCollision(string link, string obstacle)
    {
        string key = link + "|" + obstacle;
        float t;
        if (lastHit.TryGetValue(key, out t) && Time.time - t < collisionCooldown) return;

        lastHit[key] = Time.time;
        Collisions++;
        Log("collision", link + " hit " + obstacle);
        Debug.Log("COLLISION: " + link + " hit " + obstacle);
    }

    public void ReportLimit(string jointName)
    {
        float t;
        if (lastLimit.TryGetValue(jointName, out t) && Time.time - t < collisionCooldown) return;

        lastLimit[jointName] = Time.time;
        LimitWarnings++;
        Log("joint_limit", jointName);
    }

    public void ReportTaskComplete()
    {
        if (TaskComplete) return;
        TaskComplete = true;
        Log("task_complete", string.Format("at {0:F1} s", SessionSeconds));
        Debug.Log("TASK COMPLETE");
    }

    public void Log(string type, string detail)
    {
        events.Add(string.Format("{0:F2},{1},{2}", SessionSeconds, type, detail));
    }

    /// <summary>
    /// Writes the session to disk. Call it from the F5 key, from a pendant
    /// button, or by right clicking this component in the Inspector.
    /// </summary>
    [ContextMenu("Save session now")]
    public void SaveSession()
    {
        string dir = Application.persistentDataPath;

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string summaryPath = Path.Combine(dir, "summary.csv");

        if (!File.Exists(summaryPath))
        {
            File.WriteAllText(summaryPath,
                "participant,condition,attempt,collisions,limit_warnings,cycle_time_s," +
                "points_taught,path_length_deg,task_complete,session_time_s\n");
        }

        string row = string.Format(
            "{0},{1},{2},{3},{4},{5:F2},{6},{7:F0},{8},{9:F1}\n",
            participantId, condition, attempt, Collisions, LimitWarnings,
            CycleTime, PointsTaught, PathLengthDeg, TaskComplete ? 1 : 0, SessionSeconds);

        File.AppendAllText(summaryPath, row);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("time_s,event,detail");
        foreach (string e in events) sb.AppendLine(e);

        string eventsPath = Path.Combine(dir,
            string.Format("events_{0}_{1}_a{2}.csv", participantId, condition, attempt));
        File.WriteAllText(eventsPath, sb.ToString());

        Debug.Log("SAVED SESSION. Folder: " + dir);
        Debug.Log("Summary file: " + summaryPath);
    }

    /// <summary>Reset counters between attempts without restarting Unity.</summary>
    public void NewSession(string newParticipant, string newCondition, int newAttempt)
    {
        participantId = newParticipant;
        condition = newCondition;
        attempt = newAttempt;

        Collisions = 0;
        LimitWarnings = 0;
        CycleTime = 0f;
        PointsTaught = 0;
        PathLengthDeg = 0f;
        TaskComplete = false;

        events.Clear();
        lastHit.Clear();
        lastLimit.Clear();
        sessionStart = Time.time;
        Log("session_start", participantId + " " + condition + " attempt " + attempt);
    }
}
