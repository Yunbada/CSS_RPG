using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Netcode;

public class ServerConsole : MonoBehaviour
{
    private static ServerConsole instance;
    private const int MaxLogs = 50; 

    private Queue<string> itemQueue = new Queue<string>();
    private Queue<string> damageQueue = new Queue<string>();
    private Queue<string> connQueue = new Queue<string>();

    private Text itemText;
    private ScrollRect itemScroll;
    
    private Text dmgText;
    private ScrollRect dmgScroll;

    private Text connText;
    private ScrollRect connScroll;

    private Dropdown playerDropdown;
    private int lastPlayerCount = -1;

    // 데미지 묶음 처리를 위한 데미지 버퍼 (키: Attacker_Target_Skill)
    private Dictionary<string, DamageAccumulator> damageBuffer = new Dictionary<string, DamageAccumulator>();

    private class DamageAccumulator
    {
        public string attackerNick;
        public string targetNick;
        public string skillName;
        public int totalDamage;
        public int finalHp;
        public float lastUpdateTime;
    }

    public static void ShowConsole()
    {
        if (instance != null) return;

        GameObject consoleObj = new GameObject("ServerConsole_Canvas");
        Canvas canvas = consoleObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        consoleObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        consoleObj.AddComponent<GraphicRaycaster>();

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(consoleObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

        instance = consoleObj.AddComponent<ServerConsole>();

        // 3분할 생성 (좌: 접속기록, 중: 데미지, 우: 아이템)
        instance.CreatePanel("접속 기록", 0, out instance.connText, out instance.connScroll, Color.cyan);
        instance.CreatePanel("데미지 로그", 1, out instance.dmgText, out instance.dmgScroll, new Color(1f, 0.4f, 0.4f));
        instance.CreatePanel("아이템 로그", 2, out instance.itemText, out instance.itemScroll, Color.green);

        // 하단 컨트롤 패널 생성
        instance.CreateControlPanel(consoleObj);

        DontDestroyOnLoad(consoleObj);
    }

    private void CreatePanel(string titleStr, int index, out Text txtOut, out ScrollRect srOut, Color titleColor)
    {
        float third = 1f / 3f;
        float anchorMinX = index * third;
        float anchorMaxX = (index + 1) * third;

        GameObject pObj = new GameObject("Panel_" + index);
        pObj.transform.SetParent(transform, false);
        RectTransform pRt = pObj.AddComponent<RectTransform>();
        pRt.anchorMin = new Vector2(anchorMinX, 0.15f);
        pRt.anchorMax = new Vector2(anchorMaxX, 1);
        pRt.offsetMin = new Vector2(5, 5);
        pRt.offsetMax = new Vector2(-5, -5);

        Image pBg = pObj.AddComponent<Image>();
        pBg.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);

        // Title
        GameObject tObj = new GameObject("Title");
        tObj.transform.SetParent(pRt, false);
        Text t = tObj.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = titleStr; t.fontSize = 25; t.color = titleColor; t.alignment = TextAnchor.MiddleCenter;
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0, 1); tRt.anchorMax = new Vector2(1, 1);
        tRt.pivot = new Vector2(0.5f, 1);
        tRt.anchoredPosition = new Vector2(0, -10); tRt.sizeDelta = new Vector2(0, 40);

        // Scroll
        GameObject sObj = new GameObject("ScrollRect");
        sObj.transform.SetParent(pRt, false);
        RectTransform sRt = sObj.AddComponent<RectTransform>();
        sRt.anchorMin = Vector2.zero; sRt.anchorMax = Vector2.one;
        sRt.offsetMin = new Vector2(10, 10); sRt.offsetMax = new Vector2(-10, -60);
        ScrollRect sr = sObj.AddComponent<ScrollRect>();
        sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped;
        sObj.AddComponent<RectMask2D>();

        // Content
        GameObject cObj = new GameObject("Content");
        cObj.transform.SetParent(sRt, false);
        RectTransform cRt = cObj.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0,1); cRt.anchorMax = new Vector2(1,1);
        cRt.pivot = new Vector2(0,1); cRt.anchoredPosition = Vector2.zero; cRt.sizeDelta = new Vector2(0, 2000);
        VerticalLayoutGroup vlg = cObj.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false; vlg.childControlHeight = true;
        cObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Text
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(cRt, false);
        Text txt = txtObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 12; txt.color = Color.white; txt.alignment = TextAnchor.UpperLeft;
        
        sr.content = cRt;
        txtOut = txt;
        srOut = sr;
    }

    private void Update()
    {
        // 0.5초 경과 시 데미지 버퍼에 있는 로그를 화면에 플러시합니다 (누적 출력)
        List<string> keysToRemove = new List<string>();
        float now = Time.time;
        foreach (var kvp in damageBuffer)
        {
            if (now - kvp.Value.lastUpdateTime > 0.5f)
            {
                // 출력 포맷: [닉네임] (스킬명 : 데미지 총합) -> [타겟닉네임] (마지막 남은 체력)
                string log = $"[{kvp.Value.attackerNick}] ({kvp.Value.skillName} : {kvp.Value.totalDamage}) -> [{kvp.Value.targetNick}] ({kvp.Value.finalHp})";
                AddLog(instance.damageQueue, instance.dmgText, instance.dmgScroll, log, "#FF8888");
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove) damageBuffer.Remove(key);

        // 플레이어 리스트 갱신 (인원수 변동 시)
        if (RoundManager.Instance != null && RoundManager.Instance.AllPlayers.Count != lastPlayerCount)
        {
            lastPlayerCount = RoundManager.Instance.AllPlayers.Count;
            UpdatePlayerDropdown();
        }
    }

    // 서버 등에서 명시적으로 정형화된 데미지 호출 (PlayerState.cs 에서 호출)
    public static void LogDamage(string attackerNick, string targetNick, string skillName, int damage, int remainingHp)
    {
        if (instance == null) return;
        string key = attackerNick + "_" + targetNick + "_" + skillName;
        
        if (!instance.damageBuffer.ContainsKey(key))
        {
            instance.damageBuffer[key] = new DamageAccumulator 
            { 
                attackerNick = attackerNick, 
                targetNick = targetNick, 
                skillName = skillName,
                totalDamage = 0,
                finalHp = remainingHp,
                lastUpdateTime = Time.time 
            };
        }
        
        var acc = instance.damageBuffer[key];
        acc.totalDamage += damage;
        acc.finalHp = remainingHp;
        acc.lastUpdateTime = Time.time;
    }

    // 도트 데미지 명시적 호출
    public static void LogDoT(string targetNick, string dotType, int damage, int remainingHp)
    {
        if (instance == null) return;
        string log = $"[{targetNick}] ({dotType}: {damage}) -> [{targetNick}] ({remainingHp})";
        AddLog(instance.damageQueue, instance.dmgText, instance.dmgScroll, log, "#FFAAAA");
    }

    public static void LogItem(string log)
    {
        if (instance == null) return;
        AddLog(instance.itemQueue, instance.itemText, instance.itemScroll, log, "#88FF88");
    }

    public static void LogConnection(string nick, string ip)
    {
        if (instance == null) return;
        AddLog(instance.connQueue, instance.connText, instance.connScroll, $"[{nick}] 님이 접속함 / IP: {ip}", "#88FFFF");
    }

    private static void AddLog(Queue<string> q, Text txt, ScrollRect sr, string msg, string hexColor)
    {
        string tStr = System.DateTime.Now.ToString("HH:mm:ss");
        q.Enqueue($"<color={hexColor}>[{tStr}] {msg}</color>");
        if (q.Count > MaxLogs) q.Dequeue();

        txt.text = string.Join("\n", q.ToArray());
        instance.Invoke(nameof(ForceScrollBottom), 0.1f);
    }

    private void ForceScrollBottom()
    {
        if (connScroll) connScroll.verticalNormalizedPosition = 0f;
        if (dmgScroll) dmgScroll.verticalNormalizedPosition = 0f;
        if (itemScroll) itemScroll.verticalNormalizedPosition = 0f;
    }

    private void CreateControlPanel(GameObject parentObj)
    {
        GameObject cpObj = new GameObject("ControlPanel");
        cpObj.transform.SetParent(parentObj.transform, false);
        RectTransform cpRt = cpObj.AddComponent<RectTransform>();
        cpRt.anchorMin = new Vector2(0, 0);
        cpRt.anchorMax = new Vector2(1, 0.15f);
        cpRt.offsetMin = new Vector2(5, 5);
        cpRt.offsetMax = new Vector2(-5, -5);

        Image cpBg = cpObj.AddComponent<Image>();
        cpBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        HorizontalLayoutGroup hlg = cpObj.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
        hlg.spacing = 10;
        hlg.padding = new RectOffset(10, 10, 10, 10);

        DefaultControls.Resources uiResources = new DefaultControls.Resources();

        // 1. Player Dropdown
        GameObject dropdownObj = DefaultControls.CreateDropdown(uiResources);
        dropdownObj.transform.SetParent(cpObj.transform, false);
        playerDropdown = dropdownObj.GetComponent<Dropdown>();
        
        Text label = playerDropdown.captionText;
        if (label != null)
        {
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 20;
            label.color = Color.black;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
        }

        Text itemLabel = playerDropdown.itemText;
        if (itemLabel != null)
        {
            itemLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itemLabel.fontSize = 20;
            itemLabel.color = Color.black;
            itemLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            itemLabel.verticalOverflow = VerticalWrapMode.Overflow;
        }

        // Buttons
        CreateButton(cpObj, uiResources, "Kill", OnKillClicked, new Color(1f, 0.5f, 0.5f));
        CreateButton(cpObj, uiResources, "Item", OnItemClicked, new Color(0.5f, 1f, 0.5f));
        CreateButton(cpObj, uiResources, "ReStart", OnStartClicked, new Color(0.5f, 0.5f, 1f));
        CreateButton(cpObj, uiResources, "Freeze", OnFreezeClicked, new Color(1f, 1f, 0.5f));
    }

    private void CreateButton(GameObject parent, DefaultControls.Resources res, string text, UnityEngine.Events.UnityAction action, Color color)
    {
        GameObject btnObj = DefaultControls.CreateButton(res);
        btnObj.transform.SetParent(parent.transform, false);
        btnObj.GetComponent<Image>().color = color;
        Text t = btnObj.GetComponentInChildren<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 25;
        t.color = Color.black;
        btnObj.GetComponent<Button>().onClick.AddListener(action);
    }

    private void OnKillClicked()
    {
        if (playerDropdown == null || RoundManager.Instance == null) return;
        var players = RoundManager.Instance.AllPlayers;
        if (playerDropdown.value >= 0 && playerDropdown.value < players.Count)
        {
            var target = players[playerDropdown.value];
            if (target != null)
            {
                var health = target.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(9999, "Admin Kill", 9999);
                    string targetNick = "Unknown";
                    var auth = target.GetComponent<PlayerAuthentication>();
                    if (auth != null) targetNick = auth.Nickname.Value.ToString();
                    LogDamage("Admin", targetNick, "Kill Command", 9999, 0);
                }
            }
        }
    }

    private void OnItemClicked()
    {
        if (ItemDatabase.Instance != null)
        {
            ItemDatabase.Instance.RollLootDrop(Vector3.up);
            LogItem("[Admin] Spawned LootDrop at (0,1,0)");
        }
    }

    private void OnStartClicked()
    {
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.StartRound();
            AddLog(instance.connQueue, instance.connText, instance.connScroll, "[Admin] Force Round Restart", "#FFFF00");
        }
    }

    private void OnFreezeClicked()
    {
        bool isFreezing = Time.timeScale != 0f;
        Time.timeScale = isFreezing ? 0f : 1f;
        
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.SetFreezeStateClientRpc(isFreezing);
        }
        AddLog(instance.connQueue, instance.connText, instance.connScroll, $"[Admin] Game Freeze: {(isFreezing ? "ON" : "OFF")}", "#FFFF00");
    }

    private void UpdatePlayerDropdown()
    {
        if (playerDropdown == null || RoundManager.Instance == null) return;
        
        var players = RoundManager.Instance.AllPlayers;
        string currentSelection = playerDropdown.options.Count > 0 ? playerDropdown.options[playerDropdown.value].text : "";
        
        playerDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var p in players)
        {
            if (p != null)
            {
                var auth = p.GetComponent<PlayerAuthentication>();
                string nick = (auth != null && !string.IsNullOrEmpty(auth.Nickname.Value.ToString())) ? auth.Nickname.Value.ToString() : $"유저{p.OwnerClientId}";
                options.Add(nick);
            }
        }
        
        if (options.Count == 0) options.Add("No Players");
        playerDropdown.AddOptions(options);

        int newIdx = options.IndexOf(currentSelection);
        if (newIdx >= 0) playerDropdown.value = newIdx;
        else playerDropdown.value = 0;
    }
}
