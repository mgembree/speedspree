using UnityEngine;

/// <summary>
/// Renders the grapple rope (LineRenderer with sag) and a pulsing anchor marker sphere.
/// Auto-attached by GrappleAbility.Awake().
/// </summary>
public class GrappleVisuals : MonoBehaviour
{
    [Header("Rope")]
    [SerializeField] int   ropeSegments    = 18;
    [SerializeField] float ropeWidth       = 0.04f;
    [SerializeField] Color ropeColor       = new Color(0.9f, 0.85f, 0.4f);
    [SerializeField] float sagAmount       = 0.6f;
    [SerializeField] float ropeExtendSpeed = 60f;

    [Header("Anchor Marker")]
    [SerializeField] float markerSize  = 0.18f;
    [SerializeField] Color markerColor = new Color(1f, 0.4f, 0.1f);

    [Header("Rope Origin")]
    [Tooltip("Leave empty to auto-find 'GunTip' child, or falls back to camera position.")]
    [SerializeField] Transform ropeOriginOverride;

    GrappleAbility grapple;
    LineRenderer   lr;
    GameObject     anchorMarker;

    bool  ropeVisible;
    float ropeDrawProgress;
    Vector3 currentAnchor;

    void Awake()
    {
        grapple = GetComponent<GrappleAbility>();
        BuildLineRenderer();
        BuildAnchorMarker();
        Debug.Log("[GrappleVisuals] Awake complete");
    }

    void OnEnable()
    {
        if (grapple == null) grapple = GetComponent<GrappleAbility>();
        grapple.onGrappleAttach += OnAttach;
        grapple.onGrappleDetach += OnDetach;
        Debug.Log($"[GrappleVisuals] Subscribed to grapple events. lr={lr != null} marker={anchorMarker != null}");
    }

    void OnDisable()
    {
        if (grapple == null) return;
        grapple.onGrappleAttach -= OnAttach;
        grapple.onGrappleDetach -= OnDetach;
    }

    void Update()
    {
        if (!ropeVisible)
        {
            lr.enabled = false;
            if (anchorMarker != null) anchorMarker.SetActive(false);
            return;
        }

        float dist = Vector3.Distance(GetRopeOrigin(), grapple.AnchorPoint);
        ropeDrawProgress = Mathf.MoveTowards(
            ropeDrawProgress, 1f,
            ropeExtendSpeed * Time.deltaTime / Mathf.Max(dist, 0.1f));

        lr.enabled = true;
        if (anchorMarker != null) anchorMarker.SetActive(true);

        UpdateRope();
        UpdateMarker();
    }

    void OnAttach(Vector3 anchor)
    {
        currentAnchor    = anchor;
        ropeVisible      = true;
        ropeDrawProgress = 0f;
        Debug.Log($"[GrappleVisuals] OnAttach @ {anchor}");

        if (anchorMarker != null)
        {
            anchorMarker.transform.position = anchor;
            anchorMarker.SetActive(true);
        }
    }

    void OnDetach()
    {
        ropeVisible = false;
        Debug.Log("[GrappleVisuals] OnDetach — hiding rope");
    }

    void UpdateRope()
    {
        Vector3 origin      = GetRopeOrigin();
        Vector3 target      = grapple.AnchorPoint;
        Vector3 visibleTip  = Vector3.Lerp(origin, target, ropeDrawProgress);

        // Sag lessens as rope fully extends
        float sag = sagAmount * (1f - ropeDrawProgress * 0.7f);

        lr.positionCount = ropeSegments;
        for (int i = 0; i < ropeSegments; i++)
        {
            float   t   = i / (float)(ropeSegments - 1);
            Vector3 pos = Vector3.Lerp(origin, visibleTip, t);
            float   dip = 4f * t * (1f - t);   // peaks at midpoint
            pos += Vector3.down * sag * dip;
            lr.SetPosition(i, pos);
        }
    }

    void UpdateMarker()
    {
        if (anchorMarker == null) return;
        float pulse = 1f + 0.15f * Mathf.Sin(Time.time * 8f);
        anchorMarker.transform.localScale = Vector3.one * markerSize * pulse;
    }

    Vector3 GetRopeOrigin()
    {
        if (ropeOriginOverride != null) return ropeOriginOverride.position;

        var tip = transform.Find("CameraTarget/ArmsRig/ArmsCamera/GunTip")
               ?? transform.Find("GunTip");
        if (tip != null) return tip.position;

        if (Camera.main != null) return Camera.main.transform.position;
        return transform.position + Vector3.up * 1.5f;
    }

    void BuildLineRenderer()
    {
        var go = new GameObject("GrappleRope");
        go.transform.SetParent(transform);
        lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace     = true;
        lr.positionCount     = ropeSegments;
        lr.startWidth        = ropeWidth;
        lr.endWidth          = ropeWidth * 0.5f;
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
            Debug.Log($"[GrappleVisuals] Rope shader: {sh.name}");
        }
        else
        {
            Debug.LogWarning("[GrappleVisuals] No suitable shader found for rope!");
        }

        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(ropeColor, 0f),
                new GradientColorKey(ropeColor * 0.6f, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f,  0f),
                new GradientAlphaKey(0.8f, 1f)
            });
        lr.colorGradient = grad;
    }

    void BuildAnchorMarker()
    {
        anchorMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        anchorMarker.name = "GrappleAnchorMarker";
        anchorMarker.transform.localScale = Vector3.one * markerSize;
        Destroy(anchorMarker.GetComponent<Collider>());

        Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                 ?? Shader.Find("Unlit/Color")
                 ?? Shader.Find("Sprites/Default");

        if (sh != null)
        {
            var mat = new Material(sh);
            mat.color = markerColor;
            anchorMarker.GetComponent<Renderer>().material = mat;
        }

        anchorMarker.SetActive(false);
        Debug.Log("[GrappleVisuals] Anchor marker built");
    }
}
