using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mouse-look: rotates the player body on Y, camera on X.
/// Attach to the Player root; assign CameraTarget to the head/camera pivot.
/// </summary>
public class PlayerLook : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] float sensitivityX = 0.15f;
    [SerializeField] float sensitivityY = 0.15f;

    [Header("Vertical Clamp")]
    [SerializeField] float minPitch = -85f;
    [SerializeField] float maxPitch = 85f;

    [Header("References")]
    [SerializeField] Transform cameraTarget;

    InputSystem_Actions inputActions;
    float pitch;
    float rollOffset;  // set by PlayerCameraEffects

    public void SetRollOffset(float roll) => rollOffset = roll;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        LockCursor();
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();
    void OnDestroy() => inputActions.Dispose();

    void Start()
    {
        if (cameraTarget == null)
            Debug.LogWarning("PlayerLook: CameraTarget not assigned. Create a child pivot and assign it.");
    }

void Update()
    {
        Vector2 lookDelta = inputActions.Player.Look.ReadValue<Vector2>();

        transform.Rotate(Vector3.up, lookDelta.x * sensitivityX, Space.World);

        if (cameraTarget != null)
        {
            pitch -= lookDelta.y * sensitivityY;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            cameraTarget.localRotation = Quaternion.Euler(pitch, 0f, rollOffset);
        }
    }

    // ── Cursor ─────────────────────────────────────────────────────────────

    public static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
