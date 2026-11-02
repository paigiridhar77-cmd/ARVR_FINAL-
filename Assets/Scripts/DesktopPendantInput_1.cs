using UnityEngine;

/// <summary>
/// Keyboard driver for the pendant, so the whole trainer can be built and
/// tested with no headset attached.
///
/// Keys:
///   1 to 6        select joint J1 to J6
///   Up / Down     jog the selected joint (only while Space is held)
///   Space         deadman switch, must be held for the arm to move
///   [ and ]       speed override down and up
///   T             teach a point (move only)
///   C             teach a point with gripper CLOSE
///   O             teach a point with gripper OPEN
///   Backspace     delete last taught point
///   P             play the program
///   X             stop
///   K  or  F5     save the session to CSV
///
/// K is the one to use on a Mac. macOS takes the function keys for screen
/// brightness and so on, so a bare F5 never reaches Unity unless you hold Fn.
/// K does the same thing with no modifier.
///
/// You can also save without any key at all: in play mode, right click the
/// "Session Logger (Script)" header in the Inspector and choose
/// "Save session now".
///
/// This uses the old Input class, so Edit > Project Settings > Player >
/// Active Input Handling must be set to "Both".
/// </summary>
public class DesktopPendantInput : MonoBehaviour
{
    public TeachPendant pendant;

    void Start()
    {
        if (pendant == null) pendant = FindFirstObjectByType<TeachPendant>();
    }

    void Update()
    {
        if (pendant == null) return;

        for (int i = 0; i < 6; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) pendant.SelectJoint(i);
        }

        pendant.deadmanHeld = Input.GetKey(KeyCode.Space);

        float jog = 0f;
        if (Input.GetKey(KeyCode.UpArrow)) jog += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) jog -= 1f;
        pendant.jogInput = jog;

        if (Input.GetKeyDown(KeyCode.LeftBracket)) pendant.SetSpeed(pendant.speedOverride - 0.05f);
        if (Input.GetKeyDown(KeyCode.RightBracket)) pendant.SetSpeed(pendant.speedOverride + 0.05f);

        if (Input.GetKeyDown(KeyCode.T)) pendant.TeachPoint(GripperAction.None);
        if (Input.GetKeyDown(KeyCode.C)) pendant.TeachPoint(GripperAction.Close);
        if (Input.GetKeyDown(KeyCode.O)) pendant.TeachPoint(GripperAction.Open);
        if (Input.GetKeyDown(KeyCode.Backspace)) pendant.DeleteLastPoint();

        if (Input.GetKeyDown(KeyCode.P)) pendant.Play();
        if (Input.GetKeyDown(KeyCode.X)) pendant.Stop();

        // K is the reliable save key on macOS. F5 kept for Windows habit.
        if (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.F5))
        {
            if (pendant.logger != null)
            {
                pendant.logger.SaveSession();
            }
            else
            {
                Debug.LogWarning("No SessionLogger wired into the TeachPendant Logger slot.");
            }
        }
    }
}
