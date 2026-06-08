using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to the player. Drag weapon GameObjects into the Weapons list.
/// Keys 1/2/3 for direct select, scroll wheel to cycle, left click to attack.
/// </summary>
public class WeaponController : MonoBehaviour
{
    [SerializeField] List<WeaponBase> weapons = new();

    int currentIndex = -1;

    public WeaponBase CurrentWeapon { get; private set; }
    public int CurrentIndex => currentIndex;
    public IReadOnlyList<WeaponBase> Weapons => weapons;

    void Start()
    {
        foreach (var w in weapons)
            if (w != null) w.gameObject.SetActive(false);

        if (weapons.Count > 0) EquipWeapon(0);
    }

    void Update()
    {
        HandleSwitchInput();

        if (CurrentWeapon != null && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            CurrentWeapon.PrimaryAttack();
    }

    void HandleSwitchInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame && weapons.Count > 0) EquipWeapon(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame && weapons.Count > 1) EquipWeapon(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame && weapons.Count > 2) EquipWeapon(2);

        if (Mouse.current == null) return;
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll > 0f) EquipWeapon((currentIndex - 1 + weapons.Count) % weapons.Count);
        else if (scroll < 0f) EquipWeapon((currentIndex + 1) % weapons.Count);
    }

    public void EquipWeapon(int index)
    {
        if (index < 0 || index >= weapons.Count || index == currentIndex) return;

        CurrentWeapon?.OnUnequip();
        currentIndex  = index;
        CurrentWeapon = weapons[index];
        CurrentWeapon?.OnEquip();

        Debug.Log($"[Weapons] Equipped: {CurrentWeapon?.WeaponName}");
    }

    public void RegisterWeapon(WeaponBase weapon, bool equipImmediately = true)
    {
        if (weapon == null || weapons.Contains(weapon))
            return;

        weapons.Add(weapon);
        weapon.gameObject.SetActive(false);

        if (equipImmediately)
            EquipWeapon(weapons.Count - 1);

        Debug.Log($"[Weapons] Registered: {weapon.WeaponName}");
    }

    public void UnregisterWeapon(WeaponBase weapon)
    {
        if (weapon == null)
            return;

        int removedIndex = weapons.IndexOf(weapon);
        if (removedIndex < 0)
            return;

        bool wasCurrent = CurrentWeapon == weapon;
        weapons.RemoveAt(removedIndex);

        if (wasCurrent)
        {
            CurrentWeapon?.OnUnequip();
            currentIndex = -1;
            CurrentWeapon = null;

            if (weapons.Count > 0)
                EquipWeapon(Mathf.Clamp(removedIndex, 0, weapons.Count - 1));
        }
        else if (currentIndex > removedIndex)
        {
            currentIndex--;
        }

        Debug.Log($"[Weapons] Unregistered: {weapon.WeaponName}");
    }
}
