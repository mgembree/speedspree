using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Wall Run — tap Q or E while airborne near a vertical wall to latch on.
/// Ends on jump, backward input, or losing wall contact.
/// Press Jump (Space) while wall running to launch off the wall.
/// Bind equipmentSlot: 0 = Q, 1 = E.
/// </summary>
[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(PlayerMovementController))]
public class WallRunAbility : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] float wallCheckDistance = 0.85f;
    [SerializeField] LayerMask wallMask = ~0;

    [Header("Wall Run Feel")]
    [SerializeField] float maxDuration      = 1.8f;
    [SerializeField] float cooldownTime     = 0.6f;
    [Tooltip("1 = zero gravity (float level), higher = gentle downward drift.")]
    [SerializeField] float wallGravityScale = 1.1f;
    [Tooltip("Force applied toward the wall each FixedUpdate to keep the player hugging it.")]
    [SerializeField] float wallHugForce     = 20f;
    [Tooltip("Horizontal speed cap while wall running.")]
    [SerializeField] float wallRunSpeedCap  = 30f;

    [Header("Wall Jump")]
    [SerializeField] float wallJumpSide    = 9f;
    [SerializeField] float wallJumpUp      = 10f;
    [SerializeField] float wallJumpForward = 7f;

    [Header("Input")]
    [SerializeField] int equipmentSlot = 1;

    // Events — subscribe for VFX, camera effects
    public System.Action<Vector3> onWallRunStart;
    public System.Action          onWallRunEnd;

    PlayerPhysics            physics;
    PlayerMovementController movement;

    bool    isWallRunning;
    Vector3 wallNormal;
    float   wallRunTimer;
    float   cooldownTimer;
    float   defaultGravityScale;
    bool    wallJumpUsed;

    void Awake()
    {
        physics  = GetComponent<PlayerPhysics>();
        movement = GetComponent<PlayerMovementController>();
    }

    void Start()
    {
        defaultGravityScale = physics.GravityScale;
        Debug.Log($"[WallRun] Ready | slot={equipmentSlot} (0=Q, 1=E)");
    }

    void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        bool keyPressed = equipmentSlot == 0
            ? Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame
            : Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        if (movement.IsGrounded)
        {
            if (isWallRunning) EndWallRun();
            cooldownTimer = 0f;
            wallJumpUsed  = false;
            return;
        }

        if (isWallRunning)
        {
            wallRunTimer -= Time.deltaTime;

            bool jumpPressed    = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool movingBackward = movement.MoveInput.y < -0.1f;

            if (jumpPressed)
            {
                if (!wallJumpUsed) DoWallJump();
                else               EndWallRun();
                return;
            }

            if (wallRunTimer <= 0f || movingBackward || !DetectWall(out Vector3 updatedNormal))
            {
                EndWallRun();
                return;
            }

            wallNormal = updatedNormal;
        }
        else
        {
            if (keyPressed && cooldownTimer <= 0f && DetectWall(out Vector3 normal))
                StartWallRun(normal);
        }
    }

    void FixedUpdate()
    {
        if (!isWallRunning) return;

        physics.GravityScale = wallGravityScale;
        physics.AddForce(-wallNormal * wallHugForce);
    }

    // ── Wall Detection ─────────────────────────────────────────────────────

    bool DetectWall(out Vector3 normal)
    {
        normal = Vector3.zero;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        Vector3[] dirs =
        {
            -transform.right,
             transform.right,
            (-transform.right + transform.forward).normalized,
            ( transform.right + transform.forward).normalized,
        };

        float bestDist = float.MaxValue;
        bool  found    = false;

        foreach (var dir in dirs)
        {
            if (Physics.Raycast(origin, dir, out RaycastHit hit, wallCheckDistance, wallMask, QueryTriggerInteraction.Ignore))
            {
                if (Mathf.Abs(hit.normal.y) < 0.35f && hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    normal   = hit.normal;
                    found    = true;
                }
            }
        }

        return found;
    }

    // ── Activate ───────────────────────────────────────────────────────────

    void StartWallRun(Vector3 normal)
    {
        isWallRunning = true;
        wallNormal    = normal;
        wallRunTimer  = maxDuration;
        wallJumpUsed  = false;

        if (physics.Velocity.y < 0f) physics.SetVerticalVelocity(0f);

        physics.SetMaxHorizontalSpeedOverride(wallRunSpeedCap);
        onWallRunStart?.Invoke(wallNormal);
        Debug.Log($"[WallRun] Start | normal={wallNormal:F2}");
    }

    void EndWallRun()
    {
        if (!isWallRunning) return;
        isWallRunning = false;
        cooldownTimer = cooldownTime;
        physics.GravityScale = defaultGravityScale;
        physics.ClearMaxHorizontalSpeedOverride();
        onWallRunEnd?.Invoke();
        Debug.Log("[WallRun] End");
    }

    void DoWallJump()
    {
        wallJumpUsed = true;

        Camera cam = Camera.main;
        Vector3 fwd = cam != null
            ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized
            : transform.forward;

        physics.SetVerticalVelocity(0f);
        physics.AddImpulse(wallNormal * wallJumpSide + Vector3.up * wallJumpUp + fwd * wallJumpForward);

        EndWallRun();
        Debug.Log("[WallRun] Wall jump!");
    }

    // ── Public Getters ─────────────────────────────────────────────────────

    public bool    IsWallRunning  => isWallRunning;
    public Vector3 WallNormal     => wallNormal;
    public float   TimerRemaining => wallRunTimer;
    public float   CooldownNormalized => Mathf.Clamp01(cooldownTimer / cooldownTime);
}
