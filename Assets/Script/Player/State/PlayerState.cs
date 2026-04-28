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

/// <summary>
/// 플레이어 핵심 상태 데이터 (SRP 리팩토링)
/// 팀, 좀비 타입 등 게임 규칙 관련 상태만 동기화합니다.
/// 체력/전투는 PlayerHealth, 인증은 PlayerAuthentication,
/// VFX는 PlayerVFXController, 생명주기는 PlayerLifecycleManager에서 담당합니다.
/// </summary>
public class PlayerState : NetworkBehaviour
{
    public NetworkVariable<Team> currentTeam = new NetworkVariable<Team>(
        Team.Human, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public Team CurrentTeam => currentTeam.Value;

    public NetworkVariable<ZombieType> currentZombieType = new NetworkVariable<ZombieType>(
        ZombieType.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // 전체 플레이어 목록 (모든 모듈 공통 참조)
    public static System.Collections.Generic.List<PlayerState> AllPlayersList = new System.Collections.Generic.List<PlayerState>();

    // 헬퍼: 팀 판별 로직 (좀비끼리는 같은 팀으로 처리)
    public bool IsEnemy(Team otherTeam)
    {
        bool amIHuman = (this.currentTeam.Value == Team.Human);
        bool isOtherHuman = (otherTeam == Team.Human);
        return amIHuman != isOtherHuman;
    }

    public override void OnNetworkSpawn()
    {
        if (!AllPlayersList.Contains(this))
            AllPlayersList.Add(this);

        if (IsServer)
        {
            currentTeam.Value = Team.Human;
            currentZombieType.Value = ZombieType.None;
        }

        currentTeam.OnValueChanged += OnTeamChanged;
        currentZombieType.OnValueChanged += OnZombieTypeChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (AllPlayersList.Contains(this))
            AllPlayersList.Remove(this);

        if (IsServer && RoundManager.Instance != null)
        {
            RoundManager.Instance.UnregisterPlayer(this);
        }
        
        currentTeam.OnValueChanged -= OnTeamChanged;
        currentZombieType.OnValueChanged -= OnZombieTypeChanged;
    }
    
    private void OnTeamChanged(Team previous, Team current)
    {
        Debug.Log($"Player {OwnerClientId} changed team to {current}");

        if (IsServer)
        {
            var health = GetComponent<PlayerHealth>();
            if (health != null)
            {
                if (current == Team.HostZombie)
                {
                    health.maxHealth.Value = 100000;
                    health.currentHealth.Value = 100000;
                }
                else if (current == Team.NormalZombie)
                {
                    health.maxHealth.Value = 50000;
                    health.currentHealth.Value = 50000;
                }
            }
        }
    }

    private void OnZombieTypeChanged(ZombieType previous, ZombieType current)
    {
        Debug.Log($"Player {OwnerClientId} evolved into {current} Zombie!");
        // TODO: Apply evolution specific stat bumps locally or visually
    }
}
