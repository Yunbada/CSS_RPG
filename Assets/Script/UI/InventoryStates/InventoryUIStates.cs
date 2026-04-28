using UnityEngine;
using System.Collections.Generic;

namespace CSS_RPG.UI
{
    public class MainUIState : IInventoryUIState
    {
        private InventoryUIController ctx;

        public MainUIState(InventoryUIController controller)
        {
            ctx = controller;
        }

        public void Enter() { }

        public void HandleInput(int key)
        {
            if (key == 0) ctx.ChangeState(InventoryViewState.Equipment);
            else if (key == 1) ctx.ChangeState(InventoryViewState.Inventory);
            else if (key == 2) ctx.ChangeState(InventoryViewState.Crafting);
            else if (key == 3) ctx.ChangeState(InventoryViewState.TradeSearch);
            else if (key == -2) ctx.ChangeState(InventoryViewState.Debug);
        }

        public void RefreshDisplay()
        {
            ctx.View.ClearTexts();
            ctx.View.SetText(0, "1. 장비 슬롯", Color.white);
            ctx.View.SetText(1, "2. 인벤토리", Color.white);
            ctx.View.SetText(2, "3. 제작소", Color.white);
            ctx.View.SetText(3, "4. 플레이어 거래", Color.white);
            ctx.View.SetText(4, "5. [미구현]", new Color(0.5f, 0.5f, 0.5f));
            ctx.View.SetText(5, "6. [미구현]", new Color(0.5f, 0.5f, 0.5f));
            ctx.View.SetText(6, "7. [미구현]", new Color(0.5f, 0.5f, 0.5f));
            ctx.View.SetText(7, "", Color.white);
            ctx.View.SetText(8, "", Color.white);
        }

        public void Exit() { }
    }

    public class EquipmentUIState : IInventoryUIState
    {
        private InventoryUIController ctx;

        public EquipmentUIState(InventoryUIController controller)
        {
            ctx = controller;
        }

        public void Enter() { }

        public void HandleInput(int key)
        {
            if (key == -2) // 0번 → 뒤로
            {
                ctx.ChangeState(InventoryViewState.Main);
                return;
            }

            if (key >= 0 && key < 5 && ctx.Equipment != null)
            {
                int itemId = ctx.Equipment.GetEquippedItemId(EquipmentSystem.SlotOrder[key]);
                if (itemId > 0)
                {
                    ctx.Equipment.UnequipServerRpc(key);
                    Debug.Log($"[인벤토리UI] {EquipmentSystem.SlotOrder[key]} 슬롯 장비 해제");
                }
                ctx.RefreshDisplay();
            }
        }

        public void RefreshDisplay()
        {
            ctx.View.ClearTexts();
            ctx.View.SetText(0, "── [ 장비 슬롯 ] ──", new Color(1f, 0.85f, 0.3f));

            string[] slotNames = { "무기", "투구", "갑옷", "장갑", "신발" };
            for (int i = 0; i < 5; i++)
            {
                string itemName = "[없음]";
                Color color = new Color(0.5f, 0.5f, 0.5f);

                if (ctx.Equipment != null)
                {
                    var data = ctx.Equipment.GetEquippedItemData(EquipmentSystem.SlotOrder[i]);
                    if (data != null)
                    {
                        itemName = data.Name;
                        color = ctx.View.GetRarityColor(data.Rarity);
                    }
                }
                ctx.View.SetText(i + 1, $"{i + 1}. {slotNames[i]}: {itemName}", color);
            }

            ctx.View.SetText(6, "", Color.white);
            ctx.View.SetText(7, "※ 번호를 눌러 해제", new Color(0.7f, 0.7f, 0.7f));
            ctx.View.SetText(8, "0. 뒤로가기", Color.white);
        }

        public void Exit() { }
    }

    public class InventoryUIState : IInventoryUIState
    {
        private InventoryUIController ctx;

        public InventoryUIState(InventoryUIController controller)
        {
            ctx = controller;
        }

        public void Enter() { }

        public void HandleInput(int key)
        {
            if (key == -2) // 0번 → 뒤로
            {
                ctx.ChangeState(InventoryViewState.Main);
                return;
            }

            if (key == 7 && ctx.Inventory != null) // 8번 → 이전 페이지
            {
                ctx.Inventory.PrevPage();
                ctx.RefreshDisplay();
                return;
            }

            if (key == 8 && ctx.Inventory != null) // 9번 → 다음 페이지
            {
                ctx.Inventory.NextPage();
                ctx.RefreshDisplay();
                return;
            }

            if (key >= 0 && key < InventorySystem.SLOTS_PER_PAGE && ctx.Inventory != null && ctx.Equipment != null)
            {
                int actualIndex = ctx.Inventory.CurrentPage * InventorySystem.SLOTS_PER_PAGE + key;
                var slot = ctx.Inventory.GetSlot(actualIndex);
                if (!slot.IsEmpty)
                {
                    var itemData = ItemDatabase.Instance?.GetItem(slot.ItemID);
                    if (itemData != null && itemData.Type == ItemType.Equipment)
                    {
                        ctx.Equipment.EquipFromInventoryServerRpc(actualIndex);
                        Debug.Log($"[인벤토리UI] {itemData.Name} 장착 시도");
                    }
                    else if (itemData != null)
                    {
                        Debug.Log($"[인벤토리UI] {itemData.Name}은(는) 장착할 수 없는 아이템입니다.");
                    }
                }
                ctx.RefreshDisplay();
            }
        }

        public void RefreshDisplay()
        {
            ctx.View.ClearTexts();
            int page = ctx.Inventory != null ? ctx.Inventory.CurrentPage + 1 : 1;
            int totalPages = ctx.Inventory != null ? ctx.Inventory.TotalPages : 1;
            ctx.View.SetText(0, $"── [ 인벤토리 ] ({page}/{totalPages}) ──", new Color(0.3f, 0.85f, 1f));

            List<InventorySlot> pageSlots = ctx.Inventory != null
                ? ctx.Inventory.GetCurrentPageSlots()
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
                        Color color = ctx.View.GetRarityColor(itemData.Rarity);
                        ctx.View.SetText(i + 1, $"{i + 1}. {itemData.Name}{countStr}{equipTag}", color);
                    }
                    else
                    {
                        ctx.View.SetText(i + 1, $"{i + 1}. ??? (ID:{pageSlots[i].ItemID})", Color.gray);
                    }
                }
                else
                {
                    ctx.View.SetText(i + 1, $"{i + 1}. [빈 슬롯]", new Color(0.4f, 0.4f, 0.4f));
                }
            }

            ctx.View.SetText(8, "8.◀이전  9.▶다음  0.뒤로", new Color(0.7f, 0.7f, 0.7f));
        }

        public void Exit() { }
    }

    public class CraftingUIState : IInventoryUIState
    {
        private InventoryUIController ctx;
        private int craftingPage = 0;
        private const int RECIPES_PER_PAGE = 7;

        public CraftingUIState(InventoryUIController controller)
        {
            ctx = controller;
        }

        public void Enter()
        {
            craftingPage = 0;
        }

        public void HandleInput(int key)
        {
            if (key == -2) // 0번 → 뒤로
            {
                ctx.ChangeState(InventoryViewState.Main);
                return;
            }

            if (key == 7) // 8번 → 이전 페이지
            {
                if (craftingPage > 0) craftingPage--;
                ctx.RefreshDisplay();
                return;
            }

            if (key == 8) // 9번 → 다음 페이지
            {
                if (ItemDatabase.Instance != null)
                {
                    int maxPage = Mathf.Max(0, Mathf.CeilToInt((float)ItemDatabase.Instance.GetAllRecipes().Count / RECIPES_PER_PAGE) - 1);
                    if (craftingPage < maxPage) craftingPage++;
                }
                ctx.RefreshDisplay();
                return;
            }

            if (key >= 0 && key < RECIPES_PER_PAGE && ctx.Inventory != null && ItemDatabase.Instance != null)
            {
                var recipes = ItemDatabase.Instance.GetAllRecipes();
                int recipeIndex = craftingPage * RECIPES_PER_PAGE + key;
                if (recipeIndex < recipes.Count)
                {
                    ctx.Inventory.CraftByRecipeServerRpc(recipes[recipeIndex].RecipeID);
                    Debug.Log($"[제작소] {recipes[recipeIndex].ResultItemName} 제작 요청");
                }
                ctx.RefreshDisplay();
            }
        }

        public void RefreshDisplay()
        {
            ctx.View.ClearTexts();

            var recipes = ItemDatabase.Instance != null
                ? ItemDatabase.Instance.GetAllRecipes()
                : new List<RecipeData>();

            int totalCraftPages = Mathf.Max(1, Mathf.CeilToInt((float)recipes.Count / RECIPES_PER_PAGE));
            ctx.View.SetText(0, $"── [ 제작소 ] ({craftingPage + 1}/{totalCraftPages}) ──", new Color(0.3f, 1f, 0.5f));

            int startIdx = craftingPage * RECIPES_PER_PAGE;
            for (int i = 0; i < RECIPES_PER_PAGE; i++)
            {
                int rIdx = startIdx + i;

                if (rIdx < recipes.Count)
                {
                    var recipe = recipes[rIdx];
                    bool canCraft = ctx.Inventory != null && ItemDatabase.Instance.CanCraft(recipe, ctx.Inventory.GetItemCount);

                    string statusTag = canCraft ? " [제작가능]" : " [재료부족]";
                    Color nameColor = canCraft ? new Color(0.3f, 1f, 0.5f) : Color.gray;
                    ctx.View.SetText(i + 1, $"{i + 1}. {recipe.ResultItemName}{statusTag}", nameColor);
                }
                else
                {
                    ctx.View.SetText(i + 1, "", Color.white);
                }
            }

            ctx.View.SetText(8, "8.◀이전  9.▶다음  0.뒤로", new Color(0.7f, 0.7f, 0.7f));
        }

        public void Exit() { }
    }

    public class DebugUIState : IInventoryUIState
    {
        private InventoryUIController ctx;

        public DebugUIState(InventoryUIController controller)
        {
            ctx = controller;
        }

        public void Enter() { }

        public void HandleInput(int key)
        {
            if (key == -2) // 0번 → 뒤로
            {
                ctx.ChangeState(InventoryViewState.Main);
                return;
            }

            if (key == 0) // 1번 → 레벨 100
            {
                if (ctx.PlayerExp != null) ctx.PlayerExp.SetCheatLevelServerRpc(100);
                Debug.Log("디버그: 레벨 100 설정!");
            }
            else if (key == 1) // 2번 → 1차 각성
            {
                if (ctx.PlayerClass != null) ctx.PlayerClass.SetAwakeningServerRpc(1);
                Debug.Log("디버그: 1차 각성 설정!");
            }
            else if (key == 2) // 3번 → 2차 각성
            {
                if (ctx.PlayerClass != null) ctx.PlayerClass.SetAwakeningServerRpc(2);
                Debug.Log("디버그: 2차 각성 설정!");
            }
            else if (key == 3) // 4번 → 전체 초기화
            {
                if (ctx.PlayerClass != null)
                {
                    ctx.PlayerClass.ChangeClassServerRpc(PlayerClassType.None);
                    ctx.PlayerClass.SetAwakeningServerRpc(0);
                }
                if (ctx.PlayerExp != null) ctx.PlayerExp.SetCheatLevelServerRpc(1);
                Debug.Log("디버그: 전직/레벨/경험치/각성 모두 초기화!");
            }
            else if (key == 4) // 5번 → 모든 재료 10개씩 추가
            {
                if (ctx.Inventory != null)
                {
                    ctx.Inventory.DebugAddAllMaterialsServerRpc(10);
                    Debug.Log("디버그: 모든 재료 10개씩 추가 요청!");
                }
            }
            ctx.RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            ctx.View.ClearTexts();
            ctx.View.SetText(0, "── [ 디버그 ] ──", Color.cyan);
            ctx.View.SetText(1, "1. 레벨 100 설정", Color.cyan);
            ctx.View.SetText(2, "2. 1차 각성 돌파", Color.cyan);
            ctx.View.SetText(3, "3. 2차 각성 돌파", Color.cyan);
            ctx.View.SetText(4, "4. 전체 초기화", Color.cyan);
            ctx.View.SetText(5, "5. 모든 재료 +10", new Color(0f, 1f, 0.8f));
            ctx.View.SetText(6, "", Color.white);
            ctx.View.SetText(7, "", Color.white);
            ctx.View.SetText(8, "0. 뒤로가기", Color.white);
        }

        public void Exit() { }
    }

    public class TradeSearchUIState : IInventoryUIState
    {
        private InventoryUIController ctx;
        private List<ulong> nearbyPlayersCache = new List<ulong>();

        public TradeSearchUIState(InventoryUIController controller)
        {
            ctx = controller;
        }

        public void Enter() { }

        public void HandleInput(int key)
        {
            if (key == -2)
            {
                ctx.ChangeState(InventoryViewState.Main);
                return;
            }

            if (ctx.Trade == null) return;

            int reqCount = ctx.Trade.PendingRequests.Count;
            int inputNumber = key + 1;

            if (inputNumber >= 1 && inputNumber <= reqCount)
            {
                ctx.Trade.AcceptTradeServerRpc(ctx.Trade.PendingRequests[inputNumber - 1]);
                ctx.ChangeState(InventoryViewState.TradeSession);
                return;
            }

            int nearbyStartIndex = reqCount + 1;
            int nearbyCount = nearbyPlayersCache.Count;
            if (inputNumber >= nearbyStartIndex && inputNumber < nearbyStartIndex + nearbyCount)
            {
                int cacheIndex = inputNumber - nearbyStartIndex;
                ctx.Trade.RequestTradeServerRpc(nearbyPlayersCache[cacheIndex]);
                Debug.Log($"[Trade] Player_{nearbyPlayersCache[cacheIndex]} 에게 거래 요청 전송");
                ctx.RefreshDisplay();
                return;
            }
        }

        public void RefreshDisplay()
        {
            ctx.View.ClearTexts();
            if (ctx.Trade != null && ctx.Trade.IsTrading.Value)
            {
                ctx.ChangeState(InventoryViewState.TradeSession);
                return;
            }

            ctx.View.SetText(0, "── [ 플레이어 거래 ] ──", new Color(0.3f, 0.8f, 1f));
            
            int line = 1;
            if (ctx.Trade != null)
            {
                for (int i = 0; i < ctx.Trade.PendingRequests.Count && line < 4; i++)
                {
                    ctx.View.SetText(line, $"{line}. [요청 옴] Player_{ctx.Trade.PendingRequests[i]} 수락", new Color(0.2f, 1f, 0.2f));
                    line++;
                }
                
                nearbyPlayersCache = ctx.Trade.GetAllPlayers();
                for (int i = 0; i < nearbyPlayersCache.Count && line < 8; i++)
                {
                    ctx.View.SetText(line, $"{line}. [요청 하기] Player_{nearbyPlayersCache[i]}", Color.gray);
                    line++;
                }
            }
            
            ctx.View.SetText(8, "0. 뒤로가기", Color.white);
        }

        public void Exit() { }
    }

    public class TradeSessionUIState : IInventoryUIState
    {
        private InventoryUIController ctx;

        public TradeSessionUIState(InventoryUIController controller)
        {
            ctx = controller;
        }

        public void Enter() { }

        public void HandleInput(int key)
        {
            if (ctx.Trade == null) return;
            
            if (key == -2)
            {
                ctx.Trade.CancelTradeServerRpc();
                ctx.ChangeState(InventoryViewState.Main);
                return;
            }

            if (!ctx.Trade.IsTrading.Value)
            {
                ctx.ChangeState(InventoryViewState.Main);
                return;
            }

            if (key == 0)
            {
                ctx.ChangeState(InventoryViewState.TradeInventorySelect);
            }
            else if (key == 1)
            {
                ctx.Trade.SetReadyServerRpc(!ctx.Trade.IsReady.Value);
                ctx.RefreshDisplay();
            }
        }

        public void RefreshDisplay()
        {
            ctx.View.ClearTexts();
            if (ctx.Trade == null || !ctx.Trade.IsTrading.Value)
            {
                ctx.ChangeState(InventoryViewState.Main);
                return;
            }

            ctx.View.SetText(0, $"── [ 취소 0번 / 거래중 : Player_{ctx.Trade.TradePartnerId.Value} ] ──", new Color(0.3f, 0.8f, 1f));

            string myStatus = ctx.Trade.IsReady.Value ? "[준비 완료]" : "[준비 중]";
            Color myColor = ctx.Trade.IsReady.Value ? Color.green : Color.white;
            string myItem = GetItemNameForTrade(ctx.Trade.OfferedItemId.Value, ctx.Trade.OfferedItemCount.Value);
            ctx.View.SetText(1, $"[나] {myStatus} {myItem}", myColor);

            string partnerStatus = "[준비 중]";
            string partnerItem = "[빈 슬롯]";
            Color pColor = Color.white;

            if (ctx.Trade.TradePartnerId.Value != ulong.MaxValue)
            {
                if (Unity.Netcode.NetworkManager.Singleton.ConnectedClients.TryGetValue(ctx.Trade.TradePartnerId.Value, out var pClient))
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
            
            ctx.View.SetText(2, $"[상대] {partnerStatus} {partnerItem}", pColor);
            ctx.View.SetText(3, "", Color.black);
            ctx.View.SetText(4, "1. 아이템 올리기(1개씩)", new Color(1f, 0.9f, 0.5f));
            ctx.View.SetText(5, "2. 레디 / 레디 해제 토글", new Color(1f, 0.9f, 0.5f));
            ctx.View.SetText(8, "0. 거래 취소/종료", Color.red);
        }

        private string GetItemNameForTrade(int id, int count)
        {
            if (id <= 0) return "[빈 슬롯]";
            var item = ItemDatabase.Instance?.GetItem(id);
            string name = item != null ? item.Name : "Unknown";
            return count > 1 ? $"{name} x{count}" : name;
        }

        public void Exit() { }
    }

    public class TradeInventorySelectUIState : IInventoryUIState
    {
        private InventoryUIController ctx;

        public TradeInventorySelectUIState(InventoryUIController controller)
        {
            ctx = controller;
        }

        public void Enter() { }

        public void HandleInput(int key)
        {
            if (key == -2)
            {
                ctx.ChangeState(InventoryViewState.TradeSession);
                return;
            }

            if (key == 7 && ctx.Inventory != null) { ctx.Inventory.PrevPage(); ctx.RefreshDisplay(); return; }
            if (key == 8 && ctx.Inventory != null) { ctx.Inventory.NextPage(); ctx.RefreshDisplay(); return; }

            if (key >= 0 && key < InventorySystem.SLOTS_PER_PAGE && ctx.Inventory != null && ctx.Trade != null)
            {
                int actualIndex = ctx.Inventory.CurrentPage * InventorySystem.SLOTS_PER_PAGE + key;
                var slot = ctx.Inventory.GetSlot(actualIndex);
                if (!slot.IsEmpty)
                {
                    ctx.Trade.OfferItemServerRpc(slot.ItemID, 1);
                    Debug.Log($"[거래] 아이템 등록 요청: {slot.ItemID} x1");
                    ctx.ChangeState(InventoryViewState.TradeSession);
                }
                else
                {
                    ctx.RefreshDisplay();
                }
            }
        }

        public void RefreshDisplay()
        {
            ctx.View.ClearTexts();
            int page = ctx.Inventory != null ? ctx.Inventory.CurrentPage + 1 : 1;
            int totalPages = ctx.Inventory != null ? ctx.Inventory.TotalPages : 1;
            ctx.View.SetText(0, $"── [ 올릴 아이템 선택 ] ({page}/{totalPages}) ──", new Color(1f, 0.9f, 0.5f));

            List<InventorySlot> pageSlots = ctx.Inventory != null
                ? ctx.Inventory.GetCurrentPageSlots()
                : new List<InventorySlot>();

            for (int i = 0; i < InventorySystem.SLOTS_PER_PAGE; i++)
            {
                if (i < pageSlots.Count && !pageSlots[i].IsEmpty)
                {
                    var itemData = ItemDatabase.Instance?.GetItem(pageSlots[i].ItemID);
                    if (itemData != null)
                    {
                        string countStr = itemData.IsStackable ? $" (보유: x{pageSlots[i].Count})" : "";
                        Color color = ctx.View.GetRarityColor(itemData.Rarity);
                        ctx.View.SetText(i + 1, $"{i + 1}. {itemData.Name}{countStr}", color);
                    }
                }
                else
                {
                    ctx.View.SetText(i + 1, $"{i + 1}. [빈 슬롯]", new Color(0.4f, 0.4f, 0.4f));
                }
            }

            ctx.View.SetText(8, "8.◀이전  9.▶다음  0.뒤로(취소)", new Color(0.7f, 0.7f, 0.7f));
        }

        public void Exit() { }
    }
}
