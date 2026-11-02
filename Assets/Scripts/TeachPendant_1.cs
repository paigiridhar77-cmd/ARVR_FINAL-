using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GripperAction { None, Close, Open }

[System.Serializable]
public class ProgramPoint
{
    public float[] angles;
    public GripperAction action = GripperAction.None;
    public float speedScale = 0.25f;
}

/// <summary>
/// The brain of the trainer. Holds the taught program, does the jogging,
/// and plays the program back. Input sources (keyboard on desktop, VR
/// buttons in the headset) just set the public fields and call the public
/// methods on this component. They never touch the arm directly.
/// </summary>
public class TeachPendant : MonoBehaviour
{
    [Header("Wiring")]
    public RobotArm arm;
    public Gripper gripper;
    public SessionLogger logger;

    [Header("State set by whatever input you are using")]
    [Tooltip("0 to 5 for J1 to J6.")]
    public int selectedJoint = 0;

    [Tooltip("-1, 0 or +1. Held while a jog key or button is down.")]
    public float jogInput = 0f;

    [Tooltip("The arm will not move unless this is true.")]
    public bool deadmanHeld = false;

    [Range(0.05f, 1f)] public float speedOverride = 0.25f;

    [Header("Program")]
    public List<ProgramPoint> program = new List<ProgramPoint>();

    public bool IsPlaying { get; private set; }
    public string StatusLine { get; private set; }

    private float lastCycleTime = 0f;

    void Update()
    {
        if (IsPlaying)
        {
            StatusLine = "RUNNING  step " + currentStep + " of " + program.Count;
            return;
        }

        if (deadmanHeld && Mathf.Abs(jogInput) > 0.01f)
        {
            arm.JogJoint(selectedJoint, Mathf.Sign(jogInput), speedOverride);

            string limited = arm.LimitedJointThisFrame();
            if (limited != null && logger != null)
            {
                logger.ReportLimit(limited);
            }
        }

        StatusLine = string.Format("{0}  {1:F1} deg   spd {2:P0}   pts {3}   {4}",
            arm.JointName(selectedJoint),
            arm.GetPose()[selectedJoint],
            speedOverride,
            program.Count,
            deadmanHeld ? "ENABLED" : "DEADMAN OFF");
    }

    // ---------- pendant buttons ----------

    public void SelectJoint(int index)
    {
        if (IsPlaying) return;
        selectedJoint = Mathf.Clamp(index, 0, arm.JointCount - 1);
    }

    public void NextJoint()
    {
        SelectJoint((selectedJoint + 1) % arm.JointCount);
    }

    public void SetSpeed(float value)
    {
        speedOverride = Mathf.Clamp(value, 0.05f, 1f);
    }

    /// <summary>Store the current arm pose as a program point.</summary>
    public void TeachPoint(GripperAction action)
    {
        if (IsPlaying) return;

        ProgramPoint p = new ProgramPoint();
        p.angles = arm.GetPose();
        p.action = action;
        p.speedScale = speedOverride;
        program.Add(p);

        if (logger != null)
        {
            logger.PointsTaught = program.Count;
            logger.Log("teach_point", "P" + program.Count + " action=" + action);
        }
    }

    public void DeleteLastPoint()
    {
        if (IsPlaying || program.Count == 0) return;
        program.RemoveAt(program.Count - 1);
        if (logger != null)
        {
            logger.PointsTaught = program.Count;
            logger.Log("delete_point", "now " + program.Count);
        }
    }

    public void ClearProgram()
    {
        if (IsPlaying) return;
        program.Clear();
        if (logger != null) logger.PointsTaught = 0;
    }

    public void Play()
    {
        if (IsPlaying || program.Count == 0) return;
        StartCoroutine(RunProgram());
    }

    public void Stop()
    {
        StopAllCoroutines();
        IsPlaying = false;
        StatusLine = "STOPPED";
        if (logger != null) logger.Log("program_stopped", "");
    }

    private int currentStep = 0;

    private IEnumerator RunProgram()
    {
        IsPlaying = true;
        currentStep = 0;
        float startTime = Time.time;
        float totalTravel = 0f;

        if (logger != null) logger.Log("program_start", program.Count + " points");

        foreach (ProgramPoint point in program)
        {
            currentStep++;

            float[] start = arm.GetPose();
            float duration = arm.MoveDuration(point.angles, point.speedScale);
            totalTravel += arm.TravelTo(point.angles);

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);

                float[] pose = new float[start.Length];
                for (int i = 0; i < start.Length; i++)
                {
                    pose[i] = Mathf.Lerp(start[i], point.angles[i], u);
                }
                arm.SetPose(pose);
                yield return null;
            }
            arm.SetPose(point.angles);

            if (point.action == GripperAction.Close)
            {
                bool got = gripper != null && gripper.Close();
                if (logger != null) logger.Log("grip_close", got ? "picked" : "nothing in gripper");
                yield return new WaitForSeconds(0.4f);
            }
            else if (point.action == GripperAction.Open)
            {
                if (gripper != null) gripper.Open();
                if (logger != null) logger.Log("grip_open", "");
                yield return new WaitForSeconds(0.4f);
            }
        }

        lastCycleTime = Time.time - startTime;
        IsPlaying = false;
        StatusLine = string.Format("DONE  cycle {0:F2} s", lastCycleTime);

        if (logger != null)
        {
            logger.CycleTime = lastCycleTime;
            logger.PathLengthDeg = totalTravel;
            logger.Log("program_end", string.Format("cycle={0:F2}s travel={1:F0}deg", lastCycleTime, totalTravel));
        }
    }
}
