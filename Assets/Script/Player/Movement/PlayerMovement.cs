using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(InputHandle))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    private InputHandle inputHandle;
    private CharacterController characterController;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float jumpForce = 1f;
    [SerializeField] private float gravity = -9.81f;

    private Vector3 velocity;
    
    public bool IsGrounded { get; private set; }
    public Vector3 Velocity => characterController.velocity;
    
    // 상태이상 (슬로우) 계수
    private float currentSlowMultiplier = 1.0f;

    private CombatSystem combatSystem;

    private PlayerAuthentication _playerAuth;
    private Transform cachedModel;
    private Transform cachedRpgSys;

    // 슬로우 관리
    private struct SlowData { public float ratio; public float endTime; }
    private System.Collections.Generic.List<SlowData> activeSlows = new System.Collections.Generic.List<SlowData>();

    private void Awake()
    {
        inputHandle = GetComponent<InputHandle>();
        characterController = GetComponent<CharacterController>();
        combatSystem = GetComponentInChildren<CombatSystem>();
        _playerAuth = GetComponentInChildren<PlayerAuthentication>();
        
        cachedModel = transform.Find("PlayerModel");
        cachedRpgSys = transform.Find("RPG_Systems");
    }

    private void Update()
    {
        // 최적화: 캐싱된 트랜스폼 사용
        if (cachedModel != null) cachedModel.localPosition = Vector3.zero;
        if (cachedRpgSys != null) cachedRpgSys.localPosition = Vector3.zero;

        if (!IsOwner) return;

        // 지연 할당 대비
        if (_playerAuth == null) _playerAuth = GetComponentInChildren<PlayerAuthentication>();
        if (combatSystem == null) combatSystem = GetComponentInChildren<CombatSystem>();

        // ★ 핵심: 게임 미입장(격리) 중에는 이동 및 중력 완전 차단
        if (_playerAuth != null && !_playerAuth.isEnteredGame.Value)
        {
            velocity = Vector3.zero;
            return;
        }

        UpdateSlowEffects();

        // 스킬 사용 중이거나 강제 이동 중이면 일반 이동 불가
        if ((combatSystem != null && combatSystem.IsUsingSkill) || isForcedMoving) return;

        HandleMovement();
    }

    private void UpdateSlowEffects()
    {
        if (activeSlows.Count == 0)
        {
            currentSlowMultiplier = 1.0f;
            return;
        }

        float currentTime = Time.time;
        float maxSlowRatio = 0f;

        for (int i = activeSlows.Count - 1; i >= 0; i--)
        {
            if (currentTime >= activeSlows[i].endTime)
            {
                activeSlows.RemoveAt(i);
            }
            else
            {
                if (activeSlows[i].ratio > maxSlowRatio)
                {
                    maxSlowRatio = activeSlows[i].ratio;
                }
            }
        }

        currentSlowMultiplier = 1.0f - maxSlowRatio;
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
        activeSlows.Add(new SlowData { ratio = slowRatio, endTime = Time.time + duration });
    }
}
