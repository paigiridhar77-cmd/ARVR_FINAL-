using UnityEngine;

/// <summary>
/// Drives the conveyor: scrolls the belt texture and spins the drum pulleys at
/// a matching surface speed. Purely cosmetic, but a conveyor that never moves
/// reads as a grey box, and a conveyor that moves reads as a machine.
///
/// Put this on the Conveyor root. The builder wires the fields automatically.
/// </summary>
public class ConveyorAnimator : MonoBehaviour
{
    [Tooltip("Belt surface speed in metres per second.")]
    public float speed = 0.22f;

    [Tooltip("Renderer whose base map is scrolled to fake the moving belt.")]
    public Renderer beltRenderer;

    [Tooltip("How many texture tiles fit along one metre of belt.")]
    public float tilesPerMetre = 8f;

    [Tooltip("Drum pulleys and support rollers, spun about their local X axis.")]
    public Transform[] rollers;

    [Tooltip("Radius of the rollers, used so the surface speed matches the belt.")]
    public float rollerRadius = 0.048f;

    [Tooltip("Turn the belt off to freeze the cell for a screenshot.")]
    public bool running = true;

    private Material beltMat;
    private float scroll;
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    void Start()
    {
        if (beltRenderer != null) beltMat = beltRenderer.material;  // instance, not the asset
    }

    void Update()
    {
        if (!running || speed <= 0f) return;

        float d = speed * Time.deltaTime;

        if (beltMat != null)
        {
            scroll += d * tilesPerMetre;
            if (scroll > 1f) scroll -= Mathf.Floor(scroll);
            var off = new Vector2(0f, -scroll);
            if (beltMat.HasProperty(BaseMapId)) beltMat.SetTextureOffset(BaseMapId, off);
            if (beltMat.HasProperty(MainTexId)) beltMat.SetTextureOffset(MainTexId, off);
        }

        if (rollers != null && rollerRadius > 0.0001f)
        {
            float deg = d / (2f * Mathf.PI * rollerRadius) * 360f;
            for (int i = 0; i < rollers.Length; i++)
                if (rollers[i] != null) rollers[i].Rotate(deg, 0f, 0f, Space.Self);
        }
    }
}
