using Unity.Netcode;
using UnityEngine;

public enum Team
{
    Human,
    HostZombie,
    NormalZombie
}

public enum ZombieType
{
    None,
    Speed,
    Tank,
    Jump
}

public class PlayerState : NetworkBehaviour
{
    public NetworkVariable<Team> currentTeam = new NetworkVariable<Team>(Team.Human, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> maxHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public NetworkVariable<ZombieType> currentZombieType = new NetworkVariable<ZombieType>(ZombieType.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Reset to default
            currentTeam.Value = Team.Human;
            currentZombieType.Value = ZombieType.None;
            currentHealth.Value = maxHealth.Value;
            
            // Register to RoundManager
            if (RoundManager.Instance != null)
            {
                RoundManager.Instance.RegisterPlayer(this);
            }
        }
        
        currentTeam.OnValueChanged += OnTeamChanged;
        currentZombieType.OnValueChanged += OnZombieTypeChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && RoundManager.Instance != null)
        {
            RoundManager.Instance.UnregisterPlayer(this);
        }
        
        currentTeam.OnValueChanged -= OnTeamChanged;
        currentZombieType.OnValueChanged -= OnZombieTypeChanged;
    }

    // 서버 전용 함수: 피해 입히기
    public void TakeDamage(int amount, ulong killerId = 9999)
    {
        if (!IsServer) return;

        currentHealth.Value -= amount;
        if (currentHealth.Value <= 0)
        {
            Die(killerId);
        }
    }

    private void Die(ulong killerId)
    {
        if (!IsServer) return;

        // 인간이 죽으면 좀비로 변이
        if (currentTeam.Value == Team.Human)
        {
            currentTeam.Value = Team.NormalZombie;
            currentHealth.Value = maxHealth.Value; // 체력 회복
            // 좀비 변이 이벤트 호출 가능
        }
        else
        {
            // 좀비 사망 처리
            // 아이템 드랍 확률 10%
            if (Random.value <= 0.1f && killerId != 9999)
            {
                // 재료 3종 중 1개 랜덤
                MaterialType randomMat = (MaterialType)Random.Range(0, 3);
                
                // 킬러에게 즉시 지급 (물리적 드랍 대신 다이렉트 지급)
                var clientObj = NetworkManager.Singleton.ConnectedClients[killerId].PlayerObject;
                if (clientObj != null) 
                {
                    clientObj.GetComponent<InventorySystem>()?.AddMaterial(randomMat);
                }
            }
        }
    }
    
    private void OnTeamChanged(Team previous, Team current)
    {
        Debug.Log($"Player {OwnerClientId} changed team to {current}");
        if (current != Team.Human)
        {
            // TODO: Apply zombie visual/model change
        }
    }

    private void OnZombieTypeChanged(ZombieType previous, ZombieType current)
    {
        Debug.Log($"Player {OwnerClientId} evolved into {current} Zombie!");
        // TODO: Apply evolution specific stat bumps locally or visually
    }
}
