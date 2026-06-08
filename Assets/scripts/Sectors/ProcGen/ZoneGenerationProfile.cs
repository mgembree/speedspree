using UnityEngine;

[CreateAssetMenu(menuName = "SpeedSpree/ProcGen/Zone Generation Profile", fileName = "ZoneGenerationProfile")]
public class ZoneGenerationProfile : ScriptableObject
{
    [Header("Zone Footprint")]
    public Vector2 zoneSize = new Vector2(140f, 140f);
    public float hexOuterRadius = 70f;

    [Header("Lot Grid")]
    [Min(3)] public int lotsPerAxis = 8;
    [Range(0f, 0.45f)] public float borderBlendBand = 0.22f;
    [Range(0f, 1f)] public float roadCoverage = 0.18f;
    [Range(0f, 1f)] public float openSpaceCoverage = 0.08f;

    [Header("Building Spacing")]
    public Vector2 lotFillRange = new Vector2(0.75f, 0.92f);

    [Header("Road Layout")]
    [Min(2)] public int roadGridPeriod = 3;
    [Range(0f, 1f)] public float roadStaggerStrength = 0.5f;

    [Header("Building Grammar")]
    public Vector2 buildingWidthRange = new Vector2(8f, 16f);
    public Vector2 buildingDepthRange = new Vector2(8f, 16f);
    public Vector2 buildingHeightRange = new Vector2(8f, 34f);

    [Header("Props")]
    [Range(0f, 1f)] public float propSpawnChance = 0.25f;
}
