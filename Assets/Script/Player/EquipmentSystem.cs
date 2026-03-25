using UnityEngine;
using Unity.Netcode;

public class EquipmentSystem : NetworkBehaviour
{
    private StatSystem statSystem;

    private void Awake()
    {
        statSystem = GetComponent<StatSystem>();
    }

    // 서버 전용: 인벤토리에서 소재 10개 소모 성공 시 호출됨
    public void GenerateAndEquipReward(MaterialType usedMaterial)
    {
        if (!IsServer || statSystem == null) return;

        // 기획상 장비는 +% 가산, *% 곱연산 스탯 부여를 한다.
        // 현재는 무작위로 하나의 스탯을 증가시키는 임시 스탯 부여 로직

        StatType[] statTypes = (StatType[])System.Enum.GetValues(typeof(StatType));
        StatType randomStat = statTypes[Random.Range(0, statTypes.Length)];

        bool isMultiplicative = Random.value > 0.5f;
        
        // 가산은 5~20%, 곱연산은 1.05~1.2 (5%~20%)
        float value = isMultiplicative ? Random.Range(1.05f, 1.20f) : Random.Range(5f, 20f);

        StatModifier newEquip = new StatModifier(value, isMultiplicative, this);
        if (statSystem.stats.ContainsKey(randomStat))
        {
            statSystem.stats[randomStat].AddModifier(newEquip);
            Debug.Log($"Player {OwnerClientId} Equipped! Stat: {randomStat}, Value: {value}, IsMult: {isMultiplicative}");
        }
    }
}
