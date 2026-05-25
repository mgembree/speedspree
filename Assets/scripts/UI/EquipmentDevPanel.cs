using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// In-game dev panel for swapping equipment abilities at runtime.
/// Toggle with Tab. Assign one ability per slot (Q = slot 0, E = slot 1).
/// </summary>
public class EquipmentDevPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] GameObject player;

    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI slotQLabel;
    [SerializeField] TextMeshProUGUI slotELabel;
    [SerializeField] Transform buttonContainer;
    [SerializeField] GameObject buttonPrefab;

    // All equipment types available to equip
    static readonly System.Type[] AvailableEquipment = new System.Type[]
    {
        typeof(DashAbility),
        typeof(GrappleAbility),
        typeof(WallRunAbility),
        typeof(BallAndChainAbility),
        // typeof(JetpackAbility),
    };

    bool isOpen;

    void Awake()
    {
        if (player == null)
            player = GameObject.Find("Player");

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void Start()
    {
        BuildButtons();
        RefreshLabels();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            TogglePanel();
    }

    void TogglePanel()
    {
        isOpen = !isOpen;
        if (panelRoot != null)
            panelRoot.SetActive(isOpen);

        if (isOpen)
            RefreshLabels();

        // Lock / unlock cursor
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpen;
    }

    void BuildButtons()
    {
        if (buttonContainer == null || buttonPrefab == null) return;

        // Clear existing
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        foreach (var type in AvailableEquipment)
        {
            // Q slot button
            var qGO = Instantiate(buttonPrefab, buttonContainer);
            qGO.GetComponentInChildren<TextMeshProUGUI>().text = "Q: " + type.Name;
            var capturedType = type;
            qGO.GetComponent<Button>().onClick.AddListener(() => EquipToSlot(capturedType, 0));

            // E slot button
            var eGO = Instantiate(buttonPrefab, buttonContainer);
            eGO.GetComponentInChildren<TextMeshProUGUI>().text = "E: " + type.Name;
            eGO.GetComponent<Button>().onClick.AddListener(() => EquipToSlot(capturedType, 1));
        }
    }

    void EquipToSlot(System.Type abilityType, int slot)
    {
        if (player == null) return;

        // Remove any existing ability in this slot
        foreach (var type in AvailableEquipment)
        {
            var existing = player.GetComponent(type) as MonoBehaviour;
            if (existing == null) continue;

            // Check slot via reflection
            var slotField = type.GetField("equipmentSlot",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (slotField != null && (int)slotField.GetValue(existing) == slot)
                Destroy(existing);
        }

        // Add new ability
        var newAbility = player.AddComponent(abilityType) as MonoBehaviour;
        if (newAbility != null)
        {
            var slotField = abilityType.GetField("equipmentSlot",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            slotField?.SetValue(newAbility, slot);
        }

        RefreshLabels();
    }

    void RefreshLabels()
    {
        if (player == null) return;

        string qName = "Empty";
        string eName = "Empty";

        foreach (var type in AvailableEquipment)
        {
            var comp = player.GetComponent(type) as MonoBehaviour;
            if (comp == null) continue;

            var slotField = type.GetField("equipmentSlot",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (slotField == null) continue;

            int slot = (int)slotField.GetValue(comp);
            if (slot == 0) qName = type.Name;
            else if (slot == 1) eName = type.Name;
        }

        if (slotQLabel != null) slotQLabel.text = "Q Slot: " + qName;
        if (slotELabel != null) slotELabel.text = "E Slot: " + eName;
    }
}
