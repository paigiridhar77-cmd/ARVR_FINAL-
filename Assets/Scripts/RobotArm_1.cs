using UnityEngine;

/// <summary>
/// Owns the ordered list of joints and exposes the arm as a "pose",
/// which is just an array of six angles. Everything else in the project
/// talks to the arm through poses.
/// </summary>
public class RobotArm : MonoBehaviour
{
    [Tooltip("Order matters: J1 first, J6 last.")]
    public RobotJoint[] joints;

    public int JointCount { get { return joints.Length; } }

    /// <summary>Current angles of every joint.</summary>
    public float[] GetPose()
    {
        float[] pose = new float[joints.Length];
        for (int i = 0; i < joints.Length; i++)
        {
            pose[i] = joints[i].Angle;
        }
        return pose;
    }

    /// <summary>Snap the arm to a pose. Used during playback.</summary>
    public void SetPose(float[] pose)
    {
        int n = Mathf.Min(pose.Length, joints.Length);
        for (int i = 0; i < n; i++)
        {
            joints[i].SetAngle(pose[i]);
        }
    }

    public void JogJoint(int index, float direction, float speedScale)
    {
        if (index < 0 || index >= joints.Length) return;
        joints[index].Jog(direction, speedScale, Time.deltaTime);
    }

    /// <summary>Name of a joint, for logging and for the pendant screen.</summary>
    public string JointName(int index)
    {
        if (index < 0 || index >= joints.Length) return "?";
        return joints[index].jointName;
    }

    /// <summary>
    /// How long a coordinated joint move to the target pose should take.
    /// Every joint starts and stops together and the slowest one sets the
    /// duration, which is how joint motion works on a real controller.
    /// </summary>
    public float MoveDuration(float[] target, float speedScale)
    {
        float longest = 0f;
        int n = Mathf.Min(target.Length, joints.Length);
        for (int i = 0; i < n; i++)
        {
            float delta = Mathf.Abs(target[i] - joints[i].Angle);
            float speed = Mathf.Max(1f, joints[i].maxSpeed * speedScale);
            longest = Mathf.Max(longest, delta / speed);
        }
        return longest;
    }

    /// <summary>Sum of joint travel in degrees. Used as a crude path length score.</summary>
    public float TravelTo(float[] target)
    {
        float sum = 0f;
        int n = Mathf.Min(target.Length, joints.Length);
        for (int i = 0; i < n; i++)
        {
            sum += Mathf.Abs(target[i] - joints[i].Angle);
        }
        return sum;
    }

    /// <summary>Reports any joint that was driven into its limit this frame.</summary>
    public string LimitedJointThisFrame()
    {
        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i].HitLimitThisFrame) return joints[i].jointName;
        }
        return null;
    }
}
