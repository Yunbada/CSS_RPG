/// <summary>
/// 아이템 관련 열거형 및 순수 데이터 클래스 정의
/// CSV(ItemDatabase.csv) 한 행을 그대로 담는 POCO(Plain Old C# Object)
/// </summary>

// =========================================================================
// 열거형 정의
// =========================================================================

/// <summary>아이템 대분류</summary>
public enum ItemType
{
    Equipment,      // 장비 (장착 가능)
    CraftMaterial,  // 제작 재료 (일반 재료)
    RareMaterial    // 희귀 재료
}

/// <summary>장비 장착 부위 (Equipment 타입 전용)</summary>
public enum ItemSlot
{
    None,       // 장착 불가 (재료 등)
    Weapon,     // 무기
    Helmet,     // 투구
    Armor,      // 갑옷
    Gloves,     // 장갑
    Boots       // 신발
}

/// <summary>아이템 희귀도</summary>
public enum ItemRarity
{
    Common,     // 일반
    Uncommon,   // 고급
    Rare,       // 희귀
    Epic,       // 영웅
    Legendary   // 전설
}

// =========================================================================
// 데이터 클래스
// =========================================================================

/// <summary>
/// CSV 한 행에 대응하는 아이템 정의 데이터.
/// 런타임에 ItemDatabase 매니저가 파싱하여 캐싱합니다.
/// </summary>
public class ItemData
{
    public int ItemID;
    public string Name;
    public ItemType Type;
    public ItemSlot Slot;
    public ItemRarity Rarity;

    // 장비 스탯 (Equipment 타입 전용)
    public StatType StatType;
    public float StatValue;
    public bool IsMultiplicative;

    public string Description;

    // 드롭 관련
    public int DropWeight; // 드롭 테이블 가중치 (높을수록 자주 나옴)

    // 스택 가능 여부 (재료는 스택 가능, 장비는 불가)
    public bool IsStackable => Type != ItemType.Equipment;

    public override string ToString()
    {
        return $"[{ItemID}] {Name} ({Rarity} {Type})";
    }
}

/// <summary>
/// 인벤토리 안의 한 칸을 표현하는 구조체.
/// 아이템 ID와 수량을 묶어서 관리합니다.
/// </summary>
[System.Serializable]
public class InventorySlot
{
    public int ItemID;  // 0이면 빈 슬롯
    public int Count;   // 스택 수량 (장비는 항상 1)

    public bool IsEmpty => ItemID <= 0;

    public InventorySlot()
    {
        ItemID = 0;
        Count = 0;
    }

    public InventorySlot(int itemId, int count)
    {
        ItemID = itemId;
        Count = count;
    }
}

/// <summary>
/// 레시피 데이터. RecipeDatabase.csv 한 행에 대응합니다.
/// </summary>
public class RecipeData
{
    public int RecipeID;
    public string ResultItemName;  // 완성품 이름 (표시용)
    public int ResultItemID;       // 완성품 아이템 ID
    public int ResultCount;        // 완성 수량

    // 재료 (최대 3종류 지원)
    public int Material1ID;
    public int Material1Count;
    public int Material2ID;
    public int Material2Count;
    public int Material3ID;
    public int Material3Count;

    public override string ToString()
    {
        return $"[Recipe {RecipeID}] {ResultItemName}";
    }
}
