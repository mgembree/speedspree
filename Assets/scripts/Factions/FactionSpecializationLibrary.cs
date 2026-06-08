using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class FactionSpecializationDefinition
{
    public ZoneFaction Faction;
    public string DisplayName;
    [TextArea(2, 4)] public string Identity;
    [TextArea(2, 4)] public string Doctrine;
    public List<string> SignatureWeapons = new();
    public List<string> ExistingWeapons = new();
    public List<string> NewWeaponIdeas = new();
    public List<string> SpecializedEquipment = new();
    public List<string> ImplementedEquipment = new();
}

/// <summary>
/// Authoring surface for faction fantasy, signature weapons, and equipment specialization.
/// Defaults are seeded from current design notes and can be tuned in Inspector later.
/// </summary>
public class FactionSpecializationLibrary : MonoBehaviour
{
    [SerializeField] List<FactionSpecializationDefinition> factions = new();

    public IReadOnlyList<FactionSpecializationDefinition> Factions => factions;

    void Awake()
    {
        EnsureDefaults();
    }

    [ContextMenu("Reset Default Faction Definitions")]
    public void ResetDefaultDefinitions()
    {
        factions = CreateDefaultDefinitions();
    }

    public FactionSpecializationDefinition GetDefinition(ZoneFaction faction)
    {
        EnsureDefaults();
        return factions.FirstOrDefault(entry => entry.Faction == faction);
    }

    void EnsureDefaults()
    {
        if (factions == null)
            factions = new List<FactionSpecializationDefinition>();

        if (factions.Count == 0)
        {
            factions = CreateDefaultDefinitions();
            return;
        }

        HashSet<ZoneFaction> existing = new(factions.Select(entry => entry.Faction));
        foreach (var fallback in CreateDefaultDefinitions())
        {
            if (!existing.Contains(fallback.Faction))
                factions.Add(fallback);
        }
    }

    static List<FactionSpecializationDefinition> CreateDefaultDefinitions()
    {
        return new List<FactionSpecializationDefinition>
        {
            new FactionSpecializationDefinition
            {
                Faction = ZoneFaction.A,
                DisplayName = "Faction A | Ironwake Foundry",
                Identity = "Steampunk industrialists built around chain drives, recoil, boilers, and oversized mechanical mass.",
                Doctrine = "They solve problems by overpowering space with weight, kickback, blast pressure, and crushing momentum.",
                SignatureWeapons = new List<string>
                {
                    "Recoil Hammer",
                    "Pistol",
                    "Sword",
                    "Ball and Chain",
                },
                ExistingWeapons = new List<string>
                {
                    "Recoil Hammer",
                    "Pistol",
                    "Sword",
                    "Ball and Chain",
                    "Forge Cannon",
                },
                NewWeaponIdeas = new List<string>
                {
                    "Steam Ram",
                    "Chain Shotgun",
                    "Forge Mortar",
                    "Industrial Rivet Gun",
                },
                SpecializedEquipment = new List<string>
                {
                    "Hammer",
                    "Gun Recoil",
                    "Ball and Chain",
                    "Explosive Boots",
                },
                ImplementedEquipment = new List<string>
                {
                    nameof(RecoilHammer),
                    nameof(BallAndChainAbility),
                    nameof(ExplosiveBootsAbility),
                    nameof(Pistol),
                }
            },
            new FactionSpecializationDefinition
            {
                Faction = ZoneFaction.B,
                DisplayName = "Faction B | Meridian Combine",
                Identity = "Corporate futurists who reject messy body hacking in favor of clean robotics, pursuit rigs, and scalable mech support.",
                Doctrine = "They value efficient mobility and battlefield control through engineered traversal, deployables, and disciplined movement tech.",
                SignatureWeapons = new List<string>
                {
                    "Pursuit Carbine",
                    "Servo Pike",
                    "Contract SMG",
                    "Hunter Drone Launcher",
                },
                ExistingWeapons = new List<string>
                {
                    "Pistol",
                    "Sword",
                    "Pursuit Carbine",
                },
                NewWeaponIdeas = new List<string>
                {
                    "Pursuit Carbine",
                    "Servo Pike",
                    "Contract SMG",
                    "Hunter Drone Launcher",
                },
                SpecializedEquipment = new List<string>
                {
                    "Grappling Hook",
                    "Dash",
                    "Wall Running",
                    "Mech Support",
                },
                ImplementedEquipment = new List<string>
                {
                    nameof(GrappleAbility),
                    nameof(DashAbility),
                    nameof(WallRunAbility),
                    nameof(PursuitCarbine),
                }
            },
            new FactionSpecializationDefinition
            {
                Faction = ZoneFaction.C,
                DisplayName = "Faction C | Blackglass Directorate",
                Identity = "Tactical infiltrators with covert-ops aesthetics, splinter-cell discipline, and hit-and-fade execution.",
                Doctrine = "They win through stealth angles, burst repositioning, wall control, and precision strikes from unexpected vectors.",
                SignatureWeapons = new List<string>
                {
                    "Wrist Dart",
                    "Suppressed Carbine",
                    "Breach Knife",
                    "Ghostline Repeater",
                },
                ExistingWeapons = new List<string>
                {
                    "Sword",
                    "Pistol",
                    "Wrist Dart Launcher",
                },
                NewWeaponIdeas = new List<string>
                {
                    "Wrist Dart",
                    "Suppressed Carbine",
                    "Breach Knife",
                    "Ghostline Repeater",
                },
                SpecializedEquipment = new List<string>
                {
                    "Wall Bounce",
                    "Wrist Dart",
                    "Tactical Blink",
                    "Silent Mobility",
                },
                ImplementedEquipment = new List<string>
                {
                    nameof(TeleportAbility),
                    nameof(DoubleJumpAbility),
                    nameof(Sword),
                    nameof(WristDartLauncher),
                }
            },
            new FactionSpecializationDefinition
            {
                Faction = ZoneFaction.D,
                DisplayName = "Faction D | Volt Archivists",
                Identity = "Data-war zealots wielding energy systems, circuit corruption, and targeted cybernetic overload.",
                Doctrine = "They break enemy capability with precise information warfare, displacement tools, and high-energy traversal.",
                SignatureWeapons = new List<string>
                {
                    "Arc Rifle",
                    "Circuit Lance",
                    "Data Beam",
                    "EMP Prism",
                },
                ExistingWeapons = new List<string>
                {
                    "Pistol",
                    "Weapon Swap",
                    "Arc Rifle",
                },
                NewWeaponIdeas = new List<string>
                {
                    "Arc Rifle",
                    "Circuit Lance",
                    "Data Beam",
                    "EMP Prism",
                },
                SpecializedEquipment = new List<string>
                {
                    "Data Stream",
                    "Swapper",
                    "Jetpack",
                    "Energy Disruption",
                },
                ImplementedEquipment = new List<string>
                {
                    nameof(JetpackAbility),
                    nameof(WeaponSwapAbility),
                    nameof(TeleportAbility),
                }
            },
            new FactionSpecializationDefinition
            {
                Faction = ZoneFaction.E,
                DisplayName = "Faction E | Sol Ascendancy",
                Identity = "Religious zealots devoted to a distant machine-god, pursuing miracles through gravity doctrine and solar flight.",
                Doctrine = "They reshape movement itself with gravity blessings, aerial control, and faith-fueled traversal supremacy.",
                SignatureWeapons = new List<string>
                {
                    "Solar Censer",
                    "Halo Launcher",
                    "Gravity Staff",
                    "Sunshard Bow",
                },
                ExistingWeapons = new List<string>
                {
                    "Sword",
                    "Pistol",
                    "Solar Censer",
                },
                NewWeaponIdeas = new List<string>
                {
                    "Solar Censer",
                    "Halo Launcher",
                    "Gravity Staff",
                    "Sunshard Bow",
                },
                SpecializedEquipment = new List<string>
                {
                    "Low Gravity",
                    "High Gravity",
                    "Glider",
                    "Flight Blessing",
                },
                ImplementedEquipment = new List<string>
                {
                    nameof(JetpackAbility),
                    nameof(DoubleJumpAbility),
                    nameof(TeleportAbility),
                    nameof(SolarCenser),
                }
            },
        };
    }
}
