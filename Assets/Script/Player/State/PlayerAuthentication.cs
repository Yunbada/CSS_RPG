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

    public NetworkVariable<int> Gold = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // 서버 측에서 현재 접속한 유저의 정보를 보관하는 캐시 데이터
    private UserData _serverUserData;

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

        // 접속 해제 시 서버에서 직접 데이터 저장
        if (IsServer)
        {
            SaveDataToDatabase();
            Debug.Log("[PlayerAuthentication] 접속 해제 시 데이터 자동 저장 완료 (Server-Authoritative)");
        }
    }

    /// <summary>
    /// 서버가 현재 시점의 하위 컴포넌트 변수들을 모두 긁어모아 직접 CSV에 저장합니다.
    /// 라운드 종료, 접속 해제 시 호출됩니다.
    /// </summary>
    public void SaveDataToDatabase()
    {
        if (!IsServer || _serverUserData == null || CsvDatabase.Instance == null) return;

        // 레벨 & 경험치
        var pExp = GetComponentInChildren<PlayerExperience>();
        if (pExp != null)
        {
            _serverUserData.Level = pExp.Level.Value;
            _serverUserData.Exp = pExp.CurrentExp.Value;
        }

        // 전직 정보
        var pClass = GetComponentInChildren<PlayerClass>();
        if (pClass != null)
        {
            _serverUserData.ClassIndex = (int)pClass.currentClass.Value;
        }

        // 인벤토리 데이터
        var invSys = GetComponentInChildren<InventorySystem>();
        if (invSys != null)
        {
            _serverUserData.Leather = invSys.LeatherCount.Value;
            _serverUserData.Tooth = invSys.ToothCount.Value;
            _serverUserData.Skull = invSys.SkullCount.Value;
            _serverUserData.InventoryData = invSys.SerializeInventory();
        }

        // 장비 데이터
        var equipSys = GetComponentInChildren<EquipmentSystem>();
        if (equipSys != null)
        {
            _serverUserData.EquipmentData = equipSys.SerializeEquipment();
        }

        _serverUserData.Gold = Gold.Value;

        CsvDatabase.Instance.SaveUser(_serverUserData);
        Debug.Log($"[Server] 데이터 저장 완료: {_serverUserData.ID} (Lv: {_serverUserData.Level}, Gold: {_serverUserData.Gold})");
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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
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
            _serverUserData = userData;
            Gold.Value = userData.Gold;
            string json = JsonUtility.ToJson(userData);
            AuthResponseClientRpc(true, json, "Login Success");
        }
        else
        {
            AuthResponseClientRpc(false, "", "Invalid ID or Password");
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
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

        PushLocalDataToServer();
    }

    private void PushLocalDataToServer()
    {
        if (LocalUserData.Current == null) return;

        var data = LocalUserData.Current;
        var expSys = GetComponent<PlayerExperience>();
        var classSys = GetComponent<PlayerClass>();
        var invSys = GetComponent<InventorySystem>();
        var equipSys = GetComponent<EquipmentSystem>();

        if (expSys != null) expSys.LoadDataServerRpc(data.Level, data.Exp);
        if (classSys != null) classSys.ChangeClassServerRpc((PlayerClassType)data.ClassIndex);
        if (invSys != null) 
        {
            invSys.SyncLegacyMaterialsServerRpc(data.Leather, data.Tooth, data.Skull);
            if (!string.IsNullOrEmpty(data.InventoryData))
                invSys.LoadInventoryServerRpc(data.InventoryData);
        }
        if (equipSys != null && !string.IsNullOrEmpty(data.EquipmentData))
        {
            equipSys.LoadEquipmentServerRpc(data.EquipmentData);
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

    // SavePlayerDataClientRpc는 서버 주도 아키텍처 개편으로 인해 삭제되었습니다.


}
