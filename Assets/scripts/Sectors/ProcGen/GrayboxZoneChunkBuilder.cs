using UnityEngine;

/// <summary>
/// Graybox chunk builder that spawns primitive lots as a placeholder implementation.
/// </summary>
public class GrayboxZoneChunkBuilder : MonoBehaviour, IZoneChunkBuilder
{
    [Header("Graybox")]
    [SerializeField] float baseHeight = 8f;
    [SerializeField] float threatHeightMultiplier = 26f;
    [SerializeField] bool spawnRoadPlanes = true;

    public GeneratedZoneChunk BuildChunk(ZoneGenerationContext context, ZoneChunkPlan plan, Transform parent, FactionThemeDefinition primaryTheme, FactionThemeDefinition neighborTheme)
    {
        if (context == null || plan == null)
            return null;

        var root = new GameObject($"ZoneChunk_{plan.ZoneId}");
        root.transform.SetParent(parent, false);
        root.transform.position = context.ZoneWorldOrigin;

        foreach (var lot in plan.Lots)
        {
            SpawnLot(root.transform, lot, context, primaryTheme, neighborTheme);
        }

        return new GeneratedZoneChunk
        {
            ZoneId = plan.ZoneId,
            Root = root,
            Plan = plan,
        };
    }

    public void DestroyChunk(GeneratedZoneChunk chunk)
    {
        if (chunk == null || chunk.Root == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(chunk.Root);
        else
#endif
            Destroy(chunk.Root);
    }

    void SpawnLot(Transform chunkRoot, ZoneLotPlan lot, ZoneGenerationContext context, FactionThemeDefinition primaryTheme, FactionThemeDefinition neighborTheme)
    {
        if (lot.ParcelType == ZoneParcelType.Road && !spawnRoadPlanes)
            return;

        PrimitiveType primitive = lot.ParcelType == ZoneParcelType.Road || lot.ParcelType == ZoneParcelType.OpenSpace
            ? PrimitiveType.Cube
            : PrimitiveType.Cube;

        var go = GameObject.CreatePrimitive(primitive);
        go.name = $"Lot_{lot.Id}_{lot.ParcelType}";
        go.transform.SetParent(chunkRoot, false);
        go.transform.localPosition = lot.LocalCenter;
        go.transform.localRotation = Quaternion.Euler(0f, lot.RotationY, 0f);

        float h = ResolveHeight(lot, context);
        go.transform.localScale = new Vector3(lot.Size.x, h, lot.Size.y);
        go.transform.localPosition += Vector3.up * (h * 0.5f);

        ApplyMaterial(go, lot, primaryTheme, neighborTheme);
    }

    float ResolveHeight(ZoneLotPlan lot, ZoneGenerationContext context)
    {
        switch (lot.ParcelType)
        {
            case ZoneParcelType.Road:
                return 0.3f;
            case ZoneParcelType.OpenSpace:
                return 0.15f;
            case ZoneParcelType.ShopParcel:
                return 4f;
            case ZoneParcelType.MinibossArena:
                return 1.2f;
            case ZoneParcelType.BorderBlendLot:
                return baseHeight + threatHeightMultiplier * context.Threat01 * 0.7f;
            case ZoneParcelType.CoreLot:
            default:
                return baseHeight + threatHeightMultiplier * context.Threat01;
        }
    }

    void ApplyMaterial(GameObject go, ZoneLotPlan lot, FactionThemeDefinition primaryTheme, FactionThemeDefinition neighborTheme)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null)
            return;

        Material mat = null;

        if (lot.ParcelType == ZoneParcelType.Road)
            mat = primaryTheme != null ? primaryTheme.roadMaterial : null;
        else if (lot.ParcelType == ZoneParcelType.MinibossArena)
            mat = primaryTheme != null ? primaryTheme.arenaMaterial : null;
        else if (lot.ParcelType == ZoneParcelType.BorderBlendLot && lot.HasSecondaryFaction && neighborTheme != null)
            mat = neighborTheme.borderBlendMaterial != null ? neighborTheme.borderBlendMaterial : neighborTheme.primaryBuildingMaterial;
        else
            mat = primaryTheme != null ? primaryTheme.primaryBuildingMaterial : null;

        if (mat != null)
            renderer.sharedMaterial = mat;
    }
}
