using System;
using UnityEngine;

/// <summary>
/// Baseline deterministic lot planner.
/// Produces core lots, border blend lots, plus reserved shop/arena parcels.
/// </summary>
public class GridZoneLayoutPlanner : MonoBehaviour, IZoneLayoutPlanner
{
    public ZoneChunkPlan PlanLayout(ZoneGenerationContext context, ZoneGenerationProfile profile, SpecialParcelRules specialRules)
    {
        var rng = new System.Random(context.ZoneSeed);
        var plan = new ZoneChunkPlan
        {
            ZoneId = context.Zone.ZoneId,
            Seed = context.ZoneSeed,
            ZoneFootprint = profile.zoneSize,
            TemplateKind = context.TemplateKind,
        };

        float width = profile.zoneSize.x;
        float depth = profile.zoneSize.y;
        int lotsPerAxis = Mathf.Max(3, profile.lotsPerAxis);
        float cellW = width / lotsPerAxis;
        float cellD = depth / lotsPerAxis;
        int id = 0;

        bool reservedShop = false;
        bool reservedArena = false;

        int shopX = Mathf.Clamp((int)Mathf.Lerp(1, lotsPerAxis - 2, specialRules.shopNearCenterBias), 1, lotsPerAxis - 2);
        int shopY = Mathf.Clamp((int)Mathf.Lerp(1, lotsPerAxis - 2, 1f - specialRules.shopNearCenterBias), 1, lotsPerAxis - 2);
        int arenaX = Mathf.Clamp((int)Mathf.Lerp(1, lotsPerAxis - 2, specialRules.arenaNearCenterBias), 1, lotsPerAxis - 2);
        int arenaY = Mathf.Clamp((int)Mathf.Lerp(1, lotsPerAxis - 2, specialRules.arenaNearCenterBias), 1, lotsPerAxis - 2);

        for (int y = 0; y < lotsPerAxis; y++)
        {
            for (int x = 0; x < lotsPerAxis; x++)
            {
                var cellCenter = new Vector3(-width * 0.5f + cellW * (x + 0.5f), 0f, -depth * 0.5f + cellD * (y + 0.5f));
                float hexM = HexMeasure(cellCenter.x, cellCenter.z, profile.hexOuterRadius);
                if (hexM > 1f)
                    continue;

                bool isBorder = hexM > (1f - profile.borderBlendBand);
                float blendWeight = isBorder
                    ? Mathf.Clamp01((hexM - (1f - profile.borderBlendBand)) / profile.borderBlendBand)
                    : 0f;

                ZoneParcelType parcelType = ResolveParcelType(context, rng, profile, x, y, lotsPerAxis, shopX, shopY, arenaX, arenaY, isBorder, ref reservedShop, ref reservedArena);

                bool isBuilding = parcelType == ZoneParcelType.CoreLot || parcelType == ZoneParcelType.BorderBlendLot;
                float fill = isBuilding
                    ? Mathf.Lerp(profile.lotFillRange.x, profile.lotFillRange.y, (float)rng.NextDouble())
                    : 1f;

                var lot = new ZoneLotPlan
                {
                    Id = id++,
                    ParcelType = parcelType,
                    LocalCenter = cellCenter,
                    Size = new Vector2(cellW * fill, cellD * fill),
                    RotationY = 90f * rng.Next(0, 4),
                    PrimaryFaction = context.PrimaryFaction,
                    HasSecondaryFaction = context.HasNeighborFaction,
                    SecondaryFaction = context.NeighborFaction,
                    BlendWeight = blendWeight,
                };

                plan.Lots.Add(lot);
            }
        }

        return plan;
    }

    ZoneParcelType ResolveParcelType(
        ZoneGenerationContext context,
        System.Random rng,
        ZoneGenerationProfile profile,
        int x,
        int y,
        int lotsPerAxis,
        int shopX,
        int shopY,
        int arenaX,
        int arenaY,
        bool isBorder,
        ref bool reservedShop,
        ref bool reservedArena)
    {
        if (!reservedArena && context.Zone.HasMiniboss && Mathf.Abs(x - arenaX) <= 1 && Mathf.Abs(y - arenaY) <= 1)
        {
            reservedArena = true;
            return ZoneParcelType.MinibossArena;
        }

        if (!reservedShop && context.Zone.HasShopAfterZone && x == shopX && y == shopY)
        {
            reservedShop = true;
            return ZoneParcelType.ShopParcel;
        }

        switch (context.TemplateKind)
        {
            case ZoneChunkTemplateKind.Chase:
                return ResolveChaseParcel(rng, profile, x, y, lotsPerAxis, isBorder);
            case ZoneChunkTemplateKind.VerticalArena:
                return ResolveVerticalArenaParcel(rng, profile, x, y, lotsPerAxis, isBorder);
            case ZoneChunkTemplateKind.GrappleCathedral:
                return ResolveGrappleCathedralParcel(rng, profile, x, y, lotsPerAxis, isBorder);
            case ZoneChunkTemplateKind.Default:
            default:
                return ResolveDefaultParcel(rng, profile, isBorder, x, y);
        }
    }

    static ZoneParcelType ResolveDefaultParcel(System.Random rng, ZoneGenerationProfile profile, bool isBorder, int x, int y)
    {
        ZoneParcelType parcelType = isBorder ? ZoneParcelType.BorderBlendLot : ZoneParcelType.CoreLot;

        int period = Mathf.Max(2, profile.roadGridPeriod);
        int rowOffset = (y % 2 == 0) ? 0 : period / 2;
        bool onStaggeredRoad = (x + rowOffset) % period == 0;
        float roadChance = Mathf.Lerp(profile.roadCoverage, onStaggeredRoad ? 1f : 0f, profile.roadStaggerStrength);

        bool useRoad = rng.NextDouble() < roadChance;
        bool useOpen = !useRoad && rng.NextDouble() < profile.openSpaceCoverage;

        if (useRoad) parcelType = ZoneParcelType.Road;
        else if (useOpen) parcelType = ZoneParcelType.OpenSpace;

        return parcelType;
    }

    static ZoneParcelType ResolveChaseParcel(System.Random rng, ZoneGenerationProfile profile, int x, int y, int lotsPerAxis, bool isBorder)
    {
        int mid = lotsPerAxis / 2;
        bool onSpine = y == mid;
        bool onRibbon = x == mid - 1 || x == mid + 1;
        bool onShortcut = x == lotsPerAxis - 2 && y >= mid - 1;

        if (onSpine)
            return ZoneParcelType.Road;

        if (onRibbon || onShortcut)
            return ZoneParcelType.OpenSpace;

        if (isBorder)
            return ZoneParcelType.BorderBlendLot;

        return rng.NextDouble() < 0.5 ? ZoneParcelType.CoreLot : ZoneParcelType.OpenSpace;
    }

    static ZoneParcelType ResolveVerticalArenaParcel(System.Random rng, ZoneGenerationProfile profile, int x, int y, int lotsPerAxis, bool isBorder)
    {
        int mid = lotsPerAxis / 2;
        bool center = Mathf.Abs(x - mid) <= 1 && Mathf.Abs(y - mid) <= 1;
        bool ring = Mathf.Abs(x - mid) == 2 || Mathf.Abs(y - mid) == 2;

        if (center)
            return ZoneParcelType.MinibossArena;

        if (ring)
            return ZoneParcelType.CoreLot;

        if (isBorder)
            return ZoneParcelType.BorderBlendLot;

        return (x + y) % 2 == 0 ? ZoneParcelType.Road : ZoneParcelType.OpenSpace;
    }

    static ZoneParcelType ResolveGrappleCathedralParcel(System.Random rng, ZoneGenerationProfile profile, int x, int y, int lotsPerAxis, bool isBorder)
    {
        bool mainPath = x == 1 || x == lotsPerAxis - 2 || y == 1 || y == lotsPerAxis - 2;
        bool anchorNode = x % 2 == 1 && y % 2 == 1;

        if (mainPath)
            return ZoneParcelType.Road;

        if (anchorNode)
            return ZoneParcelType.CoreLot;

        if (isBorder)
            return ZoneParcelType.BorderBlendLot;

        return rng.NextDouble() < 0.35 ? ZoneParcelType.OpenSpace : ZoneParcelType.CoreLot;
    }

    // Returns 0 at center, 1 at hex boundary, >1 outside. Flat-top hex (vertices on ±X axis).
    static float HexMeasure(float px, float pz, float R)
    {
        const float sqrt3Half = 0.8660254f;
        float ax = Mathf.Abs(px) / R;
        float az = Mathf.Abs(pz) / (R * sqrt3Half);
        float diag = (Mathf.Abs(px) * sqrt3Half + Mathf.Abs(pz) * 0.5f) / (R * sqrt3Half);
        return Mathf.Max(ax, az, diag);
    }
}
