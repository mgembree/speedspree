using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles WASD movement, auto-sprint, manual sprint toggle, sliding, and jumping.
/// Reads from the project's InputSystem_Actions asset.
/// </summary>
[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovementController : MonoBehaviour
{
    // ── Tunables ───────────────────────────────────────────────────────────

    [Header("Walk / Run")]
    [SerializeField] float walkSpeed = 7f;
    [SerializeField] float sprintSpeed = 14f;
    [SerializeField] float acceleration = 25f;
    [SerializeField] float deceleration = 20f;

    [Header("Weight Feel")]
    [Tooltip("Lower values make direction changes feel heavier and less twitchy.")]
    [SerializeField, Range(0.1f, 1f)] float turnResponsiveness = 0.45f;
    [Tooltip("How much movement acceleration is allowed while airborne.")]
    [SerializeField, Range(0f, 1f)] float airControlMultiplier = 0.55f;
    [Tooltip("How strongly we brake when there is no movement input.")]
    [SerializeField, Range(0.1f, 1.2f)] float noInputBrakingMultiplier = 0.65f;

    [Header("Auto-Sprint")]
    [Tooltip("Seconds of continuous movement before auto-sprint kicks in.")]
    [SerializeField] float autoSprintDelay = 0.4f;

    [Header("Jump")]
    [SerializeField] float jumpForce = 9f;
    [SerializeField] int maxAirJumps = 0;
    [SerializeField] float coyoteTime = 0.12f;
    [SerializeField] float jumpBufferTime = 0.12f;

    [Header("Slide")]
    [SerializeField] float slideForce = 18f;
    [SerializeField] float slideDuration = 0.6f;
    [SerializeField] float slideCooldown = 0.8f;
    [SerializeField] float slideCapsuleHeight = 0.9f;

    [Header("Ground Detection")]
    [SerializeField] float groundCheckDistance = 0.12f;
    [SerializeField] LayerMask groundMask = ~0;

    // ── State ──────────────────────────────────────────────────────────────

    PlayerPhysics physics;
    CapsuleCollider capsule;
    InputSystem_Actions inputActions;
    Transform camTransform;

    Vector2 moveInput;
    bool isGrounded;
    bool isSprinting;
    bool isSliding;

    float defaultCapsuleHeight;
    float defaultCapsuleCenterY;
    float autoSprintTimer;
    float coyoteTimer;
    float jumpBufferTimer;
    float slideTimer;
    float slideCooldownTimer;
    int airJumpsUsed;
    bool slideQueued;  // pressed slide in air — fires on landing


    // Slide events — subscribed to by PlayerCameraEffects
    public System.Action onSlideStart;
    public System.Action onSlideEnd;
    public System.Action onJump;

    // ── Unity ──────────────────────────────────────────────────────────────

    void Awake()
    {
        physics = GetComponent<PlayerPhysics>();
        capsule = GetComponent<CapsuleCollider>();
        defaultCapsuleHeight = capsule.height;
        defaultCapsuleCenterY = capsule.center.y;

        inputActions = new InputSystem_Actions();
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    void Start()
    {
        // Use the main camera's yaw for directional movement
        if (Camera.main != null)
            camTransform = Camera.main.transform;

        // Subscribe to discrete input events
        inputActions.Player.Jump.performed += OnJumpPressed;
        inputActions.Player.Sprint.performed += OnSprintPressed;
        inputActions.Player.Sprint.canceled += OnSprintReleased;
        inputActions.Player.Crouch.performed += OnSlidePressed;   // LCtrl -> Slide
        inputActions.Player.Crouch.canceled += OnSlideReleased;
    }

    void OnDestroy()
    {
        inputActions.Player.Jump.performed -= OnJumpPressed;
        inputActions.Player.Sprint.performed -= OnSprintPressed;
        inputActions.Player.Sprint.canceled -= OnSprintReleased;
        inputActions.Player.Crouch.performed -= OnSlidePressed;
        inputActions.Player.Crouch.canceled -= OnSlideReleased;
        inputActions.Dispose();
    }

    void Update()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        TickGroundCheck();
        TickCoyoteTime();
        TickJumpBuffer();
        TickAutoSprint();
        TickSlide();
    }

    void FixedUpdate()
    {
        if (isSliding)
            physics.ApplyGroundFriction(isGrounded);   // let slide naturally bleed off
        else
            ApplyMovement();
    }

    // ── Ground Check ───────────────────────────────────────────────────────

void TickGroundCheck()
    {
        // Place sphere at bottom hemisphere center of capsule so it never starts inside ground
        Vector3 origin = transform.position + Vector3.up * (capsule.center.y - capsule.height * 0.5f + capsule.radius);
        bool wasGrounded = isGrounded;
        isGrounded = Physics.SphereCast(
            origin,
            capsule.radius * 0.9f,
            Vector3.down,
            out _,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
        if (isGrounded && !wasGrounded)
            OnLanded();
    }

void OnLanded()
    {
        airJumpsUsed = 0;

        // Fire queued slide on landing if W is still held and cooldown clear
        if (slideQueued && moveInput.y >= 0.1f && slideCooldownTimer <= 0f)
        {
            slideQueued = false;
            StartSlide();
        }
        else
        {
            slideQueued = false;
        }
    }

    // ── Movement ───────────────────────────────────────────────────────────

    void ApplyMovement()
    {
        Vector3 wishDir = GetWishDirection();
        float targetSpeed = (isSprinting && moveInput.magnitude > 0.1f) ? sprintSpeed : walkSpeed;

        Vector3 currentHorizontal = new Vector3(physics.Velocity.x, 0f, physics.Velocity.z);
        Vector3 targetVelocity = wishDir * targetSpeed;

        float accel = wishDir.magnitude < 0.1f ? deceleration : acceleration;

        if (wishDir.magnitude < 0.1f)
        {
            accel *= noInputBrakingMultiplier;
        }
        else if (currentHorizontal.sqrMagnitude > 0.01f)
        {
            // Reduce accel while turning, especially when reversing direction, to keep momentum weighty.
            float alignment = Mathf.InverseLerp(-1f, 1f, Vector3.Dot(currentHorizontal.normalized, wishDir));
            float turnFactor = Mathf.Lerp(turnResponsiveness, 1f, alignment);
            accel *= turnFactor;
        }

        if (!isGrounded)
            accel *= airControlMultiplier;

        // Heavier mass should reduce acceleration and braking responsiveness.
        accel /= Mathf.Max(0.1f, physics.MassMultiplier);

        Vector3 newHorizontal = Vector3.MoveTowards(currentHorizontal, targetVelocity, accel * Time.fixedDeltaTime);

        physics.SetHorizontalVelocity(newHorizontal);
    }

    Vector3 GetWishDirection()
    {
        if (moveInput.magnitude < 0.1f)
            return Vector3.zero;

        // Orient movement relative to camera yaw
        Vector3 forward = camTransform != null
            ? Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized
            : transform.forward;
        Vector3 right = camTransform != null
            ? Vector3.ProjectOnPlane(camTransform.right, Vector3.up).normalized
            : transform.right;

        return (forward * moveInput.y + right * moveInput.x).normalized;
    }

    // ── Sprint ─────────────────────────────────────────────────────────────

    void TickAutoSprint()
    {
        if (isSliding) return;

        if (moveInput.magnitude > 0.1f && isGrounded)
        {
            autoSprintTimer += Time.deltaTime;
            if (autoSprintTimer >= autoSprintDelay)
                isSprinting = true;
        }
        else
        {
            autoSprintTimer = 0f;
        }
    }

    void OnSprintPressed(InputAction.CallbackContext _) => isSprinting = true;
    void OnSprintReleased(InputAction.CallbackContext _)
    {
        isSprinting = false;
        autoSprintTimer = 0f;
    }

    // ── Jump ───────────────────────────────────────────────────────────────

    void TickCoyoteTime()
    {
        if (isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;
    }

    void TickJumpBuffer()
    {
        jumpBufferTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f && CanJump())
            ExecuteJump();
    }

    void OnJumpPressed(InputAction.CallbackContext _)
    {
        jumpBufferTimer = jumpBufferTime;

        if (CanJump())
            ExecuteJump();
    }

    bool CanJump()
    {
        if (coyoteTimer > 0f) return true;
        if (airJumpsUsed < maxAirJumps) return true;
        return false;
    }

    void ExecuteJump()
    {
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;

        if (!isGrounded) airJumpsUsed++;

        // Cancel downward velocity so jumps always feel snappy
        if (physics.Velocity.y < 0f)
            physics.SetVerticalVelocity(0f);

        physics.AddImpulse(Vector3.up * jumpForce);
        onJump?.Invoke();
    }

    // ── Slide ──────────────────────────────────────────────────────────────

void OnSlidePressed(InputAction.CallbackContext _)
    {
        if (isSliding) return;
        if (slideCooldownTimer > 0f) return;

        // Must be moving forward (W held)
        if (moveInput.y < 0.1f) return;

        if (isGrounded)
            StartSlide();
        else
            slideQueued = true;  // will fire the frame we land
    }

void OnSlideReleased(InputAction.CallbackContext _)
    {
        slideQueued = false;
        if (isSliding) EndSlide();
    }

void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;

        capsule.height = slideCapsuleHeight;
        capsule.center = new Vector3(0f, slideCapsuleHeight * 0.5f, 0f);

        // Use current velocity dir if grounded, camera forward if airborne
        Vector3 slideDir;
        if (isGrounded)
        {
            slideDir = new Vector3(physics.Velocity.x, 0f, physics.Velocity.z);
            if (slideDir.sqrMagnitude < 0.01f) slideDir = transform.forward;
            slideDir.Normalize();
            physics.SetHorizontalVelocity(slideDir * slideForce);
        }
        // If airborne, just crouch — velocity burst applies on landing via TickSlide

        onSlideStart?.Invoke();
    }

void TickSlide()
    {
        if (!isSliding) return;

        // If we just landed while sliding in air, apply the velocity burst now
        if (isGrounded && physics.HorizontalSpeed < slideForce * 0.5f)
        {
            Vector3 slideDir = new Vector3(physics.Velocity.x, 0f, physics.Velocity.z);
            if (slideDir.sqrMagnitude < 0.01f) slideDir = transform.forward;
            physics.SetHorizontalVelocity(slideDir.normalized * slideForce);
        }

        slideTimer -= Time.deltaTime;
        if (slideTimer <= 0f) EndSlide();
    }

void EndSlide()
    {
        isSliding = false;
        slideCooldownTimer = slideCooldown;
        capsule.height = defaultCapsuleHeight;
        capsule.center = new Vector3(0f, defaultCapsuleCenterY, 0f);
        StartCoroutine(SlideCooldownRoutine());
        onSlideEnd?.Invoke();
    }

    System.Collections.IEnumerator SlideCooldownRoutine()
    {
        while (slideCooldownTimer > 0f)
        {
            slideCooldownTimer -= Time.deltaTime;
            yield return null;
        }
        slideCooldownTimer = 0f;
    }

    // ── Public Getters (for UI, abilities, animations) ─────────────────────

    public bool IsGrounded => isGrounded;
    public bool IsSprinting => isSprinting;
    public bool IsSliding => isSliding;
    public Vector2 MoveInput => moveInput;
}
