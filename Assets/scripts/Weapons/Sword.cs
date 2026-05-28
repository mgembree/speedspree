using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword : WeaponBase
{
    [Header("Stats")]
    [SerializeField] float damage       = 60f;
    [SerializeField] float swingCooldown = 0.7f;
    [SerializeField] float hitRange     = 2.0f;  // distance forward from camera
    [SerializeField] float hitRadius    = 1.2f;  // overlap sphere radius
    [SerializeField] LayerMask hitMask  = ~0;

    float nextSwingTime;
    Coroutine activeSwing;

    public override void PrimaryAttack()
    {
        if (Time.time < nextSwingTime) return;
        nextSwingTime = Time.time + swingCooldown;
        activeSwing = StartCoroutine(SwingRoutine());
    }

    public override void OnUnequip()
    {
        if (activeSwing != null)
        {
            StopCoroutine(activeSwing);
            activeSwing = null;
        }
        base.OnUnequip();
    }

    IEnumerator SwingRoutine()
    {
        yield return new WaitForSeconds(0.1f);

        Vector3 center    = Cam.transform.position + Cam.transform.forward * hitRange;
        Collider[] hits   = Physics.OverlapSphere(center, hitRadius, hitMask, QueryTriggerInteraction.Ignore);
        var hitRoots      = new HashSet<GameObject>();

        foreach (var col in hits)
        {
            GameObject root = col.transform.root.gameObject;
            if (root == transform.root.gameObject) continue;
            if (!hitRoots.Add(root)) continue;
            col.GetComponent<IDamageable>()?.TakeDamage(damage, gameObject);
        }

        Debug.Log($"[Sword] Swing | hit {hitRoots.Count} target(s)");
        activeSwing = null;
    }
}
