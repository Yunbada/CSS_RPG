using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(InputHandle))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    private InputHandle inputHandle;
    private CharacterController characterController;

    [Header("Movement Options")]
    public float walkSpeed = 3f;
    public float runSpeed = 5f;
    public float jumpForce = 1f;
    public float gravity = -9.81f;

    private Vector3 velocity;
    
    public bool IsGrounded { get; private set; }
    public Vector3 Velocity => characterController.velocity;
    
    // 상태이상 (슬로우) 계수
    private float currentSlowMultiplier = 1.0f;

    private CombatSystem combatSystem;

    private void Awake()
    {
        inputHandle = GetComponent<InputHandle>();
        characterController = GetComponent<CharacterController>();
        combatSystem = GetComponent<CombatSystem>();
    }

    private void Update()
    {
        // (버그 수정) Host/Client 무관하게 PlayerModel과 RPG_Systems가 루트에서 이탈하지 않도록 강제 동기화
        Transform model = transform.Find("PlayerModel");
        if (model != null) model.localPosition = Vector3.zero;

        Transform rpgSys = transform.Find("RPG_Systems");
        if (rpgSys != null) rpgSys.localPosition = Vector3.zero;

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
        if (combatSystem != null) combatSystem.ChangeState(CombatState.Stunned);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (characterController != null)
                characterController.Move(forceVelocity * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        isForcedMoving = false;
        if (combatSystem != null && combatSystem.CurrentState == CombatState.Stunned)
        {
            combatSystem.ChangeState(CombatState.Idle);
        }
    }

    private void HandleMovement()
    {
        IsGrounded = characterController.isGrounded;
        if (IsGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        Vector3 move = transform.right * inputHandle.horizontalInput + transform.forward * inputHandle.verticalInput;
        float currentSpeed = (inputHandle.runInput ? runSpeed : walkSpeed) * currentSlowMultiplier;

        characterController.Move(move * currentSpeed * Time.deltaTime);

        if (inputHandle.jumpInput && IsGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    // =========================================================================
    // 디버프: 슬로우 처리
    // =========================================================================
    [ClientRpc]
    public void ApplySlowClientRpc(float slowRatio, float duration)
    {
        if (!IsOwner) return;
        StartCoroutine(SlowCoroutine(slowRatio, duration));
    }

    private System.Collections.IEnumerator SlowCoroutine(float slowRatio, float duration)
    {
        // 중복 슬로우인 경우 가장 강한 비율 적용 (임시 정책)
        if (currentSlowMultiplier > 1f - slowRatio) 
            currentSlowMultiplier = 1f - slowRatio;

        yield return new WaitForSeconds(duration);

        // 지속시간 뒤 원상 복구
        currentSlowMultiplier = 1.0f;
    }
}
