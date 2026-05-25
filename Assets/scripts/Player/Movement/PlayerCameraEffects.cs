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

    [Header("Jump Kick (Arms)")]
    [Tooltip("How far up the ArmsRig shifts on jump.")]
    [SerializeField] float jumpKickAmount = 0.12f;
    [Tooltip("How quickly the kick decays back to rest.")]
    [SerializeField] float jumpKickDecaySpeed = 5f;

    [Header("Wall Run Tilt")]
    [SerializeField] float wallRunTiltAngle = 7f;
    [SerializeField] float wallRunTiltSpeed = 10f;

    // References
    PlayerMovementController movement;
    
    
    GrappleAbility grapple;
ExplosiveBootsAbility boots;
DashAbility dash;
WallRunAbility wallRun;
BallAndChainAbility ballChain;

    // Arms rig (jump kick)
    Transform armsRig;
    Vector3   armsBaseLocalPos;
    float     armsJumpOffset;

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
        wallRun = GetComponent<WallRunAbility>();
        ballChain = GetComponent<BallAndChainAbility>();

        if (cameraTarget == null)
            cameraTarget = transform.Find("CameraTarget");
        if (playerLook == null)
            playerLook = GetComponent<PlayerLook>();

        // Capture ArmsRig base position for jump kick offset
        var armsRigComp = GetComponentInChildren<ArmsRig>();
        if (armsRigComp != null)
        {
            armsRig = armsRigComp.transform;
            armsBaseLocalPos = armsRig.localPosition;
        }
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
        movement.onJump       += OnJump;

        if (dash != null)
        {
            dash.onDashStart += OnDashStart;
            dash.onDashEnd   += OnDashEnd;
        }

        if (boots != null)
            boots.onBlast += OnBootsBlast;

        if (grapple != null)
            grapple.onGrappleAttach += OnGrappleAttach;

        if (wallRun != null)
        {
            wallRun.onWallRunStart += OnWallRunStart;
            wallRun.onWallRunEnd   += OnWallRunEnd;
        }

        if (ballChain != null)
        {
            ballChain.onThrow  += OnBallThrow;
            ballChain.onYank   += OnBallYank;
            ballChain.onRecall += OnBallRecall;
        }
    }

void OnDestroy()
    {
        if (movement != null)
        {
            movement.onSlideStart -= OnSlideStart;
            movement.onSlideEnd   -= OnSlideEnd;
            movement.onJump       -= OnJump;
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
        if (wallRun != null)
        {
            wallRun.onWallRunStart -= OnWallRunStart;
            wallRun.onWallRunEnd   -= OnWallRunEnd;
        }
        if (ballChain != null)
        {
            ballChain.onThrow  -= OnBallThrow;
            ballChain.onYank   -= OnBallYank;
            ballChain.onRecall -= OnBallRecall;
        }
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

        // Arms jump kick decay
        if (armsRig != null)
        {
            armsJumpOffset = Mathf.Lerp(armsJumpOffset, 0f, jumpKickDecaySpeed * Time.deltaTime);
            armsRig.localPosition = armsBaseLocalPos + Vector3.up * armsJumpOffset;
        }

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
        float lerpSpeed = sliding ? slideDownSpeed : (wallRun != null && wallRun.IsWallRunning ? wallRunTiltSpeed : slideUpSpeed);
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

    // ── Jump Kick ──────────────────────────────────────────────────────────

    void OnJump()
    {
        armsJumpOffset = jumpKickAmount;
    }

    // ── Wall Run ───────────────────────────────────────────────────────────

    void OnWallRunStart(Vector3 wallNormal)
    {
        // Tilt camera toward the wall: negative dot with camera right = wall on right = lean right
        Camera cam = Camera.main;
        float side = cam != null ? Vector3.Dot(wallNormal, cam.transform.right) : 0f;
        targetRoll = -wallRunTiltAngle * Mathf.Sign(side);
    }

    void OnWallRunEnd()
    {
        targetRoll = 0f;
    }

    // ── Ball and Chain ─────────────────────────────────────────────────────

    void OnBallThrow()
    {
        // Small FOV punch when the ball is thrown
        targetFov = baseFov + dashFovBoost * 0.5f;
        Invoke(nameof(ResetFov), 0.12f);
    }

    void OnBallYank()
    {
        // Camera dips down like a land bob when the chain suddenly yanks the player
        TriggerLandBob(0.7f);
    }

    void OnBallRecall() { }
}
