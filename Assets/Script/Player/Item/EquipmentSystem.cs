using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// 장비 시스템 (Phase 2 리라이트)
/// - 5개 슬롯(Weapon, Helmet, Armor, Gloves, Boots) 관리
/// - 장착 시 ItemDatabase에서 읽은 확정 스탯을 StatSystem에 적용
/// - 해제 시 StatModifier 제거
/// </summary>
public class EquipmentSystem : NetworkBehaviour
{
    // =========================================================================
    // 장비 슬롯 (5개)
    // =========================================================================
    public static readonly ItemSlot[] SlotOrder = {
        ItemSlot.Weapon,
        ItemSlot.Helmet,
        ItemSlot.Armor,
        ItemSlot.Gloves,
        ItemSlot.Boots
    };

    // 슬롯별 장착된 아이템 ID (0 = 미장착)
    private Dictionary<ItemSlot, int> equippedItems = new Dictionary<ItemSlot, int>();
    // 슬롯별 적용된 StatModifier (해제 시 제거용)
    private Dictionary<ItemSlot, StatModifier> appliedModifiers = new Dictionary<ItemSlot, StatModifier>();

    private StatSystem statSystem;

    // 이벤트
    public event System.Action OnEquipmentChanged;

    // 네트워크 동기화
    public NetworkVariable<Unity.Collections.FixedString512Bytes> SyncedEquipment =
        new NetworkVariable<Unity.Collections.FixedString512Bytes>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // =========================================================================
    // 생명주기
    // =========================================================================
    private void Awake()
    {
        statSystem = GetComponent<StatSystem>();

        // 슬롯 초기화
        foreach (var slot in SlotOrder)
        {
            equippedItems[slot] = 0;
            appliedModifiers[slot] = null;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner && LocalUserData.Current != null)
        {
            if (!string.IsNullOrEmpty(LocalUserData.Current.EquipmentData))
            {
                LoadEquipmentServerRpc(LocalUserData.Current.EquipmentData);
            }
        }

        SyncedEquipment.OnValueChanged += OnSyncedEquipmentChanged;
    }

    public override void OnNetworkDespawn()
    {
        SyncedEquipment.OnValueChanged -= OnSyncedEquipmentChanged;
    }

    private void OnSyncedEquipmentChanged(Unity.Collections.FixedString512Bytes oldVal, Unity.Collections.FixedString512Bytes newVal)
    {
        if (!IsServer)
        {
            DeserializeEquipment(newVal.ToString());
        }
        OnEquipmentChanged?.Invoke();
    }

    // =========================================================================
    // 장착 (서버 전용)
    // =========================================================================

    /// <summary>인벤토리 슬롯 인덱스에서 아이템을 꺼내 장비 슬롯에 착용</summary>
    [ServerRpc]
    public void EquipFromInventoryServerRpc(int inventorySlotIndex)
    {
        var inventory = GetComponent<InventorySystem>();
        if (inventory == null || statSystem == null) return;

        var slot = inventory.GetSlot(inventorySlotIndex);
        if (slot.IsEmpty) return;

        ItemData itemData = ItemDatabase.Instance?.GetItem(slot.ItemID);
        if (itemData == null || itemData.Type != ItemType.Equipment) return;
        if (itemData.Slot == ItemSlot.None) return;

        // 해당 슬롯에 이미 장비가 있으면 먼저 해제 → 인벤토리로 복귀
        if (equippedItems[itemData.Slot] > 0)
        {
            UnequipToInventory(itemData.Slot, inventory);
        }

        // 인벤토리에서 제거
        inventory.RemoveItem(inventorySlotIndex, 1);

        // 장착
        equippedItems[itemData.Slot] = itemData.ItemID;

        // 스탯 적용
        if (itemData.StatValue != 0 && statSystem.stats.ContainsKey(itemData.StatType))
        {
            var mod = new StatModifier(itemData.StatValue, itemData.IsMultiplicative, this);
            statSystem.stats[itemData.StatType].AddModifier(mod);
            appliedModifiers[itemData.Slot] = mod;
        }

        SyncToNetwork();
        SaveToCSV();
        Debug.Log($"[EquipmentSystem] {itemData.Name} 장착 완료 → {itemData.Slot} 슬롯");
    }

    // =========================================================================
    // 해제 (서버 전용)
    // =========================================================================

    /// <summary>특정 장비 슬롯의 장비를 해제하여 인벤토리로 복귀</summary>
    [ServerRpc]
    public void UnequipServerRpc(int slotOrderIndex)
    {
        if (slotOrderIndex < 0 || slotOrderIndex >= SlotOrder.Length) return;

        ItemSlot targetSlot = SlotOrder[slotOrderIndex];
        var inventory = GetComponent<InventorySystem>();
        if (inventory == null) return;

        UnequipToInventory(targetSlot, inventory);

        SyncToNetwork();
        SaveToCSV();
    }

    private void UnequipToInventory(ItemSlot targetSlot, InventorySystem inventory)
    {
        int currentItemId = equippedItems[targetSlot];
        if (currentItemId <= 0) return;

        // 스탯 제거
        if (appliedModifiers[targetSlot] != null && statSystem != null)
        {
            ItemData itemData = ItemDatabase.Instance?.GetItem(currentItemId);
            if (itemData != null && statSystem.stats.ContainsKey(itemData.StatType))
            {
                statSystem.stats[itemData.StatType].RemoveModifier(appliedModifiers[targetSlot]);
            }
            appliedModifiers[targetSlot] = null;
        }

        // 인벤토리로 복귀
        inventory.AddItem(currentItemId, 1);
        equippedItems[targetSlot] = 0;

        Debug.Log($"[EquipmentSystem] {targetSlot} 슬롯 장비 해제");
    }

    // =========================================================================
    // 조회 API
    // =========================================================================

    /// <summary>특정 슬롯에 장착된 아이템 ID (0이면 미장착)</summary>
    public int GetEquippedItemId(ItemSlot slot)
    {
        return equippedItems.ContainsKey(slot) ? equippedItems[slot] : 0;
    }

    /// <summary>특정 슬롯에 장착된 ItemData (없으면 null)</summary>
    public ItemData GetEquippedItemData(ItemSlot slot)
    {
        int id = GetEquippedItemId(slot);
        if (id <= 0 || ItemDatabase.Instance == null) return null;
        return ItemDatabase.Instance.GetItem(id);
    }

    /// <summary>슬롯 인덱스(0~4)로 장착된 아이템 이름 반환</summary>
    public string GetEquippedItemName(int slotOrderIndex)
    {
        if (slotOrderIndex < 0 || slotOrderIndex >= SlotOrder.Length) return "[없음]";
        var data = GetEquippedItemData(SlotOrder[slotOrderIndex]);
        return data != null ? data.Name : "[없음]";
    }

    // =========================================================================
    // 직렬화 / 역직렬화
    // =========================================================================
    private string SerializeEquipment()
    {
        // 형식: "Weapon:1001,Helmet:0,Armor:1003,Gloves:0,Boots:0"
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < SlotOrder.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(SlotOrder[i]).Append(':').Append(equippedItems[SlotOrder[i]]);
        }
        return sb.ToString();
    }

    private void DeserializeEquipment(string data)
    {
        if (string.IsNullOrEmpty(data)) return;

        string[] parts = data.Split(',');
        foreach (string part in parts)
        {
            string[] kv = part.Split(':');
            if (kv.Length >= 2)
            {
                if (System.Enum.TryParse(kv[0], out ItemSlot slot))
                {
                    int.TryParse(kv[1], out int itemId);
                    equippedItems[slot] = itemId;
                }
            }
        }
    }

    private void SyncToNetwork()
    {
        if (!IsServer) return;
        SyncedEquipment.Value = new Unity.Collections.FixedString512Bytes(SerializeEquipment());
    }

    // =========================================================================
    // CSV 저장/로드
    // =========================================================================
    private void SaveToCSV()
    {
        SaveEquipmentClientRpc(SerializeEquipment());
    }

    [ServerRpc]
    private void LoadEquipmentServerRpc(string equipData)
    {
        DeserializeEquipment(equipData);

        // 장착 스탯 복원
        if (statSystem != null && ItemDatabase.Instance != null)
        {
            foreach (var slot in SlotOrder)
            {
                int itemId = equippedItems[slot];
                if (itemId <= 0) continue;

                ItemData itemData = ItemDatabase.Instance.GetItem(itemId);
                if (itemData != null && itemData.StatValue != 0 && statSystem.stats.ContainsKey(itemData.StatType))
                {
                    var mod = new StatModifier(itemData.StatValue, itemData.IsMultiplicative, this);
                    statSystem.stats[itemData.StatType].AddModifier(mod);
                    appliedModifiers[slot] = mod;
                }
            }
        }

        SyncToNetwork();
    }

    [ClientRpc]
    private void SaveEquipmentClientRpc(string equipData)
    {
        if (IsOwner && LocalUserData.Current != null)
        {
            LocalUserData.Current.EquipmentData = equipData;
            CsvDatabase.Instance.SaveUser(LocalUserData.Current);
        }
    }

    // =========================================================================
    // 레거시 호환
    // =========================================================================
    public void GenerateAndEquipReward(MaterialType usedMaterial)
    {
        if (!IsServer || statSystem == null) return;

        StatType[] statTypes = (StatType[])System.Enum.GetValues(typeof(StatType));
        StatType randomStat = statTypes[Random.Range(0, statTypes.Length)];
        bool isMultiplicative = Random.value > 0.5f;
        float value = isMultiplicative ? Random.Range(1.05f, 1.20f) : Random.Range(5f, 20f);

        StatModifier newEquip = new StatModifier(value, isMultiplicative, this);
        if (statSystem.stats.ContainsKey(randomStat))
        {
            statSystem.stats[randomStat].AddModifier(newEquip);
            Debug.Log($"[EquipmentSystem-Legacy] Stat: {randomStat}, Value: {value}");
        }
    }
}
