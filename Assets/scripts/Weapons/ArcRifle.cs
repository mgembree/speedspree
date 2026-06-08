using System.Collections;
using UnityEngine;

/// <summary>
/// Faction D energy rifle.
/// Fires rapid precision arcs that chain through targets with focused energy output.
/// </summary>
public class ArcRifle : WeaponBase
{
    [Header("Stats")]
    [SerializeField] float damage = 18f;
    [SerializeField] float fireRate = 6.5f;
    [SerializeField] float range = 140f;
    [SerializeField] LayerMask hitMask = ~0;

    [Header("Magazine")]
    [SerializeField, Min(1)] int baseMagazineSize = 24;
    [SerializeField, Min(0.05f)] float reloadDuration = 1.0f;

    [Header("Arc")]
    [SerializeField] int chainCount = 2;
    [SerializeField] float chainRadius = 4f;
    [SerializeField] float chainDamageFalloff = 0.75f;
    [SerializeField] float projectileSpeed = 165f;

    int ammoInMagazine;
    float nextFireTime;
    bool isReloading;
    Coroutine reloadRoutine;

    void Awake()
    {
        weaponCategory = WeaponCategory.EnergyRifle;
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
        float projectileSpeedMultiplier = WeaponAttachmentQuery.GetProjectileSpeedMultiplier(this);
        float sizeMultiplier = WeaponAttachmentQuery.GetSizeMultiplier(this);

        Vector3 origin = Cam.transform.position;
        Vector3 direction = Cam.transform.forward;
        float effectiveRange = range * rangeMultiplier;
        float effectiveBeamRadius = 0.45f * sizeMultiplier * WeaponAttachmentQuery.GetHitRadiusMultiplier(this);

        Ray ray = new Ray(origin, direction);
        if (Physics.Raycast(ray, out RaycastHit hit, effectiveRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 impactPoint = hit.point;
            HitSpark.Spawn(impactPoint, hit.normal);
            ApplyDamage(hit.collider, damage * damageMultiplier, impactPoint, direction);
            ChainArc(impactPoint, hit.collider, damage * damageMultiplier, direction);
            Debug.Log($"[ArcRifle] Hit {hit.collider.name} | ammo={ammoInMagazine}/{GetMagazineSize()}");
        }
        else
        {
            Vector3 fallback = origin + direction * effectiveRange;
            HitSpark.Spawn(fallback, -direction);
        }

        // Use a lightweight visual projectile marker for now.
        Debug.DrawRay(origin, direction * (effectiveRange * 0.5f), Color.cyan, 0.15f);
        Debug.Log($"[ArcRifle] Fired | ammo={ammoInMagazine}/{GetMagazineSize()} | beamRadius={effectiveBeamRadius:F2} | speed={projectileSpeed * projectileSpeedMultiplier:F1}");

        if (ammoInMagazine <= 0)
            TryReload();
    }

    void ChainArc(Vector3 sourcePoint, Collider firstHit, float baseDamage, Vector3 shotDirection)
    {
        if (chainCount <= 0)
            return;

        Collider[] nearby = Physics.OverlapSphere(sourcePoint, chainRadius, hitMask, QueryTriggerInteraction.Ignore);
        int jumps = 0;

        foreach (var col in nearby)
        {
            if (col == null || col == firstHit) continue;
            if (col.transform.root == transform.root) continue;

            Vector3 chainPoint = col.ClosestPoint(sourcePoint);
            HitSpark.Spawn(chainPoint);
            col.GetComponent<IDamageable>()?.TakeDamage(baseDamage * chainDamageFalloff, gameObject);

            if (col.attachedRigidbody != null)
            {
                Vector3 away = (col.transform.position - sourcePoint).normalized;
                col.attachedRigidbody.AddForce(away * 8f, ForceMode.Impulse);
            }

            jumps++;
            if (jumps >= chainCount)
                break;
        }
    }

    void ApplyDamage(Collider target, float amount, Vector3 point, Vector3 shotDirection)
    {
        target.GetComponent<IDamageable>()?.TakeDamage(amount, gameObject);

        if (target.attachedRigidbody != null)
        {
            float knockback = 4.5f * WeaponAttachmentQuery.GetKnockbackMultiplier(this);
            target.attachedRigidbody.AddForce(shotDirection * knockback, ForceMode.Impulse);
        }
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
        Debug.Log($"[ArcRifle] Reloading for {effectiveReload:F2}s");
        yield return new WaitForSeconds(effectiveReload);

        ammoInMagazine = GetMagazineSize();
        isReloading = false;
        reloadRoutine = null;
        Debug.Log($"[ArcRifle] Reloaded | ammo={ammoInMagazine}/{GetMagazineSize()}");
    }

    int GetMagazineSize()
    {
        float magMultiplier = WeaponAttachmentQuery.GetMagazineMultiplier(this);
        return Mathf.Max(1, Mathf.RoundToInt(baseMagazineSize * magMultiplier));
    }
}
