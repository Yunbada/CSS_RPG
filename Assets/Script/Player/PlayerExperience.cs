using UnityEngine;
using Unity.Netcode;

public class PlayerExperience : NetworkBehaviour
{
    public NetworkVariable<int> Level = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> CurrentExp = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Load local persistence data
            int savedLevel = PlayerPrefs.GetInt("CSS_RPG_Level", 1);
            int savedExp = PlayerPrefs.GetInt("CSS_RPG_Exp", 0);

            // Tell server our saved data
            LoadDataServerRpc(savedLevel, savedExp);
        }
    }

    [ServerRpc]
    private void LoadDataServerRpc(int level, int exp)
    {
        Level.Value = level;
        CurrentExp.Value = exp;
    }

    // 서버 전용 함수
    public void AddExp(int amount)
    {
        if (!IsServer) return;

        // 임시 로직: 스탯 시스템의 경험치 보너스 적용
        StatSystem stats = GetComponent<StatSystem>();
        if (stats != null)
        {
            float mult = stats.GetStat(StatType.ExpBonus) / 100f;
            amount = Mathf.RoundToInt(amount * mult);
        }

        CurrentExp.Value += amount;
        
        // 레벨업 체크 (예시: 레벨당 100 * 레벨 필요 경험치)
        int requiredExp = Level.Value * 100;
        if (CurrentExp.Value >= requiredExp)
        {
            CurrentExp.Value -= requiredExp;
            Level.Value++;
            Debug.Log($"Player {OwnerClientId} Leveled Up to {Level.Value}!");
        }

        // 상태 저장 트리거
        SaveDataClientRpc(Level.Value, CurrentExp.Value);
    }

    [ClientRpc]
    private void SaveDataClientRpc(int level, int exp)
    {
        if (IsOwner)
        {
            PlayerPrefs.SetInt("CSS_RPG_Level", level);
            PlayerPrefs.SetInt("CSS_RPG_Exp", exp);
            PlayerPrefs.Save();
        }
    }
}
