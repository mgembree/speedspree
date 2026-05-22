using UnityEngine;

/// <summary>
/// First-person camera effects: slide drop/tilt, dash FOV burst, land bob.
/// Attach to the Player root alongside PlayerMovementController.
/// </summary>
public class PlayerCameraEffects : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform cameraTarget;
    [SerializeField] PlayerLook playerLook;

    [Header("Slide Camera")]
    [SerializeField] float slideDropAmount = 0.55f;
    [SerializeField] float slideTiltAngle = 4f;
    [SerializeField] float slideDownSpeed = 12f;
    [SerializeField] float slideUpSpeed = 8f;

    [Header("Dash FOV")]
    [SerializeField] float dashFovBoost = 15f;
    [SerializeField] float dashFovInSpeed = 40f;
    [SerializeField] float dashFovOutSpeed = 12f;

    [Header("Land Bob")]
    [SerializeField] float landBobAmount = 0.08f;
    [SerializeField] float landBobSpeed = 14f;

    // References
    PlayerMovementController movement;
    
    
    GrappleAbility grapple;
ExplosiveBootsAbility boots;
DashAbility dash;

    // Slide state
    bool sliding;
    float standLocalY;
    float targetLocalY;
    float targetRoll;
    float _currentRoll;

    // FOV state
    float baseFov;
    float targetFov;
    float currentFov;

    // Bob state
    float landBobOffset;
    float landBobVelocity;

    void Awake()
    {
        movement = GetComponent<PlayerMovementController>();
        dash = GetComponent<DashAbility>();
        boots   = GetComponent<ExplosiveBootsAbility>();
        grapple = GetComponent<GrappleAbility>();

        if (cameraTarget == null)
            cameraTarget = transform.Find("CameraTarget");
        if (playerLook == null)
            playerLook = GetComponent<PlayerLook>();
    }

void Start()
    {
        if (cameraTarget != null)
            standLocalY = cameraTarget.localPosition.y;
        targetLocalY = standLocalY;

        if (Camera.main != null)
        {
            baseFov    = Camera.main.fieldOfView;
            currentFov = baseFov;
            targetFov  = baseFov;
        }

        movement.onSlideStart += OnSlideStart;
        movement.onSlideEnd   += OnSlideEnd;

        if (dash != null)
        {
            dash.onDashStart += OnDashStart;
            dash.onDashEnd   += OnDashEnd;
        }

        if (boots != null)
            boots.onBlast += OnBootsBlast;

        if (grapple != null)
            grapple.onGrappleAttach += OnGrappleAttach;
    }

void OnDestroy()
    {
        if (movement != null)
        {
            movement.onSlideStart -= OnSlideStart;
            movement.onSlideEnd   -= OnSlideEnd;
        }
        if (dash != null)
        {
            dash.onDashStart -= OnDashStart;
            dash.onDashEnd   -= OnDashEnd;
        }
        if (boots != null)
            boots.onBlast -= OnBootsBlast;
        if (grapple != null)
            grapple.onGrappleAttach -= OnGrappleAttach;
    }

    void Update()
    {
        if (cameraTarget == null) return;

        float lerpSpeed = sliding ? slideDownSpeed : slideUpSpeed;

        // Camera Y (slide drop)
        float newY = Mathf.Lerp(
            cameraTarget.localPosition.y,
            targetLocalY + landBobOffset,
            lerpSpeed * Time.deltaTime);
        cameraTarget.localPosition = new Vector3(
            cameraTarget.localPosition.x, newY, cameraTarget.localPosition.z);

        // Land bob decay
        landBobOffset = Mathf.SmoothDamp(landBobOffset, 0f, ref landBobVelocity, 1f / landBobSpeed);

        // FOV burst
        if (Camera.main != null)
        {
            float fovSpeed = (currentFov < targetFov) ? dashFovInSpeed : dashFovOutSpeed;
            currentFov = Mathf.Lerp(currentFov, targetFov, fovSpeed * Time.deltaTime);
            Camera.main.fieldOfView = currentFov;

            var armsGO = GameObject.Find("ArmsCamera");
            if (armsGO != null)
            {
                var armsCam = armsGO.GetComponent<Camera>();
                if (armsCam != null) armsCam.fieldOfView = currentFov;
            }
        }
    }

    void LateUpdate()
    {
        if (playerLook == null) return;
        float lerpSpeed = sliding ? slideDownSpeed : slideUpSpeed;
        _currentRoll = Mathf.LerpAngle(_currentRoll, targetRoll, lerpSpeed * Time.deltaTime);
        playerLook.SetRollOffset(_currentRoll);
    }

    // ── Slide ──────────────────────────────────────────────────────────────

    void OnSlideStart()
    {
        sliding = true;
        targetLocalY = standLocalY - slideDropAmount;
        targetRoll = slideTiltAngle;
    }

    void OnSlideEnd()
    {
        sliding = false;
        targetLocalY = standLocalY;
        targetRoll = 0f;
    }

    // ── Dash ───────────────────────────────────────────────────────────────

    void OnDashStart()
    {
        targetFov = baseFov + dashFovBoost;
    }

    void OnBootsBlast()
    {
        // Strong FOV punch — boots feel explosive
        targetFov = baseFov + dashFovBoost * 1.4f;
        // Snap back quickly
        Invoke(nameof(ResetFov), 0.08f);
    }

    

void OnGrappleAttach(Vector3 _)
    {
        targetFov = baseFov + dashFovBoost * 0.8f;
        Invoke(nameof(ResetFov), 0.15f);
    }


void ResetFov() => targetFov = baseFov;

    
void OnDashEnd()
    {
        targetFov = baseFov;
    }

    // ── Land Bob ───────────────────────────────────────────────────────────

    public void TriggerLandBob(float intensity = 1f)
    {
        landBobOffset = -landBobAmount * Mathf.Clamp01(intensity);
        landBobVelocity = 0f;
    }
}
