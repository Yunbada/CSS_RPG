using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.XInput;

//플레이어의 입력을 계속 받아야함
public class InputHandle : MonoBehaviour
{
    #region Components
    //숫자키의 번호를 받음!
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

    //인풋값
    public string verticallInputName = "Vertical";
    public string horizontalInputName = "Horizontal";
    public float verticalInput { get; private set;}
    public float horizontalInput { get; private set;}
    public float mousexInput { get; private set;}
    public float mouseyInput { get; private set;}
    public bool jumpInput { get; private set;}
    public bool runInput { get; private set;}
    public int numInput { get; private set;}
    #endregion 


    void Update()
    {
        verticalInput = Input.GetAxis(verticallInputName);
        horizontalInput = Input.GetAxis(horizontalInputName);
        jumpInput = Input.GetKeyDown(KeyCode.Space);
        runInput = Input.GetKey(KeyCode.LeftShift);
        mousexInput =Input.GetAxis("Mouse X");
        mouseyInput = Input.GetAxis("Mouse Y");
        numInput = KeyNo();
    }

    int KeyNo()
    {
        int numberPressed = -1;
        for(int i = 0 ; i < keyCodes.Length; i ++ ){
            if(Input.GetKeyDown(keyCodes[i])){
                numberPressed = i;
            }     
        }
        return numberPressed;
    }
}
