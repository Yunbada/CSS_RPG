using UnityEngine;
using Unity.Netcode;

public class PlayerExperience : NetworkBehaviour
{
    public NetworkVariable<int> Level = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> CurrentExp = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsOwner && LocalUserData.Current != null)
        {
            LoadDataServerRpc(LocalUserData.Current.Level, LocalUserData.Current.Exp);
        }
    }

    [ServerRpc]
    private void LoadDataServerRpc(int level, int exp)
    {
        Level.Value = level;
        CurrentExp.Value = exp;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetCheatLevelServerRpc(int level)
    {
        Level.Value = level;
        CurrentExp.Value = 0;
        SaveDataClientRpc(Level.Value, CurrentExp.Value);
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
    private void SaveDataClientRpc(int newLv, int newExp)
    {
        if (IsOwner && LocalUserData.Current != null)
        {
            LocalUserData.Current.Level = newLv;
            LocalUserData.Current.Exp = newExp;
            CsvDatabase.Instance.SaveUser(LocalUserData.Current);
        }
    }
}
