using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 레시피(제작법) 데이터를 보관하는 ScriptableObject.
/// 유니티 에디터에서 [Create > RPG/Recipe Data]로 생성 가능합니다.
/// 기존 CSV 기반 RecipeData와 동일한 구조를 가집니다.
/// </summary>
[CreateAssetMenu(fileName = "NewRecipe", menuName = "RPG/Recipe Data")]
public class RecipeDataSO : ScriptableObject
{
    [Header("결과물")]
    public int recipeID;
    [Tooltip("완성되는 아이템 (SO 에셋을 드래그 앤 드롭)")]
    public ItemDataSO resultItem;
    public int resultCount = 1;

    [Header("재료 목록")]
    public List<RecipeMaterial> materials = new List<RecipeMaterial>();

    /// <summary>
    /// SO 데이터를 기존 RecipeData(POCO)로 변환합니다.
    /// </summary>
    public RecipeData ToRecipeData()
    {
        var data = new RecipeData
        {
            RecipeID = recipeID,
            ResultItemName = resultItem != null ? resultItem.itemName : "",
            ResultItemID = resultItem != null ? resultItem.itemID : 0,
            ResultCount = resultCount
        };

        if (materials.Count > 0) { data.Material1ID = materials[0].itemID; data.Material1Count = materials[0].count; }
        if (materials.Count > 1) { data.Material2ID = materials[1].itemID; data.Material2Count = materials[1].count; }
        if (materials.Count > 2) { data.Material3ID = materials[2].itemID; data.Material3Count = materials[2].count; }

        return data;
    }
}

/// <summary>
/// 레시피 재료 한 종류를 표현하는 구조체.
/// 인스펙터에서 리스트로 편집할 수 있습니다.
/// </summary>
[System.Serializable]
public class RecipeMaterial
{
    [Tooltip("재료 아이템 ID")]
    public int itemID;
    [Tooltip("필요 수량")]
    public int count = 1;
}
