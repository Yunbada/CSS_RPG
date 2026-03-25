using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(InputHandle))]
[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    private InputHandle inputHandle;
    private CharacterController characterController;

    [Header("Movement Options")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 5f;
    public float gravity = -9.81f;

    [Header("Camera Options")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    private float xRotation = 0f;

    private Vector3 velocity;
    private bool isGrounded;

    private void Awake()
    {
        inputHandle = GetComponent<InputHandle>();
        characterController = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Owner specific camera initialization
            if (cameraTransform != null && cameraTransform.TryGetComponent<Camera>(out var cam))
            {
                cam.enabled = true;
                if (cameraTransform.TryGetComponent<AudioListener>(out var listener))
                {
                    listener.enabled = true;
                }
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            // Disable camera for non-owners
            if (cameraTransform != null && cameraTransform.TryGetComponent<Camera>(out var cam))
            {
                cam.enabled = false;
                if (cameraTransform.TryGetComponent<AudioListener>(out var listener))
                {
                    listener.enabled = false;
                }
            }
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleMovement();
        HandleLook();
    }

    private void HandleLook()
    {
        if (cameraTransform == null) return;

        float mouseX = inputHandle.mousexInput * mouseSensitivity;
        float mouseY = inputHandle.mouseyInput * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        isGrounded = characterController.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        Vector3 move = transform.right * inputHandle.horizontalInput + transform.forward * inputHandle.verticalInput;
        float currentSpeed = inputHandle.runInput ? runSpeed : walkSpeed;

        characterController.Move(move * currentSpeed * Time.deltaTime);

        if (inputHandle.jumpInput && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
}
