using System;
using System.Collections.Generic;
using UnityEngine;

public enum ZoneParcelType
{
    Road,
    CoreLot,
    BorderBlendLot,
    ShopParcel,
    MinibossArena,
    OpenSpace,
}

public enum ZoneChunkTemplateKind
{
    Default,
    Chase,
    VerticalArena,
    GrappleCathedral,
}

[Serializable]
public class ZoneGenerationContext
{
    public ZoneNode Zone;
    public int ZoneSeed;
    public Vector3 ZoneWorldOrigin;
    public ZoneChunkTemplateKind TemplateKind;
    public ZoneFaction PrimaryFaction;
    public bool HasNeighborFaction;
    public ZoneFaction NeighborFaction;
    public float Threat01;
}

[Serializable]
public class ZoneLotPlan
{
    public int Id;
    public ZoneParcelType ParcelType;
    public Vector3 LocalCenter;
    public Vector2 Size;
    public float RotationY;
    public ZoneFaction PrimaryFaction;
    public bool HasSecondaryFaction;
    public ZoneFaction SecondaryFaction;
    [Range(0f, 1f)] public float BlendWeight;
}

[Serializable]
public class ZoneChunkPlan
{
    public string ZoneId;
    public int Seed;
    public Vector2 ZoneFootprint;
    public ZoneChunkTemplateKind TemplateKind;
    public List<ZoneLotPlan> Lots = new();
}

[Serializable]
public class GeneratedZoneChunk
{
    public string ZoneId;
    public GameObject Root;
    public ZoneChunkPlan Plan;
}
