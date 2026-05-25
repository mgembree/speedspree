using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ball and Chain — throw a heavy iron ball on a chain.
/// The ball bounces off surfaces; the chain yanks you toward its momentum.
/// Press Q/E (slot 0/1) to throw, press again to recall.
/// </summary>
[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(PlayerMovementController))]
public class BallAndChainAbility : MonoBehaviour
{
    [Header("Chain")]
    [Tooltip("Max rope length before the spring starts pulling the player.")]
    [SerializeField] float chainLength   = 8f;
    [SerializeField] float chainSpring   = 60f;
    [SerializeField] float chainDamper   = 2f;

    [Header("Ball")]
    [Tooltip("Mass of the ball relative to the player Rigidbody. Higher = more drag on the player.")]
    [SerializeField] float ballMass      = 8f;
    [SerializeField] float ballRadius    = 0.28f;
    [SerializeField] float throwForce    = 32f;
    [Tooltip("Bounciness of the ball PhysicsMaterial (0 = no bounce, 1 = full bounce).")]
    [SerializeField, Range(0f, 1f)] float bounciness = 0.55f;
    [Tooltip("Angular drag on the ball so it settles after bouncing.")]
    [SerializeField] float ballAngularDrag = 3f;

    [Header("Drag Speed")]
    [Tooltip("Horizontal speed cap raised while the chain is active, so swing momentum isn't clipped.")]
    [SerializeField] float chainSpeedCap = 42f;

    [Header("Cooldown")]
    [SerializeField] float cooldown = 0.6f;

    [Header("Input")]
    [SerializeField] int equipmentSlot = 1;

    // Events
    public System.Action onThrow;
    public System.Action onRecall;
    public System.Action onYank;   // fires when chain goes taut and impulse exceeds threshold

    PlayerPhysics            physics;
    PlayerMovementController movement;

    enum State { Idle, Active }
    State state = State.Idle;

    GameObject  ballObj;
    Rigidbody   ballRb;
    SpringJoint chain;
    float        cooldownTimer;

    // Yank detection — compare velocity frame-over-frame
    Vector3 prevVelocity;
    bool    yankFired;

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

        // Detect when chain suddenly yanks the player (large velocity delta in a single frame)
        Vector3 vel   = physics.Velocity;
        float   delta = (vel - prevVelocity).magnitude / Time.fixedDeltaTime;
        if (!yankFired && delta > 18f)
        {
            yankFired = true;
            onYank?.Invoke();
        }
        prevVelocity = vel;
    }

    // ── Throw ──────────────────────────────────────────────────────────────

    void Throw()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Spawn ball just in front of camera
        ballObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballObj.name = "BallAndChain_Ball";
        ballObj.transform.localScale = Vector3.one * (ballRadius * 2f);
        ballObj.transform.position   = cam.transform.position + cam.transform.forward * 0.8f;

        // Disable shadow for performance
        var renderer = ballObj.GetComponent<Renderer>();
        if (renderer != null) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Physics
        ballRb = ballObj.AddComponent<Rigidbody>();
        ballRb.mass              = ballMass;
        ballRb.angularDamping    = ballAngularDrag;
        ballRb.interpolation     = RigidbodyInterpolation.Interpolate;
        ballRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Bouncy physics material
        var physMat = new PhysicsMaterial("BallChain_Bounce");
        physMat.bounciness    = bounciness;
        physMat.dynamicFriction  = 0.3f;
        physMat.staticFriction   = 0.3f;
        physMat.bounceCombine    = PhysicsMaterialCombine.Maximum;
        physMat.frictionCombine  = PhysicsMaterialCombine.Average;
        ballObj.GetComponent<Collider>().material = physMat;

        // Prevent ball from colliding with the player itself
        var playerColliders = GetComponentsInChildren<Collider>();
        var ballCol = ballObj.GetComponent<Collider>();
        foreach (var c in playerColliders)
            Physics.IgnoreCollision(ballCol, c, true);

        // Throw!
        ballRb.AddForce(cam.transform.forward * throwForce, ForceMode.Impulse);

        // Chain joint — connects player to ball Rigidbody
        chain = gameObject.AddComponent<SpringJoint>();
        chain.connectedBody               = ballRb;
        chain.autoConfigureConnectedAnchor = false;
        chain.connectedAnchor              = Vector3.zero;  // ball center
        chain.anchor                       = Vector3.up * 0.5f;  // player waist-ish
        chain.maxDistance                  = chainLength;
        chain.minDistance                  = 0f;
        chain.spring                       = chainSpring;
        chain.damper                       = chainDamper;
        chain.tolerance                    = 0.02f;
        chain.enableCollision              = false;

        physics.SetMaxHorizontalSpeedOverride(chainSpeedCap);

        state     = State.Active;
        yankFired = false;
        prevVelocity = physics.Velocity;

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
        if (chain != null)  { Destroy(chain);   chain   = null; }
        if (ballObj != null){ Destroy(ballObj); ballObj = null; ballRb = null; }

        physics.ClearMaxHorizontalSpeedOverride();
        state = State.Idle;
    }

    void OnDestroy() => CleanUp();

    // ── Public Getters ─────────────────────────────────────────────────────

    public bool       IsActive          => state == State.Active;
    public GameObject BallObject        => ballObj;
    public Vector3    BallPosition      => ballObj != null ? ballObj.transform.position : transform.position;
    public float      CooldownNormalized => Mathf.Clamp01(cooldownTimer / cooldown);
}
