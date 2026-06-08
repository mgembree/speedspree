using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Explosive Boots — scans for nearest surface and launches the player away from it.
/// Floor: ~45° angled launch in camera-facing direction.
/// Wall:  perpendicular blast straight off the wall.
/// Bind equipmentSlot: 0 = Q, 1 = E.
/// </summary>
[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(PlayerMovementController))]
public class ExplosiveBootsAbility : MonoBehaviour
{
    [Header("Launch Force")]
    [SerializeField] float launchForce = 26f;

    [Header("Direction Blending")]
    [Tooltip("How much current velocity steers the blast on a floor bounce. 0 = pure up, 1 = fully along movement.")]
    [SerializeField, Range(0f, 1f)] float floorMoveWeight = 0.4f;
    [Tooltip("How much current velocity steers a wall blast. 0 = pure normal, 1 = fully along movement.")]
    [SerializeField, Range(0f, 1f)] float wallMoveWeight  = 0.3f;

    [Header("Charges")]
    [SerializeField] int maxCharges = 2;
    [SerializeField] float chargeRechargeTime = 3f;

    [Header("Surface Detection")]
    [SerializeField] float detectRadius = 1.8f;
    [SerializeField] LayerMask surfaceMask = ~0;

    [Header("Input")]
    [SerializeField] int equipmentSlot = 1;

    // Events
    public System.Action onBlast;
    public System.Action<int> onChargesChanged;

    // State
    PlayerPhysics physics;
    PlayerMovementController movement;
    int charges;
    float rechargeTimer;

    static readonly Vector3[] ScanDirs = new Vector3[]
    {
        Vector3.down,
        Vector3.up,
        Vector3.forward, Vector3.back,
        Vector3.left,    Vector3.right,
        new Vector3( 1, -1,  0).normalized,
        new Vector3(-1, -1,  0).normalized,
        new Vector3( 0, -1,  1).normalized,
        new Vector3( 0, -1, -1).normalized,
    };

    void Awake()
    {
        physics  = GetComponent<PlayerPhysics>();
        movement = GetComponent<PlayerMovementController>();
    }

    void Start()
    {
        charges = maxCharges;
        Debug.Log($"[ExplosiveBoots] Ready | slot={equipmentSlot} charges={charges}");
    }

    void Update()
    {
        if (charges < maxCharges)
        {
            rechargeTimer -= Time.deltaTime;
            if (rechargeTimer <= 0f)
            {
                charges++;
                rechargeTimer = chargeRechargeTime;
                onChargesChanged?.Invoke(charges);
                Debug.Log($"[ExplosiveBoots] Charge restored | charges={charges}");
            }
        }

        bool pressed = equipmentSlot == 0
            ? Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame
            : Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        if (pressed)
        {
            Debug.Log($"[ExplosiveBoots] Key pressed | charges={charges}");
            if (charges > 0) Blast();
        }
    }

    void Blast()
    {
        Vector3 launchDir = FindLaunchDirection();

        if (physics.Velocity.y < 0f)
            physics.SetVerticalVelocity(0f);

        physics.AddImpulse(launchDir * launchForce);

        charges--;
        if (charges == maxCharges - 1)
            rechargeTimer = chargeRechargeTime;

        onChargesChanged?.Invoke(charges);
        onBlast?.Invoke();

        Debug.Log($"[ExplosiveBoots] BLAST | dir={launchDir:F2} charges left={charges}");
    }

    Vector3 FindLaunchDirection()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        float closestDist = float.MaxValue;
        Vector3 bestNormal = Vector3.up;

        foreach (var dir in ScanDirs)
        {
            if (Physics.Raycast(origin, dir, out RaycastHit hit, detectRadius, surfaceMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance < closestDist)
                {
                    closestDist = hit.distance;
                    bestNormal  = hit.normal;
                }
            }
        }

        // Prefer current horizontal velocity as the movement steer so the blast
        // adds onto where the player is already going rather than overriding it.
        // Fall back to camera-facing when the player is stationary.
        Vector3 currentHoriz = new Vector3(physics.Velocity.x, 0f, physics.Velocity.z);
        Vector3 moveDir = currentHoriz.magnitude > 0.5f
            ? currentHoriz.normalized
            : (Camera.main != null
                ? Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized
                : transform.forward);

        bool isFloor = bestNormal.y > 0.7f;

        if (isFloor)
            return (Vector3.up + moveDir * floorMoveWeight).normalized;
        else
            return (bestNormal + moveDir * wallMoveWeight).normalized;
    }

    public int Charges => charges;
    public int MaxCharges => maxCharges;
    public float RechargeProgress => charges < maxCharges
        ? 1f - Mathf.Clamp01(rechargeTimer / chargeRechargeTime)
        : 1f;
}
