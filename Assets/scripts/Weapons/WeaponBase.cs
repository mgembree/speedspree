using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [SerializeField] protected string weaponName = "Weapon";

    // Lazy so weapons can start inactive in the scene without nulling cam in Awake
    Camera _cam;
    protected Camera Cam => _cam != null ? _cam : _cam = Camera.main;

    public string WeaponName => weaponName;

    public abstract void PrimaryAttack();

    public virtual void OnEquip()   => gameObject.SetActive(true);
    public virtual void OnUnequip() => gameObject.SetActive(false);
}
