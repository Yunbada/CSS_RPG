using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 플레이어 인증 및 세션 관리 (SRP 분리)
/// 로그인, 회원가입, 닉네임 설정, 게임 입장 요청을 전담합니다.
/// </summary>
public class PlayerAuthentication : NetworkBehaviour
{
    [Header("세션 상태")]
    public bool isLoggedIn = false;

    public NetworkVariable<Unity.Collections.FixedString32Bytes> Nickname = new NetworkVariable<Unity.Collections.FixedString32Bytes>(
        "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> isEnteredGame = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // 같은 게임오브젝트에 있는 형제 컴포넌트 참조 (느슨한 결합)
    private PlayerLifecycleManager lifecycleManager;

    public override void OnNetworkSpawn()
    {
        lifecycleManager = GetComponent<PlayerLifecycleManager>();

        if (IsOwner)
        {
            isLoggedIn = false;
        }

        isEnteredGame.OnValueChanged += OnEnteredGameChanged;

        // 즉시 격리/활성화 상태 적용
        if (!isEnteredGame.Value)
        {
            // 아직 게임 입장 전이라면 대기실(-1000)로 강제 이동
            var root = transform.root;
            var cc = root.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            
            root.position = new Vector3(0, -1000, 0);
            
            if (cc != null) cc.enabled = true;
            
            if (lifecycleManager != null)
                lifecycleManager.SetPlayerActiveState(false);
        }
        else
        {
            if (lifecycleManager != null)
                lifecycleManager.SetPlayerActiveState(true);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isEnteredGame.OnValueChanged -= OnEnteredGameChanged;
    }

    private void OnEnteredGameChanged(bool oldVal, bool newVal)
    {
        if (lifecycleManager != null)
        {
            lifecycleManager.SetPlayerActiveState(newVal);
        }
    }

    // =========================================================================
    // 로그인 / 회원가입 RPC
    // =========================================================================

    [Rpc(SendTo.Server, RequireOwnership = true)]
    public void LoginRequestServerRpc(string id, string pw)
    {
        if (CsvDatabase.Instance == null)
        {
            Debug.LogError("[Server] CsvDatabase.Instance is null! 데이터베이스 초기화 실패.");
            AuthResponseClientRpc(false, "", "서버 데이터베이스 오류");
            return;
        }
        var userData = CsvDatabase.Instance.LoginUser(id, pw);
        if (userData != null)
        {
            string json = JsonUtility.ToJson(userData);
            AuthResponseClientRpc(true, json, "Login Success");
        }
        else
        {
            AuthResponseClientRpc(false, "", "Invalid ID or Password");
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = true)]
    public void RegisterRequestServerRpc(string id, string pw, string nick)
    {
        if (CsvDatabase.Instance == null)
        {
            Debug.LogError("[Server] CsvDatabase.Instance is null! 데이터베이스 초기화 실패.");
            RegisterResponseClientRpc(false, "서버 데이터베이스 오류");
            return;
        }
        bool success = CsvDatabase.Instance.RegisterUser(id, pw, nick);
        if (success)
        {
            RegisterResponseClientRpc(true, "회원가입 완료! 로그인 버튼을 눌러주세요.");
        }
        else
        {
            RegisterResponseClientRpc(false, "이미 존재하는 ID입니다.");
        }
    }

    [Rpc(SendTo.Owner)]
    public void AuthResponseClientRpc(bool success, string userDataJson, string msg)
    {
        UserData data = null;
        if (success && !string.IsNullOrEmpty(userDataJson))
        {
            data = JsonUtility.FromJson<UserData>(userDataJson);
            isLoggedIn = true;
            SetNicknameServerRpc(data.Nickname);
        }
        
        if (NetworkManagerUI.Instance != null)
        {
            NetworkManagerUI.Instance.OnLoginResponse(success, data, msg);
        }
    }

    [Rpc(SendTo.Owner)]
    public void RegisterResponseClientRpc(bool success, string msg)
    {
        if (NetworkManagerUI.Instance != null)
        {
            NetworkManagerUI.Instance.OnRegisterResponse(success, msg);
        }
    }

    // =========================================================================
    // 게임 입장 요청
    // =========================================================================

    [Rpc(SendTo.Server, RequireOwnership = true)]
    public void RequestEnterGameServerRpc()
    {
        isEnteredGame.Value = true;
        
        var playerState = GetComponent<PlayerState>();
        if (RoundManager.Instance != null && playerState != null)
            RoundManager.Instance.RegisterPlayer(playerState);
        
        // 텔레포트 위치 지정 후 클라이언트 권한 오브젝트에게 이동 명령
        Vector3 spawnPos = new Vector3(0, 1, 0); 
        EnterGameClientRpc(spawnPos);
    }

    [Rpc(SendTo.Owner)]
    public void EnterGameClientRpc(Vector3 pos)
    {
        var root = transform.root;
        var cc = root.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        
        root.position = pos;
        
        if (cc != null) cc.enabled = true;
        
        if (lifecycleManager != null)
        {
            lifecycleManager.SetPlayerActiveState(true);
        }
    }

    // =========================================================================
    // 닉네임 설정
    // =========================================================================

    [Rpc(SendTo.Server)]
    public void SetNicknameServerRpc(string nick)
    {
        Nickname.Value = nick;
        string ip = "알수없음";
        if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is Unity.Netcode.Transports.UTP.UnityTransport utp)
        {
            ip = "Client " + OwnerClientId; 
        }
        ServerConsole.LogConnection(nick, ip);
    }

    /// <summary>라운드 종료 후 모든 플레이어 데이터 저장 (클라이언트에서 실행)</summary>
    [ClientRpc]
    public void SavePlayerDataClientRpc(int expReward, int goldReward)
    {
        if (!IsOwner || LocalUserData.Current == null) return;
        var pExp = GetComponentInChildren<PlayerExperience>();
        if (pExp != null)
        {
            LocalUserData.Current.Level = pExp.Level.Value;
            LocalUserData.Current.Exp = pExp.CurrentExp.Value;
        }
        LocalUserData.Current.Gold += goldReward;
        CsvDatabase.Instance.SaveUser(LocalUserData.Current);
        Debug.Log($"[SaveData] EXP+{expReward}, Gold+{goldReward} 저장 완료");
    }
}
