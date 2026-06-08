using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Pistol : WeaponBase
{
    [Header("Stats")]
    [SerializeField] float damage   = 25f;
    [SerializeField] float range    = 150f;
    [SerializeField] float fireRate = 3f;       // shots per second
    [SerializeField] LayerMask hitMask = ~0;

    [Header("Magazine")]
    [SerializeField, Min(1)] int baseMagazineSize = 8;
    [SerializeField, Min(0.05f)] float reloadDuration = 1.1f;

    float nextFireTime;
    int ammoInMagazine;
    bool isReloading;
    Coroutine reloadRoutine;

    void Awake()
    {
        weaponCategory = WeaponCategory.Rifle;
        ammoInMagazine = GetMagazineSize();
    }

    void OnEnable()
    {
        if (ammoInMagazine <= 0)
            ammoInMagazine = GetMagazineSize();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            TryReload();
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

        float effectiveFireRate = Mathf.Max(0.1f, fireRate * WeaponAttachmentQuery.GetFireRateMultiplier(this));
        nextFireTime = Time.time + 1f / effectiveFireRate;
        Fire();
    }

    void Fire()
    {
        ammoInMagazine = Mathf.Max(0, ammoInMagazine - 1);

        float effectiveDamage = damage * WeaponAttachmentQuery.GetDamageMultiplier(this);
        float effectiveRange = range * WeaponAttachmentQuery.GetRangeMultiplier(this);

        Ray ray = new Ray(Cam.transform.position, Cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, effectiveRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            HitSpark.Spawn(hit.point, hit.normal);
            hit.collider.GetComponent<IDamageable>()?.TakeDamage(effectiveDamage, gameObject);
            Debug.Log($"[Pistol] Hit: {hit.collider.name} at {hit.distance:F1}m | ammo={ammoInMagazine}/{GetMagazineSize()}");
        }

        if (ammoInMagazine <= 0)
            TryReload();
    }

    void TryReload()
    {
        if (isReloading) return;

        int magazineSize = GetMagazineSize();
        if (ammoInMagazine >= magazineSize) return;

        if (reloadRoutine != null)
            StopCoroutine(reloadRoutine);

        reloadRoutine = StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        isReloading = true;

        float effectiveReload = Mathf.Max(0.05f, reloadDuration * WeaponAttachmentQuery.GetReloadMultiplier(this));
        Debug.Log($"[Pistol] Reloading for {effectiveReload:F2}s");
        yield return new WaitForSeconds(effectiveReload);

        ammoInMagazine = GetMagazineSize();
        isReloading = false;
        reloadRoutine = null;
        Debug.Log($"[Pistol] Reloaded | ammo={ammoInMagazine}/{GetMagazineSize()}");
    }

    int GetMagazineSize()
    {
        float magazineScale = WeaponAttachmentQuery.GetMagazineMultiplier(this);
        return Mathf.Max(1, Mathf.RoundToInt(baseMagazineSize * magazineScale));
    }
}
