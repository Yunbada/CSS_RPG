using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LobbyUIElements
{
    public InputField ipInput;
    public InputField idInput;
    public InputField pwInput;
    public InputField nickInput;
    public Text messageText;
    
    public Button loginBtn;
    public Button registerBtn;
    public Button startClientBtn;
    public Button myInfoBtn;
    
    public Text welcomeText;
    public GameObject authGroup;
    public GameObject myInfoPanel;

    // 패치노트용 텍스트 (글작성 가능 공간 대체 표시)
    public Text patchNoteContentText;
}

public static class LobbyUIBuilder
{
    public static LobbyUIElements Build(NetworkManagerUI controller)
    {
        LobbyUIElements elements = new LobbyUIElements();

        // 1. 이벤트 시스템 확인
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // 2. 캔버스 생성/설정
        GameObject canvasObj = new GameObject("LobbyUI_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // 배경 패널
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 1f); // 어두운 회색
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 유틸 함수
        Text CreateText(string name, Transform parent, Vector2 anchoredPos, Vector2 size, int fontSize, Color color, TextAnchor align, Vector2 pivot, Vector2 anchor)
        {
            GameObject txtObj = new GameObject(name);
            txtObj.transform.SetParent(parent, false);
            RectTransform rt = txtObj.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = font; txt.fontSize = fontSize; txt.color = color; txt.alignment = align;
            return txt;
        }

        InputField CreateInputField(string name, Transform parent, Vector2 anchoredPos, Vector2 size, string placeholderText, Vector2 pivot, Vector2 anchor, InputField.ContentType type = InputField.ContentType.Standard)
        {
            GameObject inputObj = new GameObject(name);
            inputObj.transform.SetParent(parent, false);
            RectTransform rt = inputObj.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
            
            Image img = inputObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 1f); // 입력칸 배경

            InputField inputField = inputObj.AddComponent<InputField>();
            
            // Placeholder (입력 전 안내 텍스트) - Stretch 처리
            GameObject phObj = new GameObject("Placeholder");
            phObj.transform.SetParent(inputObj.transform, false);
            RectTransform phRt = phObj.AddComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(10, 0); phRt.offsetMax = new Vector2(-10, 0);
            Text phTxt = phObj.AddComponent<Text>();
            phTxt.font = font; phTxt.fontSize = 24; phTxt.color = new Color(0.5f, 0.5f, 0.5f); phTxt.alignment = TextAnchor.MiddleLeft;
            phTxt.text = placeholderText;

            // 실제 입력되는 Text - Stretch 처리
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(inputObj.transform, false);
            RectTransform txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(10, 0); txtRt.offsetMax = new Vector2(-10, 0);
            Text txt = txtObj.AddComponent<Text>();
            txt.font = font; txt.fontSize = 24; txt.color = Color.white; txt.alignment = TextAnchor.MiddleLeft;
            txt.supportRichText = false;

            inputField.textComponent = txt;
            inputField.placeholder = phTxt;
            inputField.contentType = type;
            return inputField;
        }

        // 라벨 + 인풋필드 그룹퍼 함수
        InputField CreateLabeledInput(string name, Transform parent, Vector2 anchoredPos, string labelStr, string phStr, InputField.ContentType type)
        {
            GameObject rowObj = new GameObject(name + "_Row");
            rowObj.transform.SetParent(parent, false);
            RectTransform rt = rowObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1); rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = new Vector2(300, 40);

            // 좌측 라벨 (넓이 80)
            CreateText(name + "Label", rowObj.transform, new Vector2(-110, 0), new Vector2(80, 40), 24, Color.white, TextAnchor.MiddleRight, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)).text = labelStr;

            // 우측 인풋 (넓이 200)
            return CreateInputField(name, rowObj.transform, new Vector2(50, 0), new Vector2(200, 40), phStr, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), type);
        }

        Button CreateButton(string name, Transform parent, Vector2 anchoredPos, Vector2 size, string btnText, Vector2 pivot, Vector2 anchor, Color bgColor)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();

            Text txt = CreateText("Text", btnObj.transform, Vector2.zero, size, 24, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            txt.text = btnText;

            return btn;
        }

        // 3. UI 조립

        // Top Left: 옵션 아이콘(임시 텍스트)
        CreateText("OptionText", canvasObj.transform, new Vector2(50, -50), new Vector2(100, 50), 32, Color.white, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1)).text = "⚙ 옵션";

        // Top Center: 게임 타이틀
        Text title = CreateText("TitleText", canvasObj.transform, new Vector2(0, -50), new Vector2(500, 100), 80, Color.yellow, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        title.text = "CSS_RPG";
        Shadow shadow = title.gameObject.AddComponent<Shadow>();
        shadow.effectColor = Color.black; shadow.effectDistance = new Vector2(4, -4);

        // Top Right: 서버 주소 & 시작/내정보
        CreateText("ServerTitle", canvasObj.transform, new Vector2(-150, -50), new Vector2(250, 40), 30, Color.white, TextAnchor.MiddleCenter, new Vector2(1, 1), new Vector2(1, 1)).text = "서버 주소";
        elements.ipInput = CreateInputField("ServerInput", canvasObj.transform, new Vector2(-150, -100), new Vector2(250, 50), "127.0.0.1", new Vector2(1, 1), new Vector2(1, 1));
        elements.startClientBtn = CreateButton("StartBtn", canvasObj.transform, new Vector2(-150, -170), new Vector2(250, 50), "시작 (Connect)", new Vector2(1, 1), new Vector2(1, 1), new Color(0.2f, 0.6f, 0.2f));
        elements.myInfoBtn = CreateButton("MyInfoBtn", canvasObj.transform, new Vector2(-150, -230), new Vector2(250, 50), "내정보 (My Info)", new Vector2(1, 1), new Vector2(1, 1), new Color(0.2f, 0.4f, 0.8f));

        // 시작/내정보 기본 비활성화
        elements.startClientBtn.interactable = false;
        elements.myInfoBtn.interactable = false;

        // Left Middle: 패치노트 영역
        CreateText("PatchNoteTitle", canvasObj.transform, new Vector2(50, -150), new Vector2(300, 50), 40, Color.cyan, TextAnchor.LowerLeft, new Vector2(0, 1), new Vector2(0, 1)).text = "[ 패치노트 ]";
        
        GameObject patchPanel = new GameObject("PatchNoteScrollView");
        patchPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform prt = patchPanel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0, 0); prt.anchorMax = new Vector2(0, 1);
        prt.pivot = new Vector2(0, 0);
        prt.anchoredPosition = new Vector2(50, 50); // 하단부터 50 간격
        prt.sizeDelta = new Vector2(500, -260); // 세로 길이 자동 계산

        Image pImg = patchPanel.AddComponent<Image>();
        pImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        ScrollRect scrollRect = patchPanel.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        patchPanel.AddComponent<RectMask2D>();

        GameObject patchContent = new GameObject("Content");
        patchContent.transform.SetParent(patchPanel.transform, false);
        RectTransform crt = patchContent.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
        crt.pivot = new Vector2(0, 1);
        crt.anchoredPosition = Vector2.zero; crt.sizeDelta = new Vector2(0, 1000); // 넉넉한 글작성 공간

        Text patchTxt = CreateText("Text", patchContent.transform, new Vector2(10, -10), new Vector2(-20, -20), 24, Color.white, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1));
        patchTxt.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
        patchTxt.text = "1. 첫 번째 패치 내역입니다.\n2. 인벤토리 버그 수정\n3. 거래 시스템 연동\n\n<작성 공간>";
        elements.patchNoteContentText = patchTxt;

        scrollRect.content = crt;

        // Bottom Right: 로그인 / 회원가입 그룹
        GameObject authGroupObj = new GameObject("AuthGroup");
        authGroupObj.transform.SetParent(canvasObj.transform, false);
        RectTransform art = authGroupObj.AddComponent<RectTransform>();
        art.anchorMin = new Vector2(1, 0); art.anchorMax = new Vector2(1, 0);
        art.pivot = new Vector2(1, 0);
        art.anchoredPosition = new Vector2(-150, 50);
        art.sizeDelta = new Vector2(400, 300);
        elements.authGroup = authGroupObj;

        CreateText("AuthTitle", authGroupObj.transform, new Vector2(0, 40), new Vector2(400, 40), 30, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1)).text = "로그인 / 회원가입";
        
        elements.idInput = CreateLabeledInput("IDInput", authGroupObj.transform, new Vector2(0, -20), "ID:", "아이디", InputField.ContentType.Standard);
        elements.pwInput = CreateLabeledInput("PWInput", authGroupObj.transform, new Vector2(0, -70), "PW:", "비밀번호", InputField.ContentType.Password);
        
        elements.nickInput = CreateLabeledInput("NickInput", authGroupObj.transform, new Vector2(0, -120), "닉네임:", "표시될 이름", InputField.ContentType.Standard);
        // 기본으로 숨김 처리 (부모 Row째로 숨김)
        elements.nickInput.transform.parent.gameObject.SetActive(false);
        
        elements.loginBtn = CreateButton("LoginBtn", authGroupObj.transform, new Vector2(-80, -180), new Vector2(140, 50), "로그인", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Color(0.4f, 0.4f, 0.4f));
        elements.registerBtn = CreateButton("RegisterBtn", authGroupObj.transform, new Vector2(80, -180), new Vector2(140, 50), "회원가입", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Color(0.4f, 0.4f, 0.4f));

        elements.messageText = CreateText("MsgText", authGroupObj.transform, new Vector2(0, -250), new Vector2(400, 40), 20, Color.red, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        elements.messageText.text = "";

        // Bottom Right (로그인 시 닉네임 노출 텍스트) - 시작 시 숨김
        elements.welcomeText = CreateText("WelcomeText", canvasObj.transform, new Vector2(-150, 150), new Vector2(400, 100), 30, Color.white, TextAnchor.MiddleCenter, new Vector2(1, 0), new Vector2(1, 0));
        elements.welcomeText.gameObject.SetActive(false);


        // Center: 내정보 팝업 패널 (시작 시 꺼둠)
        GameObject myInfoObj = new GameObject("MyInfoPanel");
        myInfoObj.transform.SetParent(canvasObj.transform, false);
        Image infoBg = myInfoObj.AddComponent<Image>();
        infoBg.color = new Color(0, 0, 0, 0.9f);
        RectTransform infoRt = myInfoObj.GetComponent<RectTransform>();
        infoRt.anchorMin = new Vector2(0.5f, 0.5f); infoRt.anchorMax = new Vector2(0.5f, 0.5f);
        infoRt.pivot = new Vector2(0.5f, 0.5f);
        infoRt.anchoredPosition = new Vector2(0, 0); // 중앙
        infoRt.sizeDelta = new Vector2(400, 500);
        elements.myInfoPanel = myInfoObj;
        
        CreateText("MyInfoTitle", myInfoObj.transform, new Vector2(0, -20), new Vector2(400, 50), 35, Color.green, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1)).text = "=== 내 정 보 ===";
        CreateText("MyInfoStatsText", myInfoObj.transform, new Vector2(0, -80), new Vector2(360, 400), 24, Color.white, TextAnchor.UpperLeft, new Vector2(0.5f, 1), new Vector2(0.5f, 1)).text = 
            "[직업] 무투가\n\n[Lv] 1\n[Exp] 0 / 100\n\n[스탯 정보]\n- 힘: 10\n- 민첩: 10\n- 재화: 미구현\n\n*(게임 시작 후 반영됨)*";
        
        Button xBtn = CreateButton("CloseBtn", myInfoObj.transform, new Vector2(170, -25), new Vector2(50, 50), "X", new Vector2(0.5f, 1), new Vector2(0.5f, 1), Color.red);
        xBtn.onClick.AddListener(() => elements.myInfoPanel.SetActive(false));
        
        elements.myInfoPanel.SetActive(false);

        return elements;
    }
}
