using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Orchestrates per-zone procedural chunk generation from HexZoneMapFramework metadata.
/// This is a skeleton controller intended for iterative expansion.
/// </summary>
public class ZoneChunkGenerationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] HexZoneMapFramework mapFramework;
    [SerializeField] ZoneGenerationProfile defaultProfile;
    [SerializeField] SpecialParcelRules specialParcelRules;
    [SerializeField] List<FactionThemeDefinition> factionThemes = new();

    [Header("Services")]
    [SerializeField] MonoBehaviour layoutPlannerBehaviour;
    [SerializeField] MonoBehaviour chunkBuilderBehaviour;

    [Header("Placement")]
    [SerializeField] float zoneSpacing = 170f;

    [Header("Template Debug")]
    [SerializeField] bool generateTemplateTestRig = true;
    [SerializeField] float templateTestSpacing = 220f;

    readonly Dictionary<string, GeneratedZoneChunk> activeChunks = new();

    IZoneLayoutPlanner layoutPlanner;
    IZoneChunkBuilder chunkBuilder;

    void Awake()
    {
        ResolveServiceReferences();
    }

    [ContextMenu("Generate All Zones (Debug)")]
    public void GenerateAllZonesDebug()
    {
        if (!CanGenerate()) return;
        ClearAllChunks();

        if (mapFramework.Zones == null || mapFramework.Zones.Count == 0)
        {
            Debug.Log("[ZoneProcGen] No zones found — generating map first.");
            mapFramework.GenerateMapFromInspector();
        }

        if (mapFramework.Zones == null || mapFramework.Zones.Count == 0)
        {
            Debug.LogWarning("[ZoneProcGen] Map still empty after generation attempt. Check HexZoneMapFramework settings.", this);
            return;
        }

        foreach (var zone in mapFramework.Zones)
            GenerateZone(zone);

        Debug.Log($"[ZoneProcGen] Generated {activeChunks.Count} zone chunks.");
    }

    [ContextMenu("Generate Template Test Rig")]
    public void GenerateTemplateTestRigDebug()
    {
        if (!CanGenerate()) return;
        ClearAllChunks();

        if (!generateTemplateTestRig)
        {
            Debug.Log("[ZoneProcGen] Template test rig is disabled.");
            return;
        }

        var templates = new[]
        {
            ZoneChunkTemplateKind.Chase,
            ZoneChunkTemplateKind.VerticalArena,
            ZoneChunkTemplateKind.GrappleCathedral,
        };

        for (int i = 0; i < templates.Length; i++)
        {
            ZoneGenerationContext context = BuildTemplateContext(templates[i], i);
            ZoneChunkPlan plan = layoutPlanner.PlanLayout(context, defaultProfile, specialParcelRules);

            FactionThemeDefinition primaryTheme = GetTheme(context.PrimaryFaction);
            FactionThemeDefinition neighborTheme = context.HasNeighborFaction ? GetTheme(context.NeighborFaction) : null;

            GeneratedZoneChunk chunk = chunkBuilder.BuildChunk(context, plan, transform, primaryTheme, neighborTheme);
            if (chunk != null)
                activeChunks[plan.ZoneId] = chunk;
        }

        Debug.Log($"[ZoneProcGen] Generated template rig with {activeChunks.Count} chunk(s).");
    }

    [ContextMenu("Clear Generated Chunks")]
    public void ClearAllChunks()
    {
        foreach (var kv in activeChunks)
            chunkBuilder?.DestroyChunk(kv.Value);

        activeChunks.Clear();
    }

    public GeneratedZoneChunk GenerateZone(ZoneNode zone)
    {
        if (!CanGenerate() || zone == null)
            return null;

        if (activeChunks.TryGetValue(zone.ZoneId, out var existing))
            return existing;

        ZoneGenerationContext context = BuildContext(zone);
        ZoneChunkPlan plan = layoutPlanner.PlanLayout(context, defaultProfile, specialParcelRules);

        FactionThemeDefinition primaryTheme = GetTheme(context.PrimaryFaction);
        FactionThemeDefinition neighborTheme = context.HasNeighborFaction ? GetTheme(context.NeighborFaction) : null;

        GeneratedZoneChunk chunk = chunkBuilder.BuildChunk(context, plan, transform, primaryTheme, neighborTheme);
        if (chunk != null)
            activeChunks[zone.ZoneId] = chunk;

        return chunk;
    }

    public ZoneGenerationContext BuildContext(ZoneNode zone)
    {
        var context = new ZoneGenerationContext
        {
            Zone = zone,
            ZoneSeed = BuildZoneSeed(zone),
            ZoneWorldOrigin = AxialToWorld(zone.Q, zone.R, zoneSpacing),
            TemplateKind = ZoneChunkTemplateKind.Default,
            PrimaryFaction = zone.Faction,
            Threat01 = mapFramework.Radius <= 0 ? 0f : Mathf.Clamp01((float)zone.ThreatLevel / (mapFramework.Radius + 1f)),
        };

        ZoneNode neighbor = FindNeighborWithDifferentFaction(zone);
        if (neighbor != null)
        {
            context.HasNeighborFaction = true;
            context.NeighborFaction = neighbor.Faction;
        }

        return context;
    }

    ZoneGenerationContext BuildTemplateContext(ZoneChunkTemplateKind templateKind, int index)
    {
        ZoneNode zone = new ZoneNode
        {
            Q = index * 3,
            R = 0,
            Ring = index,
            ThreatLevel = index + 1,
            Faction = (ZoneFaction)(index % Enum.GetValues(typeof(ZoneFaction)).Length),
            HasShopAfterZone = false,
            HasMiniboss = templateKind == ZoneChunkTemplateKind.VerticalArena,
            IsStartZone = index == 0,
            IsFinalZone = false,
            BaseReward = 0,
        };

        return new ZoneGenerationContext
        {
            Zone = zone,
            ZoneSeed = BuildZoneSeed(zone) + (int)templateKind * 997,
            ZoneWorldOrigin = new Vector3(index * templateTestSpacing, 0f, 0f),
            TemplateKind = templateKind,
            PrimaryFaction = zone.Faction,
            HasNeighborFaction = false,
            NeighborFaction = zone.Faction,
            Threat01 = Mathf.Clamp01(0.35f + index * 0.25f),
        };
    }

    bool CanGenerate()
    {
        ResolveServiceReferences();

        bool valid = mapFramework != null && defaultProfile != null && specialParcelRules != null && layoutPlanner != null && chunkBuilder != null;
        if (valid)
            return true;

        string missing =
            (mapFramework == null ? " MapFramework" : string.Empty) +
            (defaultProfile == null ? " DefaultProfile" : string.Empty) +
            (specialParcelRules == null ? " SpecialParcelRules" : string.Empty) +
            (layoutPlanner == null ? " LayoutPlanner" : string.Empty) +
            (chunkBuilder == null ? " ChunkBuilder" : string.Empty);

        Debug.LogWarning($"[ZoneProcGen] Generation skipped. Missing:{missing}", this);
        return false;
    }

    void ResolveServiceReferences()
    {
        if (layoutPlannerBehaviour != null)
            layoutPlanner = layoutPlannerBehaviour as IZoneLayoutPlanner;

        if (chunkBuilderBehaviour != null)
            chunkBuilder = chunkBuilderBehaviour as IZoneChunkBuilder;

        if (mapFramework == null)
            mapFramework = FindFirstObjectByType<HexZoneMapFramework>();
    }

    int BuildZoneSeed(ZoneNode zone)
    {
        int seed = 17;
        seed = seed * 31 + zone.Q;
        seed = seed * 31 + zone.R;
        seed = seed * 31 + (int)zone.Faction;
        seed = seed * 31 + zone.ThreatLevel;
        return seed;
    }

    ZoneNode FindNeighborWithDifferentFaction(ZoneNode zone)
    {
        if (mapFramework == null || zone == null)
            return null;

        int[,] dirs = { { 1, 0 }, { 0, 1 }, { -1, 1 }, { -1, 0 }, { 0, -1 }, { 1, -1 } };
        for (int i = 0; i < 6; i++)
        {
            int nq = zone.Q + dirs[i, 0];
            int nr = zone.R + dirs[i, 1];
            ZoneNode neighbor = mapFramework.TryGetZone(nq, nr);
            if (neighbor != null && neighbor.Faction != zone.Faction)
                return neighbor;
        }

        return null;
    }

    FactionThemeDefinition GetTheme(ZoneFaction faction)
    {
        return factionThemes.FirstOrDefault(theme => theme != null && theme.faction == faction);
    }

    static Vector3 AxialToWorld(int q, int r, float spacing)
    {
        const float sqrt3 = 1.7320508f;
        float x = spacing * (sqrt3 * q + (sqrt3 * 0.5f) * r);
        float z = spacing * (1.5f * r);
        return new Vector3(x, 0f, z);
    }
}
