using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 인벤토리 UI 상태머신 컨트롤러
/// N키 패널이 열려있을 때 서브화면(메인/장비/인벤토리/제작소/디버그)을 관리합니다.
/// SkillSystem에서 키 입력을 받아 이 클래스에서 화면 전환 및 표시를 담당합니다.
/// </summary>
public enum InventoryViewState
{
    Main,       // 메인 메뉴 (1=장비, 2=인벤, 3=제작, 0=디버그)
    Equipment,  // 장비 슬롯 화면
    Inventory,  // 인벤토리 보유 현황
    Crafting,   // 제작소
    Debug,       // 디버그 메뉴
    TradeSearch, // 거래할 플레이어/요청 찾기
    TradeSession,// 실제 1:1 거래 중
    TradeInventorySelect // 거래에 올릴 아이템 선택
}

public class InventoryUIController
{
    // 현재 상태
    public InventoryViewState CurrentState { get; private set; } = InventoryViewState.Main;

    // 제작소 페이지
    private int craftingPage = 0;
    private const int RECIPES_PER_PAGE = 7;

    // 참조
    private InventorySystem inventory;
    private EquipmentSystem equipment;
    private PlayerClass playerClass;
    private PlayerExperience playerExp;
    private PlayerTradeSystem trade;

    private List<ulong> nearbyPlayersCache = new List<ulong>();

    public InventoryUIController(InventorySystem inv, EquipmentSystem equip, PlayerClass pClass, PlayerExperience pExp, PlayerTradeSystem pTrade)
    {
        inventory = inv;
        equipment = equip;
        playerClass = pClass;
        playerExp = pExp;
        trade = pTrade;
    }

    /// <summary>패널이 열릴 때 메인 메뉴로 초기화</summary>
    public void ResetToMain()
    {
        CurrentState = InventoryViewState.Main;
        craftingPage = 0;
    }

    /// <summary>키 입력 처리. pressedKey: 0~8 (Alpha1~9), -2 (Alpha0)</summary>
    public void HandleInput(int pressedKey, UIGameHUD hud)
    {
        switch (CurrentState)
        {
            case InventoryViewState.Main:
                HandleMainInput(pressedKey, hud);
                break;
            case InventoryViewState.Equipment:
                HandleEquipmentInput(pressedKey, hud);
                break;
            case InventoryViewState.Inventory:
                HandleInventoryInput(pressedKey, hud);
                break;
            case InventoryViewState.Crafting:
                HandleCraftingInput(pressedKey, hud);
                break;
            case InventoryViewState.Debug:
                HandleDebugInput(pressedKey, hud);
                break;
            case InventoryViewState.TradeSearch:
                HandleTradeSearchInput(pressedKey, hud);
                break;
            case InventoryViewState.TradeSession:
                HandleTradeSessionInput(pressedKey, hud);
                break;
            case InventoryViewState.TradeInventorySelect:
                HandleTradeInventorySelectInput(pressedKey, hud);
                break;
        }
    }

    /// <summary>현재 상태에 맞게 UI 텍스트 갱신</summary>
    public void RefreshDisplay(UIGameHUD hud)
    {
        if (hud == null || hud.inventoryTexts == null) return;

        switch (CurrentState)
        {
            case InventoryViewState.Main:
                DisplayMain(hud);
                break;
            case InventoryViewState.Equipment:
                DisplayEquipment(hud);
                break;
            case InventoryViewState.Inventory:
                DisplayInventory(hud);
                break;
            case InventoryViewState.Crafting:
                DisplayCrafting(hud);
                break;
            case InventoryViewState.Debug:
                DisplayDebug(hud);
                break;
            case InventoryViewState.TradeSearch:
                DisplayTradeSearch(hud);
                break;
            case InventoryViewState.TradeSession:
                DisplayTradeSession(hud);
                break;
            case InventoryViewState.TradeInventorySelect:
                DisplayTradeInventorySelect(hud);
                break;
        }
    }

    // =========================================================================
    // 메인 메뉴
    // =========================================================================
    private void HandleMainInput(int key, UIGameHUD hud)
    {
        if (key == 0) { CurrentState = InventoryViewState.Equipment; RefreshDisplay(hud); }
        else if (key == 1) { CurrentState = InventoryViewState.Inventory; RefreshDisplay(hud); }
        else if (key == 2) { CurrentState = InventoryViewState.Crafting; craftingPage = 0; RefreshDisplay(hud); }
        else if (key == 3) { CurrentState = InventoryViewState.TradeSearch; RefreshDisplay(hud); }
        else if (key == -2) { CurrentState = InventoryViewState.Debug; RefreshDisplay(hud); }
    }

    private void DisplayMain(UIGameHUD hud)
    {
        ClearTexts(hud);
        SetText(hud, 0, "1. 장비 슬롯", Color.white);
        SetText(hud, 1, "2. 인벤토리", Color.white);
        SetText(hud, 2, "3. 제작소", Color.white);
        SetText(hud, 3, "4. 플레이어 거래", Color.white);
        SetText(hud, 4, "5. [미구현]", new Color(0.5f, 0.5f, 0.5f));
        SetText(hud, 5, "6. [미구현]", new Color(0.5f, 0.5f, 0.5f));
        SetText(hud, 6, "7. [미구현]", new Color(0.5f, 0.5f, 0.5f));
        SetText(hud, 7, "", Color.white);
        SetText(hud, 8, "", Color.white);
    }

    // =========================================================================
    // 장비 슬롯
    // =========================================================================
    private void HandleEquipmentInput(int key, UIGameHUD hud)
    {
        if (key == -2) // 0번 → 뒤로
        {
            CurrentState = InventoryViewState.Main;
            RefreshDisplay(hud);
            return;
        }

        // 1~5번 (key 0~4) → 해당 슬롯 해제
        if (key >= 0 && key < 5 && equipment != null)
        {
            int itemId = equipment.GetEquippedItemId(EquipmentSystem.SlotOrder[key]);
            if (itemId > 0)
            {
                equipment.UnequipServerRpc(key);
                Debug.Log($"[인벤토리UI] {EquipmentSystem.SlotOrder[key]} 슬롯 장비 해제");
            }
            // 짧은 딜레이 후 갱신 (1프레임 뒤)
            RefreshDisplay(hud);
        }
    }

    private void DisplayEquipment(UIGameHUD hud)
    {
        ClearTexts(hud);
        SetText(hud, 0, "── [ 장비 슬롯 ] ──", new Color(1f, 0.85f, 0.3f));

        string[] slotNames = { "무기", "투구", "갑옷", "장갑", "신발" };
        for (int i = 0; i < 5; i++)
        {
            string itemName = "[없음]";
            Color color = new Color(0.5f, 0.5f, 0.5f);

            if (equipment != null)
            {
                var data = equipment.GetEquippedItemData(EquipmentSystem.SlotOrder[i]);
                if (data != null)
                {
                    itemName = data.Name;
                    color = GetRarityColor(data.Rarity);
                }
            }
            SetText(hud, i + 1, $"{i + 1}. {slotNames[i]}: {itemName}", color);
        }

        SetText(hud, 6, "", Color.white);
        SetText(hud, 7, "※ 번호를 눌러 해제", new Color(0.7f, 0.7f, 0.7f));
        SetText(hud, 8, "0. 뒤로가기", Color.white);
    }

    // =========================================================================
    // 인벤토리
    // =========================================================================
    private void HandleInventoryInput(int key, UIGameHUD hud)
    {
        if (key == -2) // 0번 → 뒤로
        {
            CurrentState = InventoryViewState.Main;
            RefreshDisplay(hud);
            return;
        }

        if (key == 7 && inventory != null) // 8번 → 이전 페이지
        {
            inventory.PrevPage();
            RefreshDisplay(hud);
            return;
        }

        if (key == 8 && inventory != null) // 9번 → 다음 페이지
        {
            inventory.NextPage();
            RefreshDisplay(hud);
            return;
        }

        // 1~7번 (key 0~6) → 해당 아이템 장착 시도
        if (key >= 0 && key < InventorySystem.SLOTS_PER_PAGE && inventory != null && equipment != null)
        {
            int actualIndex = inventory.CurrentPage * InventorySystem.SLOTS_PER_PAGE + key;
            var slot = inventory.GetSlot(actualIndex);
            if (!slot.IsEmpty)
            {
                var itemData = ItemDatabase.Instance?.GetItem(slot.ItemID);
                if (itemData != null && itemData.Type == ItemType.Equipment)
                {
                    equipment.EquipFromInventoryServerRpc(actualIndex);
                    Debug.Log($"[인벤토리UI] {itemData.Name} 장착 시도");
                }
                else if (itemData != null)
                {
                    Debug.Log($"[인벤토리UI] {itemData.Name}은(는) 장착할 수 없는 아이템입니다.");
                }
            }
            RefreshDisplay(hud);
        }
    }

    private void DisplayInventory(UIGameHUD hud)
    {
        ClearTexts(hud);
        int page = inventory != null ? inventory.CurrentPage + 1 : 1;
        int totalPages = inventory != null ? inventory.TotalPages : 1;
        SetText(hud, 0, $"── [ 인벤토리 ] ({page}/{totalPages}) ──", new Color(0.3f, 0.85f, 1f));

        List<InventorySlot> pageSlots = inventory != null
            ? inventory.GetCurrentPageSlots()
            : new List<InventorySlot>();

        for (int i = 0; i < InventorySystem.SLOTS_PER_PAGE; i++)
        {
            if (i < pageSlots.Count && !pageSlots[i].IsEmpty)
            {
                var itemData = ItemDatabase.Instance?.GetItem(pageSlots[i].ItemID);
                if (itemData != null)
                {
                    string countStr = itemData.IsStackable ? $" x{pageSlots[i].Count}" : "";
                    string equipTag = itemData.Type == ItemType.Equipment ? " [장비]" : "";
                    Color color = GetRarityColor(itemData.Rarity);
                    SetText(hud, i + 1, $"{i + 1}. {itemData.Name}{countStr}{equipTag}", color);
                }
                else
                {
                    SetText(hud, i + 1, $"{i + 1}. ??? (ID:{pageSlots[i].ItemID})", Color.gray);
                }
            }
            else
            {
                SetText(hud, i + 1, $"{i + 1}. [빈 슬롯]", new Color(0.4f, 0.4f, 0.4f));
            }
        }

        SetText(hud, 8, "8.◀이전  9.▶다음  0.뒤로", new Color(0.7f, 0.7f, 0.7f));
    }

    // =========================================================================
    // 제작소
    // =========================================================================
    private void HandleCraftingInput(int key, UIGameHUD hud)
    {
        if (key == -2) // 0번 → 뒤로
        {
            CurrentState = InventoryViewState.Main;
            RefreshDisplay(hud);
            return;
        }

        if (key == 7) // 8번 → 이전 페이지
        {
            if (craftingPage > 0) craftingPage--;
            RefreshDisplay(hud);
            return;
        }

        if (key == 8) // 9번 → 다음 페이지
        {
            if (ItemDatabase.Instance != null)
            {
                int maxPage = Mathf.Max(0, Mathf.CeilToInt((float)ItemDatabase.Instance.GetAllRecipes().Count / RECIPES_PER_PAGE) - 1);
                if (craftingPage < maxPage) craftingPage++;
            }
            RefreshDisplay(hud);
            return;
        }

        // 1~7번 (key 0~6) → 레시피 제작
        if (key >= 0 && key < RECIPES_PER_PAGE && inventory != null && ItemDatabase.Instance != null)
        {
            var recipes = ItemDatabase.Instance.GetAllRecipes();
            int recipeIndex = craftingPage * RECIPES_PER_PAGE + key;
            if (recipeIndex < recipes.Count)
            {
                inventory.CraftByRecipeServerRpc(recipes[recipeIndex].RecipeID);
                Debug.Log($"[제작소] {recipes[recipeIndex].ResultItemName} 제작 요청");
            }
            RefreshDisplay(hud);
        }
    }

    private void DisplayCrafting(UIGameHUD hud)
    {
        ClearTexts(hud);

        var recipes = ItemDatabase.Instance != null
            ? ItemDatabase.Instance.GetAllRecipes()
            : new List<RecipeData>();

        int totalCraftPages = Mathf.Max(1, Mathf.CeilToInt((float)recipes.Count / RECIPES_PER_PAGE));
        SetText(hud, 0, $"── [ 제작소 ] ({craftingPage + 1}/{totalCraftPages}) ──", new Color(0.3f, 1f, 0.5f));

        int startIdx = craftingPage * RECIPES_PER_PAGE;
        for (int i = 0; i < RECIPES_PER_PAGE; i++)
        {
            int rIdx = startIdx + i;

            if (rIdx < recipes.Count)
            {
                var recipe = recipes[rIdx];
                bool canCraft = inventory != null && ItemDatabase.Instance.CanCraft(recipe, inventory.GetItemCount);

                string statusTag = canCraft ? " [제작가능]" : " [재료부족]";
                Color nameColor = canCraft ? new Color(0.3f, 1f, 0.5f) : Color.gray;
                SetText(hud, i + 1, $"{i + 1}. {recipe.ResultItemName}{statusTag}", nameColor);
            }
            else
            {
                SetText(hud, i + 1, "", Color.white);
            }
        }

        SetText(hud, 8, "8.◀이전  9.▶다음  0.뒤로", new Color(0.7f, 0.7f, 0.7f));
    }

    // =========================================================================
    // 디버그
    // =========================================================================
    private void HandleDebugInput(int key, UIGameHUD hud)
    {
        if (key == -2) // 0번 → 뒤로
        {
            CurrentState = InventoryViewState.Main;
            RefreshDisplay(hud);
            return;
        }

        if (key == 0) // 1번 → 레벨 100
        {
            if (playerExp != null) playerExp.SetCheatLevelServerRpc(100);
            Debug.Log("디버그: 레벨 100 설정!");
        }
        else if (key == 1) // 2번 → 1차 각성
        {
            if (playerClass != null) playerClass.SetAwakeningServerRpc(1);
            Debug.Log("디버그: 1차 각성 설정!");
        }
        else if (key == 2) // 3번 → 2차 각성
        {
            if (playerClass != null) playerClass.SetAwakeningServerRpc(2);
            Debug.Log("디버그: 2차 각성 설정!");
        }
        else if (key == 3) // 4번 → 전체 초기화
        {
            if (playerClass != null)
            {
                playerClass.ChangeClassServerRpc(PlayerClassType.None);
                playerClass.SetAwakeningServerRpc(0);
            }
            if (playerExp != null) playerExp.SetCheatLevelServerRpc(1);
            Debug.Log("디버그: 전직/레벨/경험치/각성 모두 초기화!");
        }
        else if (key == 4) // 5번 → 모든 재료 10개씩 추가
        {
            if (inventory != null)
            {
                inventory.DebugAddAllMaterialsServerRpc(10);
                Debug.Log("디버그: 모든 재료 10개씩 추가 요청!");
            }
        }
        RefreshDisplay(hud);
    }

    private void DisplayDebug(UIGameHUD hud)
    {
        ClearTexts(hud);
        SetText(hud, 0, "── [ 디버그 ] ──", Color.cyan);
        SetText(hud, 1, "1. 레벨 100 설정", Color.cyan);
        SetText(hud, 2, "2. 1차 각성 돌파", Color.cyan);
        SetText(hud, 3, "3. 2차 각성 돌파", Color.cyan);
        SetText(hud, 4, "4. 전체 초기화", Color.cyan);
        SetText(hud, 5, "5. 모든 재료 +10", new Color(0f, 1f, 0.8f));
        SetText(hud, 6, "", Color.white);
        SetText(hud, 7, "", Color.white);
        SetText(hud, 8, "0. 뒤로가기", Color.white);
    }

    // =========================================================================
    // 거래 탐색 (TradeSearch)
    // =========================================================================
    private void HandleTradeSearchInput(int key, UIGameHUD hud)
    {
        if (key == -2) // 0번 -> 뒤로
        {
            CurrentState = InventoryViewState.Main;
            RefreshDisplay(hud);
            return;
        }

        if (trade == null) return;

        int reqCount = trade.PendingRequests.Count;
        int inputNumber = key + 1; // 1번부터 시작 (key=0일때 1번)

        // 1번~ : 수락 버튼
        if (inputNumber >= 1 && inputNumber <= reqCount)
        {
            trade.AcceptTradeServerRpc(trade.PendingRequests[inputNumber - 1]);
            CurrentState = InventoryViewState.TradeSession; // 즉각 세션으로
            RefreshDisplay(hud);
            return;
        }

        // 그 이후 : 주변 플레이어 요청 버튼
        int nearbyStartIndex = reqCount + 1;
        int nearbyCount = nearbyPlayersCache.Count;
        if (inputNumber >= nearbyStartIndex && inputNumber < nearbyStartIndex + nearbyCount)
        {
            int cacheIndex = inputNumber - nearbyStartIndex;
            trade.RequestTradeServerRpc(nearbyPlayersCache[cacheIndex]);
            Debug.Log($"[Trade] Player_{nearbyPlayersCache[cacheIndex]} 에게 거래 요청 전송");
            RefreshDisplay(hud);
            return;
        }
    }

    private void DisplayTradeSearch(UIGameHUD hud)
    {
        ClearTexts(hud);
        if (trade != null && trade.IsTrading.Value)
        {
            CurrentState = InventoryViewState.TradeSession;
            RefreshDisplay(hud);
            return;
        }

        SetText(hud, 0, "── [ 플레이어 거래 ] ──", new Color(0.3f, 0.8f, 1f));
        
        int line = 1;
        if (trade != null)
        {
            // 나에게 온 요청 목록
            for (int i = 0; i < trade.PendingRequests.Count && line < 4; i++)
            {
                SetText(hud, line, $"{line}. [요청 옴] Player_{trade.PendingRequests[i]} 수락", new Color(0.2f, 1f, 0.2f));
                line++;
            }
            
            // 전체 접속 플레이어 탐색 (거리 제한 없음)
            nearbyPlayersCache = trade.GetAllPlayers();
            for (int i = 0; i < nearbyPlayersCache.Count && line < 8; i++)
            {
                SetText(hud, line, $"{line}. [요청 하기] Player_{nearbyPlayersCache[i]}", Color.gray);
                line++;
            }
        }
        
        SetText(hud, 8, "0. 뒤로가기", Color.white);
    }

    // =========================================================================
    // 거래 세션 (TradeSession)
    // =========================================================================
    private void HandleTradeSessionInput(int key, UIGameHUD hud)
    {
        if (trade == null) return;
        
        if (key == -2) // 0번 -> 취소
        {
            trade.CancelTradeServerRpc();
            CurrentState = InventoryViewState.Main;
            RefreshDisplay(hud);
            return;
        }

        if (!trade.IsTrading.Value)
        {
            CurrentState = InventoryViewState.Main;
            RefreshDisplay(hud);
            return;
        }

        if (key == 0) // 1번 -> 아이템 올리기
        {
            CurrentState = InventoryViewState.TradeInventorySelect;
            RefreshDisplay(hud);
        }
        else if (key == 1) // 2번 -> 준비 완료 토글
        {
            trade.SetReadyServerRpc(!trade.IsReady.Value);
            RefreshDisplay(hud); // 즉각 갱신 후 서버에서 동기화 올때 다시 Refresh
        }
    }

    private void DisplayTradeSession(UIGameHUD hud)
    {
        ClearTexts(hud);
        if (trade == null || !trade.IsTrading.Value)
        {
            CurrentState = InventoryViewState.Main;
            RefreshDisplay(hud);
            return;
        }

        SetText(hud, 0, $"── [ 취소 0번 / 거래중 : Player_{trade.TradePartnerId.Value} ] ──", new Color(0.3f, 0.8f, 1f));

        // 내 상태
        string myStatus = trade.IsReady.Value ? "[준비 완료]" : "[준비 중]";
        Color myColor = trade.IsReady.Value ? Color.green : Color.white;
        string myItem = GetItemNameForTrade(trade.OfferedItemId.Value, trade.OfferedItemCount.Value);
        SetText(hud, 1, $"[나] {myStatus} {myItem}", myColor);

        // 파트너 상태 찾기
        string partnerStatus = "[준비 중]";
        string partnerItem = "[빈 슬롯]";
        Color pColor = Color.white;

        if (trade.TradePartnerId.Value != ulong.MaxValue)
        {
            if (Unity.Netcode.NetworkManager.Singleton.ConnectedClients.TryGetValue(trade.TradePartnerId.Value, out var pClient))
            {
                var pTrade = pClient.PlayerObject.GetComponentInChildren<PlayerTradeSystem>();
                if (pTrade != null)
                {
                    partnerStatus = pTrade.IsReady.Value ? "[준비 완료]" : "[준비 중]";
                    partnerItem = GetItemNameForTrade(pTrade.OfferedItemId.Value, pTrade.OfferedItemCount.Value);
                    pColor = pTrade.IsReady.Value ? Color.green : Color.white;
                }
            }
        }
        
        SetText(hud, 2, $"[상대] {partnerStatus} {partnerItem}", pColor);
        SetText(hud, 3, "", Color.black);
        SetText(hud, 4, "1. 아이템 올리기(1개씩)", new Color(1f, 0.9f, 0.5f));
        SetText(hud, 5, "2. 레디 / 레디 해제 토글", new Color(1f, 0.9f, 0.5f));
        SetText(hud, 8, "0. 거래 취소/종료", Color.red);
    }
    
    private string GetItemNameForTrade(int id, int count)
    {
        if (id <= 0) return "[빈 슬롯]";
        var item = ItemDatabase.Instance?.GetItem(id);
        string name = item != null ? item.Name : "Unknown";
        return count > 1 ? $"{name} x{count}" : name;
    }

    // =========================================================================
    // 거래용 인벤토리 선택 UI (TradeInventorySelect)
    // =========================================================================
    private void HandleTradeInventorySelectInput(int key, UIGameHUD hud)
    {
        if (key == -2)
        {
            CurrentState = InventoryViewState.TradeSession;
            RefreshDisplay(hud);
            return;
        }

        if (key == 7 && inventory != null) { inventory.PrevPage(); RefreshDisplay(hud); return; }
        if (key == 8 && inventory != null) { inventory.NextPage(); RefreshDisplay(hud); return; }

        if (key >= 0 && key < InventorySystem.SLOTS_PER_PAGE && inventory != null && trade != null)
        {
            int actualIndex = inventory.CurrentPage * InventorySystem.SLOTS_PER_PAGE + key;
            var slot = inventory.GetSlot(actualIndex);
            if (!slot.IsEmpty)
            {
                // 1개씩 등록
                trade.OfferItemServerRpc(slot.ItemID, 1);
                Debug.Log($"[거래] 아이템 등록 요청: {slot.ItemID} x1");
                CurrentState = InventoryViewState.TradeSession; 
            }
            RefreshDisplay(hud);
        }
    }

    private void DisplayTradeInventorySelect(UIGameHUD hud)
    {
        ClearTexts(hud);
        int page = inventory != null ? inventory.CurrentPage + 1 : 1;
        int totalPages = inventory != null ? inventory.TotalPages : 1;
        SetText(hud, 0, $"── [ 올릴 아이템 선택 ] ({page}/{totalPages}) ──", new Color(1f, 0.9f, 0.5f));

        List<InventorySlot> pageSlots = inventory != null
            ? inventory.GetCurrentPageSlots()
            : new List<InventorySlot>();

        for (int i = 0; i < InventorySystem.SLOTS_PER_PAGE; i++)
        {
            if (i < pageSlots.Count && !pageSlots[i].IsEmpty)
            {
                var itemData = ItemDatabase.Instance?.GetItem(pageSlots[i].ItemID);
                if (itemData != null)
                {
                    string countStr = itemData.IsStackable ? $" (보유: x{pageSlots[i].Count})" : "";
                    Color color = GetRarityColor(itemData.Rarity);
                    SetText(hud, i + 1, $"{i + 1}. {itemData.Name}{countStr}", color);
                }
            }
            else
            {
                SetText(hud, i + 1, $"{i + 1}. [빈 슬롯]", new Color(0.4f, 0.4f, 0.4f));
            }
        }

        SetText(hud, 8, "8.◀이전  9.▶다음  0.뒤로(취소)", new Color(0.7f, 0.7f, 0.7f));
    }

    // =========================================================================
    // 유틸리티
    // =========================================================================
    private void ClearTexts(UIGameHUD hud)
    {
        if (hud.inventoryTexts == null) return;
        for (int i = 0; i < hud.inventoryTexts.Length; i++)
        {
            if (hud.inventoryTexts[i] != null)
            {
                hud.inventoryTexts[i].text = "";
                hud.inventoryTexts[i].color = Color.white;
            }
        }
    }

    private void SetText(UIGameHUD hud, int index, string text, Color color)
    {
        if (hud.inventoryTexts == null || index < 0 || index >= hud.inventoryTexts.Length) return;
        if (hud.inventoryTexts[index] == null) return;
        hud.inventoryTexts[index].text = text;
        hud.inventoryTexts[index].color = color;
    }

    private string BuildMaterialString(RecipeData recipe)
    {
        var parts = new List<string>();

        if (recipe.Material1ID > 0) parts.Add(FormatMaterial(recipe.Material1ID, recipe.Material1Count));
        if (recipe.Material2ID > 0) parts.Add(FormatMaterial(recipe.Material2ID, recipe.Material2Count));
        if (recipe.Material3ID > 0) parts.Add(FormatMaterial(recipe.Material3ID, recipe.Material3Count));

        return string.Join(" + ", parts);
    }

    private string FormatMaterial(int itemId, int required)
    {
        string name = "???";
        int owned = 0;
        if (ItemDatabase.Instance != null)
        {
            var data = ItemDatabase.Instance.GetItem(itemId);
            if (data != null) name = data.Name;
        }
        if (inventory != null) owned = inventory.GetItemCount(itemId);

        return $"{name}({owned}/{required})";
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return Color.white;
            case ItemRarity.Uncommon: return new Color(0.3f, 1f, 0.3f);     // 초록
            case ItemRarity.Rare: return new Color(0.3f, 0.5f, 1f);         // 파랑
            case ItemRarity.Epic: return new Color(0.7f, 0.3f, 1f);         // 보라
            case ItemRarity.Legendary: return new Color(1f, 0.65f, 0f);     // 주황
            default: return Color.white;
        }
    }
}
