using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword : WeaponBase
{
    [Header("Stats")]
    [SerializeField] float damage        = 60f;
    [SerializeField] float swingCooldown = 0.7f;
    [SerializeField] float hitRange      = 2.0f;
    [SerializeField] float hitRadius     = 1.2f;
    [SerializeField] LayerMask hitMask   = ~0;

    [Header("Swing Animation")]
    [SerializeField] Vector3 swingOffset        = new Vector3(75f, 0f, -10f);  // euler offset from rest
    [SerializeField] float   swingOutDuration   = 0.12f;
    [SerializeField] float   swingBackDuration  = 0.22f;

    float      nextSwingTime;
    Coroutine  activeSwing;
    Quaternion restRotation;

    void OnEnable()
    {
        restRotation = transform.localRotation;
    }

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
        Quaternion swungRotation = restRotation * Quaternion.Euler(swingOffset);

        // Swing out to apex
        float elapsed = 0f;
        while (elapsed < swingOutDuration)
        {
            elapsed += Time.deltaTime;
            transform.localRotation = Quaternion.Lerp(restRotation, swungRotation, elapsed / swingOutDuration);
            yield return null;
        }
        transform.localRotation = swungRotation;

        // Register hits at apex
        RegisterHits();

        // Return to rest
        elapsed = 0f;
        while (elapsed < swingBackDuration)
        {
            elapsed += Time.deltaTime;
            transform.localRotation = Quaternion.Lerp(swungRotation, restRotation, elapsed / swingBackDuration);
            yield return null;
        }
        transform.localRotation = restRotation;

        activeSwing = null;
    }

    void RegisterHits()
    {
        Vector3    center   = Cam.transform.position + Cam.transform.forward * hitRange;
        Collider[] colliders = Physics.OverlapSphere(center, hitRadius, hitMask, QueryTriggerInteraction.Ignore);
        var hitRoots = new HashSet<GameObject>();

        foreach (var col in colliders)
        {
            GameObject root = col.transform.root.gameObject;
            if (root == transform.root.gameObject) continue;
            if (!hitRoots.Add(root)) continue;

            Vector3 hitPoint = col.ClosestPoint(center);
            HitSpark.Spawn(hitPoint);
            col.GetComponent<IDamageable>()?.TakeDamage(damage, gameObject);
        }

        Debug.Log($"[Sword] Swing | hit {hitRoots.Count} target(s)");
    }
}
