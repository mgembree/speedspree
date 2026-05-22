using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Jetpack equipment ability.
/// Hold Q or E to thrust upward, consuming fuel. Fuel recharges on the ground.
/// Bind equipmentSlot: 0 = Q, 1 = E.
/// </summary>
[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(PlayerMovementController))]
public class JetpackAbility : MonoBehaviour
{
    [Header("Thrust")]
    [SerializeField] float thrustForce = 18f;         // upward force while active
    [SerializeField] float maxVerticalSpeed = 12f;    // terminal ascent speed

    [Header("Fuel")]
    [SerializeField] float maxFuel = 1.5f;            // seconds of thrust
    [SerializeField] float fuelRechargeRate = 1f;     // seconds per second on ground
    [SerializeField] float rechargeDelay = 0.4f;      // seconds after landing before recharge starts

    [Header("Feel")]
    [SerializeField] float thrustCancelGravityScale = 0.4f;  // reduce gravity while thrusting

    [Header("Input")]
    [SerializeField] int equipmentSlot = 1;

    // Events
    public System.Action onThrustStart;
    public System.Action onThrustEnd;
    public System.Action<float> onFuelChanged;   // 0-1 normalized

    // State
    PlayerPhysics physics;
    PlayerMovementController movement;

    float fuel;
    bool thrusting;
    bool keyHeld;
    float rechargeTimer;
    float defaultGravityScale;

    void Awake()
    {
        physics = GetComponent<PlayerPhysics>();
        movement = GetComponent<PlayerMovementController>();
    }

void Start()
    {
        fuel = maxFuel;
        defaultGravityScale = physics.GravityScale;
        Debug.Log($"[Jetpack] Initialized | slot={equipmentSlot} fuel={fuel} gravScale={defaultGravityScale}");
    }

void Update()
    {
        // Read key hold state directly
        bool prevKeyHeld = keyHeld;
        keyHeld = equipmentSlot == 0
            ? Keyboard.current != null && Keyboard.current.qKey.isPressed
            : Keyboard.current != null && Keyboard.current.eKey.isPressed;

        if (keyHeld && !prevKeyHeld)
            Debug.Log($"[Jetpack] Key pressed | grounded={movement.IsGrounded} fuel={fuel:F2}");

        bool wasThrusting = thrusting;
        thrusting = keyHeld && !movement.IsGrounded && fuel > 0f;

        if (keyHeld && !thrusting && !wasThrusting)
            Debug.Log($"[Jetpack] Key held but NOT thrusting — grounded={movement.IsGrounded} fuel={fuel:F2} enabled={enabled} gameObject={gameObject.name}");

        if (thrusting && !wasThrusting)
        {
            Debug.Log($"[Jetpack] Thrust START | fuel={fuel:F2}");
            onThrustStart?.Invoke();
        }
        if (!thrusting && wasThrusting)
        {
            Debug.Log($"[Jetpack] Thrust END | fuel={fuel:F2}");
            onThrustEnd?.Invoke();
        }

        if (thrusting)
        {
            fuel -= Time.deltaTime;
            fuel = Mathf.Max(fuel, 0f);
            rechargeTimer = rechargeDelay;
            onFuelChanged?.Invoke(fuel / maxFuel);
        }
        else if (movement.IsGrounded)
        {
            if (rechargeTimer > 0f)
                rechargeTimer -= Time.deltaTime;
            else
            {
                fuel += fuelRechargeRate * Time.deltaTime;
                fuel = Mathf.Min(fuel, maxFuel);
                onFuelChanged?.Invoke(fuel / maxFuel);
            }
        }

        physics.GravityScale = thrusting ? thrustCancelGravityScale : defaultGravityScale;
    }

void FixedUpdate()
    {
        if (!thrusting) return;
        Debug.Log($"[Jetpack] FixedUpdate thrust | vy={physics.Velocity.y:F2} maxVY={maxVerticalSpeed}");
        if (physics.Velocity.y < maxVerticalSpeed)
            physics.AddForce(Vector3.up * thrustForce);
    }

    // ── Public Getters ─────────────────────────────────────────────────────

    public bool IsThrusting => thrusting;
    public float FuelNormalized => maxFuel > 0f ? fuel / maxFuel : 0f;
    public float Fuel => fuel;
}
