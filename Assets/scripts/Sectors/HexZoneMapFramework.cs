using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ZoneFaction
{
    A,
    B,
    C,
    D,
    E,
}

[Serializable]
public class FactionRewardProfile
{
    public ZoneFaction RunnerFaction;
    [Min(0f)] public float VersusA = 1f;
    [Min(0f)] public float VersusB = 1f;
    [Min(0f)] public float VersusC = 1f;
    [Min(0f)] public float VersusD = 1f;
    [Min(0f)] public float VersusE = 1f;

    public float GetMultiplier(ZoneFaction zoneFaction)
    {
        switch (zoneFaction)
        {
            case ZoneFaction.A: return VersusA;
            case ZoneFaction.B: return VersusB;
            case ZoneFaction.C: return VersusC;
            case ZoneFaction.D: return VersusD;
            case ZoneFaction.E: return VersusE;
            default: return 1f;
        }
    }
}

[Serializable]
public class ZoneNode
{
    public int Q;
    public int R;
    public int Ring;
    public int ThreatLevel;
    public ZoneFaction Faction;
    public bool HasShopAfterZone;
    public bool HasMiniboss;
    public bool IsFinalZone;
    public bool IsStartZone;
    public int BaseReward;

    public string ZoneId => $"({Q},{R})";
}

/// <summary>
/// Generates a hex-of-hexes run map where each zone stores faction, threat level,
/// shop markers, and miniboss flags.
///
/// SideLength 5 means a radius-4 hex map (61 total zones).
/// </summary>
public class HexZoneMapFramework : MonoBehaviour
{
    [Header("Map Shape")]
    [SerializeField, Min(2)] int sideLength = 5;

    [Header("Run Setup")]
    [SerializeField] ZoneFaction playerChosenFaction = ZoneFaction.A;
    [SerializeField] int seed = 12345;

    [Header("Shops")]
    [SerializeField, Range(0f, 1f)] float shopChance = 0.18f;
    [SerializeField, Min(0)] int minShopCount = 6;

    [Header("Rewards")]
    [SerializeField, Min(1)] int baseRewardPerThreat = 40;
    [SerializeField, Min(0)] int minibossRewardBonus = 30;
    [SerializeField] List<FactionRewardProfile> rewardProfiles = new();

    [Header("Miniboss Tuning")]
    [SerializeField, Min(0)] int minibossesPerRing = 1;

    [Header("Debug")]
    [SerializeField] bool generateOnStart = true;

    [SerializeField] List<ZoneNode> zones = new();

    System.Random rng;

    public IReadOnlyList<ZoneNode> Zones => zones;
    public ZoneFaction PlayerChosenFaction => playerChosenFaction;
    public int Radius => Mathf.Max(1, sideLength - 1);
    public ZoneNode FinalZone => zones.FirstOrDefault(z => z.IsFinalZone);
    public ZoneNode StartZone => zones.FirstOrDefault(z => z.IsStartZone);

    void Start()
    {
        if (generateOnStart)
            GenerateMap(playerChosenFaction, seed);
    }

    [ContextMenu("Generate Map (Current Seed)")]
    public void GenerateMapFromInspector()
    {
        GenerateMap(playerChosenFaction, seed);
    }

    public void GenerateMap(ZoneFaction chosenFaction, int mapSeed)
    {
        rng = new System.Random(mapSeed);
        EnsureRewardProfiles();
        zones = BuildHexZones(Radius);

        AssignThreatByRing(zones, Radius);
        AssignFactions();
        AssignStartZone();
        AssignShops();
        AssignMinibosses();
        AssignBaseRewards();

        Debug.Log(BuildDebugSummary());
    }

    public ZoneNode TryGetZone(int q, int r)
    {
        return zones.FirstOrDefault(z => z.Q == q && z.R == r);
    }

    public string BuildDebugSummary()
    {
        if (zones.Count == 0)
            return "[MapGen] No zones generated.";

        int shopCount = zones.Count(z => z.HasShopAfterZone);
        int minibossCount = zones.Count(z => z.HasMiniboss);
        int totalBaseReward = zones.Sum(z => z.BaseReward);

        string header =
            $"[MapGen] sideLength={sideLength} radius={Radius} zones={zones.Count} seed={seed}\n" +
            $"[MapGen] freeRoam=true chosenFaction={playerChosenFaction} " +
            $"start={StartZone?.ZoneId ?? "none"} final={FinalZone?.ZoneId ?? "none"}\n" +
            $"[MapGen] shops={shopCount} (random chance={shopChance:F2} min={minShopCount}) minibosses={minibossCount} totalBaseReward={totalBaseReward}";

        var outer = zones.Where(z => z.Ring == Radius)
            .GroupBy(z => z.Faction)
            .OrderBy(g => g.Key)
            .Select(g => $"{g.Key}:{g.Count()}");

        string outerCounts = "[MapGen] outerRingFactionCounts " + string.Join(" | ", outer);
        return header + "\n" + outerCounts;
    }

    static List<ZoneNode> BuildHexZones(int radius)
    {
        var result = new List<ZoneNode>();

        for (int q = -radius; q <= radius; q++)
        {
            int rMin = Mathf.Max(-radius, -q - radius);
            int rMax = Mathf.Min(radius, -q + radius);

            for (int r = rMin; r <= rMax; r++)
            {
                int s = -q - r;
                int ring = Mathf.Max(Mathf.Abs(q), Mathf.Abs(r), Mathf.Abs(s));

                result.Add(new ZoneNode
                {
                    Q = q,
                    R = r,
                    Ring = ring,
                    IsFinalZone = ring == 0,
                });
            }
        }

        return result;
    }

    static void AssignThreatByRing(List<ZoneNode> source, int radius)
    {
        foreach (var z in source)
            z.ThreatLevel = radius - z.Ring + 1;
    }

    void AssignFactions()
    {
        foreach (var z in zones)
            z.Faction = RandomFaction();
    }

    void AssignStartZone()
    {
        foreach (var z in zones)
            z.IsStartZone = false;

        var candidates = zones.Where(z => !z.IsFinalZone).ToList();

        if (candidates.Count == 0)
            candidates = zones.ToList();

        ZoneNode start = candidates[rng.Next(candidates.Count)];
        start.IsStartZone = true;
    }

    void AssignShops()
    {
        foreach (var z in zones)
            z.HasShopAfterZone = false;

        var candidates = zones.Where(z => !z.IsFinalZone).ToList();
        Shuffle(candidates);

        int targetShops = Mathf.Max(minShopCount, Mathf.RoundToInt(candidates.Count * shopChance));
        targetShops = Mathf.Clamp(targetShops, 0, candidates.Count);

        for (int i = 0; i < targetShops; i++)
            candidates[i].HasShopAfterZone = true;
    }

    void AssignMinibosses()
    {
        foreach (var z in zones)
            z.HasMiniboss = false;

        for (int ring = Radius; ring >= 1; ring--)
        {
            var ringZones = zones.Where(z => z.Ring == ring && !z.IsStartZone).ToList();
            if (ringZones.Count == 0) continue;

            Shuffle(ringZones);
            int count = Mathf.Min(minibossesPerRing, ringZones.Count);

            for (int i = 0; i < count; i++)
                ringZones[i].HasMiniboss = true;
        }
    }

    void AssignBaseRewards()
    {
        foreach (var z in zones)
        {
            int reward = Mathf.Max(1, z.ThreatLevel * baseRewardPerThreat);
            if (z.HasMiniboss)
                reward += minibossRewardBonus;

            if (z.IsFinalZone)
                reward += Mathf.RoundToInt(baseRewardPerThreat * 1.5f);

            z.BaseReward = reward;
        }
    }

    public int GetZoneRewardForFaction(ZoneNode zone, ZoneFaction runnerFaction)
    {
        if (zone == null)
            return 0;

        float mult = GetRewardMultiplier(runnerFaction, zone.Faction);
        return Mathf.Max(1, Mathf.RoundToInt(zone.BaseReward * mult));
    }

    public float GetRewardMultiplier(ZoneFaction runnerFaction, ZoneFaction zoneFaction)
    {
        EnsureRewardProfiles();
        var profile = rewardProfiles.FirstOrDefault(p => p.RunnerFaction == runnerFaction);
        if (profile == null)
            return 1f;

        return Mathf.Max(0f, profile.GetMultiplier(zoneFaction));
    }

    void EnsureRewardProfiles()
    {
        if (rewardProfiles == null)
            rewardProfiles = new List<FactionRewardProfile>();

        var existing = new HashSet<ZoneFaction>(rewardProfiles.Select(p => p.RunnerFaction));
        foreach (ZoneFaction faction in Enum.GetValues(typeof(ZoneFaction)))
        {
            if (!existing.Contains(faction))
                rewardProfiles.Add(CreateDefaultProfile(faction));
        }
    }

    static FactionRewardProfile CreateDefaultProfile(ZoneFaction runnerFaction)
    {
        // Explicit default rivalry map:
        // - Same-faction clears pay less.
        // - One primary rival pays the most.
        // - One secondary rival pays moderately more.
        // - Remaining factions pay slightly more than baseline.
        switch (runnerFaction)
        {
            case ZoneFaction.A:
                return new FactionRewardProfile
                {
                    RunnerFaction = ZoneFaction.A,
                    VersusA = 0.90f,
                    VersusB = 1.10f,
                    VersusC = 1.35f,
                    VersusD = 1.20f,
                    VersusE = 1.05f,
                };

            case ZoneFaction.B:
                return new FactionRewardProfile
                {
                    RunnerFaction = ZoneFaction.B,
                    VersusA = 1.05f,
                    VersusB = 0.90f,
                    VersusC = 1.10f,
                    VersusD = 1.35f,
                    VersusE = 1.20f,
                };

            case ZoneFaction.C:
                return new FactionRewardProfile
                {
                    RunnerFaction = ZoneFaction.C,
                    VersusA = 1.20f,
                    VersusB = 1.05f,
                    VersusC = 0.90f,
                    VersusD = 1.10f,
                    VersusE = 1.35f,
                };

            case ZoneFaction.D:
                return new FactionRewardProfile
                {
                    RunnerFaction = ZoneFaction.D,
                    VersusA = 1.35f,
                    VersusB = 1.20f,
                    VersusC = 1.05f,
                    VersusD = 0.90f,
                    VersusE = 1.10f,
                };

            case ZoneFaction.E:
            default:
                return new FactionRewardProfile
                {
                    RunnerFaction = ZoneFaction.E,
                    VersusA = 1.10f,
                    VersusB = 1.35f,
                    VersusC = 1.20f,
                    VersusD = 1.05f,
                    VersusE = 0.90f,
                };
        }
    }

    ZoneFaction RandomFaction()
    {
        int value = rng.Next(0, 5);
        return (ZoneFaction)value;
    }

    void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}