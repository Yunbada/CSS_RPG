using UnityEngine;

/// <summary>
/// 개별 아이템의 정적 데이터를 보관하는 ScriptableObject.
/// 유니티 에디터에서 [Create > RPG/Item Data]로 생성 가능합니다.
/// 기존 CSV 기반 ItemData와 동일한 필드를 가지며,
/// 향후 CSV 없이 에디터에서 직접 아이템을 기획할 때 사용합니다.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "RPG/Item Data")]
public class ItemDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public int itemID;
    public string itemName;
    [TextArea(2, 4)]
    public string description;

    [Header("분류")]
    public ItemType type;
    public ItemSlot slot;
    public ItemRarity rarity;

    [Header("장비 스탯 (Equipment 타입 전용)")]
    [Tooltip("이 장비가 올려주는 스탯 종류")]
    public StatType statType;
    [Tooltip("스탯 수치 (예: 공격력 +15)")]
    public float statValue;
    [Tooltip("true면 곱연산(%), false면 합연산(+)")]
    public bool isMultiplicative;

    [Header("드롭 설정")]
    [Tooltip("드롭 테이블 가중치 (높을수록 자주 등장)")]
    public int dropWeight;

    [Header("시각 효과 (선택)")]
    [Tooltip("인벤토리에 표시할 아이콘")]
    public Sprite itemIcon;
    [Tooltip("월드에 드롭될 때 보이는 3D 모델")]
    public GameObject worldDropPrefab;

    /// <summary>
    /// SO 데이터를 기존 ItemData(POCO)로 변환합니다.
    /// 기존 CSV 기반 시스템과 호환성을 유지합니다.
    /// </summary>
    public ItemData ToItemData()
    {
        return new ItemData
        {
            ItemID = itemID,
            Name = itemName,
            Type = type,
            Slot = slot,
            Rarity = rarity,
            StatType = statType,
            StatValue = statValue,
            IsMultiplicative = isMultiplicative,
            Description = description,
            DropWeight = dropWeight
        };
    }
}
