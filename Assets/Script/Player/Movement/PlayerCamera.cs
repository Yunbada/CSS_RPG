using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(InputHandle))]
public class PlayerCamera : NetworkBehaviour
{
    private InputHandle inputHandle;

    [Header("Camera Options")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    
    private float xRotation = 0f;

    private void Awake()
    {
        inputHandle = GetComponent<InputHandle>();
        if (cameraTransform == null)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
            else Debug.LogWarning("[PlayerCamera] CameraTransform is missing! Mouse look won't work.");
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // 메인 씬 카메라의 AudioListener 비활성화 (2 AudioListeners 경고 방지)
            if (Camera.main != null && Camera.main.TryGetComponent<AudioListener>(out var mainListener))
            {
                mainListener.enabled = false;
            }

            if (cameraTransform != null && cameraTransform.TryGetComponent<Camera>(out var cam))
            {
                cam.enabled = true;
                if (cameraTransform.TryGetComponent<AudioListener>(out var listener))
                    listener.enabled = true;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            if (cameraTransform != null && cameraTransform.TryGetComponent<Camera>(out var cam))
            {
                cam.enabled = false;
                if (cameraTransform.TryGetComponent<AudioListener>(out var listener))
                    listener.enabled = false;
            }
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

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
}
