using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Melee weapon that blasts the player backward on every swing.
/// Aim down → rocket jump. Aim forward → backward launch. Aim at a wall → side launch.
/// Recoil direction is opposite to camera aim, with a minimum upward component.
/// </summary>
public class RecoilHammer : WeaponBase
{
    [Header("Stats")]
    [SerializeField] float damage        = 80f;
    [SerializeField] float swingCooldown = 0.9f;
    [SerializeField] float hitRange      = 2.2f;
    [SerializeField] float hitRadius     = 1.5f;
    [SerializeField] LayerMask hitMask   = ~0;

    [Header("Recoil")]
    [Tooltip("Total launch force applied opposite to aim direction.")]
    [SerializeField] float recoilForce    = 24f;
    [Tooltip("Additive upward kick so forward swings still lift you slightly.")]
    [SerializeField] float recoilUpward   = 3f;
    [Tooltip("Impulse applied to hit Rigidbodies in the swing direction.")]
    [SerializeField] float objectKnockback = 18f;

    [Header("Swing Animation")]
    [SerializeField] Vector3 swingOffset       = new Vector3(110f, 0f, 0f);
    [SerializeField] float   swingOutDuration  = 0.15f;
    [SerializeField] float   swingBackDuration = 0.30f;

    PlayerPhysics        physics;
    float                nextSwingTime;
    Coroutine            activeSwing;
    Quaternion           restRotation;

    void Awake()
    {
        physics = GetComponentInParent<PlayerPhysics>();
    }

    void OnEnable() => restRotation = transform.localRotation;

    public override void PrimaryAttack()
    {
        if (Time.time < nextSwingTime) return;
        nextSwingTime = Time.time + swingCooldown;
        if (activeSwing != null) StopCoroutine(activeSwing);
        activeSwing = StartCoroutine(SwingRoutine());
    }

    public override void OnUnequip()
    {
        if (activeSwing != null)
        {
            StopCoroutine(activeSwing);
            activeSwing = null;
            transform.localRotation = restRotation;
        }
        base.OnUnequip();
    }

    IEnumerator SwingRoutine()
    {
        Quaternion swungRot = restRotation * Quaternion.Euler(swingOffset);

        float elapsed = 0f;
        while (elapsed < swingOutDuration)
        {
            elapsed += Time.deltaTime;
            transform.localRotation = Quaternion.Lerp(restRotation, swungRot, elapsed / swingOutDuration);
            yield return null;
        }
        transform.localRotation = swungRot;

        ApplyRecoil();
        RegisterHits();

        elapsed = 0f;
        while (elapsed < swingBackDuration)
        {
            elapsed += Time.deltaTime;
            transform.localRotation = Quaternion.Lerp(swungRot, restRotation, elapsed / swingBackDuration);
            yield return null;
        }
        transform.localRotation = restRotation;

        activeSwing = null;
    }

    void ApplyRecoil()
    {
        if (physics == null) return;

        // Recoil fires opposite to camera aim; clamp out downward component
        // so looking straight up doesn't slam the player into the ground.
        Vector3 dir = -Cam.transform.forward;
        if (dir.y < 0f) dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = -transform.forward; // edge case: aiming straight up

        physics.AddImpulse(dir.normalized * recoilForce + Vector3.up * recoilUpward);
        Debug.Log($"[Hammer] Recoil | dir={dir.normalized:F2}");
    }

    void RegisterHits()
    {
        Vector3    center    = Cam.transform.position + Cam.transform.forward * hitRange;
        Vector3    knockDir  = Cam.transform.forward;
        Collider[] colliders = Physics.OverlapSphere(center, hitRadius, hitMask, QueryTriggerInteraction.Ignore);
        var hitRoots = new HashSet<GameObject>();

        foreach (var col in colliders)
        {
            GameObject root = col.transform.root.gameObject;
            if (root == transform.root.gameObject) continue;
            if (!hitRoots.Add(root)) continue;

            HitSpark.Spawn(col.ClosestPoint(center));
            col.GetComponent<IDamageable>()?.TakeDamage(damage, gameObject);

            Rigidbody hitRb = col.attachedRigidbody;
            if (hitRb != null)
                hitRb.AddForce(knockDir * objectKnockback, ForceMode.Impulse);
        }

        Debug.Log($"[Hammer] Hit {hitRoots.Count} target(s)");
    }
}
