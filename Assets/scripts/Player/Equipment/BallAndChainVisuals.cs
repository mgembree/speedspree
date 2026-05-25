using UnityEngine;

/// <summary>
/// Renders the chain rope (LineRenderer with sag) and styles the ball sphere.
/// Auto-attached by BallAndChainAbility.Awake().
/// </summary>
public class BallAndChainVisuals : MonoBehaviour
{
    [Header("Chain Rope")]
    [SerializeField] int   ropeSegments    = 14;
    [SerializeField] float ropeWidth       = 0.05f;
    [SerializeField] Color ropeColor       = new Color(0.65f, 0.65f, 0.65f);
    [SerializeField] float sagAmount       = 0.8f;

    [Header("Ball Appearance")]
    [SerializeField] Color ballColor       = new Color(0.2f, 0.2f, 0.2f);
    [SerializeField] float ballPulseSpeed  = 3f;
    [SerializeField] float ballPulseAmount = 0.06f;

    BallAndChainAbility ability;
    LineRenderer lr;
    Material     ballMat;

    void Awake()
    {
        ability = GetComponent<BallAndChainAbility>();
        BuildLineRenderer();
    }

    void OnEnable()
    {
        if (ability == null) ability = GetComponent<BallAndChainAbility>();
        ability.onThrow  += OnThrow;
        ability.onRecall += OnRecall;
    }

    void OnDisable()
    {
        if (ability == null) return;
        ability.onThrow  -= OnThrow;
        ability.onRecall -= OnRecall;
    }

    void Update()
    {
        if (!ability.IsActive)
        {
            lr.enabled = false;
            return;
        }

        lr.enabled = true;
        UpdateChain();
        UpdateBall();
    }

    void OnThrow()
    {
        // Style the ball once it's spawned
        StyleBall();
    }

    void OnRecall()
    {
        lr.enabled = false;
        ballMat    = null;
    }

    // ── Rope ───────────────────────────────────────────────────────────────

    void UpdateChain()
    {
        Vector3 origin = GetChainOrigin();
        Vector3 target = ability.BallPosition;

        lr.positionCount = ropeSegments;
        for (int i = 0; i < ropeSegments; i++)
        {
            float   t   = i / (float)(ropeSegments - 1);
            Vector3 pos = Vector3.Lerp(origin, target, t);

            // Catenary-style sag — droops most at midpoint, less when rope is taut
            float dist = Vector3.Distance(origin, target);
            float sag  = sagAmount * Mathf.Clamp01(1f - dist / 12f);
            float dip  = 4f * t * (1f - t);
            pos += Vector3.down * sag * dip;

            lr.SetPosition(i, pos);
        }
    }

    // ── Ball ───────────────────────────────────────────────────────────────

    void StyleBall()
    {
        if (ability.BallObject == null) return;

        var renderer = ability.BallObject.GetComponent<Renderer>();
        if (renderer == null) return;

        Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                 ?? Shader.Find("Unlit/Color")
                 ?? Shader.Find("Sprites/Default");

        if (sh != null)
        {
            ballMat = new Material(sh);
            ballMat.color = ballColor;
            renderer.material = ballMat;
        }
    }

    void UpdateBall()
    {
        if (ballMat == null || ability.BallObject == null) return;

        // Subtle pulse on the ball color brightness
        float pulse = 1f + ballPulseAmount * Mathf.Sin(Time.time * ballPulseSpeed);
        ballMat.color = ballColor * pulse;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    Vector3 GetChainOrigin()
    {
        // Try to get the GunTip as the visual throw origin
        var tip = transform.Find("CameraTarget/ArmsRig/ArmsCamera/GunTip")
               ?? transform.Find("GunTip");
        if (tip != null) return tip.position;

        if (Camera.main != null) return Camera.main.transform.position;
        return transform.position + Vector3.up;
    }

    void BuildLineRenderer()
    {
        var go = new GameObject("BallChainRope");
        go.transform.SetParent(transform);
        lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace     = true;
        lr.positionCount     = ropeSegments;
        lr.startWidth        = ropeWidth;
        lr.endWidth          = ropeWidth * 0.7f;
        lr.numCapVertices    = 4;
        lr.numCornerVertices = 4;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        lr.enabled           = false;

        Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                 ?? Shader.Find("Unlit/Color")
                 ?? Shader.Find("Sprites/Default");

        if (sh != null)
        {
            var mat = new Material(sh);
            mat.color = ropeColor;
            lr.material = mat;

            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(ropeColor, 0f),
                    new GradientColorKey(ropeColor * 0.5f, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f,  0f),
                    new GradientAlphaKey(0.9f, 1f)
                });
            lr.colorGradient = grad;
        }
    }
}
