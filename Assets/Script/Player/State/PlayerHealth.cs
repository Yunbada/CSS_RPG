using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 플레이어 체력 및 피격 처리 (SRP 분리)
/// 체력 관리, 데미지 처리, 사망, DoT, 회복, 전투 RPC를 전담합니다.
/// IDamageable 인터페이스를 구현합니다.
/// </summary>
public class PlayerHealth : NetworkBehaviour, IDamageable
{
    [Header("체력 동기화")]
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(150, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> maxHealth = new NetworkVariable<int>(150, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // 스킬 사용 중 무적 상태
    public NetworkVariable<bool> isInvincible = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // 서버 측 누적 데미지 카운터 (라운드 종료 시 EXP 환산에 사용)
    public NetworkVariable<int> totalDamageDealt = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // 좀비 전환 카운트 (인간을 죽여서 좀비로 만든 횟수, 라운드 보상용)
    public NetworkVariable<int> zombieConversionCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // IDamageable 인터페이스 구현
    private PlayerState playerState;
    public Team CurrentTeam => playerState != null ? playerState.CurrentTeam : Team.Human;
    public Transform EntityTransform => transform;
    public NetworkObject GetNetworkObject() => NetworkObject;

    // 좀비 사망 시 발생하는 전역 이벤트 (킬러의 ClientId, 사망자의 팀, 사망 위치 전달)
    public static event System.Action<ulong, Team, Vector3> OnAnyZombieDied;

    // 형제 컴포넌트 참조
    private PlayerVFXController vfxController;
    private PlayerAuthentication auth;

    [Header("Weapon Elements")]
    [SerializeField] private Transform weaponTransform;
    public Transform WeaponTransform => weaponTransform;

    public override void OnNetworkSpawn()
    {
        playerState = GetComponent<PlayerState>();
        vfxController = GetComponent<PlayerVFXController>();
        auth = GetComponent<PlayerAuthentication>();

        if (IsServer)
        {
            currentHealth.Value = maxHealth.Value;
            isInvincible.Value = false;
        }
    }

    // =========================================================================
    // 서버 전용 함수: 피해 입히기
    // =========================================================================
    public void TakeDamage(int amount, string skillName = "일반공격", ulong killerId = 9999)
    {
        if (!IsServer) return;

        // 무적 상태면 데미지 무시
        if (isInvincible.Value) return;

        amount = Mathf.Max(1, amount); // 최소 데미지 1 보장
        currentHealth.Value -= amount;

        // 닉네임 파싱
        string tNick = auth != null && !string.IsNullOrEmpty(auth.Nickname.Value.ToString()) 
            ? auth.Nickname.Value.ToString() 
            : $"유저{OwnerClientId}";
        string aNick = $"유저{killerId}";
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(killerId, out var client) && client.PlayerObject != null)
        {
            var aAuth = client.PlayerObject.GetComponentInChildren<PlayerAuthentication>();
            if (aAuth != null && !string.IsNullOrEmpty(aAuth.Nickname.Value.ToString()))
                aNick = aAuth.Nickname.Value.ToString();
        }

        ServerConsole.LogDamage(aNick, tNick, skillName, amount, currentHealth.Value);

        if (currentHealth.Value <= 0)
        {
            Die(killerId);
        }
    }

    // =========================================================================
    // 무적 / 힐 / 공격 RPC
    // =========================================================================

    // 클라이언트 -> 서버 무적 상태 변경 요청
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetInvincibleServerRpc(bool value)
    {
        isInvincible.Value = value;
    }

    // 클라이언트 -> 서버 힐 요청
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void HealServerRpc(int amount)
    {
        currentHealth.Value = Mathf.Min(currentHealth.Value + amount, maxHealth.Value);
    }

    // 클라이언트 -> 서버 공격 요청 (타격 지점 포함)
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AttackTargetServerRpc(NetworkObjectReference targetRef, int damage, string skillName, Vector3 hitPosition)
    {
        if (targetRef.TryGet(out NetworkObject targetObj))
        {
            var damageable = targetObj.GetComponentInChildren<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, skillName, OwnerClientId);
                
                // 서버 측 누적 데미지 카운터 합산
                totalDamageDealt.Value += damage;
            }
            else
            {
                Debug.LogWarning($"[AttackTargetServerRpc] targetObj '{targetObj.name}'에서 IDamageable을 찾을 수 없음!");
            }
        }
    }

    // =========================================================================
    // 넉백/텔레포트 RPC
    // =========================================================================

    // 클라이언트 -> 서버 강제 이동(넉업/넉백) 요청
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void KnockUpServerRpc(Vector3 forceVelocity, float duration)
    {
        KnockUpClientRpc(forceVelocity, duration);
    }

    [ClientRpc]
    private void KnockUpClientRpc(Vector3 forceVelocity, float duration)
    {
        if (IsOwner)
        {
            var movement = GetComponentInParent<PlayerMovement>();
            if (movement != null)
                movement.ApplyForcedMovement(forceVelocity, duration);
            else
                Debug.LogWarning("[KnockUp] PlayerMovement를 찾을 수 없음!");
        }
    }

    [ClientRpc]
    public void TeleportClientRpc(Vector3 position)
    {
        if (IsOwner)
        {
            var movement = GetComponentInParent<PlayerMovement>();
            if (movement != null)
            {
                var charCtrl = movement.GetComponent<UnityEngine.CharacterController>();
                if (charCtrl != null) charCtrl.enabled = false;
                movement.transform.position = position;
                if (charCtrl != null) charCtrl.enabled = true;
            }
        }
    }

    // =========================================================================
    // 라운드 카운터 리셋
    // =========================================================================

    /// <summary>라운드 종료 시 서버 측 카운터 전체 리셋</summary>
    public void ResetRoundCounters()
    {
        if (!IsServer) return;
        totalDamageDealt.Value = 0;
        zombieConversionCount.Value = 0;
        if (playerState != null)
        {
            playerState.currentZombieType.Value = ZombieType.None;
        }
    }

    // =========================================================================
    // 복합 상태이상 지속 피해 (DoT) 처리
    // =========================================================================
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ApplyDoTServerRpc(int damageType, int totalTicks, float tickInterval, float damagePerTick)
    {
        StartCoroutine(DoTCoroutine(damageType, totalTicks, tickInterval, damagePerTick, ulong.MaxValue));
    }

    /// <summary>공격자 추적 버전 - DoT 데미지를 공격자의 TotalDamageDealt에 합산</summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ApplyDoTWithAttackerServerRpc(int damageType, int totalTicks, float tickInterval, float damagePerTick, ulong attackerClientId)
    {
        StartCoroutine(DoTCoroutine(damageType, totalTicks, tickInterval, damagePerTick, attackerClientId));
    }

    private System.Collections.IEnumerator DoTCoroutine(int damageType, int totalTicks, float tickInterval, float damagePerTick, ulong attackerClientId)
    {
        for (int i = 0; i < totalTicks; i++)
        {
            yield return new WaitForSeconds(tickInterval);
            if (!IsServer || currentHealth.Value <= 0) yield break;

            int computedDamage = 0;
            switch (damageType)
            {
                case 0: // 고정 피해
                    computedDamage = Mathf.RoundToInt(damagePerTick);
                    break;
                case 1: // 최대체력 비례 피해 (%)
                    computedDamage = Mathf.RoundToInt(maxHealth.Value * (damagePerTick / 100f));
                    break;
                case 2: // 공격력 비례 피해 (%)
                    computedDamage = Mathf.RoundToInt(damagePerTick);
                    break;
            }

            if (computedDamage > 0)
            {
                currentHealth.Value = Mathf.Max(0, currentHealth.Value - computedDamage);
                string tNick = auth != null && !string.IsNullOrEmpty(auth.Nickname.Value.ToString()) 
                    ? auth.Nickname.Value.ToString() 
                    : $"유저{OwnerClientId}";
                string dType = damageType == 0 ? "도트" : damageType == 1 ? "최대체력비례" : "공격력비례";
                ServerConsole.LogDoT(tNick, dType, computedDamage, currentHealth.Value);
                
                // 공격자의 누적 데미지에 합산
                if (attackerClientId != ulong.MaxValue &&
                    NetworkManager.Singleton.ConnectedClients.TryGetValue(attackerClientId, out var atkClient) &&
                    atkClient.PlayerObject != null)
                {
                    var atkHealth = atkClient.PlayerObject.GetComponentInChildren<PlayerHealth>();
                    if (atkHealth != null) atkHealth.totalDamageDealt.Value += computedDamage;
                }

                // DoT 이펙트(빨간색 플래시 등)용 7번 타격 섬광 사용
                if (vfxController != null)
                {
                    vfxController.SpawnSkillVFXClientRpc(7, transform.position + Vector3.up, Quaternion.identity);
                }

                if (currentHealth.Value <= 0)
                {
                    Die(9999);
                    yield break;
                }
            }
        }
    }

    /// <summary>DoT 데미지를 공격자 클라이언트의 CombatSystem에 동기화</summary>
    [ClientRpc]
    private void NotifyDoTDamageClientRpc(int damage, ulong attackerClientId)
    {
        if (IsServer) return;
        if (NetworkManager.Singleton.LocalClientId != attackerClientId) return;
        
        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer != null)
        {
            var combat = localPlayer.GetComponentInChildren<CombatSystem>();
            if (combat != null) combat.AddDotDamage(damage);
        }
    }

    // =========================================================================
    // 사망 처리
    // =========================================================================
    private void Die(ulong killerId)
    {
        if (!IsServer) return;

        Team diedTeam = playerState != null ? playerState.currentTeam.Value : Team.Human;

        // 물리적 3D 아이템 드롭 (숙주 좀비 처치 시 무조건 1회 드롭)
        if (diedTeam == Team.HostZombie && ItemDatabase.Instance != null)
        {
            ItemDatabase.Instance.RollLootDrop(transform.position + Vector3.up * 1.5f);
        }

        // 사망 이벤트 전파
        if (killerId != 9999)
        {
            if (diedTeam != Team.Human)
            {
                OnAnyZombieDied?.Invoke(killerId, diedTeam, transform.position);
            }

            // 킬러(좀비)의 전환 카운트 증가 (인간이 죽어서 좀비가 된 경우)
            if (diedTeam == Team.Human)
            {
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(killerId, out var killerClient)
                    && killerClient.PlayerObject != null)
                {
                    var killerHealth = killerClient.PlayerObject.GetComponentInChildren<PlayerHealth>();
                    if (killerHealth != null)
                    {
                        var killerState = killerClient.PlayerObject.GetComponentInChildren<PlayerState>();
                        if (killerState != null && killerState.currentTeam.Value != Team.Human)
                        {
                            killerHealth.zombieConversionCount.Value++;
                        }
                    }
                }
            }
        }

        // 인간 또는 숙주 좀비 사망 → 일반 좀비로 리스폰
        if (playerState != null && (playerState.currentTeam.Value == Team.Human || playerState.currentTeam.Value == Team.HostZombie))
        {
            playerState.currentTeam.Value = Team.NormalZombie;
            // 일반 좀비 진화 초기화 (다시 선택 가능하게)
            playerState.currentZombieType.Value = ZombieType.None;
            
            var paladin = GetComponentInChildren<PaladinSkillExecutor>();
            if (paladin != null) paladin.ResetShieldEnergy();
        }
        else
        {
            // 좀비가 죽은 경우 체력만 복구하여 제자리 리스폰
            currentHealth.Value = maxHealth.Value;
        }
        
        // 스폰 장소로 이동 (좀비 리스폰 위치)
        TeleportClientRpc(new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f)));
    }
}
