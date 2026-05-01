using UnityEngine;
using Unity.Netcode;

public class NetworkManagerUI : MonoBehaviour
{
    public static NetworkManagerUI Instance { get; private set; }

    private LobbyUIElements lobbyUI;

    private void Awake()
    {
        Instance = this;

        // CsvDatabase가 없다면 자동 추가 (서버 연동 전 MVP 용)
        if (FindFirstObjectByType<CsvDatabase>() == null)
        {
            gameObject.AddComponent<CsvDatabase>();
        }

        // ItemDatabase가 없다면 자동 추가 (아이템 정적 데이터 매니저)
        if (FindFirstObjectByType<ItemDatabase>() == null)
        {
            gameObject.AddComponent<ItemDatabase>();
        }

        // 기존 자식 오브젝트(수작업 UI) 모두 숨김
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }

        // 절차적 UI(스케치 기반) 자동 생성
        lobbyUI = LobbyUIBuilder.Build(this);

        // 이벤트 연결
        if (lobbyUI.loginBtn != null) lobbyUI.loginBtn.onClick.AddListener(OnLoginClicked);
        if (lobbyUI.registerBtn != null) lobbyUI.registerBtn.onClick.AddListener(OnRegisterClicked);
        if (lobbyUI.serverConnectBtn != null) lobbyUI.serverConnectBtn.onClick.AddListener(OnServerConnectClicked);
        if (lobbyUI.startClientBtn != null) lobbyUI.startClientBtn.onClick.AddListener(OnEnterGameClicked);
        
        // 내정보 버튼 로직: 클릭 시 텍스트 갱신 후 패널 On/Off
        if (lobbyUI.myInfoBtn != null) 
        {
            lobbyUI.myInfoBtn.onClick.AddListener(() => 
            {
                if (lobbyUI.myInfoPanel != null)
                {
                    bool isActive = !lobbyUI.myInfoPanel.activeSelf;
                    if (isActive) RefreshMyInfoPanel();
                    lobbyUI.myInfoPanel.SetActive(isActive);
                }
            });
        }
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void Update()
    {
        // [ 와 ] 키보드 동시 입력 시 서버 헤드리스/은닉 실행
        if ((Input.GetKey(KeyCode.LeftBracket) && Input.GetKeyDown(KeyCode.RightBracket)) ||
            (Input.GetKey(KeyCode.RightBracket) && Input.GetKeyDown(KeyCode.LeftBracket)))
        {
            Debug.Log("Server shortcut pressed!");
            OnStartServerClicked();
        }
    }

    public void OnServerConnectClicked()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;
        
        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        if (transport != null)
        {
            string targetIp = (lobbyUI.ipInput != null && !string.IsNullOrEmpty(lobbyUI.ipInput.text)) ? lobbyUI.ipInput.text.Trim() : "127.0.0.1";
            transport.SetConnectionData(targetIp, 7777); 
        }
        
        if (lobbyUI.serverStatusText != null)
        {
            lobbyUI.serverStatusText.text = "🟡 연결 중...";
            lobbyUI.serverStatusText.color = Color.yellow;
        }
        
        NetworkManager.Singleton.StartClient();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            if (lobbyUI.serverStatusText != null)
            {
                lobbyUI.serverStatusText.text = "🟢 온라인";
                lobbyUI.serverStatusText.color = Color.green;
            }

            ShowMessage("서버에 접속했습니다. 플레이어 준비 중...", Color.yellow);

            // ★ PlayerObject는 연결 직후 바로 스폰되지 않으므로 준비될 때까지 기다림
            StartCoroutine(WaitForPlayerObjectAndEnableUI());
        }
    }

    private System.Collections.IEnumerator WaitForPlayerObjectAndEnableUI()
    {
        // 최대 5초 동안 PlayerObject가 생길 때까지 대기
        float elapsed = 0f;
        while (elapsed < 5f)
        {
            var playerObj = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (playerObj != null)
            {
                // PlayerObject 준비 완료 → UI 활성화
                if (lobbyUI.idInput != null) lobbyUI.idInput.interactable = true;
                if (lobbyUI.pwInput != null) lobbyUI.pwInput.interactable = true;
                if (lobbyUI.nickInput != null) lobbyUI.nickInput.interactable = true;
                if (lobbyUI.loginBtn != null) lobbyUI.loginBtn.interactable = true;
                if (lobbyUI.registerBtn != null) lobbyUI.registerBtn.interactable = true;
                ShowMessage("서버에 접속했습니다. 로그인해주세요.", Color.yellow);
                yield break;
            }
            elapsed += UnityEngine.Time.deltaTime;
            yield return null;
        }
        // 5초 초과 시 연결 실패로 간주
        ShowMessage("플레이어 초기화 오류. 재접속 해주세요.", Color.red);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // 내 클라이언트이거나 서버가 끊어졌을 때 (Timeout)
        if (clientId == NetworkManager.Singleton.LocalClientId || clientId == 0)
        {
            if (lobbyUI.serverStatusText != null)
            {
                lobbyUI.serverStatusText.text = "🔴 오프라인";
                lobbyUI.serverStatusText.color = Color.red;
            }

            if (lobbyUI.idInput != null) lobbyUI.idInput.interactable = false;
            if (lobbyUI.pwInput != null) lobbyUI.pwInput.interactable = false;
            if (lobbyUI.nickInput != null) lobbyUI.nickInput.interactable = false;
            if (lobbyUI.loginBtn != null) lobbyUI.loginBtn.interactable = false;
            if (lobbyUI.registerBtn != null) lobbyUI.registerBtn.interactable = false;

            ShowMessage("서버 비활성화 또는 연결 실패", Color.red);
        }
    }

    public void OnLoginClicked()
    {
        string id = lobbyUI.idInput != null ? lobbyUI.idInput.text : "";
        string pw = lobbyUI.pwInput != null ? lobbyUI.pwInput.text : "";

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
        {
            ShowMessage("ID나 PW를 입력하세요.");
            return;
        }

        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null)
        {
            ShowMessage("아직 서버와 연결 중입니다. 잠시 후 다시 시도하세요.", Color.yellow);
            return;
        }

        // PlayerAuthentication은 RPG_Systems 자식에 있으므로 GetComponentInChildren 사용
        var pAuth = localPlayer.GetComponentInChildren<PlayerAuthentication>();
        if (pAuth != null)
        {
            ShowMessage("로그인 요청 중...", Color.yellow);
            pAuth.LoginRequestServerRpc(id, pw);
        }
        else
        {
            ShowMessage("플레이어 상태 오류. 재접속 해주세요.", Color.red);
        }
    }

    public void OnLoginResponse(bool success, UserData userData, string msg)
    {
        if (success && userData != null)
        {
            LocalUserData.Current = userData;

            // 클라이언트 CsvDatabase 캐시에 즉시 등록 (향후 SaveUser가 정상 작동하도록)
            if (CsvDatabase.Instance != null)
            {
                CsvDatabase.Instance.SaveUser(userData);
            }

            ShowMessage("로그인 성공!", Color.green);
            
            if (lobbyUI.authGroup != null) lobbyUI.authGroup.SetActive(false);
            if (lobbyUI.welcomeText != null)
            {
                lobbyUI.welcomeText.gameObject.SetActive(true);
                lobbyUI.welcomeText.text = $"[{userData.Nickname}]\n<size=20>{userData.ID}</size>";
            }

            if (lobbyUI.startClientBtn != null) lobbyUI.startClientBtn.interactable = true;
            if (lobbyUI.myInfoBtn != null) lobbyUI.myInfoBtn.interactable = true;
        }
        else
        {
            ShowMessage(msg, Color.red);
        }
    }

    public void OnRegisterClicked()
    {
        if (lobbyUI.nickInput != null && !lobbyUI.nickInput.transform.parent.gameObject.activeSelf)
        {
            lobbyUI.nickInput.transform.parent.gameObject.SetActive(true);
            ShowMessage("회원가입 모드: 하단의 닉네임도 적어주세요.", Color.yellow);
            return;
        }

        string id = lobbyUI.idInput != null ? lobbyUI.idInput.text : "";
        string pw = lobbyUI.pwInput != null ? lobbyUI.pwInput.text : "";
        string nick = lobbyUI.nickInput != null ? lobbyUI.nickInput.text : "";

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw) || string.IsNullOrEmpty(nick))
        {
            ShowMessage("ID, PW, 닉네임 3칸을 모두 채워주세요.");
            return;
        }

        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer != null)
        {
            // PlayerAuthentication은 RPG_Systems 자식에 있으므로 GetComponentInChildren 사용
            var pAuth = localPlayer.GetComponentInChildren<PlayerAuthentication>();
            if (pAuth != null)
            {
                pAuth.RegisterRequestServerRpc(id, pw, nick);
            }
        }
    }

    public void OnRegisterResponse(bool success, string msg)
    {
        if (success)
        {
            ShowMessage(msg, Color.green);
            if (lobbyUI.nickInput != null) lobbyUI.nickInput.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            ShowMessage(msg, Color.red);
        }
    }

    public void ShowMessage(string msg, Color? color = null)
    {
        if (lobbyUI != null && lobbyUI.messageText != null)
        {
            lobbyUI.messageText.text = msg;
            lobbyUI.messageText.color = color ?? Color.red;
        }
    }

    private void RefreshMyInfoPanel()
    {
        if (LocalUserData.Current != null)
        {
            var data = LocalUserData.Current;
            string classStr = "초보자";
            if (data.ClassIndex == 1) classStr = "주먹쟁이";
            else if (data.ClassIndex == 2) classStr = "검투사";
            else if (data.ClassIndex == 3) classStr = "법사";
            else if (data.ClassIndex == 4) classStr = "성기사";

            string st = $"[닉네임] {data.Nickname}\n[직업] {classStr}\n\n[Lv] {data.Level}\n[Exp] {data.Exp}\n\n[보유 자원]\n가죽: {data.Leather}\n이빨: {data.Tooth}\n뼈: {data.Skull}\n\n*(현재 능력치/상태)*";
            
            var txtObj = lobbyUI.myInfoPanel.transform.Find("MyInfoStatsText");
            if (txtObj != null)
            {
                var txt = txtObj.GetComponent<UnityEngine.UI.Text>();
                if (txt != null) txt.text = st;
            }
        }
    }

    public void OnStartHostClicked()
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient) return;
        
        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        if (transport != null)
        {
            // Host: 서버 리슨 0.0.0.0 (외부 허용), 파라미터는 (서버주소, 포트, 리슨주소)
            transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
        }
        
        NetworkManager.Singleton.StartHost();
        HideCanvas();
    }

    public void OnEnterGameClicked()
    {
        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer != null)
        {
            // PlayerAuthentication은 RPG_Systems 자식에 있으므로 GetComponentInChildren 사용
            var pAuth = localPlayer.GetComponentInChildren<PlayerAuthentication>();
            if (pAuth != null)
            {
                pAuth.RequestEnterGameServerRpc();
            }
        }
        HideCanvas();
    }

    public void OnStartServerClicked()
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient) return;
        
        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
        }
        
        NetworkManager.Singleton.StartServer();
        HideCanvas();

        // 서버 전용 콘솔 UI 띄우기
        ServerConsole.ShowConsole();
    }

    private void HideCanvas()
    {
        // 최상위 캔버스를 포함하여 전체 비활성화
        if (lobbyUI != null && lobbyUI.loginBtn != null)
        {
            var c = lobbyUI.loginBtn.GetComponentInParent<Canvas>();
            if (c != null) c.gameObject.SetActive(false);
        }

        var mainMenu = GameObject.Find("MainMenu_Canvas");
        if (mainMenu != null)
        {
            mainMenu.SetActive(false);
        }
    }
}
