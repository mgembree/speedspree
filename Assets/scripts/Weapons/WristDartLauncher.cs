using System.Collections;
using UnityEngine;

/// <summary>
/// Faction C tactical launcher.
/// Precision dart shot with a follow-up bonus if you re-hit quickly.
/// </summary>
public class WristDartLauncher : WeaponBase
{
    [Header("Stats")]
    [SerializeField] float damage = 26f;
    [SerializeField] float markedBonusDamage = 18f;
    [SerializeField] float range = 150f;
    [SerializeField] float fireRate = 2.8f;
    [SerializeField] LayerMask hitMask = ~0;

    [Header("Mark")]
    [SerializeField] float markDuration = 2.5f;

    [Header("Magazine")]
    [SerializeField, Min(1)] int baseMagazineSize = 6;
    [SerializeField, Min(0.05f)] float reloadDuration = 1.0f;

    float nextFireTime;
    int ammoInMagazine;
    bool isReloading;
    Coroutine reloadRoutine;

    Collider markedTarget;
    float markExpireTime;

    void Awake()
    {
        weaponCategory = WeaponCategory.Precision;
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
        float knockbackMultiplier = WeaponAttachmentQuery.GetKnockbackMultiplier(this);

        Ray ray = new Ray(Cam.transform.position, Cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range * rangeMultiplier, hitMask, QueryTriggerInteraction.Ignore))
        {
            float totalDamage = damage;
            if (markedTarget != null && markedTarget == hit.collider && Time.time <= markExpireTime)
                totalDamage += markedBonusDamage;

            hit.collider.GetComponent<IDamageable>()?.TakeDamage(totalDamage * damageMultiplier, gameObject);
            HitSpark.Spawn(hit.point, hit.normal);

            if (hit.rigidbody != null)
                hit.rigidbody.AddForce(Cam.transform.forward * (5f * knockbackMultiplier), ForceMode.Impulse);

            markedTarget = hit.collider;
            markExpireTime = Time.time + markDuration;
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
