using UnityEngine;

public enum WeaponCategory
{
    Generic,
    Sidearm,
    Rifle,
    HeavyRanged,
    Laser,
    Melee,
    HeavyMelee,
    Energy,
    EnergyRifle,
    Precision,
}

public abstract class WeaponBase : MonoBehaviour
{
    [SerializeField] protected string weaponName = "Weapon";
    [SerializeField] protected WeaponCategory weaponCategory = WeaponCategory.Generic;

    // Lazy so weapons can start inactive in the scene without nulling cam in Awake
    Camera _cam;
    protected Camera Cam => _cam != null ? _cam : _cam = Camera.main;

    public string WeaponName => weaponName;
    public WeaponCategory Category => weaponCategory;
    public WeaponFamily Families => GetFamilies(weaponCategory);

    public abstract void PrimaryAttack();

    public virtual void OnEquip()   => gameObject.SetActive(true);
    public virtual void OnUnequip() => gameObject.SetActive(false);

    protected static WeaponFamily GetFamilies(WeaponCategory category)
    {
        switch (category)
        {
            case WeaponCategory.Sidearm:
                return WeaponFamily.Gun;
            case WeaponCategory.Rifle:
                return WeaponFamily.Gun | WeaponFamily.Rifle;
            case WeaponCategory.HeavyRanged:
                return WeaponFamily.Gun | WeaponFamily.Rifle | WeaponFamily.Heavy;
            case WeaponCategory.Laser:
                return WeaponFamily.Laser | WeaponFamily.Energy;
            case WeaponCategory.Melee:
                return WeaponFamily.Melee;
            case WeaponCategory.HeavyMelee:
                return WeaponFamily.Melee | WeaponFamily.Heavy;
            case WeaponCategory.Energy:
                return WeaponFamily.Energy;
            case WeaponCategory.EnergyRifle:
                return WeaponFamily.Gun | WeaponFamily.Rifle | WeaponFamily.Laser | WeaponFamily.Energy;
            case WeaponCategory.Precision:
                return WeaponFamily.Gun | WeaponFamily.Rifle | WeaponFamily.Precision;
            case WeaponCategory.Generic:
            default:
                return WeaponFamily.Any;
        }
    }
}
