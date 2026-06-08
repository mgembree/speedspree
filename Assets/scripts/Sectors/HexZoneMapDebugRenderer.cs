using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Runtime debug renderer for HexZoneMapFramework.
/// Press M to toggle map visibility.
/// </summary>
public class HexZoneMapDebugRenderer : MonoBehaviour
{
    struct RenderZone
    {
        public ZoneNode Zone;
        public Vector2 Center;
        public Vector2[] Corners;
    }

    [Header("References")]
    [SerializeField] HexZoneMapFramework map;
    [SerializeField] FactionSpecializationLibrary factionLibrary;

    [Header("Toggle")]
    [SerializeField] bool visibleOnStart = false;

    [Header("Layout")]
    [SerializeField] Vector2 panelMargin = new Vector2(24f, 24f);
    [SerializeField] Vector2 panelSize = new Vector2(1050f, 760f);
    [SerializeField] float nodeScale = 28f;
    [SerializeField] float hexRadius = 34f;
    [SerializeField] float hexLineWidth = 2f;

    bool visible;
    ZoneNode selectedZone;
    readonly List<RenderZone> renderZones = new();
    GUIStyle titleStyle;
    GUIStyle tileStyle;
    GUIStyle infoStyle;
    GUIStyle markerStyle;
    Texture2D fillTexture;

    void Awake()
    {
        if (map == null)
            map = FindFirstObjectByType<HexZoneMapFramework>();

        if (factionLibrary == null)
            factionLibrary = FindFirstObjectByType<FactionSpecializationLibrary>();

        visible = visibleOnStart;
        fillTexture = Texture2D.whiteTexture;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            ToggleMap();
    }

    void ToggleMap()
    {
        visible = !visible;

        if (!visible || map == null)
            return;

        if (map.Zones == null || map.Zones.Count == 0)
            map.GenerateMapFromInspector();

        selectedZone = map.StartZone;
    }

    void OnGUI()
    {
        if (!visible || map == null)
            return;

        EnsureStyles();

        Rect panel = new Rect(panelMargin.x, panelMargin.y, panelSize.x, panelSize.y);
        GUI.Box(panel, GUIContent.none);

        Rect titleRect = new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, 24f);
        GUI.Label(titleRect, "Run Map (Press M to close)", titleStyle);

        Rect mapRect = new Rect(panel.x + 16f, panel.y + 38f, panel.width * 0.68f, panel.height - 56f);
        Rect infoRect = new Rect(mapRect.xMax + 14f, mapRect.y, panel.xMax - (mapRect.xMax + 26f), mapRect.height);

        DrawMap(mapRect);
        DrawInfo(infoRect);
    }

    void DrawMap(Rect rect)
    {
        var zones = map.Zones;
        if (zones == null || zones.Count == 0)
        {
            GUI.Label(rect, "No zones generated.", infoStyle);
            return;
        }

        var points = new Dictionary<ZoneNode, Vector2>(zones.Count);
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (var z in zones)
        {
            Vector2 p = AxialToPixel(z.Q, z.R, nodeScale);
            points[z] = p;

            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        Vector2 mapCenter = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        Vector2 screenCenter = rect.center;
        renderZones.Clear();

        foreach (var z in zones.OrderByDescending(z => z.Ring))
        {
            Vector2 center = points[z] - mapCenter + screenCenter;
            Vector2[] corners = BuildHexCorners(center, hexRadius);

            renderZones.Add(new RenderZone
            {
                Zone = z,
                Center = center,
                Corners = corners,
            });

            DrawZoneTile(center, corners, z);
        }

        HandleMapClick(rect);
    }

    void HandleMapClick(Rect mapRect)
    {
        Event e = Event.current;
        if (e == null || e.type != EventType.MouseDown || e.button != 0)
            return;

        if (!mapRect.Contains(e.mousePosition))
            return;

        for (int i = renderZones.Count - 1; i >= 0; i--)
        {
            var item = renderZones[i];
            if (PointInPolygon(e.mousePosition, item.Corners))
            {
                selectedZone = item.Zone;
                e.Use();
                return;
            }
        }
    }

    void DrawZoneTile(Vector2 center, Vector2[] corners, ZoneNode zone)
    {
        Color fill = GetFactionColor(zone.Faction);
        fill.a = 0.35f;
        DrawPolygonFill(center, hexRadius * 1.18f, fill);

        bool isSelected = zone == selectedZone;
        Color lineColor = isSelected ? Color.white : new Color(0.09f, 0.09f, 0.09f, 1f);
        float lineWidth = isSelected ? hexLineWidth + 1f : hexLineWidth;
        DrawHexOutline(corners, lineColor, lineWidth);

        string mainText = $"{zone.Faction}\nT{zone.ThreatLevel}";
        if (zone.IsFinalZone) mainText += "\nFINAL";
        else if (zone.IsStartZone) mainText += "\nSTART";

        Rect labelRect = new Rect(center.x - 26f, center.y - 20f, 52f, 40f);
        GUI.Label(labelRect, mainText, tileStyle);

        if (zone.BaseReward > 0)
            GUI.Label(new Rect(center.x - 20f, center.y + 20f, 40f, 14f), zone.BaseReward.ToString(), markerStyle);

        if (zone.HasShopAfterZone)
            GUI.Label(new Rect(center.x + 14f, center.y - 30f, 14f, 14f), "$", markerStyle);

        if (zone.HasMiniboss)
            GUI.Label(new Rect(center.x - 28f, center.y - 30f, 20f, 14f), "M", markerStyle);
    }

    void DrawInfo(Rect rect)
    {
        GUI.Box(rect, GUIContent.none);

        float y = rect.y + 10f;
        float x = rect.x + 10f;

        ZoneNode start = map.StartZone;
        ZoneNode final = map.FinalZone;

        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), "Legend", titleStyle); y += 26f;

        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), "Faction: Tile color + letter", infoStyle); y += 20f;
        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), "Threat: T#", infoStyle); y += 20f;
        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), "Base reward shown under faction", infoStyle); y += 20f;
        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), "Shop after zone: $", infoStyle); y += 20f;
        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), "Miniboss: M", infoStyle); y += 30f;

        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), "Run Info", titleStyle); y += 26f;
        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), $"Start: {FormatZone(start)}", infoStyle); y += 20f;
        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), $"Final: {FormatZone(final)}", infoStyle); y += 20f;
        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), $"Total Zones: {map.Zones.Count}", infoStyle); y += 20f;
        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), $"Shops: {map.Zones.Count(z => z.HasShopAfterZone)}", infoStyle); y += 20f;
        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), $"Minibosses: {map.Zones.Count(z => z.HasMiniboss)}", infoStyle); y += 30f;

        ZoneNode inspect = selectedZone ?? start;
        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), "Selected Zone", titleStyle); y += 26f;

        if (inspect == null)
        {
            GUI.Label(new Rect(x, y, rect.width - 16f, 20f), "Click a hex to inspect", infoStyle);
            return;
        }

        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), $"Zone: {inspect.ZoneId}", infoStyle); y += 20f;
        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), $"Faction: {inspect.Faction}", infoStyle); y += 20f;
        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), $"Threat: T{inspect.ThreatLevel}", infoStyle); y += 20f;
        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), $"Base Reward: {inspect.BaseReward}", infoStyle); y += 20f;
        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), $"Shop After: {(inspect.HasShopAfterZone ? "Yes" : "No")}", infoStyle); y += 20f;
        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), $"Miniboss: {(inspect.HasMiniboss ? "Yes" : "No")}", infoStyle); y += 26f;

        ZoneFaction playerFaction = map.PlayerChosenFaction;
        int payout = map.GetZoneRewardForFaction(inspect, playerFaction);
        float mult = map.GetRewardMultiplier(playerFaction, inspect.Faction);
        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), $"Payout ({playerFaction}): {payout} ({mult:F2}x)", infoStyle); y += 24f;

        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), "Faction Payouts", titleStyle); y += 24f;
        foreach (ZoneFaction f in System.Enum.GetValues(typeof(ZoneFaction)))
        {
            int factionPayout = map.GetZoneRewardForFaction(inspect, f);
            float factionMult = map.GetRewardMultiplier(f, inspect.Faction);
            GUI.Label(new Rect(x, y, rect.width - 16f, 20f), $"{f}: {factionPayout} ({factionMult:F2}x)", infoStyle);
            y += 18f;
        }

        if (factionLibrary == null)
            return;

        FactionSpecializationDefinition def = factionLibrary.GetDefinition(inspect.Faction);
        if (def == null)
            return;

        y += 14f;
        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), "Faction Identity", titleStyle); y += 24f;
        y = DrawWrappedLabel(new Rect(x, y, rect.width - 16f, 60f), def.DisplayName, titleStyle) + 2f;
        y = DrawWrappedLabel(new Rect(x, y, rect.width - 16f, 88f), def.Identity, infoStyle) + 4f;
        y = DrawWrappedLabel(new Rect(x, y, rect.width - 16f, 96f), def.Doctrine, infoStyle) + 8f;

        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), "Current Weapons", titleStyle); y += 22f;
        y = DrawWrappedLabel(new Rect(x, y, rect.width - 16f, 60f), string.Join(", ", def.ExistingWeapons), infoStyle) + 8f;

        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), "New Weapon Ideas", titleStyle); y += 22f;
        y = DrawWrappedLabel(new Rect(x, y, rect.width - 16f, 72f), string.Join(", ", def.NewWeaponIdeas), infoStyle) + 8f;

        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), "Signature Weapons", titleStyle); y += 22f;
        y = DrawWrappedLabel(new Rect(x, y, rect.width - 16f, 60f), string.Join(", ", def.SignatureWeapons), infoStyle) + 8f;

        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), "Specialized Equipment", titleStyle); y += 22f;
        y = DrawWrappedLabel(new Rect(x, y, rect.width - 16f, 60f), string.Join(", ", def.SpecializedEquipment), infoStyle) + 8f;

        GUI.Label(new Rect(x, y, rect.width - 16f, 20f), "Implemented Hooks", titleStyle); y += 22f;
        DrawWrappedLabel(new Rect(x, y, rect.width - 16f, 60f), string.Join(", ", def.ImplementedEquipment), infoStyle);
    }

    static string FormatZone(ZoneNode zone)
    {
        if (zone == null)
            return "none";

        return $"{zone.ZoneId} [{zone.Faction}] T{zone.ThreatLevel}";
    }

    static Vector2 AxialToPixel(int q, int r, float scale)
    {
        const float sqrt3 = 1.7320508f;
        float x = scale * (sqrt3 * q + (sqrt3 * 0.5f) * r);
        float y = scale * (1.5f * r);
        return new Vector2(x, y);
    }

    static Vector2[] BuildHexCorners(Vector2 center, float radius)
    {
        var corners = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float angleDeg = 60f * i - 30f;
            float angle = angleDeg * Mathf.Deg2Rad;
            corners[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        return corners;
    }

    void DrawHexOutline(Vector2[] corners, Color color, float width)
    {
        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 a = corners[i];
            Vector2 b = corners[(i + 1) % corners.Length];
            DrawLine(a, b, color, width);
        }
    }

    void DrawPolygonFill(Vector2 center, float radius, Color color)
    {
        Color prev = GUI.color;
        GUI.color = color;
        Rect r = new Rect(center.x - radius * 0.55f, center.y - radius * 0.55f, radius * 1.1f, radius * 1.1f);
        GUI.DrawTexture(r, fillTexture);
        GUI.color = prev;
    }

    static bool PointInPolygon(Vector2 p, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];

            bool intersects = ((a.y > p.y) != (b.y > p.y)) &&
                              (p.x < (b.x - a.x) * (p.y - a.y) / Mathf.Max(0.0001f, b.y - a.y) + a.x);
            if (intersects)
                inside = !inside;
        }

        return inside;
    }

    static void DrawLine(Vector2 a, Vector2 b, Color color, float width)
    {
        Matrix4x4 prevMatrix = GUI.matrix;
        Color prevColor = GUI.color;

        Vector2 delta = b - a;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        float length = delta.magnitude;

        GUI.color = color;
        GUIUtility.RotateAroundPivot(angle, a);
        GUI.DrawTexture(new Rect(a.x, a.y - width * 0.5f, length, width), Texture2D.whiteTexture);

        GUI.matrix = prevMatrix;
        GUI.color = prevColor;
    }

    static Color GetFactionColor(ZoneFaction faction)
    {
        switch (faction)
        {
            case ZoneFaction.A: return new Color(0.88f, 0.45f, 0.45f, 0.92f);
            case ZoneFaction.B: return new Color(0.88f, 0.78f, 0.42f, 0.92f);
            case ZoneFaction.C: return new Color(0.45f, 0.77f, 0.50f, 0.92f);
            case ZoneFaction.D: return new Color(0.43f, 0.64f, 0.90f, 0.92f);
            case ZoneFaction.E: return new Color(0.74f, 0.53f, 0.86f, 0.92f);
            default: return new Color(0.85f, 0.85f, 0.85f, 0.92f);
        }
    }

    static float DrawWrappedLabel(Rect rect, string text, GUIStyle style)
    {
        GUI.Label(rect, text, style);
        return rect.y + style.CalcHeight(new GUIContent(text), rect.width);
    }

    void EnsureStyles()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        if (tileStyle == null)
        {
            tileStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black }
            };
        }

        if (infoStyle == null)
        {
            infoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };
        }

        if (markerStyle == null)
        {
            markerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
    }
}
