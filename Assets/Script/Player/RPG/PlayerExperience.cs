using UnityEngine;
using Unity.Netcode;

public class PlayerExperience : NetworkBehaviour
{
    public NetworkVariable<int> Level = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> CurrentExp = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
    }

    [ServerRpc]
    public void LoadDataServerRpc(int level, int exp)
    {
        Level.Value = level;
        CurrentExp.Value = exp;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetCheatLevelServerRpc(int level)
    {
        Level.Value = level;
        CurrentExp.Value = 0;
        var auth = GetComponent<PlayerAuthentication>();
        if (auth != null) auth.SaveDataToDatabase();
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
    }
}
