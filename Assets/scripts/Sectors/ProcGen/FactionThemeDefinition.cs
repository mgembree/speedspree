using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SpeedSpree/ProcGen/Faction Theme Definition", fileName = "FactionThemeDefinition")]
public class FactionThemeDefinition : ScriptableObject
{
    [Header("Identity")]
    public ZoneFaction faction;
    public string displayName;
    [TextArea(2, 4)] public string styleSummary;

    [Header("Look")]
    public Material primaryBuildingMaterial;
    public Material borderBlendMaterial;
    public Material roadMaterial;
    public Material arenaMaterial;
    public Color accentColor = Color.white;

    [Header("Modular Sets")]
    public List<GameObject> coreBuildingPrefabs = new();
    public List<GameObject> borderBuildingPrefabs = new();
    public List<GameObject> propPrefabs = new();
    public List<GameObject> shopSignPrefabs = new();
    public List<GameObject> arenaPropPrefabs = new();
}
