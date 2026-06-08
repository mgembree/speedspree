using UnityEngine;

[CreateAssetMenu(menuName = "SpeedSpree/ProcGen/Special Parcel Rules", fileName = "SpecialParcelRules")]
public class SpecialParcelRules : ScriptableObject
{
    [Header("Shop Parcel")]
    public Vector2 shopParcelSize = new Vector2(16f, 16f);
    [Range(0f, 1f)] public float shopNearCenterBias = 0.65f;
    public float shopClearRadius = 10f;

    [Header("Miniboss Arena")]
    public Vector2 arenaSize = new Vector2(34f, 34f);
    public float arenaClearRadius = 22f;
    [Range(0f, 1f)] public float arenaNearCenterBias = 0.85f;
}
