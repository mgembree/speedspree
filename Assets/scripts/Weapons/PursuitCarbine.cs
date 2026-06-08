using System.Collections;
using UnityEngine;

/// <summary>
/// Faction B precision mobility rifle.
/// Fires short bursts with controllable spread and fast engagement cadence.
/// </summary>
public class PursuitCarbine : WeaponBase
{
    [Header("Stats")]
    [SerializeField] float damage = 16f;
    [SerializeField] float range = 130f;
    [SerializeField] float fireRate = 5.2f;
    [SerializeField] float spreadDegrees = 1.3f;
    [SerializeField] LayerMask hitMask = ~0;

    [Header("Burst")]
    [SerializeField, Min(1)] int burstCount = 3;
    [SerializeField, Min(0.01f)] float burstInterval = 0.06f;

    [Header("Magazine")]
    [SerializeField, Min(1)] int baseMagazineSize = 24;
    [SerializeField, Min(0.05f)] float reloadDuration = 1.1f;

    float nextFireTime;
    int ammoInMagazine;
    bool isReloading;
    bool isBursting;
    Coroutine burstRoutine;
    Coroutine reloadRoutine;

    void Awake()
    {
        weaponCategory = WeaponCategory.Rifle;
        ammoInMagazine = GetMagazineSize();
    }

    public override void PrimaryAttack()
    {
        if (isReloading || isBursting) return;
        if (ammoInMagazine <= 0)
        {
            TryReload();
            return;
        }

        float effectiveFireRate = fireRate * WeaponAttachmentQuery.GetFireRateMultiplier(this);
        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + 1f / Mathf.Max(0.05f, effectiveFireRate);
        burstRoutine = StartCoroutine(BurstRoutine());
    }

    IEnumerator BurstRoutine()
    {
        isBursting = true;

        float rangeMultiplier = WeaponAttachmentQuery.GetRangeMultiplier(this);
        float damageMultiplier = WeaponAttachmentQuery.GetDamageMultiplier(this);
        float projectileSpeedMultiplier = WeaponAttachmentQuery.GetProjectileSpeedMultiplier(this);
        float knockbackMultiplier = WeaponAttachmentQuery.GetKnockbackMultiplier(this);

        int fired = 0;
        while (fired < burstCount && ammoInMagazine > 0)
        {
            ammoInMagazine = Mathf.Max(0, ammoInMagazine - 1);

            Vector3 dir = GetSpreadDirection(spreadDegrees / Mathf.Max(0.1f, WeaponAttachmentQuery.GetRecoilMultiplier(this)));
            float effectiveRange = range * rangeMultiplier;

            Ray ray = new Ray(Cam.transform.position, dir);
            if (Physics.Raycast(ray, out RaycastHit hit, effectiveRange, hitMask, QueryTriggerInteraction.Ignore))
            {
                HitSpark.Spawn(hit.point, hit.normal);
                hit.collider.GetComponent<IDamageable>()?.TakeDamage(damage * damageMultiplier, gameObject);

                if (hit.rigidbody != null)
                    hit.rigidbody.AddForce(dir * (6f * knockbackMultiplier), ForceMode.Impulse);
            }

            fired++;
            Debug.DrawRay(Cam.transform.position, dir * (18f * projectileSpeedMultiplier), Color.green, 0.12f);

            if (fired < burstCount)
                yield return new WaitForSeconds(burstInterval);
        }

        isBursting = false;
        burstRoutine = null;

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

    Vector3 GetSpreadDirection(float spread)
    {
        Vector2 random = Random.insideUnitCircle * Mathf.Tan(spread * Mathf.Deg2Rad);
        Vector3 direction = Cam.transform.forward + Cam.transform.right * random.x + Cam.transform.up * random.y;
        return direction.normalized;
    }
}
