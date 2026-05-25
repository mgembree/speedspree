using UnityEngine;

/// <summary>
/// Manages player velocity, momentum, and external forces.
/// Equipment abilities push forces through AddImpulse / AddForce.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerPhysics : MonoBehaviour
{
    [Header("Momentum")]
    [SerializeField] float groundFriction = 8f;
    [SerializeField] float airFriction = 1.5f;
    [SerializeField] float maxHorizontalSpeed = 20f;

    [Header("Mass")]
    [Tooltip("Scales Rigidbody mass and all impulse responsiveness.")]
    [SerializeField, Min(0.1f)] float massMultiplier = 1f;

    [Header("Gravity")]
    [SerializeField] float gravityScale = 2.5f;
    [SerializeField] float maxFallSpeed = 40f;

    Rigidbody rb;
    float baseMass;

    // Accumulated this-frame forces from abilities
    Vector3 pendingImpulse;
    Vector3 pendingForce;

    public Rigidbody Rigidbody => rb;
    public Vector3 Velocity => rb.linearVelocity;
    public float GravityScale
    {
        get => gravityScale;
        set => gravityScale = value;
    }
    public float MassMultiplier => massMultiplier;
    public float HorizontalSpeed => new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        baseMass = Mathf.Max(0.01f, rb.mass);
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.mass = baseMass * Mathf.Max(0.1f, massMultiplier);
    }

    void FixedUpdate()
    {
        ApplyGravity();
        ApplyPendingForces();
        ClampHorizontalSpeed();
        ClampFallSpeed();
    }

    // ── Public API for movement and abilities ──────────────────────────────

    /// <summary>Direct velocity set on the horizontal plane (used by movement controller).</summary>
    public void SetHorizontalVelocity(Vector3 horizontal)
    {
        rb.linearVelocity = new Vector3(horizontal.x, rb.linearVelocity.y, horizontal.z);
    }

    public void SetVerticalVelocity(float y)
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, y, rb.linearVelocity.z);
    }

    /// <summary>Instant velocity kick — use for jumps, dashes, grapple snaps.</summary>
    public void AddImpulse(Vector3 impulse)
    {
        pendingImpulse += impulse;
    }

    /// <summary>Continuous force this frame — use for sustained thrust, etc.</summary>
    public void AddForce(Vector3 force)
    {
        pendingForce += force;
    }

    // ── Friction helpers ───────────────────────────────────────────────────

    public void ApplyGroundFriction(bool isGrounded)
    {
        float friction = isGrounded ? groundFriction : airFriction;
        Vector3 horizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, friction * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(horizontal.x, rb.linearVelocity.y, horizontal.z);
    }

    // ── Private ────────────────────────────────────────────────────────────

    void ApplyGravity()
    {
        rb.AddForce(Physics.gravity * (gravityScale - 1f), ForceMode.Acceleration);
    }

    void ApplyPendingForces()
    {
        if (pendingImpulse != Vector3.zero)
        {
            rb.AddForce(pendingImpulse, ForceMode.Impulse);
            pendingImpulse = Vector3.zero;
        }
        if (pendingForce != Vector3.zero)
        {
            rb.AddForce(pendingForce, ForceMode.Force);
            pendingForce = Vector3.zero;
        }
    }

    void ClampHorizontalSpeed()
    {
        Vector3 horizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horizontal.magnitude > maxHorizontalSpeed)
        {
            horizontal = horizontal.normalized * maxHorizontalSpeed;
            rb.linearVelocity = new Vector3(horizontal.x, rb.linearVelocity.y, horizontal.z);
        }
    }

    void ClampFallSpeed()
    {
        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -maxFallSpeed, rb.linearVelocity.z);
    }
}
