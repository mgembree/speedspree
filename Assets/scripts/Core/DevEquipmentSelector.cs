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
        typeof(WallRunAbility),
        typeof(BallAndChainAbility),
        typeof(TeleportAbility),
        typeof(WeaponSwapAbility),
    };

    static readonly System.Type[] WeaponTypes = new System.Type[]
    {
        typeof(Sword),
        typeof(Pistol),
        typeof(RecoilHammer),
        typeof(ForgeCannon),
        typeof(ArcRifle),
        typeof(PursuitCarbine),
        typeof(WristDartLauncher),
        typeof(SolarCenser),
    };

    bool showPanel = false;
    Rect windowRect = new Rect(20, 20, 460, 680);
    Vector2 scrollPos;
    GameObject player;
    WeaponController weaponController;

    static readonly WeaponAttachmentPreset[] AttachmentPresets = new WeaponAttachmentPreset[]
    {
        WeaponAttachmentPreset.LaserFocuser,
        WeaponAttachmentPreset.ExtendedMag,
        WeaponAttachmentPreset.EnlargedWeapon,
        WeaponAttachmentPreset.RecoilDamper,
        WeaponAttachmentPreset.QuickRack,
        WeaponAttachmentPreset.HeatSink,
        WeaponAttachmentPreset.SmartLink,
        WeaponAttachmentPreset.ChainLink,
        WeaponAttachmentPreset.Stabilizer,
        WeaponAttachmentPreset.OverdriveCell,
    };

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

        if (weaponController == null && player != null)
            weaponController = player.GetComponent<WeaponController>();

        if (player == null)
        {
            GUILayout.Label("Player not found.");
            GUI.DragWindow();
            return;
        }

        scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUILayout.Height(windowRect.height - 38f));

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
                    if (type == typeof(BallAndChainAbility) && player.GetComponent<BallAndChainVisuals>() == null)
                        player.AddComponent<BallAndChainVisuals>();
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
                    if (type == typeof(BallAndChainAbility))
                    {
                        var vis = player.GetComponent<BallAndChainVisuals>();
                        if (vis != null) Destroy(vis);
                    }
                    Destroy(comp);
                }
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(14);
        GUILayout.Label("── Weapon Attachments ──");

        WeaponBase currentWeapon = weaponController != null ? weaponController.CurrentWeapon : null;
        if (currentWeapon == null)
        {
            GUILayout.Label("No weapon equipped.");
        }
        else
        {
            GUILayout.Label($"Current Weapon: {currentWeapon.WeaponName} ({currentWeapon.Category})");

            WeaponAttachmentModifier[] attachments = currentWeapon.GetComponents<WeaponAttachmentModifier>();
            if (attachments.Length > 0)
            {
                GUILayout.Label("Mounted Attachments:");
                foreach (var attachment in attachments)
                {
                    if (attachment == null) continue;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("  " + attachment.DisplayName, GUILayout.Width(180));
                    GUILayout.Label(attachment.Rarity.ToString(), GUILayout.Width(80));
                    if (GUILayout.Button("R+", GUILayout.Width(35)))
                        CycleAttachmentRarity(attachment);
                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        Destroy(attachment);
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(6);
            GUILayout.Label("Add Attachments:");
            foreach (var preset in AttachmentPresets)
            {
                bool alreadyMounted = WeaponAttachmentQuery.HasPreset(currentWeapon, preset);
                GUILayout.BeginHorizontal();
                GUILayout.Label(preset.ToString(), GUILayout.Width(180));
                if (GUILayout.Button(alreadyMounted ? "Mounted" : "Add", GUILayout.Width(70)) && !alreadyMounted)
                    AddAttachmentToCurrentWeapon(currentWeapon, preset);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            if (GUILayout.Button("Clear All Attachments"))
                ClearAttachments(currentWeapon);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Set All Rarity", GUILayout.Width(120));
            foreach (AttachmentRarity rarity in System.Enum.GetValues(typeof(AttachmentRarity)))
            {
                if (GUILayout.Button(rarity.ToString(), GUILayout.Width(75)))
                    SetAllAttachmentRarity(currentWeapon, rarity);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(12);
            GUILayout.Label("── Weapon Test Spawn ──");
            GUILayout.Label("Spawn a real weapon entry and equip it immediately for testing.");
            foreach (var weaponType in WeaponTypes)
            {
                bool alreadyHas = weaponController != null && weaponController.Weapons != null && HasWeaponType(weaponType);
                GUILayout.BeginHorizontal();
                GUILayout.Label(weaponType.Name, GUILayout.Width(180));
                if (GUILayout.Button(alreadyHas ? "Spawned" : "Spawn", GUILayout.Width(70)) && !alreadyHas)
                    SpawnWeaponForTesting(weaponType);
                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Remove Spawned Test Weapons"))
                RemoveSpawnedTestWeapons();

            GUILayout.Space(6);
            GUILayout.Label("Quick Notes:");
            GUILayout.Label("Laser Focuser: lasers/energy only");
            GUILayout.Label("Extended Mag: guns/rifles only");
            GUILayout.Label("Chain Link: heavy melee only");
            GUILayout.Label("R+ cycles attachment rarity");
        }

        GUILayout.EndScrollView();

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

    void AddAttachmentToCurrentWeapon(WeaponBase currentWeapon, WeaponAttachmentPreset preset)
    {
        if (currentWeapon == null) return;

        var attachment = currentWeapon.gameObject.AddComponent<WeaponAttachmentModifier>();
        attachment.Configure(preset);
    }

    void ClearAttachments(WeaponBase currentWeapon)
    {
        if (currentWeapon == null) return;

        foreach (var attachment in currentWeapon.GetComponents<WeaponAttachmentModifier>())
        {
            if (attachment != null)
                Destroy(attachment);
        }
    }

    void CycleAttachmentRarity(WeaponAttachmentModifier attachment)
    {
        if (attachment == null) return;

        int count = System.Enum.GetValues(typeof(AttachmentRarity)).Length;
        int next = ((int)attachment.Rarity + 1) % count;
        attachment.SetRarity((AttachmentRarity)next);
    }

    void SetAllAttachmentRarity(WeaponBase currentWeapon, AttachmentRarity rarity)
    {
        if (currentWeapon == null) return;

        foreach (var attachment in currentWeapon.GetComponents<WeaponAttachmentModifier>())
        {
            if (attachment != null)
                attachment.SetRarity(rarity);
        }
    }

    bool HasWeaponType(System.Type weaponType)
    {
        if (weaponController == null || weaponType == null)
            return false;

        foreach (var weapon in weaponController.Weapons)
        {
            if (weapon != null && weapon.GetType() == weaponType)
                return true;
        }

        return false;
    }

    void SpawnWeaponForTesting(System.Type weaponType)
    {
        if (weaponController == null || player == null || weaponType == null)
            return;

        var go = new GameObject("Dev_" + weaponType.Name);
        go.transform.SetParent(player.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        var weapon = go.AddComponent(weaponType) as WeaponBase;
        if (weapon == null)
        {
            Destroy(go);
            return;
        }

        weaponController.RegisterWeapon(weapon, true);
    }

    void RemoveSpawnedTestWeapons()
    {
        if (weaponController == null)
            return;

        var spawnedWeapons = new System.Collections.Generic.List<WeaponBase>();
        foreach (var weapon in weaponController.Weapons)
        {
            if (weapon != null && weapon.gameObject.name.StartsWith("Dev_"))
                spawnedWeapons.Add(weapon);
        }

        foreach (var weapon in spawnedWeapons)
        {
            weaponController.UnregisterWeapon(weapon);
            if (weapon != null)
                Destroy(weapon.gameObject);
        }
    }
}
