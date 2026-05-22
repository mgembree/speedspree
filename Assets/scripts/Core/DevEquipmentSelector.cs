using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Runtime dev panel (toggle with Tab) to enable/disable equipment abilities on the Player.
/// Remove or disable this GameObject before shipping.
/// </summary>
public class DevEquipmentSelector : MonoBehaviour
{
    // All known equipment types — add new ones here as you build them
    static readonly System.Type[] EquipmentTypes = new System.Type[]
    {
        typeof(DashAbility),
        typeof(JetpackAbility),
        typeof(ExplosiveBootsAbility),
        typeof(GrappleAbility),
    };

    bool showPanel = false;
    Rect windowRect = new Rect(20, 20, 280, 400);
    GameObject player;

void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame)
        {
            showPanel = !showPanel;
            Cursor.lockState = showPanel ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = showPanel;
        }
    }

    void OnGUI()
    {
        if (!showPanel) return;
        windowRect = GUI.Window(9999, windowRect, DrawWindow, "⚙ Equipment Selector [Tab]");
    }

    void DrawWindow(int id)
    {
        if (player == null)
            player = GameObject.Find("Player");

        if (player == null)
        {
            GUILayout.Label("Player not found.");
            GUI.DragWindow();
            return;
        }

        GUILayout.Space(4);
        GUILayout.Label("Q Slot", EditorStyleBold());
        DrawSlot(0);

        GUILayout.Space(8);
        GUILayout.Label("E Slot", EditorStyleBold());
        DrawSlot(1);

        GUILayout.Space(12);
        GUILayout.Label("── Active Components ──");
        foreach (var type in EquipmentTypes)
        {
            var comp = player.GetComponent(type);
            if (comp != null)
            {
                var mb = comp as MonoBehaviour;
                bool active = mb != null && mb.enabled;
                GUILayout.BeginHorizontal();
                GUILayout.Label(type.Name + (active ? " ✓" : " ✗"), GUILayout.Width(180));
                if (mb != null && GUILayout.Button(active ? "Disable" : "Enable", GUILayout.Width(70)))
                    mb.enabled = !mb.enabled;
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.Space(8);
        GUILayout.Label("── Add / Remove ──");
        foreach (var type in EquipmentTypes)
        {
            var comp = player.GetComponent(type);
            GUILayout.BeginHorizontal();
            GUILayout.Label(type.Name, GUILayout.Width(160));
            if (comp == null)
            {
                if (GUILayout.Button("Add", GUILayout.Width(55)))
                {
                    player.AddComponent(type);
                    // Auto-add companion visuals component
                    if (type == typeof(GrappleAbility) && player.GetComponent<GrappleVisuals>() == null)
                        player.AddComponent<GrappleVisuals>();
                }
            }
            else
            {
                if (GUILayout.Button("Remove", GUILayout.Width(75)))
                {
                    // Remove companion visuals too
                    if (type == typeof(GrappleAbility))
                    {
                        var vis = player.GetComponent<GrappleVisuals>();
                        if (vis != null) Destroy(vis);
                    }
                    Destroy(comp);
                }
            }
            GUILayout.EndHorizontal();
        }

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    void DrawSlot(int slot)
    {
        foreach (var type in EquipmentTypes)
        {
            var comp = player.GetComponent(type) as MonoBehaviour;
            if (comp == null) continue;

            // Read slot via reflection if field exists
            var field = type.GetField("equipmentSlot",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null) continue;

            int compSlot = (int)field.GetValue(comp);
            if (compSlot != slot) continue;

            GUILayout.BeginHorizontal();
            GUILayout.Label("  " + type.Name, GUILayout.Width(180));
            if (GUILayout.Button("✕", GUILayout.Width(25)))
                Destroy(comp);
            GUILayout.EndHorizontal();
        }

        // Add button for this slot
        foreach (var type in EquipmentTypes)
        {
            if (player.GetComponent(type) != null) continue;

            if (GUILayout.Button("+ Add " + type.Name + " → Slot " + slot, GUILayout.Height(22)))
            {
                var newComp = player.AddComponent(type);
                var field = type.GetField("equipmentSlot",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(newComp, slot);
            }
        }
    }

    GUIStyle EditorStyleBold()
    {
        var s = new GUIStyle(GUI.skin.label);
        s.fontStyle = FontStyle.Bold;
        return s;
    }
}
