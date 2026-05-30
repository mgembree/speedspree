using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Press Q or E to teleport to the surface you're aiming at.
/// Spawns sparks at origin and destination. Bind via equipmentSlot: 0=Q, 1=E.
/// </summary>
[RequireComponent(typeof(PlayerPhysics))]
public class TeleportAbility : MonoBehaviour
{
    [Header("Teleport")]
    [SerializeField] float     range        = 40f;
    [SerializeField] float     cooldown     = 1.5f;
    [SerializeField] LayerMask teleportMask = ~0;

    [Header("Input")]
    [SerializeField] int equipmentSlot = 0;

    PlayerPhysics physics;
    float         cooldownTimer;

    void Awake() => physics = GetComponent<PlayerPhysics>();

    void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        bool keyPressed = equipmentSlot == 0
            ? Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame
            : Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        if (keyPressed && cooldownTimer <= 0f)
            TryTeleport();
    }

    void TryTeleport()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, range, teleportMask, QueryTriggerInteraction.Ignore))
            return;

        Vector3 dest = hit.point + hit.normal * 0.6f;

        // Snap feet to floor surface cleanly
        if (hit.normal.y > 0.6f)
        {
            dest = new Vector3(hit.point.x, hit.point.y + 0.05f, hit.point.z);
            physics.SetVerticalVelocity(0f);
        }

        HitSpark.Spawn(transform.position);   // origin poof
        transform.position = dest;
        HitSpark.Spawn(dest);                 // destination poof

        cooldownTimer = cooldown;
        Debug.Log($"[Teleport] Blinked to {dest}");
    }

    public float CooldownNormalized => Mathf.Clamp01(cooldownTimer / cooldown);
}
