using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ball and Chain — throw a heavy iron ball on a chain.
/// While the chain is taut the ball continuously drags the player toward it.
/// Press Q/E (slot 0/1) to throw, press again to recall.
/// </summary>
[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(PlayerMovementController))]
public class BallAndChainAbility : MonoBehaviour
{
    [Header("Chain")]
    [Tooltip("Max rope length before pull engages.")]
    [SerializeField] float chainLength = 8f;
    [Tooltip("Continuous force applied to the player toward the ball when the chain is taut.")]
    [SerializeField] float pullForce   = 45f;
    [Tooltip("Force multiplier when grounded to overcome ground friction.")]
    [SerializeField] float groundedPullMultiplier = 3.5f;

    [Header("Ball")]
    [Tooltip("Mass of the ball relative to the player Rigidbody.")]
    [SerializeField] float ballMass        = 8f;
    [SerializeField] float ballRadius      = 0.28f;
    [SerializeField] float throwForce      = 32f;
    [Tooltip("Bounciness of the ball PhysicsMaterial (0 = no bounce, 1 = full bounce).")]
    [SerializeField, Range(0f, 1f)] float bounciness = 0.55f;
    [Tooltip("Angular drag on the ball so it settles after bouncing.")]
    [SerializeField] float ballAngularDrag = 3f;

    [Header("Drag Speed")]
    [Tooltip("Horizontal speed cap raised while the chain is active so pull momentum isn't clipped.")]
    [SerializeField] float chainSpeedCap = 42f;

    [Header("Cooldown")]
    [SerializeField] float cooldown = 0.6f;

    [Header("Input")]
    [SerializeField] int equipmentSlot = 1;

    // Events
    public System.Action onThrow;
    public System.Action onRecall;
    public System.Action onYank;   // fires the first time the chain goes taut

    PlayerPhysics            physics;
    PlayerMovementController movement;

    enum State { Idle, Active }
    State state = State.Idle;

    GameObject ballObj;
    Rigidbody  ballRb;
    float      cooldownTimer;

    bool  chainTaut;

    void Awake()
    {
        physics  = GetComponent<PlayerPhysics>();
        movement = GetComponent<PlayerMovementController>();

        if (GetComponent<BallAndChainVisuals>() == null)
            gameObject.AddComponent<BallAndChainVisuals>();
    }

    void Start()
    {
        Debug.Log($"[BallChain] Ready | slot={equipmentSlot} (0=Q, 1=E) chainLen={chainLength}");
    }

    void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        bool keyDown = equipmentSlot == 0
            ? Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame
            : Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        switch (state)
        {
            case State.Idle:
                if (keyDown && cooldownTimer <= 0f)
                    Throw();
                break;

            case State.Active:
                if (keyDown)
                    Recall();
                break;
        }
    }

    void FixedUpdate()
    {
        if (state != State.Active || ballRb == null) return;

        Vector3 towardBall = ballRb.position - transform.position;
        float   dist       = towardBall.magnitude;

        if (dist > chainLength)
        {
            // Chain is taut — drag player continuously toward the ball
            // Apply stronger force when grounded to overcome friction
            float force = pullForce;
            if (movement.IsGrounded)
                force *= groundedPullMultiplier;

            physics.AddForce(towardBall.normalized * force);

            if (!chainTaut)
            {
                chainTaut = true;
                onYank?.Invoke();
            }
        }
        else
        {
            chainTaut = false;
        }
    }

    // ── Throw ──────────────────────────────────────────────────────────────

    void Throw()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        ballObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballObj.name = "BallAndChain_Ball";
        ballObj.transform.localScale = Vector3.one * (ballRadius * 2f);
        ballObj.transform.position   = cam.transform.position + cam.transform.forward * 0.8f;

        var renderer = ballObj.GetComponent<Renderer>();
        if (renderer != null) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        ballRb = ballObj.AddComponent<Rigidbody>();
        ballRb.mass                   = ballMass;
        ballRb.angularDamping         = ballAngularDrag;
        ballRb.interpolation          = RigidbodyInterpolation.Interpolate;
        ballRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var physMat = new PhysicsMaterial("BallChain_Bounce");
        physMat.bounciness      = bounciness;
        physMat.dynamicFriction = 0.3f;
        physMat.staticFriction  = 0.3f;
        physMat.bounceCombine   = PhysicsMaterialCombine.Maximum;
        physMat.frictionCombine = PhysicsMaterialCombine.Average;
        ballObj.GetComponent<Collider>().material = physMat;

        var playerColliders = GetComponentsInChildren<Collider>();
        var ballCol = ballObj.GetComponent<Collider>();
        foreach (var c in playerColliders)
            Physics.IgnoreCollision(ballCol, c, true);

        ballRb.linearVelocity = physics.Velocity;
        ballRb.AddForce(cam.transform.forward * throwForce, ForceMode.Impulse);

        physics.SetMaxHorizontalSpeedOverride(chainSpeedCap);

        state     = State.Active;
        chainTaut = false;

        onThrow?.Invoke();
        Debug.Log($"[BallChain] Thrown | force={throwForce} chainLen={chainLength}");
    }

    // ── Recall ─────────────────────────────────────────────────────────────

    void Recall()
    {
        CleanUp();
        cooldownTimer = cooldown;
        onRecall?.Invoke();
        Debug.Log("[BallChain] Recalled");
    }

    void CleanUp()
    {
        if (ballObj != null) { Destroy(ballObj); ballObj = null; ballRb = null; }

        physics.ClearMaxHorizontalSpeedOverride();
        state = State.Idle;
    }

    void OnDestroy() => CleanUp();

    // ── Public Getters ─────────────────────────────────────────────────────

    public bool       IsActive           => state == State.Active;
    public GameObject BallObject         => ballObj;
    public Vector3    BallPosition       => ballObj != null ? ballObj.transform.position : transform.position;
    public float      CooldownNormalized => Mathf.Clamp01(cooldownTimer / cooldown);
}
