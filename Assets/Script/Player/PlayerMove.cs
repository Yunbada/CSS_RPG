using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.Haptics;
public class PlayerMove : NetworkBehaviour
{

    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float runSpeed = 7f;

    [SerializeField] float gravity = -9.81f;
    [SerializeField] float jumpHeight = 2f;

    [SerializeField] float mouseSpeed = 1f;
    [SerializeField] private InputHandle Inputhandle;
    [SerializeField] private CharacterController cc;
    [SerializeField] private Camera Camera;

    private float xRot;

    Vector3 velo;



    void Start()
    {
        cam = Camera.transform();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if(!IsOwner) return;
        
        MoveAndJump();
        Look();
    }



    void MoveAndJump()
    {
        float h = Inputhandle.horizontalInput;
        float v = Inputhandle.verticalInput;

        bool grounded = cc.isGrounded;
        if (grounded && velo.y < 0) velo.y = -2f;

        float curSpeed = Inputhandle.runInput ? runSpeed : walkSpeed;
        Vector3 movDir = transform.right * h + transform.forward * v;

        cc.Move(movDir * curSpeed * Time.deltaTime);

        if (Inputhandle.jumpInput && grounded) velo.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        velo.y += gravity * Time.deltaTime;

        cc.Move(velo * Time.deltaTime);
    }

    void Look()
    {
        float mouseX = Inputhandle.mousexInput * mouseSpeed;
        float mouseY = Inputhandle.mouseyInput * mouseSpeed;

        xRot -= mouseY;
        xRot = Mathf.Clamp(xRot, -90f, 90f);

        Camera.main.transform.localRotation = Quaternion.Euler(xRot, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
    
}

///키입력 > 가상함수 > 분기점(가상함수 타이머, 다른 키 입력시 타이머 초기화 > 타스킬 사용) 
/// > Skill Controller(플레이어 사용) > Skill(가상함수로 스킬 구현) > Skill을 오버라이딩 해서 사용 
/// 상속 : 플레이어 -> 같은 스킬을 사용해도 전직에 따른 스킬 구현 가능 