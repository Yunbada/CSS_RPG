using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(InputHandle))]
public class PlayerCamera : NetworkBehaviour
{
    private InputHandle inputHandle;
    private PlayerAuthentication _playerAuth;

    [Header("Camera Options")]
    public Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 2f;
    
    private float xRotation = 0f;

    private void Awake()
    {
        inputHandle = GetComponent<InputHandle>();
        _playerAuth = GetComponentInChildren<PlayerAuthentication>();
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

            // ★ 핵심 수정: 게임 입장 여부에 따라 플레이어 카메라 on/off
            if (_playerAuth == null) _playerAuth = GetComponentInChildren<PlayerAuthentication>();
            bool isEntered = _playerAuth != null && _playerAuth.isEnteredGame.Value;

            if (cameraTransform != null && cameraTransform.TryGetComponent<Camera>(out var cam))
            {
                cam.enabled = isEntered;
                if (cameraTransform.TryGetComponent<AudioListener>(out var listener))
                    listener.enabled = isEntered;
            }
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

        // ★ 격리 중에는 카메라 회전 처리하지 않음
        if (_playerAuth == null) _playerAuth = GetComponentInChildren<PlayerAuthentication>();
        if (_playerAuth != null && !_playerAuth.isEnteredGame.Value) return;

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
