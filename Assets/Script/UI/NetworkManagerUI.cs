using UnityEngine;
using Unity.Netcode;

public class NetworkManagerUI : MonoBehaviour
{
    // Legacy fields removed

    private LobbyUIElements lobbyUI;

    private void Awake()
    {
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
        if (lobbyUI.startClientBtn != null) lobbyUI.startClientBtn.onClick.AddListener(OnStartClientClicked);
        
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

    public void OnLoginClicked()
    {
        string id = lobbyUI.idInput != null ? lobbyUI.idInput.text : "";
        string pw = lobbyUI.pwInput != null ? lobbyUI.pwInput.text : "";

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
        {
            ShowMessage("ID나 PW를 입력하세요.");
            return;
        }

        // 로그인 확인
        var userData = CsvDatabase.Instance.LoginUser(id, pw);

        // 로그인 성공 처리
        if (userData != null)
        {
            LocalUserData.Current = userData;
            ShowMessage("로그인 성공!", Color.green);
            
            // 로그인/가입창 가리기
            if (lobbyUI.authGroup != null) lobbyUI.authGroup.SetActive(false);
            
            // 닉네임 및 사용자 정보 띄우기
            if (lobbyUI.welcomeText != null)
            {
                lobbyUI.welcomeText.gameObject.SetActive(true);
                lobbyUI.welcomeText.text = $"[{userData.Nickname}]\n<size=20>{userData.ID}</size>";
            }

            // 시작 / 내정보 버튼 활성화
            if (lobbyUI.startClientBtn != null) lobbyUI.startClientBtn.interactable = true;
            if (lobbyUI.myInfoBtn != null) lobbyUI.myInfoBtn.interactable = true;
        }
        else
        {
            ShowMessage("ID 또는 PW가 일치하지 않습니다.", Color.red);
        }
    }

    public void OnRegisterClicked()
    {
        // 1. 닉네임 칸이 숨겨져 있다면 보여주고 안내 메시지 띄움
        if (lobbyUI.nickInput != null && !lobbyUI.nickInput.transform.parent.gameObject.activeSelf)
        {
            lobbyUI.nickInput.transform.parent.gameObject.SetActive(true);
            ShowMessage("회원가입 모드: 하단의 닉네임도 적어주세요.", Color.yellow);
            return;
        }

        // 2. 닉네임 칸이 켜진 상태에서 가입 시도
        string id = lobbyUI.idInput != null ? lobbyUI.idInput.text : "";
        string pw = lobbyUI.pwInput != null ? lobbyUI.pwInput.text : "";
        string nick = lobbyUI.nickInput != null ? lobbyUI.nickInput.text : "";

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw) || string.IsNullOrEmpty(nick))
        {
            ShowMessage("ID, PW, 닉네임 3칸을 모두 채워주세요.");
            return;
        }

        if (CsvDatabase.Instance.RegisterUser(id, pw, nick))
        {
            ShowMessage("회원가입 완료! 로그인 버튼을 눌러주세요.", Color.green);
            // 가입 완료 후 다시 닉네임 입력칸 숨기기 (선택적 편의 기능)
            if (lobbyUI.nickInput != null) lobbyUI.nickInput.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            ShowMessage("이미 존재하는 ID입니다.", Color.red);
        }
    }

    private void ShowMessage(string msg, Color? color = null)
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

    public void OnStartClientClicked()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;
        
        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        if (transport != null)
        {
            string targetIp = (lobbyUI.ipInput != null && !string.IsNullOrEmpty(lobbyUI.ipInput.text)) ? lobbyUI.ipInput.text.Trim() : "127.0.0.1";
            transport.SetConnectionData(targetIp, 7777); 
        }
        
        NetworkManager.Singleton.StartClient();
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
