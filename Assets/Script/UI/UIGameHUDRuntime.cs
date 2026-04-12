using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// 기존 UIBuilder.cs + UIGameHUD.cs + ClassSelectionController.cs 의 기능을
/// 단일 독립 MonoBehaviour로 통합한 런타임 전용 게임 HUD.
/// 프리팹/씬에 의존하지 않고 RuntimeInitializeOnLoadMethod로 자동 생성됩니다.
/// </summary>
public class UIGameHUDRuntime : MonoBehaviour
{
    private static UIGameHUDRuntime instance;
    private GameObject canvasObj;

    // =========================================================================
    // HUD 텍스트 참조 (원본 UIGameHUD 필드 구조와 동일)
    // =========================================================================
    [Header("Match Info")]
    private Text timerText;
    private Text humanCountText;
    private Text zombieCountText;

    [Header("Player Info")]
    private Text hpText;
    private Text ammoText;
    private Text[] skillTexts;
    private Text classNameText;

    [Header("Right Info")]
    private Text expText;

    [Header("Center Info")]
    private Text totalDamageText;

    // =========================================================================
    // 전직 메뉴
    // =========================================================================
    private GameObject classPanel;
    private PlayerClass pClass;

    // =========================================================================
    // 자동 초기화
    // =========================================================================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Initialize()
    {
        if (instance != null) return;
        var go = new GameObject("UIGameHUD_Manager");
        DontDestroyOnLoad(go);
        go.AddComponent<UIGameHUDRuntime>();
    }

    private void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // =========================================================================
    // Canvas & HUD 생성 (원본 UIBuilder.CreateGameHUD 구조 1:1 복원)
    // =========================================================================
    private void CreateHUD()
    {
        if (canvasObj != null) return;

        // ----- 1. Root Canvas 생성 -----
        canvasObj = new GameObject("MainCanvas_Runtime");
        DontDestroyOnLoad(canvasObj);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f; // 버그#3: 가로/세로 비율을 균형있게 반영하여 창모드 대응

        canvasObj.AddComponent<GraphicRaycaster>();

        // ----- 2. 텍스트 생성 헬퍼 (원본 UIBuilder.CreateText와 동일) -----
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Text CreateText(string name, Vector2 anchoredPos, Vector2 size, int fontSize,
                        Color color, TextAnchor align,
                        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            GameObject txtObj = new GameObject(name);
            txtObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rt = txtObj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = align;
            txt.raycastTarget = false;

            Shadow shadow = txtObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(2, -2);

            return txt;
        }

        // ================= 패널 UI 조립 시작 (원본 UIBuilder 배치 그대로) =================

        // 화면 정중앙 (크로스헤어)
        Text crosshair = CreateText("Crosshair", Vector2.zero, new Vector2(40, 40), 30,
            new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        crosshair.text = "+";

        // 크로스헤어 바로 아래 (누적 데미지)
        totalDamageText = CreateText("TotalDamageText", new Vector2(0, -30), new Vector2(200, 40), 22,
            new Color(1f, 0.4f, 0.4f, 0.9f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        totalDamageText.text = "DMG: 0";

        // 중앙 상단 (타이머)
        timerText = CreateText("TimerText", new Vector2(0, -50), new Vector2(400, 80), 60,
            Color.white, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

        // 좌측 상단 (생존자 수)
        humanCountText = CreateText("HumanCount", new Vector2(50, -50), new Vector2(300, 60), 40,
            Color.cyan, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        // 우측 상단 (좀비 수)
        zombieCountText = CreateText("ZombieCount", new Vector2(-50, -50), new Vector2(300, 60), 40,
            Color.red, TextAnchor.UpperRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));

        // 좌측 하단 (체력 & 탄약)
        hpText = CreateText("HPText", new Vector2(50, 50), new Vector2(500, 80), 50,
            Color.green, TextAnchor.LowerLeft,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        ammoText = CreateText("AmmoText", new Vector2(50, 150), new Vector2(300, 60), 40,
            Color.yellow, TextAnchor.LowerLeft,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));

        // 좌측 중앙 (현재 전직 이름)
        classNameText = CreateText("ClassNameText", new Vector2(50, 260), new Vector2(400, 50), 40,
            new Color(0.5f, 1f, 0.5f), TextAnchor.MiddleLeft,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        classNameText.text = "< 직업 없음 >";

        // 좌측 중앙 (스킬 상태창 9개)
        skillTexts = new Text[9];
        for (int i = 0; i < 9; i++)
        {
            skillTexts[i] = CreateText($"Skill_{i + 1}", new Vector2(50, 200 - (i * 50)),
                new Vector2(400, 50), 30, new Color(1f, 0.8f, 0f), TextAnchor.MiddleLeft,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        }

        // 우측 중앙 (레벨 및 경험치 표시)
        expText = CreateText("ExpLevelText", new Vector2(-50, 300), new Vector2(300, 80), 30,
            Color.white, TextAnchor.MiddleRight,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
        expText.text = "<Lv. 00>\n<Exp: 0000 / 0000>";

        // ================= 전직 메뉴 (C키 호출) =================
        classPanel = new GameObject("ClassSelectionPanel");
        classPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform cpRt = classPanel.AddComponent<RectTransform>();
        cpRt.anchorMin = new Vector2(0.5f, 0.5f);
        cpRt.anchorMax = new Vector2(0.5f, 0.5f);
        cpRt.pivot = new Vector2(0.5f, 0.5f);
        cpRt.sizeDelta = new Vector2(800, 500);

        Image cpImg = classPanel.AddComponent<Image>();
        cpImg.color = new Color(0, 0, 0, 0.85f);

        Text titleTxt = CreateText("Title", new Vector2(0, 150), new Vector2(800, 100), 50,
            Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        titleTxt.text = "전직을 선택하세요";
        titleTxt.transform.SetParent(classPanel.transform, false);

        CreateClassButton(font, "무투가 (Fighter)", 1, new Vector2(-150, 20));
        CreateClassButton(font, "검사 (Swordsman)", 2, new Vector2(150, 20));
        CreateClassButton(font, "거너 (Gunner)", 3, new Vector2(-150, -80));
        CreateClassButton(font, "마법사 (Mage)", 4, new Vector2(150, -80));
        CreateClassButton(font, "성기사 (Paladin)", 5, new Vector2(0, -180));

        classPanel.SetActive(false);
        canvasObj.SetActive(false); // 접속 전에는 숨김
    }

    private void CreateClassButton(Font font, string name, int classIndex, Vector2 pos)
    {
        GameObject btnObj = new GameObject($"Btn_{name}");
        btnObj.transform.SetParent(classPanel.transform, false);
        RectTransform brt = btnObj.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = pos;
        brt.sizeDelta = new Vector2(250, 80);

        Image bImg = btnObj.AddComponent<Image>();
        bImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => OnClassSelected(classIndex));

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform trt = txtObj.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;
        Text txt = txtObj.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = 40;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = name;
    }

    private void OnClassSelected(int classIndex)
    {
        if (pClass != null)
        {
            pClass.ChangeClass((PlayerClassType)classIndex);
        }
        if (classPanel != null) classPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // =========================================================================
    // 매 프레임 업데이트 (원본 UIGameHUD.Update 로직 복원)
    // =========================================================================
    private void Update()
    {
        // 네트워크 미접속 시 숨기기
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient ||
            NetworkManager.Singleton.LocalClient == null ||
            NetworkManager.Singleton.LocalClient.PlayerObject == null)
        {
            if (canvasObj != null) canvasObj.SetActive(false);
            return;
        }

        // 최초 접속 시 HUD 생성
        if (canvasObj == null) CreateHUD();
        canvasObj.SetActive(true);

        var localObj = NetworkManager.Singleton.LocalClient.PlayerObject;
        pClass = localObj.GetComponentInChildren<PlayerClass>();

        // --- 전직 메뉴 C키 토글 (원본 ClassSelectionController 로직) ---
        // 좀비는 전직 메뉴를 열 수 없음
        var localState = localObj.GetComponentInChildren<PlayerState>();
        bool isHuman = localState != null && localState.currentTeam.Value == Team.Human;

        if (isHuman && pClass != null && pClass.currentClass.Value == PlayerClassType.None)
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                bool willOpen = !classPanel.activeSelf;
                classPanel.SetActive(willOpen);
                if (willOpen) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
                else { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            }
        }
        else if (classPanel != null && classPanel.activeSelf)
        {
            classPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // --- HUD 정보 갱신 (원본 UIGameHUD 로직) ---
        UpdateMatchInfo();
        UpdatePlayerInfo(localObj.gameObject, localState);
    }

    // =========================================================================
    // 매치 정보 업데이트 (타이머, 인원수)
    // =========================================================================
    private void UpdateMatchInfo()
    {
        if (RoundManager.Instance == null) return;

        float timer = RoundManager.Instance.roundTimer.Value;
        int min = Mathf.FloorToInt(timer / 60f);
        int sec = Mathf.FloorToInt(timer % 60f);

        if (timerText != null) timerText.text = $"{min:D2}:{sec:D2}";

        int humanCount = 0;
        int zombieCount = 0;
        foreach (var p in PlayerState.AllPlayersList)
        {
            if (p != null)
            {
                if (p.currentTeam.Value == Team.Human) humanCount++;
                else zombieCount++;
            }
        }
        if (humanCountText != null) humanCountText.text = $"Humans: {humanCount}";
        if (zombieCountText != null) zombieCountText.text = $"Zombies: {zombieCount}";
    }

    // =========================================================================
    // 플레이어 정보 업데이트 (HP, 스킬, 경험치, 데미지 등)
    // =========================================================================
    private void UpdatePlayerInfo(GameObject localObj, PlayerState state)
    {
        // 버그#1: 팀(인간/좀비) 판별
        bool isZombie = state != null && state.currentTeam.Value != Team.Human;

        // HP
        if (hpText != null && state != null)
        {
            hpText.text = $"HP: {state.currentHealth.Value} / {state.maxHealth.Value}";
        }

        // Exp / Level
        var pExp = localObj.GetComponentInChildren<PlayerExperience>();
        if (expText != null && pExp != null)
        {
            int maxExp = pExp.Level.Value * 100;
            expText.text = $"<Lv. {pExp.Level.Value}>\n<Exp: {pExp.CurrentExp.Value} / {maxExp}>";
        }

        // 누적 데미지 (서버 측 NetworkVariable에서 읽음 - DoT 포함 정확한 값)
        if (totalDamageText != null && state != null)
        {
            totalDamageText.text = $"DMG: {state.totalDamageDealt.Value}";
        }

        // 버그#1: 전직 이름 표시 - 좀비면 "좀비"로 표시
        if (classNameText != null)
        {
            if (isZombie)
            {
                // 좀비 타입에 따라 표시
                if (state.currentZombieType.Value == ZombieType.None)
                    classNameText.text = "< 좀비 >";
                else
                    classNameText.text = $"< 좀비 ({state.currentZombieType.Value}) >";
                classNameText.color = Color.red;
            }
            else if (pClass != null)
            {
                if (pClass.currentClass.Value == PlayerClassType.None)
                    classNameText.text = "< 직업 없음 >";
                else
                    classNameText.text = $"< {pClass.currentClass.Value} >";
                classNameText.color = new Color(0.5f, 1f, 0.5f);
            }
        }

        // 버그#4: Ammo / Shield Energy - 좀비면 충전량 숨김
        if (ammoText != null)
        {
            if (isZombie)
            {
                // 좀비는 충전량/탄약 표시 안 함
                ammoText.gameObject.SetActive(false);
            }
            else if (pClass != null && pClass.currentClass.Value == PlayerClassType.Gunner)
            {
                ammoText.gameObject.SetActive(true);
                ammoText.text = "Ammo: 30 / ∞";
            }
            else if (pClass != null && pClass.currentClass.Value == PlayerClassType.Paladin)
            {
                ammoText.gameObject.SetActive(true);
                int currentEnergy = 0;
                var executor = localObj.GetComponentInChildren<PaladinSkillExecutor>();
                if (executor != null)
                {
                    currentEnergy = executor.ShieldEnergy;
                }
                ammoText.text = $"충전량: {currentEnergy}%";
            }
            else
            {
                ammoText.gameObject.SetActive(false);
            }
        }

        // 스킬 UI (SkillSystem 데이터 직접 폴링)
        // 버그#1: 좀비면 스킬 표시를 비움
        var skillSys = localObj.GetComponentInChildren<SkillSystem>();
        if (skillSys != null && skillTexts != null)
        {
            for (int i = 0; i < 9 && i < skillTexts.Length; i++)
            {
                if (skillTexts[i] == null) continue;

                if (isZombie)
                {
                    skillTexts[i].text = "";
                    continue;
                }

                var s = skillSys.currentSkills[i];
                if (string.IsNullOrEmpty(s.skillName))
                {
                    skillTexts[i].text = "";
                    continue;
                }

                if (s.currentCooldown > 0f)
                {
                    skillTexts[i].color = Color.gray;
                    skillTexts[i].text = $"{i + 1}. {s.skillName} [{Mathf.CeilToInt(s.currentCooldown)}초]";
                }
                else
                {
                    skillTexts[i].color = new Color(1f, 0.8f, 0f);
                    skillTexts[i].text = $"{i + 1}. {s.skillName}";
                }
            }
        }
    }

    // =========================================================================
    // 외부에서 호출 가능한 스킬 UI 업데이트 (SkillSystem 호환)
    // =========================================================================
    public void UpdateSkillUI(int index, string skillName, float currentCooldown, float maxCooldown)
    {
        if (skillTexts == null || index < 0 || index >= skillTexts.Length || skillTexts[index] == null)
            return;

        Text targetText = skillTexts[index];

        if (string.IsNullOrEmpty(skillName))
        {
            targetText.text = "";
            return;
        }

        if (currentCooldown > 0f)
        {
            targetText.color = Color.gray;
            targetText.text = $"{index + 1}. {skillName} [{Mathf.CeilToInt(currentCooldown)}초]";
        }
        else
        {
            targetText.color = new Color(1f, 0.8f, 0f);
            targetText.text = $"{index + 1}. {skillName}";
        }
    }
}
