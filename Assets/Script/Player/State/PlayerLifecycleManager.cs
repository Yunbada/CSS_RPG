using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 플레이어 생명주기 관리 (SRP 분리)
/// 게임 입장/퇴장 시 하위 컴포넌트 활성화/비활성화, 격리 이동을 전담합니다.
/// </summary>
public class PlayerLifecycleManager : NetworkBehaviour
{
    /// <summary>
    /// 플레이어의 활성 상태를 설정합니다.
    /// 렌더러, 충돌체, 이동/전투/스킬 시스템을 켜고 끕니다.
    /// </summary>
    public void SetPlayerActiveState(bool active)
    {
        var root = transform.root;

        // 모든 클라이언트: 루트 기준 모델/렌더러 토글
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers) r.enabled = active;

        // 서버 전용: 충돌체 토글
        if (IsServer)
        {
            var cc = root.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = active;
        }

        // 로컬 오너 전용: 코루틴으로 지연 처리
        if (IsOwner)
        {
            StartCoroutine(ApplyActiveStateCoroutine(active));
        }
    }

    private System.Collections.IEnumerator ApplyActiveStateCoroutine(bool active)
    {
        yield return new WaitForEndOfFrame();

        var root = transform.root;

        var cc = root.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = active;

        var mov = root.GetComponent<PlayerMovement>();
        if (mov != null) mov.enabled = active;

        var input = root.GetComponent<InputHandle>();
        if (input != null) input.enabled = active;

        var cs = root.GetComponentInChildren<CombatSystem>();
        if (cs != null) cs.enabled = active;

        var skillSys = root.GetComponentInChildren<SkillSystem>();
        if (skillSys != null) skillSys.enabled = active;
        
        var pCam = root.GetComponent<PlayerCamera>();
        if (pCam != null) pCam.enabled = active;
        
        // HUD 토글 (이벤트 기반 리팩토링 대상)
        var hud = FindFirstObjectByType<UIGameHUDRuntime>(FindObjectsInactive.Include);
        if (hud != null)
        {
            hud.gameObject.SetActive(active);
        }

        if (!active)
        {
            root.position = new Vector3(0, -1000, 0);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            root.position = new Vector3(0, 1, 0);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            var pCamComp = root.GetComponent<PlayerCamera>();
            if (pCamComp != null && pCamComp.cameraTransform != null)
            {
                if (pCamComp.cameraTransform.TryGetComponent<Camera>(out var cam))
                    cam.enabled = true;
                if (pCamComp.cameraTransform.TryGetComponent<AudioListener>(out var listener))
                    listener.enabled = true;
            }

            // SkillSystem / CombatSystem 지연 초기화
            var pClass = root.GetComponentInChildren<PlayerClass>();
            if (pClass != null)
            {
                if (skillSys != null)
                {
                    skillSys.enabled = true;
                    skillSys.InitializeSkillSystem(pClass);
                }
                if (cs != null)
                {
                    cs.enabled = true;
                    cs.InitializeCombatSystem();
                }
            }

            // 로그인 UI 비활성화
            var mainMenu = GameObject.Find("LobbyUI_Canvas");
            if (mainMenu != null)
            {
                mainMenu.SetActive(false);
            }
        }
    }
}
