using System.Collections;
using UnityEngine;

/// <summary>
/// Faction A heavy industrial weapon.
/// Fires a crushing blast with strong knockback and slow cadence.
/// </summary>
public class ForgeCannon : WeaponBase
{
    [Header("Stats")]
    [SerializeField] float damage = 42f;
    [SerializeField] float fireRate = 1.1f;
    [SerializeField] float range = 120f;
    [SerializeField] float projectileSpeed = 95f;
    [SerializeField] LayerMask hitMask = ~0;

    [Header("Magazine")]
    [SerializeField, Min(1)] int baseMagazineSize = 4;
    [SerializeField, Min(0.05f)] float reloadDuration = 1.5f;

    [Header("Impact")]
    [SerializeField] float impactForce = 28f;
    [SerializeField] float impactRadius = 1.1f;

    int ammoInMagazine;
    float nextFireTime;
    bool isReloading;
    Coroutine reloadRoutine;

    void Awake()
    {
        weaponCategory = WeaponCategory.HeavyRanged;
        ammoInMagazine = GetMagazineSize();
    }

    void OnEnable()
    {
        if (ammoInMagazine <= 0)
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

        if (Time.time < nextFireTime) return;

        float effectiveFireRate = fireRate * WeaponAttachmentQuery.GetFireRateMultiplier(this);
        nextFireTime = Time.time + 1f / Mathf.Max(0.05f, effectiveFireRate);
        Fire();
    }

    void Fire()
    {
        ammoInMagazine = Mathf.Max(0, ammoInMagazine - 1);

        float damageMultiplier = WeaponAttachmentQuery.GetDamageMultiplier(this);
        float rangeMultiplier = WeaponAttachmentQuery.GetRangeMultiplier(this);
        float sizeMultiplier = WeaponAttachmentQuery.GetSizeMultiplier(this);
        float recoilMultiplier = WeaponAttachmentQuery.GetRecoilMultiplier(this);
        float knockbackMultiplier = WeaponAttachmentQuery.GetKnockbackMultiplier(this);
        float projectileSpeedMultiplier = WeaponAttachmentQuery.GetProjectileSpeedMultiplier(this);
        float effectiveProjectileSpeed = projectileSpeed * projectileSpeedMultiplier;

        Vector3 origin = Cam.transform.position;
        Vector3 direction = Cam.transform.forward;
        float effectiveRange = range * rangeMultiplier;
        float effectiveImpactRadius = impactRadius * sizeMultiplier;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, effectiveRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 impactPoint = hit.point;
            HitSpark.Spawn(impactPoint, hit.normal);
            hit.collider.GetComponent<IDamageable>()?.TakeDamage(damage * damageMultiplier, gameObject);

            if (hit.rigidbody != null)
                hit.rigidbody.AddForce(direction * (impactForce * knockbackMultiplier), ForceMode.Impulse);

            Vector3 blastPoint = impactPoint + hit.normal * 0.15f;
            foreach (var col in Physics.OverlapSphere(blastPoint, effectiveImpactRadius, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (col == hit.collider) continue;
                col.GetComponent<IDamageable>()?.TakeDamage((damage * 0.35f) * damageMultiplier, gameObject);

                if (col.attachedRigidbody != null)
                {
                    Vector3 away = (col.transform.position - blastPoint).normalized;
                    col.attachedRigidbody.AddForce(away * (impactForce * 0.5f * knockbackMultiplier), ForceMode.Impulse);
                }
            }
        }
        else
        {
            Vector3 fallbackPoint = origin + direction * effectiveRange;
            HitSpark.Spawn(fallbackPoint, -direction);
        }

        Debug.Log($"[ForgeCannon] Fired | ammo={ammoInMagazine}/{GetMagazineSize()} | range={effectiveRange:F1} | speed={effectiveProjectileSpeed:F1}");

        if (ammoInMagazine <= 0)
            TryReload();
    }

    void TryReload()
    {
        if (isReloading) return;

        if (reloadRoutine != null)
            StopCoroutine(reloadRoutine);

        reloadRoutine = StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        isReloading = true;

        float effectiveReload = reloadDuration * WeaponAttachmentQuery.GetReloadMultiplier(this);
        Debug.Log($"[ForgeCannon] Reloading for {effectiveReload:F2}s");
        yield return new WaitForSeconds(effectiveReload);

        ammoInMagazine = GetMagazineSize();
        isReloading = false;
        reloadRoutine = null;
        Debug.Log($"[ForgeCannon] Reloaded | ammo={ammoInMagazine}/{GetMagazineSize()}");
    }

    int GetMagazineSize()
    {
        float magMultiplier = WeaponAttachmentQuery.GetMagazineMultiplier(this);
        return Mathf.Max(1, Mathf.RoundToInt(baseMagazineSize * magMultiplier));
    }
}
