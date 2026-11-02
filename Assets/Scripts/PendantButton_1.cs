using UnityEngine;

/// <summary>
/// One physical button on the VR pendant. Add this to a small cube on the
/// pendant model, then add an XRSimpleInteractable next to it and wire its
/// events in the Inspector:
///
///   Select Entered  -> PendantButton.OnPressed
///   Select Exited   -> PendantButton.OnReleased
///
/// Jog buttons need press AND release because jogging is continuous while
/// held. The others only need press.
///
/// Doing it this way keeps all the VR specific wiring in one small script,
/// so nothing in TeachPendant has to know whether the input came from a
/// keyboard or a controller.
/// </summary>
public class PendantButton : MonoBehaviour
{
    public enum Function
    {
        JogPositive,
        JogNegative,
        NextJoint,
        SelectJoint,
        SpeedUp,
        SpeedDown,
        Deadman,
        TeachMove,
        TeachClose,
        TeachOpen,
        DeleteLast,
        Play,
        Stop,
        SaveSession
    }

    public TeachPendant pendant;
    public Function function = Function.JogPositive;

    [Tooltip("Only used when function is SelectJoint. 0 is J1.")]
    public int jointIndex = 0;

    [Header("Optional feedback")]
    public Transform capTransform;
    public float pressDepth = 0.004f;

    private Vector3 capRest;

    void Start()
    {
        if (pendant == null) pendant = FindFirstObjectByType<TeachPendant>();
        if (capTransform != null) capRest = capTransform.localPosition;
    }

    public void OnPressed()
    {
        if (pendant == null) return;

        if (capTransform != null)
        {
            capTransform.localPosition = capRest - new Vector3(0f, pressDepth, 0f);
        }

        switch (function)
        {
            case Function.JogPositive: pendant.jogInput = 1f; break;
            case Function.JogNegative: pendant.jogInput = -1f; break;
            case Function.NextJoint: pendant.NextJoint(); break;
            case Function.SelectJoint: pendant.SelectJoint(jointIndex); break;
            case Function.SpeedUp: pendant.SetSpeed(pendant.speedOverride + 0.05f); break;
            case Function.SpeedDown: pendant.SetSpeed(pendant.speedOverride - 0.05f); break;
            case Function.Deadman: pendant.deadmanHeld = true; break;
            case Function.TeachMove: pendant.TeachPoint(GripperAction.None); break;
            case Function.TeachClose: pendant.TeachPoint(GripperAction.Close); break;
            case Function.TeachOpen: pendant.TeachPoint(GripperAction.Open); break;
            case Function.DeleteLast: pendant.DeleteLastPoint(); break;
            case Function.Play: pendant.Play(); break;
            case Function.Stop: pendant.Stop(); break;
            case Function.SaveSession:
                if (pendant.logger != null) pendant.logger.SaveSession();
                break;
        }
    }

    public void OnReleased()
    {
        if (pendant == null) return;

        if (capTransform != null) capTransform.localPosition = capRest;

        if (function == Function.JogPositive || function == Function.JogNegative)
        {
            pendant.jogInput = 0f;
        }
        else if (function == Function.Deadman)
        {
            pendant.deadmanHeld = false;
        }
    }
}
