using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

/// <summary>
/// 아이템 및 레시피 CSV를 파싱하여 캐싱하는 싱글톤 매니저.
/// CsvDatabase(플레이어 데이터)와 별도로, 아이템 정적 정보를 관리합니다.
/// Awake 시 자동 로드되며 DontDestroyOnLoad로 유지됩니다.
/// </summary>
public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    // 아이템 정의 캐시
    private Dictionary<int, ItemData> itemCache = new Dictionary<int, ItemData>();
    // 레시피 정의 캐시
    private List<RecipeData> recipeCache = new List<RecipeData>();
    // 드롭 테이블 (가중치 기반)
    private List<ItemData> dropTable = new List<ItemData>();
    private int totalDropWeight = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadItemDatabase();
            LoadRecipeDatabase();
            BuildDropTable();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =========================================================================
    // CSV 로딩
    // =========================================================================

    private void LoadItemDatabase()
    {
        string filePath = Application.dataPath + "/ItemDatabase.csv";
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[ItemDatabase] ItemDatabase.csv를 찾을 수 없습니다: {filePath}");
            return;
        }

        string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
        for (int i = 1; i < lines.Length; i++) // 첫 줄은 헤더
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] cols = lines[i].Split(',');
            if (cols.Length < 10)
            {
                Debug.LogWarning($"[ItemDatabase] {i+1}번째 줄 컬럼 부족, 무시: {lines[i]}");
                continue;
            }

            try
            {
                ItemData item = new ItemData();
                int.TryParse(cols[0].Trim(), out item.ItemID);
                item.Name = cols[1].Trim();
                item.Type = ParseEnum<ItemType>(cols[2].Trim());
                item.Slot = ParseEnum<ItemSlot>(cols[3].Trim());
                item.Rarity = ParseEnum<ItemRarity>(cols[4].Trim());
                item.StatType = ParseEnum<StatType>(cols[5].Trim());
                float.TryParse(cols[6].Trim(), out item.StatValue);
                bool.TryParse(cols[7].Trim(), out item.IsMultiplicative);
                item.Description = cols[8].Trim();
                int.TryParse(cols[9].Trim(), out item.DropWeight);

                itemCache[item.ItemID] = item;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ItemDatabase] {i+1}번째 줄 파싱 실패: {e.Message}");
            }
        }

        Debug.Log($"[ItemDatabase] 아이템 {itemCache.Count}종 로드 완료.");
    }

    private void LoadRecipeDatabase()
    {
        string filePath = Application.dataPath + "/RecipeDatabase.csv";
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[ItemDatabase] RecipeDatabase.csv를 찾을 수 없습니다: {filePath}");
            return;
        }

        string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] cols = lines[i].Split(',');
            if (cols.Length < 10)
            {
                Debug.LogWarning($"[ItemDatabase] RecipeDB {i+1}번째 줄 컬럼 부족, 무시");
                continue;
            }

            try
            {
                RecipeData recipe = new RecipeData();
                int.TryParse(cols[0].Trim(), out recipe.RecipeID);
                recipe.ResultItemName = cols[1].Trim();
                int.TryParse(cols[2].Trim(), out recipe.ResultItemID);
                int.TryParse(cols[3].Trim(), out recipe.ResultCount);
                int.TryParse(cols[4].Trim(), out recipe.Material1ID);
                int.TryParse(cols[5].Trim(), out recipe.Material1Count);
                int.TryParse(cols[6].Trim(), out recipe.Material2ID);
                int.TryParse(cols[7].Trim(), out recipe.Material2Count);
                int.TryParse(cols[8].Trim(), out recipe.Material3ID);
                int.TryParse(cols[9].Trim(), out recipe.Material3Count);

                recipeCache.Add(recipe);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ItemDatabase] RecipeDB {i+1}번째 줄 파싱 실패: {e.Message}");
            }
        }

        Debug.Log($"[ItemDatabase] 레시피 {recipeCache.Count}종 로드 완료.");
    }

    private void BuildDropTable()
    {
        dropTable.Clear();
        totalDropWeight = 0;

        foreach (var item in itemCache.Values)
        {
            if (item.DropWeight > 0)
            {
                dropTable.Add(item);
                totalDropWeight += item.DropWeight;
            }
        }

        Debug.Log($"[ItemDatabase] 드롭 테이블 {dropTable.Count}종, 총 가중치 {totalDropWeight} 구성 완료.");
    }

    // =========================================================================
    // 조회 API
    // =========================================================================

    /// <summary>아이템 ID로 ItemData 조회. 없으면 null 반환.</summary>
    public ItemData GetItem(int itemId)
    {
        itemCache.TryGetValue(itemId, out ItemData result);
        return result;
    }

    /// <summary>전체 아이템 목록 반환</summary>
    public IReadOnlyDictionary<int, ItemData> GetAllItems() => itemCache;

    /// <summary>전체 레시피 목록 반환</summary>
    public IReadOnlyList<RecipeData> GetAllRecipes() => recipeCache;

    /// <summary>특정 아이템 타입만 필터링하여 반환</summary>
    public List<ItemData> GetItemsByType(ItemType type)
    {
        List<ItemData> result = new List<ItemData>();
        foreach (var item in itemCache.Values)
        {
            if (item.Type == type) result.Add(item);
        }
        return result;
    }

    /// <summary>
    /// 가중치 기반 랜덤 드롭. 좀비 처치 시 호출하여 아이템 ID를 결정합니다.
    /// DropWeight가 높을수록 자주 등장합니다.
    /// </summary>
    public ItemData RollDrop()
    {
        if (dropTable.Count == 0 || totalDropWeight <= 0) return null;

        int roll = UnityEngine.Random.Range(0, totalDropWeight);
        int cumulative = 0;

        foreach (var item in dropTable)
        {
            cumulative += item.DropWeight;
            if (roll < cumulative)
            {
                return item;
            }
        }

        // 안전장치 (도달할 일 없음)
        return dropTable[dropTable.Count - 1];
    }

    /// <summary>
    /// 특정 레시피의 제작 가능 여부를 확인합니다.
    /// inventoryChecker: 아이템 ID를 넣으면 보유 수량을 반환하는 함수
    /// </summary>
    public bool CanCraft(RecipeData recipe, System.Func<int, int> inventoryChecker)
    {
        if (recipe == null || inventoryChecker == null) return false;

        if (recipe.Material1ID > 0 && inventoryChecker(recipe.Material1ID) < recipe.Material1Count) return false;
        if (recipe.Material2ID > 0 && inventoryChecker(recipe.Material2ID) < recipe.Material2Count) return false;
        if (recipe.Material3ID > 0 && inventoryChecker(recipe.Material3ID) < recipe.Material3Count) return false;

        return true;
    }

    // =========================================================================
    // 유틸리티
    // =========================================================================

    private T ParseEnum<T>(string value) where T : struct
    {
        if (Enum.TryParse(value, true, out T result))
            return result;
        return default;
    }
}
