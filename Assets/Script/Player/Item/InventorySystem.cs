using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Text;

public enum MaterialType
{
    RottenLeather, // 썩은 가죽
    RottenTooth,   // 썩은 이빨
    BrokenSkull    // 깨진 두개골
}

/// <summary>
/// 인벤토리 시스템 (Phase 2 리라이트)
/// - 슬롯 배열 기반 아이템 보관 (무제한 페이지, 페이지당 7슬롯 표시)
/// - 좀비 처치 시 ItemDatabase 가중치 기반 자동 획득
/// - CSV 영구 저장 연동
/// - 기존 MaterialType/LeatherCount 등 레거시 로직은 하위 호환을 위해 유지
/// </summary>
public class InventorySystem : NetworkBehaviour
{
    // =========================================================================
    // 상수
    // =========================================================================
    public const int SLOTS_PER_PAGE = 7; // 한 페이지에 표시할 슬롯 수 (8,9키는 페이지 전환용)
    public const int MAX_STACK = 99;     // 스택 가능 아이템 최대 수량

    // =========================================================================
    // 인벤토리 데이터
    // =========================================================================
    private List<InventorySlot> slots = new List<InventorySlot>();
    public int CurrentPage { get; private set; } = 0;
    public int TotalPages => Mathf.Max(1, Mathf.CeilToInt((float)slots.Count / SLOTS_PER_PAGE));

    // 네트워크 동기화용 직렬화 문자열 (서버 → 클라이언트)
    public NetworkVariable<Unity.Collections.FixedString4096Bytes> SyncedInventory =
        new NetworkVariable<Unity.Collections.FixedString4096Bytes>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // =========================================================================
    // 레거시 호환 (기존 재료 카운트 - 기존 코드가 참조할 수 있으므로 유지)
    // =========================================================================
    public NetworkVariable<int> LeatherCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> ToothCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> SkullCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // =========================================================================
    // 이벤트 (UI 갱신용)
    // =========================================================================
    public event System.Action OnInventoryChanged;

    // =========================================================================
    // 생명주기
    // =========================================================================
    public override void OnNetworkSpawn()
    {
        if (IsOwner && LocalUserData.Current != null)
        {
            // 레거시 재료 동기화
            SyncLegacyMaterialsServerRpc(
                LocalUserData.Current.Leather,
                LocalUserData.Current.Tooth,
                LocalUserData.Current.Skull);

            // 새 인벤토리 데이터 로드
            if (!string.IsNullOrEmpty(LocalUserData.Current.InventoryData))
            {
                LoadInventoryServerRpc(LocalUserData.Current.InventoryData);
            }
        }

        if (IsServer)
        {
            PlayerState.OnAnyZombieDied += HandleZombieDied;
        }

        // 클라이언트에서 동기화 변수 변경 감지
        SyncedInventory.OnValueChanged += OnSyncedInventoryChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            PlayerState.OnAnyZombieDied -= HandleZombieDied;
        }
        SyncedInventory.OnValueChanged -= OnSyncedInventoryChanged;
    }

    private void OnSyncedInventoryChanged(Unity.Collections.FixedString4096Bytes oldVal, Unity.Collections.FixedString4096Bytes newVal)
    {
        if (!IsServer) // 클라이언트만 동기화 수신
        {
            DeserializeSlots(newVal.ToString());
        }
        OnInventoryChanged?.Invoke();
    }

    // =========================================================================
    // 좀비 처치 → 아이템 자동 획득
    // =========================================================================
    private void HandleZombieDied(ulong killerId)
    {
        if (killerId != OwnerClientId) return;

        // 30% 확률로 아이템 드롭 (가중치 기반)
        if (Random.value <= 0.3f && ItemDatabase.Instance != null)
        {
            ItemData droppedItem = ItemDatabase.Instance.RollDrop();
            if (droppedItem != null)
            {
                AddItem(droppedItem.ItemID, 1);
                Debug.Log($"[InventorySystem] Player {OwnerClientId} 획득: {droppedItem.Name}");
            }
        }

        // 레거시 재료 드롭 (하위 호환, 10% 확률)
        if (Random.value <= 0.1f)
        {
            MaterialType randomMat = (MaterialType)Random.Range(0, 3);
            AddLegacyMaterial(randomMat, 1);
        }
    }

    // =========================================================================
    // 아이템 추가 (서버 전용)
    // =========================================================================
    public bool AddItem(int itemId, int count = 1)
    {
        if (!IsServer) return false;
        if (itemId <= 0 || count <= 0) return false;

        ItemData itemData = ItemDatabase.Instance?.GetItem(itemId);
        if (itemData == null)
        {
            Debug.LogWarning($"[InventorySystem] 알 수 없는 아이템 ID: {itemId}");
            return false;
        }

        if (itemData.IsStackable)
        {
            // 스택 가능: 기존 슬롯에 합산
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].ItemID == itemId)
                {
                    slots[i].Count = Mathf.Min(slots[i].Count + count, MAX_STACK);
                    SyncToNetwork();
                    SaveToCSV();
                    return true;
                }
            }
        }

        // 빈 슬롯 찾기 또는 새 슬롯 추가
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty)
            {
                slots[i].ItemID = itemId;
                slots[i].Count = Mathf.Min(count, itemData.IsStackable ? MAX_STACK : 1);
                SyncToNetwork();
                SaveToCSV();
                return true;
            }
        }

        // 빈 슬롯이 없으면 새 슬롯 추가
        slots.Add(new InventorySlot(itemId, Mathf.Min(count, itemData.IsStackable ? MAX_STACK : 1)));
        SyncToNetwork();
        SaveToCSV();
        return true;
    }

    // =========================================================================
    // 아이템 제거 (서버 전용)
    // =========================================================================
    public bool RemoveItem(int slotIndex, int count = 1)
    {
        if (!IsServer) return false;
        if (slotIndex < 0 || slotIndex >= slots.Count) return false;
        if (slots[slotIndex].IsEmpty) return false;

        slots[slotIndex].Count -= count;
        if (slots[slotIndex].Count <= 0)
        {
            slots[slotIndex].ItemID = 0;
            slots[slotIndex].Count = 0;
        }

        SyncToNetwork();
        SaveToCSV();
        return true;
    }

    /// <summary>특정 아이템 ID로 수량 제거 (레시피 소모용)</summary>
    public bool RemoveItemById(int itemId, int count)
    {
        if (!IsServer) return false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].ItemID == itemId)
            {
                if (slots[i].Count >= count)
                {
                    slots[i].Count -= count;
                    if (slots[i].Count <= 0)
                    {
                        slots[i].ItemID = 0;
                        slots[i].Count = 0;
                    }
                    SyncToNetwork();
                    SaveToCSV();
                    return true;
                }
            }
        }
        return false;
    }

    // =========================================================================
    // 조회 API
    // =========================================================================

    /// <summary>특정 슬롯 데이터 반환 (읽기 전용)</summary>
    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return new InventorySlot();
        return slots[index];
    }

    /// <summary>전체 슬롯 수</summary>
    public int SlotCount => slots.Count;

    /// <summary>현재 페이지 슬롯 목록 (최대 7개)</summary>
    public List<InventorySlot> GetCurrentPageSlots()
    {
        var result = new List<InventorySlot>();
        int startIndex = CurrentPage * SLOTS_PER_PAGE;
        for (int i = 0; i < SLOTS_PER_PAGE; i++)
        {
            int idx = startIndex + i;
            if (idx < slots.Count)
                result.Add(slots[idx]);
            else
                result.Add(new InventorySlot());
        }
        return result;
    }

    /// <summary>특정 아이템 ID의 총 보유 수량 (레시피 검증용)</summary>
    public int GetItemCount(int itemId)
    {
        int total = 0;
        foreach (var slot in slots)
        {
            if (slot.ItemID == itemId)
                total += slot.Count;
        }
        return total;
    }

    // =========================================================================
    // 페이지 전환
    // =========================================================================
    public void NextPage()
    {
        if (CurrentPage < TotalPages - 1)
            CurrentPage++;
    }

    public void PrevPage()
    {
        if (CurrentPage > 0)
            CurrentPage--;
    }

    // =========================================================================
    // 레시피 기반 제작
    // =========================================================================
    [ServerRpc]
    public void CraftByRecipeServerRpc(int recipeId)
    {
        if (ItemDatabase.Instance == null) return;

        var allRecipes = ItemDatabase.Instance.GetAllRecipes();
        RecipeData recipe = null;
        foreach (var r in allRecipes)
        {
            if (r.RecipeID == recipeId) { recipe = r; break; }
        }

        if (recipe == null)
        {
            Debug.LogWarning($"[InventorySystem] 레시피 ID {recipeId}를 찾을 수 없음");
            return;
        }

        // 재료 보유 확인
        if (!ItemDatabase.Instance.CanCraft(recipe, GetItemCount))
        {
            Debug.Log($"[InventorySystem] 재료 부족으로 제작 불가: {recipe.ResultItemName}");
            NotifyCraftResultClientRpc(false, recipe.ResultItemName);
            return;
        }

        // 재료 소모
        if (recipe.Material1ID > 0) RemoveItemById(recipe.Material1ID, recipe.Material1Count);
        if (recipe.Material2ID > 0) RemoveItemById(recipe.Material2ID, recipe.Material2Count);
        if (recipe.Material3ID > 0) RemoveItemById(recipe.Material3ID, recipe.Material3Count);

        // 완성품 지급
        AddItem(recipe.ResultItemID, recipe.ResultCount);
        Debug.Log($"[InventorySystem] 제작 성공: {recipe.ResultItemName} x{recipe.ResultCount}");
        NotifyCraftResultClientRpc(true, recipe.ResultItemName);
    }

    [ClientRpc]
    private void NotifyCraftResultClientRpc(bool success, string itemName)
    {
        if (IsOwner)
        {
            Debug.Log(success
                ? $"[제작] {itemName} 제작 완료!"
                : $"[제작] {itemName} 재료가 부족합니다.");
        }
    }

    // =========================================================================
    // 직렬화 / 역직렬화 (네트워크 동기화 + CSV 저장)
    // =========================================================================
    private string SerializeSlots()
    {
        // 형식: "itemId:count;itemId:count;..."
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < slots.Count; i++)
        {
            if (i > 0) sb.Append(';');
            sb.Append(slots[i].ItemID).Append(':').Append(slots[i].Count);
        }
        return sb.ToString();
    }

    private void DeserializeSlots(string data)
    {
        slots.Clear();
        if (string.IsNullOrEmpty(data)) return;

        string[] parts = data.Split(';');
        foreach (string part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;
            string[] kv = part.Split(':');
            if (kv.Length >= 2)
            {
                int.TryParse(kv[0], out int id);
                int.TryParse(kv[1], out int count);
                slots.Add(new InventorySlot(id, count));
            }
        }
    }

    private void SyncToNetwork()
    {
        if (!IsServer) return;
        SyncedInventory.Value = new Unity.Collections.FixedString4096Bytes(SerializeSlots());
    }

    // =========================================================================
    // CSV 저장/로드
    // =========================================================================
    private void SaveToCSV()
    {
        SaveDataClientRpc(
            LeatherCount.Value, ToothCount.Value, SkullCount.Value,
            SerializeSlots());
    }

    [ServerRpc]
    private void LoadInventoryServerRpc(string inventoryData)
    {
        DeserializeSlots(inventoryData);
        SyncToNetwork();
    }

    [ClientRpc]
    private void SaveDataClientRpc(int l, int t, int s, string inventoryData)
    {
        if (IsOwner && LocalUserData.Current != null)
        {
            LocalUserData.Current.Leather = l;
            LocalUserData.Current.Tooth = t;
            LocalUserData.Current.Skull = s;
            LocalUserData.Current.InventoryData = inventoryData;
            CsvDatabase.Instance.SaveUser(LocalUserData.Current);
        }
    }

    // =========================================================================
    // 레거시 재료 호환
    // =========================================================================
    [ServerRpc]
    private void SyncLegacyMaterialsServerRpc(int l, int t, int s)
    {
        LeatherCount.Value = l;
        ToothCount.Value = t;
        SkullCount.Value = s;
    }

    public void AddLegacyMaterial(MaterialType type, int amount = 1)
    {
        if (!IsServer) return;
        switch (type)
        {
            case MaterialType.RottenLeather: LeatherCount.Value += amount; break;
            case MaterialType.RottenTooth: ToothCount.Value += amount; break;
            case MaterialType.BrokenSkull: SkullCount.Value += amount; break;
        }
        SaveToCSV();
    }

    // 레거시 제작 (하위호환)
    public void RequestCraft(MaterialType type)
    {
        if (IsOwner) CraftLegacyServerRpc(type);
    }

    [ServerRpc]
    private void CraftLegacyServerRpc(MaterialType type)
    {
        int cost = 10;
        bool canCraft = false;
        switch (type)
        {
            case MaterialType.RottenLeather:
                if (LeatherCount.Value >= cost) { LeatherCount.Value -= cost; canCraft = true; } break;
            case MaterialType.RottenTooth:
                if (ToothCount.Value >= cost) { ToothCount.Value -= cost; canCraft = true; } break;
            case MaterialType.BrokenSkull:
                if (SkullCount.Value >= cost) { SkullCount.Value -= cost; canCraft = true; } break;
        }
        if (canCraft)
        {
            SaveToCSV();
            GetComponent<EquipmentSystem>()?.GenerateAndEquipReward(type);
        }
    }

    // =========================================================================
    // 디버그 전용
    // =========================================================================
    /// <summary>디버그: 모든 재료 아이템을 지정 수량만큼 추가</summary>
    [ServerRpc]
    public void DebugAddAllMaterialsServerRpc(int amount = 10)
    {
        if (ItemDatabase.Instance == null) return;

        foreach (var item in ItemDatabase.Instance.GetAllItems().Values)
        {
            if (item.Type == ItemType.CraftMaterial || item.Type == ItemType.RareMaterial)
            {
                AddItem(item.ItemID, amount);
            }
        }
        Debug.Log($"[InventorySystem] 디버그: 모든 재료 {amount}개씩 추가 완료");
    }
}
