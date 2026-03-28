using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(InputHandle))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerAnimation : NetworkBehaviour
{
    private InputHandle inputHandle;
    private PlayerMovement playerMovement;
    private Animator animator;
    private OwnerNetworkAnimator networkAnimator;

    // Animator 파라미터 해시
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int TurnValueHash = Animator.StringToHash("TurnValue");

    private void Awake()
    {
        inputHandle = GetComponent<InputHandle>();
        playerMovement = GetComponent<PlayerMovement>();
        animator = GetComponent<Animator>(); // 루트에서 직접 참조
        networkAnimator = GetComponent<OwnerNetworkAnimator>(); // 루트에서 직접 참조
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleAnimation();
    }

    private void HandleAnimation()
    {
        if (animator == null) return;

        // 입력 기반 이동 파라미터 
        float moveX = inputHandle.horizontalInput;
        float moveY = inputHandle.verticalInput;
        float inputMagnitude = new Vector2(moveX, moveY).magnitude;

        // Speed
        float speed = Mathf.Clamp01(inputMagnitude);

        // Turn
        float turnValue = inputHandle.mousexInput;

        // 파라미터 세팅
        animator.SetFloat(MoveXHash, moveX, 0.1f, Time.deltaTime);
        animator.SetFloat(MoveYHash, moveY, 0.1f, Time.deltaTime);
        animator.SetFloat(SpeedHash, speed);
        animator.SetBool(IsRunningHash, inputHandle.runInput);
        animator.SetBool(IsGroundedHash, playerMovement.IsGrounded);
        animator.SetFloat(TurnValueHash, turnValue);

        // 점프 트리거
        if (inputHandle.jumpInput && playerMovement.IsGrounded)
        {
            animator.SetTrigger(IsJumpingHash);
            if (networkAnimator != null)
                networkAnimator.SetTrigger("IsJumping");
        }
    }
}
