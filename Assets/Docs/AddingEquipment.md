# Adding New Equipment

This guide covers how to create a new equipment ability and wire it up to the in-game dev panel so it can be tested at runtime.

---

## 1. Create the Ability Script

Create a new `MonoBehaviour` under `Assets/scripts/Player/Equipment/`.

### Required structure

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// One-line description of what this equipment does.
/// Bind equipmentSlot: 0 = Q, 1 = E.
/// </summary>
[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(PlayerMovementController))]
public class MyNewAbility : MonoBehaviour
{
    // ── Serialized settings ────────────────────────────────────────────────
    [Header("Feel")]
    [SerializeField] float someForce = 10f;

    [Header("Input")]
    [Tooltip("Which equipment slot this is bound to: 0 = Q, 1 = E")]
    [SerializeField] int equipmentSlot = 0;   // <-- REQUIRED field, exact name

    // ── Optional events (subscribe for VFX / audio / UI) ──────────────────
    public System.Action onActivate;

    // ── Internal state ─────────────────────────────────────────────────────
    PlayerPhysics physics;
    PlayerMovementController movement;

    void Awake()
    {
        physics  = GetComponent<PlayerPhysics>();
        movement = GetComponent<PlayerMovementController>();
    }

    void Start()
    {
        Debug.Log($"[MyNewAbility] Ready | slot={equipmentSlot}");
    }

    void Update()
    {
        bool pressed = equipmentSlot == 0
            ? Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame
            : Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        if (pressed) Activate();
    }

    void Activate()
    {
        // ... your ability logic ...
        onActivate?.Invoke();
    }

    // ── Public getters (for HUD / other systems) ───────────────────────────
    public bool IsActive => false; // replace with real state
}
```

### Rules

| Rule | Why |
|---|---|
| `[SerializeField] int equipmentSlot` must be the exact field name | `EquipmentDevPanel` finds the slot via reflection using this name |
| Inherit `MonoBehaviour` | The dev panel calls `AddComponent` / `Destroy` at runtime |
| `[RequireComponent(typeof(PlayerPhysics))]` | Keeps the Inspector honest; abilities should never assume those components exist without requiring them |
| Use `Keyboard.current.xKey.wasPressedThisFrame` (not `Input.GetKeyDown`) | Consistent with the rest of the equipment system |

---

## 2. Register with the Dev Panel

Open `Assets/scripts/UI/EquipmentDevPanel.cs` and add your type to the `AvailableEquipment` array:

```csharp
static readonly System.Type[] AvailableEquipment = new System.Type[]
{
    typeof(DashAbility),
    typeof(GrappleAbility),
    typeof(JetpackAbility),
    typeof(WallRunAbility),
    typeof(ExplosiveBootsAbility),
    typeof(BallAndChainAbility),
    typeof(MyNewAbility),   // <-- add your type here
};
```

That's it. The panel auto-generates Q/E buttons for every registered type at runtime.

---

## 3. Test in the Editor

1. Enter Play Mode.
2. Press **Tab** to open the dev panel.
3. Click **Q: MyNewAbility** or **E: MyNewAbility** to equip it to that slot.
4. The current slot labels update immediately. Press Q or E in-game to activate.

---

## 4. Optional: Visual Companion Component

If your ability needs a line renderer, particle system, or other visual that should be managed separately, create a companion `MonoBehaviour` (e.g. `MyNewAbilityVisuals.cs`) and auto-attach it from `Awake`:

```csharp
void Awake()
{
    // ...
    if (GetComponent<MyNewAbilityVisuals>() == null)
        gameObject.AddComponent<MyNewAbilityVisuals>();
}
```

See `GrappleVisuals.cs` and `BallAndChainVisuals.cs` for reference implementations.

---

## Existing Equipment Reference

| Class | Default Slot | Activation | Notes |
|---|---|---|---|
| `DashAbility` | Q (0) | Press | Air dashes limited, refill on land |
| `GrappleAbility` | Q (0) | Press (toggle) | SpringJoint swing; fires camera ray |
| `JetpackAbility` | E (1) | Hold | Fuel-based; recharges on ground |
| `WallRunAbility` | E (1) | Hold near wall | Wall-jump on Space; max duration |
| `ExplosiveBootsAbility` | E (1) | Press | Charge-based; scans nearest surface |
| `BallAndChainAbility` | E (1) | Press (toggle) | SpringJoint chain; yank detection |
