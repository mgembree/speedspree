using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Grapple — fires a rope to an anchor point and lets the player swing.
/// Press Q/E to attach, press again to detach.
/// While attached, rope remains fixed length and only constrains swing motion.
/// </summary>
[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(PlayerMovementController))]
public class GrappleAbility : MonoBehaviour
{
    [Header("Range")]
    [SerializeField] float maxRange = 40f;
    [SerializeField] LayerMask grappleMask = ~0;

    [Header("Swing")]
    [SerializeField] float swingSpring    = 80f;
    [SerializeField] float swingDamper    = 2f;
    [SerializeField] float swingMassScale = 4.5f;
    [Tooltip("Continuous force toward the anchor while swinging — boosts swing speed like Spider-Man.")]
    [SerializeField] float swingPullForce = 30f;

    [Header("Momentum")]
    [Tooltip("Speed cap while grappling — lets swing momentum exceed normal movement cap.")]
    [SerializeField] float swingSpeedCap  = 40f;
    [Tooltip("Velocity multiplier applied on detach. 1 = no boost, 1.4 = 40% exit speed bonus.")]
    [SerializeField] float releaseBoost   = 1.35f;

    [Header("Cooldown")]
    [SerializeField] float cooldown = 0.4f;

    [Header("Input")]
    [SerializeField] int equipmentSlot = 0;

    public System.Action<Vector3> onGrappleAttach;
    public System.Action          onGrappleDetach;

    PlayerPhysics            physics;
    PlayerMovementController movement;

    enum GrappleState { Idle, Attached }
    GrappleState state = GrappleState.Idle;

    Vector3     anchorPoint;
    float       cooldownTimer;
    SpringJoint swingJoint;

    void Awake()
    {
        physics  = GetComponent<PlayerPhysics>();
        movement = GetComponent<PlayerMovementController>();

        if (GetComponent<GrappleVisuals>() == null)
            gameObject.AddComponent<GrappleVisuals>();
    }

    void Start()
    {
        Debug.Log($"[Grapple] Ready | slot={equipmentSlot} range={maxRange}");
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        bool keyDown = equipmentSlot == 0
            ? Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame
            : Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        switch (state)
        {
            case GrappleState.Idle:
                if (keyDown && cooldownTimer <= 0f)
                    TryFire();
                break;

            case GrappleState.Attached:
                if (keyDown)
                    Detach();
                break;
        }
    }

    void FixedUpdate()
    {
        if (state != GrappleState.Attached || swingJoint == null) return;

        Vector3 toAnchor = anchorPoint - transform.position;
        float   dist     = toAnchor.magnitude;

        // Rope shortens as player swings inward — never lengthens
        if (dist < swingJoint.maxDistance)
            swingJoint.maxDistance = dist;

        // Only assist when rope is taut — pulling toward anchor while slack fights the arc
        if (dist >= swingJoint.maxDistance * 0.95f)
            physics.AddForce(toAnchor.normalized * swingPullForce);
    }

    void TryFire()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRange, grappleMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("[Grapple] Miss — nothing in range");
            return;
        }

        anchorPoint = hit.point;
        Attach();
        Debug.Log($"[Grapple] Attached to {hit.collider.name} @ {anchorPoint:F1} dist={hit.distance:F1}");
    }

    void Attach()
    {
        state = GrappleState.Attached;

        swingJoint = gameObject.AddComponent<SpringJoint>();
        swingJoint.autoConfigureConnectedAnchor = false;
        swingJoint.connectedAnchor = anchorPoint;

        float dist = Vector3.Distance(transform.position, anchorPoint);
        swingJoint.maxDistance = dist;
        swingJoint.minDistance = 0f;
        swingJoint.spring      = swingSpring;
        swingJoint.damper      = swingDamper;
        swingJoint.tolerance   = 0.025f;
        swingJoint.massScale   = swingMassScale;
        swingJoint.enableCollision = false;

        physics.SetMaxHorizontalSpeedOverride(swingSpeedCap);

        onGrappleAttach?.Invoke(anchorPoint);
        Debug.Log($"[Grapple] Rope attached | ropeLen={dist:F1}");
    }

    void Detach()
    {
        if (swingJoint != null)
        {
            Destroy(swingJoint);
            swingJoint = null;
        }

        // Slingshot boost — rewards releasing at the right point in the arc
        if (releaseBoost > 1f)
            physics.AddImpulse(physics.Velocity * (releaseBoost - 1f));

        physics.ClearMaxHorizontalSpeedOverride();

        state         = GrappleState.Idle;
        cooldownTimer = cooldown;
        onGrappleDetach?.Invoke();
        Debug.Log("[Grapple] Detached");
    }

    public bool    IsActive           => state != GrappleState.Idle;
    public bool    IsAttached         => state == GrappleState.Attached;
    public Vector3 AnchorPoint        => anchorPoint;
    public float   CurrentRopeLength  => swingJoint != null ? swingJoint.maxDistance : 0f;
    public float   CooldownNormalized => Mathf.Clamp01(cooldownTimer / cooldown);
}
