using System.Collections;
using UnityEngine;

public class DummyEnemy : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] float maxHealth    = 100f;
    [SerializeField] float respawnDelay = 3f;   // 0 = destroy on death, >0 = respawn

    [Header("Feedback")]
    [SerializeField] Color baseColor        = Color.white;
    [SerializeField] Color hitColor         = Color.red;
    [SerializeField] Color deadColor        = Color.grey;
    [SerializeField] float hitFlashDuration = 0.15f;

    [Header("Health Bar")]
    [Tooltip("Assign a child flat-cube scaled on X to visualise health (optional).")]
    [SerializeField] Transform healthBarFill;

    float    currentHealth;
    Renderer rend;
    bool     isDead;

    Vector3    spawnPos;
    Quaternion spawnRot;

    void Awake()
    {
        rend      = GetComponentInChildren<Renderer>();
        spawnPos  = transform.position;
        spawnRot  = transform.rotation;
    }

    void Start()
    {
        currentHealth = maxHealth;
        RefreshHealthBar();
        if (rend != null) rend.material.color = baseColor;
    }

    public void TakeDamage(float amount, GameObject source)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Debug.Log($"[Dummy] {name} hit for {amount} | HP: {currentHealth}/{maxHealth}");

        RefreshHealthBar();
        StopCoroutine(nameof(HitFlash));
        StartCoroutine(nameof(HitFlash));

        if (currentHealth <= 0f) Die();
    }

    void Die()
    {
        isDead = true;
        if (rend != null) rend.material.color = deadColor;
        Debug.Log($"[Dummy] {name} destroyed!");

        if (respawnDelay > 0f)
            StartCoroutine(nameof(RespawnRoutine));
        else
            gameObject.SetActive(false);
    }

    IEnumerator HitFlash()
    {
        if (rend == null) yield break;
        rend.material.color = hitColor;
        yield return new WaitForSeconds(hitFlashDuration);
        rend.material.color = isDead ? deadColor : baseColor;
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        transform.SetPositionAndRotation(spawnPos, spawnRot);
        currentHealth = maxHealth;
        isDead        = false;
        RefreshHealthBar();
        if (rend != null) rend.material.color = baseColor;
        Debug.Log($"[Dummy] {name} respawned!");
    }

    void RefreshHealthBar()
    {
        if (healthBarFill == null) return;
        float pct = currentHealth / maxHealth;
        healthBarFill.localScale = new Vector3(pct, healthBarFill.localScale.y, healthBarFill.localScale.z);
    }

    // Shows attack range gizmo in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    public float HealthPercent => currentHealth / maxHealth;
    public bool  IsDead        => isDead;
}
