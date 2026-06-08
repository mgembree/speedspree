using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Flags]
public enum WeaponFamily
{
    None = 0,
    Gun = 1 << 0,
    Rifle = 1 << 1,
    Laser = 1 << 2,
    Melee = 1 << 3,
    Heavy = 1 << 4,
    Energy = 1 << 5,
    Precision = 1 << 6,
    Mobility = 1 << 7,
    Explosive = 1 << 8,
    Utility = 1 << 9,
    Any = ~0,
}

public enum WeaponAttachmentPreset
{
    LaserFocuser,
    ExtendedMag,
    EnlargedWeapon,
    RecoilDamper,
    QuickRack,
    HeatSink,
    SmartLink,
    ChainLink,
    Stabilizer,
    OverdriveCell,
}

public enum AttachmentRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
}

[Serializable]
public class WeaponAttachmentPresetInfo
{
    public WeaponAttachmentPreset Preset;
    public string DisplayName;
    [TextArea(2, 4)] public string Description;
    public WeaponFamily AllowedFamilies = WeaponFamily.Any;

    public bool Supports(WeaponFamily families)
    {
        if (AllowedFamilies == WeaponFamily.Any)
            return true;

        return (AllowedFamilies & families) != 0;
    }
}

/// <summary>
/// Runtime attachment component that can be added to a weapon in the dev tool.
/// The attachment applies preset multipliers to compatible weapon families.
/// </summary>
public class WeaponAttachmentModifier : MonoBehaviour
{
    [Header("Preset")]
    [SerializeField] WeaponAttachmentPreset preset;

    [Header("Compatibility")]
    [SerializeField] WeaponFamily allowedFamilies = WeaponFamily.Any;

    [Header("Rarity")]
    [SerializeField] AttachmentRarity rarity = AttachmentRarity.Common;

    [Header("Stats")]
    [SerializeField, Min(0f)] float damageMultiplier = 1f;
    [SerializeField, Min(0f)] float fireRateMultiplier = 1f;
    [SerializeField, Min(0f)] float rangeMultiplier = 1f;
    [SerializeField, Min(0f)] float magazineMultiplier = 1f;
    [SerializeField, Min(0f)] float reloadMultiplier = 1f;
    [SerializeField, Min(0f)] float sizeMultiplier = 1f;
    [SerializeField, Min(0f)] float recoilMultiplier = 1f;
    [SerializeField, Min(0f)] float hitRadiusMultiplier = 1f;
    [SerializeField, Min(0f)] float knockbackMultiplier = 1f;
    [SerializeField, Min(0f)] float lungeMultiplier = 1f;
    [SerializeField, Min(0f)] float projectileSpeedMultiplier = 1f;

    [Header("Presentation")]
    [SerializeField] string displayName;
    [TextArea(2, 4)] [SerializeField] string description;

    public WeaponAttachmentPreset Preset => preset;
    public AttachmentRarity Rarity => rarity;
    public WeaponFamily AllowedFamilies => allowedFamilies;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? preset.ToString() : displayName;
    public string Description => description;
    public float DamageMultiplier => ApplyRarity(damageMultiplier);
    public float FireRateMultiplier => ApplyRarity(fireRateMultiplier);
    public float RangeMultiplier => ApplyRarity(rangeMultiplier);
    public float MagazineMultiplier => ApplyRarity(magazineMultiplier);
    public float ReloadMultiplier => ApplyRarity(reloadMultiplier);
    public float SizeMultiplier => ApplyRarity(sizeMultiplier);
    public float RecoilMultiplier => ApplyRarity(recoilMultiplier);
    public float HitRadiusMultiplier => ApplyRarity(hitRadiusMultiplier);
    public float KnockbackMultiplier => ApplyRarity(knockbackMultiplier);
    public float LungeMultiplier => ApplyRarity(lungeMultiplier);
    public float ProjectileSpeedMultiplier => ApplyRarity(projectileSpeedMultiplier);

    void Reset()
    {
        ApplyPreset(preset);
    }

    void OnValidate()
    {
        ApplyPreset(preset);
    }

    public void Configure(WeaponAttachmentPreset newPreset)
    {
        preset = newPreset;
        ApplyPreset(newPreset);
    }

    public void SetRarity(AttachmentRarity newRarity)
    {
        rarity = newRarity;
    }

    public bool Supports(WeaponFamily weaponFamilies)
    {
        if (allowedFamilies == WeaponFamily.Any)
            return true;

        return (allowedFamilies & weaponFamilies) != 0;
    }

    void ApplyPreset(WeaponAttachmentPreset newPreset)
    {
        switch (newPreset)
        {
            case WeaponAttachmentPreset.LaserFocuser:
                displayName = "Laser Focuser";
                description = "Tightens the beam profile and accelerates laser weapon cadence.";
                allowedFamilies = WeaponFamily.Laser | WeaponFamily.Energy;
                damageMultiplier = 1.00f;
                fireRateMultiplier = 1.35f;
                rangeMultiplier = 1.15f;
                magazineMultiplier = 1.00f;
                reloadMultiplier = 0.95f;
                sizeMultiplier = 0.98f;
                recoilMultiplier = 0.90f;
                hitRadiusMultiplier = 1.00f;
                knockbackMultiplier = 1.00f;
                lungeMultiplier = 1.00f;
                projectileSpeedMultiplier = 1.15f;
                break;

            case WeaponAttachmentPreset.ExtendedMag:
                displayName = "Extended Mag";
                description = "Increases magazine size and keeps sustained fire going longer.";
                allowedFamilies = WeaponFamily.Gun | WeaponFamily.Rifle;
                damageMultiplier = 1.00f;
                fireRateMultiplier = 0.98f;
                rangeMultiplier = 1.00f;
                magazineMultiplier = 1.50f;
                reloadMultiplier = 0.95f;
                sizeMultiplier = 1.00f;
                recoilMultiplier = 1.00f;
                hitRadiusMultiplier = 1.00f;
                knockbackMultiplier = 1.00f;
                lungeMultiplier = 1.00f;
                projectileSpeedMultiplier = 1.00f;
                break;

            case WeaponAttachmentPreset.EnlargedWeapon:
                displayName = "Enlarged Weapon";
                description = "Upscales the weapon model and improves contact coverage at the cost of speed.";
                allowedFamilies = WeaponFamily.Any;
                damageMultiplier = 1.08f;
                fireRateMultiplier = 0.93f;
                rangeMultiplier = 1.00f;
                magazineMultiplier = 1.00f;
                reloadMultiplier = 1.00f;
                sizeMultiplier = 1.20f;
                recoilMultiplier = 1.05f;
                hitRadiusMultiplier = 1.15f;
                knockbackMultiplier = 1.05f;
                lungeMultiplier = 1.00f;
                projectileSpeedMultiplier = 1.00f;
                break;

            case WeaponAttachmentPreset.RecoilDamper:
                displayName = "Recoil Damper";
                description = "Reduces kickback and lets the weapon stay on target more easily.";
                allowedFamilies = WeaponFamily.Gun | WeaponFamily.Rifle | WeaponFamily.Heavy | WeaponFamily.Laser;
                damageMultiplier = 1.00f;
                fireRateMultiplier = 1.03f;
                rangeMultiplier = 1.00f;
                magazineMultiplier = 1.00f;
                reloadMultiplier = 1.00f;
                sizeMultiplier = 1.00f;
                recoilMultiplier = 0.72f;
                hitRadiusMultiplier = 1.00f;
                knockbackMultiplier = 1.00f;
                lungeMultiplier = 1.00f;
                projectileSpeedMultiplier = 1.00f;
                break;

            case WeaponAttachmentPreset.QuickRack:
                displayName = "Quick Rack";
                description = "Speeds up cycling and reload motion for weapons that benefit from repeated bursts.";
                allowedFamilies = WeaponFamily.Gun | WeaponFamily.Rifle | WeaponFamily.Heavy;
                damageMultiplier = 0.98f;
                fireRateMultiplier = 1.18f;
                rangeMultiplier = 1.00f;
                magazineMultiplier = 0.95f;
                reloadMultiplier = 0.82f;
                sizeMultiplier = 1.00f;
                recoilMultiplier = 1.00f;
                hitRadiusMultiplier = 1.00f;
                knockbackMultiplier = 1.00f;
                lungeMultiplier = 1.00f;
                projectileSpeedMultiplier = 1.00f;
                break;

            case WeaponAttachmentPreset.HeatSink:
                displayName = "Heat Sink";
                description = "Improves heat management and allows energy weapons to fire longer with less stall.";
                allowedFamilies = WeaponFamily.Laser | WeaponFamily.Energy;
                damageMultiplier = 0.98f;
                fireRateMultiplier = 1.14f;
                rangeMultiplier = 1.00f;
                magazineMultiplier = 1.00f;
                reloadMultiplier = 0.84f;
                sizeMultiplier = 1.00f;
                recoilMultiplier = 1.00f;
                hitRadiusMultiplier = 1.00f;
                knockbackMultiplier = 1.00f;
                lungeMultiplier = 1.00f;
                projectileSpeedMultiplier = 1.05f;
                break;

            case WeaponAttachmentPreset.SmartLink:
                displayName = "Smart Link";
                description = "Adds targeting assistance and extends effective engagement range.";
                allowedFamilies = WeaponFamily.Gun | WeaponFamily.Rifle | WeaponFamily.Laser;
                damageMultiplier = 1.02f;
                fireRateMultiplier = 1.05f;
                rangeMultiplier = 1.12f;
                magazineMultiplier = 1.00f;
                reloadMultiplier = 1.00f;
                sizeMultiplier = 1.00f;
                recoilMultiplier = 0.95f;
                hitRadiusMultiplier = 1.00f;
                knockbackMultiplier = 1.00f;
                lungeMultiplier = 1.00f;
                projectileSpeedMultiplier = 1.05f;
                break;

            case WeaponAttachmentPreset.ChainLink:
                displayName = "Chain Link";
                description = "Adds chained mass and inertia, improving force transfer on heavy melee hits.";
                allowedFamilies = WeaponFamily.Melee | WeaponFamily.Heavy;
                damageMultiplier = 1.10f;
                fireRateMultiplier = 0.92f;
                rangeMultiplier = 1.00f;
                magazineMultiplier = 1.00f;
                reloadMultiplier = 1.00f;
                sizeMultiplier = 1.12f;
                recoilMultiplier = 1.05f;
                hitRadiusMultiplier = 1.16f;
                knockbackMultiplier = 1.10f;
                lungeMultiplier = 1.00f;
                projectileSpeedMultiplier = 1.00f;
                break;

            case WeaponAttachmentPreset.Stabilizer:
                displayName = "Stabilizer";
                description = "Improves handling and steadies the weapon during aggressive movement.";
                allowedFamilies = WeaponFamily.Gun | WeaponFamily.Rifle | WeaponFamily.Laser | WeaponFamily.Melee;
                damageMultiplier = 1.00f;
                fireRateMultiplier = 1.02f;
                rangeMultiplier = 1.03f;
                magazineMultiplier = 1.00f;
                reloadMultiplier = 1.00f;
                sizeMultiplier = 1.00f;
                recoilMultiplier = 0.82f;
                hitRadiusMultiplier = 1.00f;
                knockbackMultiplier = 1.00f;
                lungeMultiplier = 1.00f;
                projectileSpeedMultiplier = 1.00f;
                break;

            case WeaponAttachmentPreset.OverdriveCell:
                displayName = "Overdrive Cell";
                description = "Feeds the weapon unstable power for harder hits and hotter output.";
                allowedFamilies = WeaponFamily.Laser | WeaponFamily.Energy;
                damageMultiplier = 1.20f;
                fireRateMultiplier = 0.92f;
                rangeMultiplier = 1.00f;
                magazineMultiplier = 1.00f;
                reloadMultiplier = 0.90f;
                sizeMultiplier = 1.00f;
                recoilMultiplier = 1.00f;
                hitRadiusMultiplier = 1.00f;
                knockbackMultiplier = 1.00f;
                lungeMultiplier = 1.00f;
                projectileSpeedMultiplier = 1.10f;
                break;

            default:
                displayName = newPreset.ToString();
                description = "Attachment preset.";
                allowedFamilies = WeaponFamily.Any;
                damageMultiplier = 1f;
                fireRateMultiplier = 1f;
                rangeMultiplier = 1f;
                magazineMultiplier = 1f;
                reloadMultiplier = 1f;
                sizeMultiplier = 1f;
                recoilMultiplier = 1f;
                hitRadiusMultiplier = 1f;
                knockbackMultiplier = 1f;
                lungeMultiplier = 1f;
                projectileSpeedMultiplier = 1f;
                break;
        }
    }

    float ApplyRarity(float baseMultiplier)
    {
        float delta = baseMultiplier - 1f;
        return 1f + delta * GetRarityScalar(rarity);
    }

    static float GetRarityScalar(AttachmentRarity value)
    {
        switch (value)
        {
            case AttachmentRarity.Uncommon: return 1.15f;
            case AttachmentRarity.Rare: return 1.30f;
            case AttachmentRarity.Epic: return 1.50f;
            case AttachmentRarity.Legendary: return 1.80f;
            case AttachmentRarity.Common:
            default:
                return 1f;
        }
    }
}

public static class WeaponAttachmentQuery
{
    public static WeaponAttachmentModifier[] GetAttachments(WeaponBase weapon)
    {
        if (weapon == null)
            return Array.Empty<WeaponAttachmentModifier>();

        return weapon.GetComponents<WeaponAttachmentModifier>()
            .Where(attachment => attachment != null && attachment.Supports(weapon.Families))
            .ToArray();
    }

    public static bool HasPreset(WeaponBase weapon, WeaponAttachmentPreset preset)
    {
        return GetAttachments(weapon).Any(attachment => attachment.Preset == preset);
    }

    public static float GetDamageMultiplier(WeaponBase weapon) => Multiply(weapon, attachment => attachment.DamageMultiplier);
    public static float GetFireRateMultiplier(WeaponBase weapon) => Multiply(weapon, attachment => attachment.FireRateMultiplier);
    public static float GetRangeMultiplier(WeaponBase weapon) => Multiply(weapon, attachment => attachment.RangeMultiplier);
    public static float GetMagazineMultiplier(WeaponBase weapon) => Multiply(weapon, attachment => attachment.MagazineMultiplier);
    public static float GetReloadMultiplier(WeaponBase weapon) => Multiply(weapon, attachment => attachment.ReloadMultiplier);
    public static float GetSizeMultiplier(WeaponBase weapon) => Multiply(weapon, attachment => attachment.SizeMultiplier);
    public static float GetRecoilMultiplier(WeaponBase weapon) => Multiply(weapon, attachment => attachment.RecoilMultiplier);
    public static float GetHitRadiusMultiplier(WeaponBase weapon) => Multiply(weapon, attachment => attachment.HitRadiusMultiplier);
    public static float GetKnockbackMultiplier(WeaponBase weapon) => Multiply(weapon, attachment => attachment.KnockbackMultiplier);
    public static float GetLungeMultiplier(WeaponBase weapon) => Multiply(weapon, attachment => attachment.LungeMultiplier);
    public static float GetProjectileSpeedMultiplier(WeaponBase weapon) => Multiply(weapon, attachment => attachment.ProjectileSpeedMultiplier);

    public static int GetMagazineBonus(WeaponBase weapon)
    {
        if (weapon == null)
            return 0;

        float bonus = 0f;
        foreach (var attachment in GetAttachments(weapon))
        {
            if (attachment.MagazineMultiplier > 1f)
                bonus += Mathf.RoundToInt((attachment.MagazineMultiplier - 1f) * 10f);
        }

        return Mathf.Max(0, Mathf.RoundToInt(bonus));
    }

    static float Multiply(WeaponBase weapon, Func<WeaponAttachmentModifier, float> selector)
    {
        float value = 1f;
        foreach (var attachment in GetAttachments(weapon))
            value *= Mathf.Max(0f, selector(attachment));

        return value;
    }
}
