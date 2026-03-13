using UnityEngine;

public class PlayerMove : MonoBehaviour
{

    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float runSpeed = 7f;

    [SerializeField] float gravity = -9.81f;
    [SerializeField] float jumpHeight = 2f;

    [SerializeField] float mouseSpeed = 1.5f;

    private float xRot;
    private KeyCode[] keyCodes = {
    KeyCode.Alpha1,
    KeyCode.Alpha2,
    KeyCode.Alpha3,
    KeyCode.Alpha4,
    KeyCode.Alpha5,
    KeyCode.Alpha6,
    KeyCode.Alpha7,
    KeyCode.Alpha8,
    KeyCode.Alpha9,
    };    

    Vector3 velo;
    Transform camTr;

    CharacterController cc;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        camTr = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        MoveAndJump();
        Look();
        KeyNo();
    }


    void MoveAndJump()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        bool grounded = cc.isGrounded;
        if (grounded && velo.y < 0) velo.y = -2f;

        float curSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        Vector3 movDir = transform.right * h + transform.forward * v;

        cc.Move(movDir * curSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && grounded) velo.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        velo.y += gravity * Time.deltaTime;

        cc.Move(velo * Time.deltaTime);
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSpeed;

        xRot -= mouseY;
        xRot = Mathf.Clamp(xRot, -90f, 90f);

        camTr.localRotation = Quaternion.Euler(xRot, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
    
    void KeyNo()
    {
        for(int i = 0 ; i < keyCodes.Length; i ++ ){
            if(Input.GetKeyDown(keyCodes[i])){
            int numberPressed = i+1;
            Debug.Log(numberPressed);
            }     
        }
    }
}

///키입력 > 가상함수 > 분기점(가상함수 타이머, 다른 키 입력시 타이머 초기화 > 타스킬 사용) 
/// > Skill Controller(플레이어 사용) > Skill(가상함수로 스킬 구현) > Skill을 오버라이딩 해서 사용 
/// 상속 : 플레이어 -> 같은 스킬을 사용해도 전직에 따른 스킬 구현 가능 