using UnityEngine;

public class Pistol : WeaponBase
{
    [Header("Stats")]
    [SerializeField] float damage   = 25f;
    [SerializeField] float range    = 150f;
    [SerializeField] float fireRate = 3f;       // shots per second
    [SerializeField] LayerMask hitMask = ~0;

    float nextFireTime;

    public override void PrimaryAttack()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + 1f / fireRate;
        Fire();
    }

    void Fire()
    {
        Ray ray = new Ray(Cam.transform.position, Cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            HitSpark.Spawn(hit.point, hit.normal);
            hit.collider.GetComponent<IDamageable>()?.TakeDamage(damage, gameObject);
            Debug.Log($"[Pistol] Hit: {hit.collider.name} at {hit.distance:F1}m");
        }
    }
}
