using UnityEngine;

/// <summary>
/// The rotating amber beacon on the cell post. It pulses rather than strobes,
/// because a hard flash is unpleasant in a headset.
/// </summary>
public class BeaconLight : MonoBehaviour
{
    public Renderer domeRenderer;
    public Light domeLight;
    public Color colour = new Color(1f, 0.45f, 0.05f);
    public float cyclesPerSecond = 0.8f;
    public float minIntensity = 0.15f;
    public float maxIntensity = 2.4f;
    [Tooltip("Degrees per second the dome spins, for the look of a rotating mirror.")]
    public float spin = 180f;

    private Material domeMat;
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        if (domeRenderer != null)
        {
            domeMat = domeRenderer.material;
            domeMat.EnableKeyword("_EMISSION");
        }
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * cyclesPerSecond * Mathf.PI * 2f) + 1f) * 0.5f;
        float k = Mathf.Lerp(minIntensity, maxIntensity, t * t);

        if (domeMat != null && domeMat.HasProperty(EmissionId))
            domeMat.SetColor(EmissionId, colour * k);

        if (domeLight != null) domeLight.intensity = k * 0.8f;

        if (spin != 0f) transform.Rotate(0f, spin * Time.deltaTime, 0f, Space.Self);
    }
}
