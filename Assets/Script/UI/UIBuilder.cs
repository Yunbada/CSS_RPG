using UnityEngine;
using UnityEngine.UI;

public static class UIBuilder
{
    public static GameObject CreateGameHUD(PlayerClass localPlayerClass)
    {
        // 1. Root Canvas 생성
        GameObject canvasObj = new GameObject("MainCanvas_Runtime");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // 항상 최상단에 렌더링되도록
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // 2. HUD 컨트롤러 부착
        UIGameHUD hud = canvasObj.AddComponent<UIGameHUD>();

        // 시스템 기본 폰트 로드
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 텍스트 생성 헬퍼 함수
        Text CreateText(string name, Vector2 anchoredPos, Vector2 size, int fontSize, Color color, TextAnchor align, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
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
            txt.raycastTarget = false; // 클릭 이벤트 무시 (성능 최적화)
            
            // 가독성을 위한 그림자 효과
            Shadow shadow = txtObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(2, -2);
            
            return txt;
        }

        // ================= 패널 UI 조립 시작 =================

        // 화면 정중앙 (크로스헤어)
        Text crosshair = CreateText("Crosshair", Vector2.zero, new Vector2(40, 40), 30, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        crosshair.text = "+";

        // 중앙 상단 (타이머)
        hud.timerText = CreateText("TimerText", new Vector2(0, -50), new Vector2(400, 80), 60, Color.white, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        
        // 좌측 상단 (생존자 수)
        hud.humanCountText = CreateText("HumanCount", new Vector2(50, -50), new Vector2(300, 60), 40, Color.cyan, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        
        // 우측 상단 (좀비 수)
        hud.zombieCountText = CreateText("ZombieCount", new Vector2(-50, -50), new Vector2(300, 60), 40, Color.red, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));

        // 좌측 하단 (체력 & 탄약)
        hud.hpText = CreateText("HPText", new Vector2(50, 50), new Vector2(500, 80), 50, Color.green, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        hud.ammoText = CreateText("AmmoText", new Vector2(50, 150), new Vector2(300, 60), 40, Color.yellow, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));

        // 좌측 중앙 (현재 전직 이름)
        hud.classNameText = CreateText("ClassNameText", new Vector2(50, 260), new Vector2(400, 50), 40, new Color(0.5f, 1f, 0.5f), TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        hud.classNameText.text = "< 직업 없음 >";

        // 좌측 중앙 (스킬 상태창 9개)
        hud.skillTexts = new Text[9];
        for(int i = 0; i < 9; i++)
        {
            // 중앙에서 Y좌표 최상단을 200으로 잡고 아래로 50씩 내려가며 배치
            hud.skillTexts[i] = CreateText($"Skill_{i+1}", new Vector2(50, 200 - (i * 50)), new Vector2(400, 50), 30, new Color(1f, 0.8f, 0f), TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        }

        // 폰트 조절 매니저 (UISettingsManager) 호환성 연동
        var settingsMgr = Object.FindObjectOfType<UISettingsManager>();
        if (settingsMgr != null)
        {
            settingsMgr.allUITexts.Add(hud.timerText);
            settingsMgr.allUITexts.Add(hud.humanCountText);
            settingsMgr.allUITexts.Add(hud.zombieCountText);
            settingsMgr.allUITexts.Add(hud.hpText);
            settingsMgr.allUITexts.Add(hud.ammoText);
            settingsMgr.allUITexts.Add(hud.classNameText);
            foreach (var st in hud.skillTexts) settingsMgr.allUITexts.Add(st);
        }

        // ================= 전직 메뉴 (C키 호출) =================
        GameObject classPanelObj = new GameObject("ClassSelectionPanel");
        classPanelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform cpRt = classPanelObj.AddComponent<RectTransform>();
        cpRt.anchorMin = new Vector2(0.5f, 0.5f);
        cpRt.anchorMax = new Vector2(0.5f, 0.5f);
        cpRt.pivot = new Vector2(0.5f, 0.5f);
        cpRt.sizeDelta = new Vector2(800, 500);
        
        Image cpImg = classPanelObj.AddComponent<Image>();
        cpImg.color = new Color(0, 0, 0, 0.85f);
        
        // 전직 안내 텍스트 (명시적으로 panel의 자식으로 셋팅하여 같이 숨겨지도록 수정)
        Text titleTxt = CreateText("Title", new Vector2(0, 150), new Vector2(800, 100), 50, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        titleTxt.text = "전직을 선택하세요";
        titleTxt.transform.SetParent(classPanelObj.transform, false);

        var classCtrl = canvasObj.AddComponent<ClassSelectionController>();
        classCtrl.panel = classPanelObj;
        classCtrl.pClass = localPlayerClass;

        void CreateClassButton(string name, int classIndex, Vector2 pos)
        {
            GameObject btnObj = new GameObject($"Btn_{name}");
            btnObj.transform.SetParent(classPanelObj.transform, false);
            RectTransform brt = btnObj.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = pos;
            brt.sizeDelta = new Vector2(250, 80);
            
            Image bImg = btnObj.AddComponent<Image>();
            bImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => classCtrl.OnClassSelected(classIndex));
            
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

        CreateClassButton("무투가 (Fighter)", 1, new Vector2(-150, 20));
        CreateClassButton("검사 (Swordsman)", 2, new Vector2(150, 20));
        CreateClassButton("거너 (Gunner)", 3, new Vector2(-150, -80));
        CreateClassButton("마법사 (Mage)", 4, new Vector2(150, -80));

        classPanelObj.SetActive(false); // 기본 숨김 처리

        return canvasObj;
    }
}

public class ClassSelectionController : MonoBehaviour
{
    public GameObject panel;
    public PlayerClass pClass;

    void Update()
    {
        // 이미 전직을 고른 상태라면 C 메뉴를 열 수 없게 영구 차단
        if (pClass != null && pClass.currentClass.Value != PlayerClassType.None)
        {
            if (panel != null && panel.activeSelf)
            {
                panel.SetActive(false);
                LockCursor();
            }
            return;
        }

        // C키로 창을 켜고 끄기 (버튼 클릭 전용이므로 숫자키 전직 제거)
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (panel != null)
            {
                bool willOpen = !panel.activeSelf;
                panel.SetActive(willOpen);

                if (willOpen)
                    UnlockCursor();
                else
                    LockCursor();
            }
        }
    }

    public void OnClassSelected(int classIndex)
    {
        if (pClass != null)
        {
            pClass.ChangeClass((PlayerClassType)classIndex);
        }
        if (panel != null) panel.SetActive(false);
        LockCursor();
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
