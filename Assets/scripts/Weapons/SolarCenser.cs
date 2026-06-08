using System.Collections;
using UnityEngine;

/// <summary>
/// Faction E radiant weapon.
/// Creates a solar impact pulse that damages and lifts targets in an area.
/// </summary>
public class SolarCenser : WeaponBase
{
    [Header("Stats")]
    [SerializeField] float damage = 30f;
    [SerializeField] float range = 95f;
    [SerializeField] float fireRate = 1.7f;
    [SerializeField] LayerMask hitMask = ~0;

    [Header("Solar Pulse")]
    [SerializeField] float pulseRadius = 3.2f;
    [SerializeField] float liftImpulse = 9f;
    [SerializeField] float outwardImpulse = 6f;

    [Header("Magazine")]
    [SerializeField, Min(1)] int baseMagazineSize = 8;
    [SerializeField, Min(0.05f)] float reloadDuration = 1.3f;

    float nextFireTime;
    int ammoInMagazine;
    bool isReloading;
    Coroutine reloadRoutine;

    void Awake()
    {
        weaponCategory = WeaponCategory.Energy;
        ammoInMagazine = GetMagazineSize();
    }

    public override void PrimaryAttack()
    {
        if (isReloading) return;
        if (ammoInMagazine <= 0)
        {
            TryReload();
            return;
        }

        float effectiveFireRate = fireRate * WeaponAttachmentQuery.GetFireRateMultiplier(this);
        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + 1f / Mathf.Max(0.05f, effectiveFireRate);
        Fire();
    }

    void Fire()
    {
        ammoInMagazine = Mathf.Max(0, ammoInMagazine - 1);

        float damageMultiplier = WeaponAttachmentQuery.GetDamageMultiplier(this);
        float rangeMultiplier = WeaponAttachmentQuery.GetRangeMultiplier(this);
        float radiusMultiplier = WeaponAttachmentQuery.GetHitRadiusMultiplier(this);
        float knockbackMultiplier = WeaponAttachmentQuery.GetKnockbackMultiplier(this);

        Vector3 origin = Cam.transform.position;
        Vector3 dir = Cam.transform.forward;

        Vector3 impactPoint = origin + dir * (range * rangeMultiplier);
        if (Physics.Raycast(origin, dir, out RaycastHit hit, range * rangeMultiplier, hitMask, QueryTriggerInteraction.Ignore))
            impactPoint = hit.point;

        float effectiveRadius = pulseRadius * radiusMultiplier;

        foreach (var col in Physics.OverlapSphere(impactPoint, effectiveRadius, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (col == null || col.transform.root == transform.root) continue;

            col.GetComponent<IDamageable>()?.TakeDamage(damage * damageMultiplier, gameObject);
            HitSpark.Spawn(col.ClosestPoint(impactPoint));

            if (col.attachedRigidbody != null)
            {
                Vector3 away = (col.transform.position - impactPoint).normalized;
                Vector3 force = (away * outwardImpulse + Vector3.up * liftImpulse) * knockbackMultiplier;
                col.attachedRigidbody.AddForce(force, ForceMode.Impulse);
            }
        }

        if (ammoInMagazine <= 0)
            TryReload();
    }

    void TryReload()
    {
        if (isReloading) return;
        if (ammoInMagazine >= GetMagazineSize()) return;

        if (reloadRoutine != null)
            StopCoroutine(reloadRoutine);

        reloadRoutine = StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        isReloading = true;
        float effectiveReload = reloadDuration * WeaponAttachmentQuery.GetReloadMultiplier(this);
        yield return new WaitForSeconds(Mathf.Max(0.05f, effectiveReload));

        ammoInMagazine = GetMagazineSize();
        isReloading = false;
        reloadRoutine = null;
    }

    int GetMagazineSize()
    {
        float magMultiplier = WeaponAttachmentQuery.GetMagazineMultiplier(this);
        return Mathf.Max(1, Mathf.RoundToInt(baseMagazineSize * magMultiplier));
    }
}
