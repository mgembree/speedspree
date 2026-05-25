using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Directional dash equipment ability.
/// Bind to Q or E via the Equipment slot system.
/// Air dashes are limited and refill on landing.
/// </summary>
[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(PlayerMovementController))]
public class DashAbility : MonoBehaviour
{
    [Header("Dash Feel")]
    [SerializeField] float dashSpeed = 45f;
    [SerializeField] float dashDuration = 0.18f;
    [SerializeField] float groundCooldown = 0.4f;

    [Header("Air Dashes")]
    [SerializeField] int maxAirDashes = 2;

    [Header("Input")]
    [Tooltip("Which equipment slot this is bound to: 0 = Q, 1 = E")]
    [SerializeField] int equipmentSlot = 0;

    // Events — subscribe for VFX, audio, camera effects
    public System.Action onDashStart;
    public System.Action onDashEnd;

    // State
    PlayerPhysics physics;
    PlayerMovementController movement;
    InputSystem_Actions inputActions;
    Transform camTransform;

    bool isDashing;
    float dashTimer;
    float cooldownTimer;
    int airDashesUsed;
    Vector3 dashDir;

    void Awake()
    {
        physics = GetComponent<PlayerPhysics>();
        movement = GetComponent<PlayerMovementController>();
        inputActions = new InputSystem_Actions();
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

void Start()
    {
        if (Camera.main != null)
            camTransform = Camera.main.transform;
        Debug.Log("[Dash] Ready — slot " + equipmentSlot + " (0=Q, 1=E)");
    }

void OnDestroy()
    {
        inputActions.Dispose();
    }

void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // Poll keyboard directly — reliable regardless of InputActions asset state
        bool pressed = equipmentSlot == 0
            ? Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame
            : Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        if (pressed)
        {
            Debug.Log($"[Dash] Key pressed. CanDash={CanDash()} grounded={movement.IsGrounded} airUsed={airDashesUsed}/{maxAirDashes} cd={cooldownTimer:F2}");
            if (CanDash()) StartDash();
        }

        if (!isDashing) return;
        dashTimer -= Time.deltaTime;
        if (dashTimer <= 0f) EndDash();
    }

    void FixedUpdate()
    {
        if (!isDashing) return;
        // Lock velocity to dash direction for the duration
        physics.SetHorizontalVelocity(dashDir * dashSpeed);
        physics.SetVerticalVelocity(0f);   // level dash, no gravity pull
    }

    void LateUpdate()
    {
        // Detect landing to refill air dashes
        if (movement.IsGrounded)
            airDashesUsed = 0;
    }

    // ── Activation ─────────────────────────────────────────────────────────

void OnActivate(InputAction.CallbackContext ctx) { }

    bool CanDash()
    {
        if (isDashing) return false;
        if (cooldownTimer > 0f) return false;
        if (!movement.IsGrounded && airDashesUsed >= maxAirDashes) return false;
        return true;
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        cooldownTimer = groundCooldown;

        if (!movement.IsGrounded)
            airDashesUsed++;

        dashDir = GetDashDirection();

        // Interrupt slide if dashing out of one
        onDashStart?.Invoke();
    }

    void EndDash()
    {
        isDashing = false;

        // Preserve horizontal momentum — bleed into movement naturally
        physics.SetHorizontalVelocity(dashDir * dashSpeed * 0.6f);

        onDashEnd?.Invoke();
    }

    // ── Direction ──────────────────────────────────────────────────────────

    Vector3 GetDashDirection()
    {
        Vector2 moveInput = movement.MoveInput;

        // If no input, dash forward relative to camera
        if (moveInput.magnitude < 0.1f)
        {
            return camTransform != null
                ? Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized
                : transform.forward;
        }

        Vector3 forward = camTransform != null
            ? Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized
            : transform.forward;
        Vector3 right = camTransform != null
            ? Vector3.ProjectOnPlane(camTransform.right, Vector3.up).normalized
            : transform.right;

        return (forward * moveInput.y + right * moveInput.x).normalized;
    }

    // ── Public Getters ─────────────────────────────────────────────────────

    public bool IsDashing => isDashing;
    public int AirDashesRemaining => maxAirDashes - airDashesUsed;
    public float CooldownNormalized => Mathf.Clamp01(cooldownTimer / groundCooldown);
}
