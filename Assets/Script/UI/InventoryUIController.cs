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
    Debug       // 디버그 메뉴
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

    public InventoryUIController(InventorySystem inv, EquipmentSystem equip, PlayerClass pClass, PlayerExperience pExp)
    {
        inventory = inv;
        equipment = equip;
        playerClass = pClass;
        playerExp = pExp;
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
        else if (key == -2) { CurrentState = InventoryViewState.Debug; RefreshDisplay(hud); }
    }

    private void DisplayMain(UIGameHUD hud)
    {
        ClearTexts(hud);
        SetText(hud, 0, "1. 장비 슬롯", Color.white);
        SetText(hud, 1, "2. 인벤토리", Color.white);
        SetText(hud, 2, "3. 제작소", Color.white);
        SetText(hud, 3, "4. [미구현]", new Color(0.5f, 0.5f, 0.5f));
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
