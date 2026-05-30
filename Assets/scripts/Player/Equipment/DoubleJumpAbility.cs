using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(PlayerMovementController))]
public class DoubleJumpAbility : MonoBehaviour
{
    [SerializeField] float jumpForce  = 9f;
    [SerializeField] int   extraJumps = 1;

    PlayerPhysics            physics;
    PlayerMovementController movement;
    WallRunAbility           wallRun;

    int   jumpsRemaining;
    float airTime;

    void Awake()
    {
        physics  = GetComponent<PlayerPhysics>();
        movement = GetComponent<PlayerMovementController>();
        wallRun  = GetComponent<WallRunAbility>();
    }

    void Update()
    {
        if (movement.IsGrounded)
        {
            jumpsRemaining = extraJumps;
            airTime        = 0f;
            return;
        }

        airTime += Time.deltaTime;

        // Let WallRunAbility handle space while wall running
        if (wallRun != null && wallRun.IsWallRunning) return;

        if (Keyboard.current == null || !Keyboard.current.spaceKey.wasPressedThisFrame) return;
        if (airTime < 0.15f || jumpsRemaining <= 0) return;

        jumpsRemaining--;
        physics.SetVerticalVelocity(Mathf.Max(0f, physics.Velocity.y));
        physics.AddImpulse(Vector3.up * jumpForce);
        Debug.Log("[DoubleJump] Extra jump!");
    }
}
