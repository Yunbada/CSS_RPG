using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(InputHandle))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    private InputHandle inputHandle;
    private CharacterController characterController;

    [Header("Movement Options")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 1f;
    public float gravity = -9.81f;

    private Vector3 velocity;
    
    public bool IsGrounded { get; private set; }
    public Vector3 Velocity => characterController.velocity;

    private CombatSystem combatSystem;

    private void Awake()
    {
        inputHandle = GetComponent<InputHandle>();
        characterController = GetComponent<CharacterController>();
        combatSystem = GetComponent<CombatSystem>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (combatSystem == null)
            combatSystem = GetComponentInChildren<CombatSystem>();

        // 스킬 사용 중이거나 강제 이동 중이면 일반 이동 불가
        if ((combatSystem != null && combatSystem.IsUsingSkill) || isForcedMoving) return;

        HandleMovement();
    }

    // =========================================================================
    // 강제 이동 (넉에어/넉백 처리용)
    // =========================================================================
    private bool isForcedMoving = false;

    public void ResetGravity()
    {
        velocity.y = 0f;
    }

    public void ApplyForcedMovement(Vector3 forceVelocity, float duration)
    {
        if (!IsOwner) return;
        StartCoroutine(ForcedMovementCoroutine(forceVelocity, duration));
    }

    private System.Collections.IEnumerator ForcedMovementCoroutine(Vector3 forceVelocity, float duration)
    {
        isForcedMoving = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (characterController != null)
                characterController.Move(forceVelocity * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        isForcedMoving = false;
    }

    private void HandleMovement()
    {
        IsGrounded = characterController.isGrounded;
        if (IsGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        Vector3 move = transform.right * inputHandle.horizontalInput + transform.forward * inputHandle.verticalInput;
        float currentSpeed = inputHandle.runInput ? runSpeed : walkSpeed;

        characterController.Move(move * currentSpeed * Time.deltaTime);

        if (inputHandle.jumpInput && IsGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
}
