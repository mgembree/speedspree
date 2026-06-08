using UnityEngine;

public interface IZoneLayoutPlanner
{
    ZoneChunkPlan PlanLayout(ZoneGenerationContext context, ZoneGenerationProfile profile, SpecialParcelRules specialRules);
}

public interface IZoneChunkBuilder
{
    GeneratedZoneChunk BuildChunk(ZoneGenerationContext context, ZoneChunkPlan plan, Transform parent, FactionThemeDefinition primaryTheme, FactionThemeDefinition neighborTheme);
    void DestroyChunk(GeneratedZoneChunk chunk);
}
