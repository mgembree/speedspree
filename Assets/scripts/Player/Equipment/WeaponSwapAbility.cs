using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Throws the currently equipped weapon as a physics projectile,
/// then teleports the player to where it lands and re-equips it.
/// Press Q/E (slot 0/1) to activate.
/// </summary>
[RequireComponent(typeof(PlayerPhysics))]
public class WeaponSwapAbility : MonoBehaviour
{
    [Header("Throw")]
    [Tooltip("Forward impulse added to player velocity when the weapon is thrown.")]
    [SerializeField] float throwForce    = 24f;
    [Tooltip("Extra upward arc on the throw so it clears geometry.")]
    [SerializeField] float throwUpward   = 3f;

    [Header("Teleport")]
    [Tooltip("Seconds after throwing before the player blinks to the weapon.")]
    [SerializeField] float teleportDelay = 0.65f;

    [Header("Cooldown")]
    [SerializeField] float cooldown = 2.0f;

    [Header("Input")]
    [SerializeField] int equipmentSlot = 0;

    PlayerPhysics    physics;
    WeaponController weaponController;
    float            cooldownTimer;
    bool             isActive;
    Coroutine        swapRoutine;

    void Awake()
    {
        physics          = GetComponent<PlayerPhysics>();
        weaponController = GetComponent<WeaponController>();
    }

    void Start()
    {
        Debug.Log($"[WeaponSwap] Ready | slot={equipmentSlot}");
    }

    void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        bool keyDown = equipmentSlot == 0
            ? Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame
            : Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        if (keyDown && cooldownTimer <= 0f && !isActive)
            TrySwap();
    }

    void TrySwap()
    {
        if (weaponController == null || weaponController.CurrentWeapon == null)
        {
            Debug.Log("[WeaponSwap] No weapon equipped — nothing to throw");
            return;
        }

        swapRoutine = StartCoroutine(SwapRoutine());
    }

    IEnumerator SwapRoutine()
    {
        isActive = true;

        WeaponBase savedWeapon = weaponController.CurrentWeapon;
        savedWeapon.OnUnequip();

        Camera cam = Camera.main;
        Vector3 throwDir = cam != null
            ? (cam.transform.forward + Vector3.up * (throwUpward / throwForce)).normalized
            : (transform.forward + Vector3.up * (throwUpward / throwForce)).normalized;

        // Spawn a physics proxy that flies like a thrown weapon
        var dummy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dummy.name = "WeaponSwap_Thrown";
        dummy.transform.localScale = Vector3.one * 0.25f;
        dummy.transform.position   = cam != null
            ? cam.transform.position + cam.transform.forward * 0.6f
            : transform.position + Vector3.up * 0.5f;

        var dummyCol = dummy.GetComponent<Collider>();
        foreach (var c in GetComponentsInChildren<Collider>())
            Physics.IgnoreCollision(dummyCol, c, true);

        var rb = dummy.AddComponent<Rigidbody>();
        rb.linearVelocity = physics.Velocity + throwDir * throwForce;
        rb.angularVelocity = new Vector3(Random.Range(2f, 5f), Random.Range(2f, 5f), 0f);

        HitSpark.Spawn(transform.position);

        yield return new WaitForSeconds(teleportDelay);

        // Blink to where the weapon landed
        Vector3 dest = dummy.transform.position + Vector3.up * 0.1f;
        transform.position = dest;
        physics.SetVerticalVelocity(0f);
        HitSpark.Spawn(dest);

        Destroy(dummy);

        // Re-equip the same weapon
        savedWeapon.OnEquip();

        cooldownTimer = cooldown;
        isActive      = false;
        swapRoutine   = null;

        Debug.Log($"[WeaponSwap] Blinked to {dest:F1} | re-equipped {savedWeapon.WeaponName}");
    }

    void OnDestroy()
    {
        if (swapRoutine != null)
            StopCoroutine(swapRoutine);
    }

    public float CooldownNormalized => Mathf.Clamp01(cooldownTimer / cooldown);
    public bool  IsActive           => isActive;
}
