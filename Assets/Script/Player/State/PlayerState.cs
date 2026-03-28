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

    // 스킬 사용 중 무적 상태
    public NetworkVariable<bool> isInvincible = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Reset to default
            currentTeam.Value = Team.Human;
            currentZombieType.Value = ZombieType.None;
            currentHealth.Value = maxHealth.Value;
            isInvincible.Value = false;
            
            // Register to RoundManager
            if (RoundManager.Instance != null)
            {
                RoundManager.Instance.RegisterPlayer(this);
            }
        }
        
        if (IsOwner)
        {
            // 오직 C# 코드로만 런타임에 완벽한 UI 캔버스를 창조 (프리팹/인스펙터 연결이 전혀 필요 없음)
            UIBuilder.CreateGameHUD(GetComponent<PlayerClass>());

            var mainMenu = GameObject.Find("MainMenu_Canvas");
            if (mainMenu != null)
            {
                mainMenu.SetActive(false);
            }
        }

        currentTeam.OnValueChanged += OnTeamChanged;
        currentZombieType.OnValueChanged += OnZombieTypeChanged;
        
        // 스폰 시 현재 팀 색상 초기화 (늦게 접속한 클라이언트나 시작 시 좀비인 경우)
        ApplyTeamColor(currentTeam.Value);
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

        // 무적 상태면 데미지 무시
        if (isInvincible.Value)
        {
            Debug.Log($"[TakeDamage] Player {OwnerClientId} is invincible! Damage {amount} blocked.");
            return;
        }

        amount = Mathf.Max(1, amount); // 최소 데미지 1 보장
        currentHealth.Value -= amount;
        Debug.Log($"[TakeDamage] Player {OwnerClientId} 받았음: {amount}. 남은 HP: {currentHealth.Value}");

        if (currentHealth.Value <= 0)
        {
            Die(killerId);
        }
    }

    // 클라이언트 -> 서버 무적 상태 변경 요청
    [ServerRpc(RequireOwnership = false)]
    public void SetInvincibleServerRpc(bool value)
    {
        isInvincible.Value = value;
    }

    // 클라이언트 -> 서버 힐 요청
    [ServerRpc(RequireOwnership = false)]
    public void HealServerRpc(int amount)
    {
        currentHealth.Value = Mathf.Min(currentHealth.Value + amount, maxHealth.Value);
    }

    // 클라이언트 -> 서버 공격 요청 (CombatSystem이 MonoBehaviour이므로 여기서 RPC 처리)
    [ServerRpc(RequireOwnership = false)]
    public void AttackTargetServerRpc(NetworkObjectReference targetRef, int damage)
    {
        if (targetRef.TryGet(out NetworkObject targetObj))
        {
            var targetState = targetObj.GetComponent<PlayerState>();
            if (targetState != null)
            {
                targetState.TakeDamage(damage, OwnerClientId);
            }
        }
    }

    // 클라이언트 -> 서버 강제 이동(넉업/넉백) 요청
    [ServerRpc(RequireOwnership = false)]
    public void KnockUpServerRpc(Vector3 forceVelocity, float duration)
    {
        KnockUpClientRpc(forceVelocity, duration);
    }

    [ClientRpc]
    private void KnockUpClientRpc(Vector3 forceVelocity, float duration)
    {
        // 실제 물리적인 이동은 해당 캐릭터의 소유자(Client)가 수행해야 정상 동기화됨
        if (IsOwner)
        {
            var movement = GetComponent<PlayerMovement>();
            if (movement != null)
                movement.ApplyForcedMovement(forceVelocity, duration);
        }
    }

    // 좀비 사망 시 발생하는 전역 이벤트 (킬러의 ClientId 전달)
    public static event System.Action<ulong> OnAnyZombieDied;

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
            // 사망 이벤트 전파 (인벤토리 시스템 등에서 수신하여 아이템 지급 처리)
            if (killerId != 9999)
            {
                OnAnyZombieDied?.Invoke(killerId);
            }
        }
    }
    
    private void OnTeamChanged(Team previous, Team current)
    {
        Debug.Log($"Player {OwnerClientId} changed team to {current}");
        ApplyTeamColor(current);
    }

    private Material zombieOverlayMaterial;

    private void ApplyTeamColor(Team team)
    {
        bool isZombie = (team != Team.Human);
        
        if (zombieOverlayMaterial == null)
        {
            zombieOverlayMaterial = new Material(Shader.Find("Sprites/Default"));
            zombieOverlayMaterial.color = new Color(1f, 0f, 0f, 0.5f); 
            zombieOverlayMaterial.name = "ZombieOverlay";
        }

        // 특정 이름을 가진 자식 SkinnedMeshRenderer 찾기 (이름: bodymesh)
        SkinnedMeshRenderer targetSmr = null;
        var smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var smr in smrs)
        {
            if (smr.gameObject.name.ToLower().Contains("bodymesh"))
            {
                targetSmr = smr;
                break;
            }
        }

        if (targetSmr != null)
        {
            ApplyOverlay(targetSmr, isZombie);
            Debug.Log($"[ApplyTeamColor] {OwnerClientId} ({team}) -> {targetSmr.gameObject.name} 에 SMR 오버레이 적용됨. isZombie={isZombie}");
            return;
        }

        // 못 찾았다면 전체 렌더러에 폴백 (Fallback)
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Debug.Log($"[ApplyTeamColor] {OwnerClientId} ({team}) -> 정확한 bodymesh를 찾지 못하여 {renderers.Length}개의 모든 렌더러에 적용 시도.");
        foreach (var rend in renderers)
        {
            ApplyOverlay(rend, isZombie);
        }
    }

    private void ApplyOverlay(Renderer rend, bool isZombie)
    {
        var mats = new System.Collections.Generic.List<Material>(rend.sharedMaterials);
        
        // 기존 좀비 오버레이 제거
        mats.RemoveAll(m => m != null && m.name.StartsWith("ZombieOverlay"));

        // 좀비라면 오버레이 추가
        if (isZombie)
        {
            mats.Add(zombieOverlayMaterial);
        }
        
        rend.materials = mats.ToArray();
    }

    private void OnZombieTypeChanged(ZombieType previous, ZombieType current)
    {
        Debug.Log($"Player {OwnerClientId} evolved into {current} Zombie!");
        // TODO: Apply evolution specific stat bumps locally or visually
    }
}
