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

public class PlayerState : NetworkBehaviour, IDamageable
{
    public NetworkVariable<Team> currentTeam = new NetworkVariable<Team>(Team.Human, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public Team CurrentTeam => currentTeam.Value;
    public Transform EntityTransform => transform;
    public NetworkObject GetNetworkObject() => NetworkObject;

    public NetworkVariable<Unity.Collections.FixedString32Bytes> Nickname = new NetworkVariable<Unity.Collections.FixedString32Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(150, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> maxHealth = new NetworkVariable<int>(150, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public NetworkVariable<ZombieType> currentZombieType = new NetworkVariable<ZombieType>(ZombieType.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // 서버 측 누적 데미지 카운터 (라운드 종료 시 EXP 환산에 사용)
    public NetworkVariable<int> totalDamageDealt = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // 좀비 전환 카운트 (인간을 죽여서 좀비로 만든 횟수, 라운드 보상용)
    public NetworkVariable<int> zombieConversionCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public static System.Collections.Generic.List<PlayerState> AllPlayersList = new System.Collections.Generic.List<PlayerState>();

    // 헬퍼: 팀 판별 로직 (좀비끼리는 같은 팀으로 처리)
    public bool IsEnemy(Team otherTeam)
    {
        bool amIHuman = (this.currentTeam.Value == Team.Human);
        bool isOtherHuman = (otherTeam == Team.Human);
        return amIHuman != isOtherHuman;
    }

    // 스킬 사용 중 무적 상태
    public NetworkVariable<bool> isInvincible = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Skill VFX Prefabs (Abstract Energy)")]
    [SerializeField] private GameObject vfxStraight;
    [SerializeField] private GameObject vfxRising;
    [SerializeField] private GameObject vfxTyphoon;
    [SerializeField] private GameObject vfxRupture;
    [SerializeField] private GameObject vfxBuff;
    [SerializeField] private GameObject vfxLightning;
    [SerializeField] private GameObject vfxOrb;
    [SerializeField] private GameObject vfxSmear;
    [SerializeField] private GameObject vfxAbstractFlash;

    [Header("Hit Impact VFX (Networked)")]
    [SerializeField] private GameObject hitImpactNormal;
    [SerializeField] private GameObject hitImpactCritical;
    [SerializeField] private GameObject hitImpactDoT;

    [Header("Weapon Elements")]
    [SerializeField] private Transform weaponTransform;
    public Transform WeaponTransform => weaponTransform;

    public override void OnNetworkSpawn()
    {
        if (!AllPlayersList.Contains(this))
            AllPlayersList.Add(this);

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
        
        if (IsOwner && LocalUserData.Current != null)
        {
            SetNicknameServerRpc(LocalUserData.Current.Nickname);

            var mainMenu = GameObject.Find("MainMenu_Canvas");
            if (mainMenu != null)
            {
                mainMenu.SetActive(false);
            }
        }

        currentTeam.OnValueChanged += OnTeamChanged;
        currentZombieType.OnValueChanged += OnZombieTypeChanged;
        
    }

    public override void OnNetworkDespawn()
    {
        if (AllPlayersList.Contains(this))
            AllPlayersList.Remove(this);

        if (IsServer && RoundManager.Instance != null)
        {
            RoundManager.Instance.UnregisterPlayer(this);
        }
        
        currentTeam.OnValueChanged -= OnTeamChanged;
        currentZombieType.OnValueChanged -= OnZombieTypeChanged;
    }

    [Rpc(SendTo.Server)]
    public void SetNicknameServerRpc(string nick)
    {
        Nickname.Value = nick;
        string ip = "알수없음";
        if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is Unity.Netcode.Transports.UTP.UnityTransport utp)
        {
            // NGO UTP에서는 현재 접속한 클라이언트의 접속 주소를 반환할 수 있는 기능이 제한적일 수 있으나,
            // 보통 Local IP 등은 간단히 표시하거나, 혹은 NetworkManager.ConnectedClients를 통해 우회합니다.
            // 여기서는 임시로 ClientId 매핑을 사용합니다.
            ip = "Client " + OwnerClientId; 
        }
        ServerConsole.LogConnection(nick, ip);
    }

    // 서버 전용 함수: 피해 입히기
    public void TakeDamage(int amount, string skillName = "일반공격", ulong killerId = 9999)
    {
        if (!IsServer) return;

        // 무적 상태면 데미지 무시
        if (isInvincible.Value) return;

        amount = Mathf.Max(1, amount); // 최소 데미지 1 보장
        currentHealth.Value -= amount;

        // 닉네임 파싱
        string tNick = string.IsNullOrEmpty(Nickname.Value.ToString()) ? $"유저{OwnerClientId}" : Nickname.Value.ToString();
        string aNick = $"유저{killerId}";
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(killerId, out var client) && client.PlayerObject != null)
        {
            var aState = client.PlayerObject.GetComponent<PlayerState>();
            if (aState != null && !string.IsNullOrEmpty(aState.Nickname.Value.ToString()))
                aNick = aState.Nickname.Value.ToString();
        }

        ServerConsole.LogDamage(aNick, tNick, skillName, amount, currentHealth.Value);

        if (currentHealth.Value <= 0)
        {
            Die(killerId);
        }
    }

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
            // 대상이 IDamageable을 구현했는지 검사 (PlayerState 외에도 MonsterState 등 타격 가능)
            var damageable = targetObj.GetComponentInChildren<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, skillName, OwnerClientId);
                
                // 서버 측 누적 데미지 카운터 합산
                totalDamageDealt.Value += damage;
                
                // 모든 클라이언트에게 타격 이펙트 재생 요청
                PlayHitEffectClientRpc(hitPosition);
            }
            else
            {
                Debug.LogWarning($"[AttackTargetServerRpc] targetObj '{targetObj.name}'에서 IDamageable을 찾을 수 없음!");
            }
        }
    }

    /// <summary>라운드 종료 시 서버 측 카운터 전체 리셋</summary>
    public void ResetRoundCounters()
    {
        if (!IsServer) return;
        totalDamageDealt.Value = 0;
        zombieConversionCount.Value = 0;
        currentZombieType.Value = ZombieType.None;
    }

    /// <summary>라운드 종료 후 모든 플레이어 데이터 저장 (클라이언트에서 실행)</summary>
    [ClientRpc]
    public void SavePlayerDataClientRpc(int expReward, int goldReward)
    {
        if (!IsOwner || LocalUserData.Current == null) return;
        // EXP/Gold는 이미 서버에서 AddExp 등으로 처리된 후 동기화됨
        // 여기서는 로컬 저장만 수행
        var pExp = GetComponentInChildren<PlayerExperience>();
        if (pExp != null)
        {
            LocalUserData.Current.Level = pExp.Level.Value;
            LocalUserData.Current.Exp = pExp.CurrentExp.Value;
        }
        LocalUserData.Current.Gold += goldReward;
        CsvDatabase.Instance.SaveUser(LocalUserData.Current);
        Debug.Log($"[SaveData] EXP+{expReward}, Gold+{goldReward} 저장 완료");
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
        // 공격자의 CombatSystem 캐싱 (서버 측 데미지 카운터 업데이트용)
        CombatSystem attackerCombat = null;
        if (attackerClientId != ulong.MaxValue && NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(attackerClientId, out var attackerClient))
        {
            if (attackerClient.PlayerObject != null)
                attackerCombat = attackerClient.PlayerObject.GetComponentInChildren<CombatSystem>();
        }

        for (int i = 0; i < totalTicks; i++)
        {
            yield return new WaitForSeconds(tickInterval);
            if (!IsServer || currentHealth.Value <= 0) yield break; // 죽었으면 중단

            int computedDamage = 0;
            switch (damageType)
            {
                case 0: // 고정 피해
                    computedDamage = Mathf.RoundToInt(damagePerTick);
                    break;
                case 1: // 최대체력 비례 피해 (%)
                    computedDamage = Mathf.RoundToInt(maxHealth.Value * (damagePerTick / 100f));
                    break;
                case 2: // 공격력 비례 피해 (%) - 여기선 가변 인자 자체가 공격력이 치환되어 넘어왔다고 간주하여 고정 피해 처리
                    computedDamage = Mathf.RoundToInt(damagePerTick);
                    break;
            }

            if (computedDamage > 0)
            {
                currentHealth.Value = Mathf.Max(0, currentHealth.Value - computedDamage);
                string tNick = string.IsNullOrEmpty(Nickname.Value.ToString()) ? $"유저{OwnerClientId}" : Nickname.Value.ToString();
                string dType = damageType == 0 ? "도트" : damageType == 1 ? "최대체력비례" : "공격력비례";
                ServerConsole.LogDoT(tNick, dType, computedDamage, currentHealth.Value);
                
                // 공격자의 누적 데미지에 합산 (서버 측 CombatSystem 로컬 + PlayerState NetworkVariable)
                if (attackerClientId != ulong.MaxValue &&
                    NetworkManager.Singleton.ConnectedClients.TryGetValue(attackerClientId, out var atkClient) &&
                    atkClient.PlayerObject != null)
                {
                    var atkState = atkClient.PlayerObject.GetComponentInChildren<PlayerState>();
                    if (atkState != null) atkState.totalDamageDealt.Value += computedDamage;
                }

                // DoT 이펙트(빨간색 플래시 등)용 7번 타격 섬광 사용
                SpawnSkillVFXClientRpc(7, transform.position + Vector3.up, Quaternion.identity);

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
        // 호스트는 서버 측에서 이미 처리했으므로 중복 방지
        if (IsServer) return;
        // 자신이 공격자인 경우에만 처리
        if (NetworkManager.Singleton.LocalClientId != attackerClientId) return;
        
        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer != null)
        {
            var combat = localPlayer.GetComponentInChildren<CombatSystem>();
            if (combat != null) combat.AddDotDamage(damage);
        }
    }

    [ClientRpc]
    private void PlayHitEffectClientRpc(Vector3 position)
    {
        // 공용 타격 이펙트는 무투가 스킬 전용 시스템으로 대체되어 사용하지 않음
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SpawnSkillVFXServerRpc(int vfxType, Vector3 position, Quaternion rotation)
    {
        SpawnSkillVFXClientRpc(vfxType, position, rotation);
    }

    [ClientRpc]
    private void SpawnSkillVFXClientRpc(int vfxType, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = null;
        switch (vfxType)
        {
            case 0: prefab = vfxStraight; break;
            case 1: prefab = vfxRising; break;
            case 2: prefab = vfxTyphoon; break;
            case 3: prefab = vfxRupture; break;
            case 4: prefab = vfxLightning; break;
            case 5: prefab = vfxOrb; break;
            case 6: prefab = vfxSmear; break;
            case 7: prefab = vfxAbstractFlash; break;
        }

        if (prefab != null)
        {
            GameObject vfx = Instantiate(prefab, position, rotation);
            Destroy(vfx, 1.5f); // 생존 시간 1.5초 확장
        }
    }

    // 클라이언트 -> 서버 강제 이동(넉업/넉백) 요청
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
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
            // PlayerMovement는 루트에 있으므로 GetComponentInParent 사용
            var movement = GetComponentInParent<PlayerMovement>();
            if (movement != null)
                movement.ApplyForcedMovement(forceVelocity, duration);
            else
                Debug.LogWarning("[KnockUp] PlayerMovement를 찾을 수 없음!");
        }
    }

    // 좀비 사망 시 발생하는 전역 이벤트 (킬러의 ClientId 전달)
    public static event System.Action<ulong> OnAnyZombieDied;

    private void Die(ulong killerId)
    {
        if (!IsServer) return;

        // 사망 이벤트 전파 (인벤토리 시스템 등에서 수신하여 아이템 지급 처리)
        if (killerId != 9999)
        {
            OnAnyZombieDied?.Invoke(killerId);

            // 킬러(좀비)의 전환 카운트 증가 (인간이 죽어서 좀비가 된 경우)
            if (currentTeam.Value == Team.Human)
            {
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(killerId, out var killerClient)
                    && killerClient.PlayerObject != null)
                {
                    var killerState = killerClient.PlayerObject.GetComponentInChildren<PlayerState>();
                    if (killerState != null && killerState.currentTeam.Value != Team.Human)
                    {
                        killerState.zombieConversionCount.Value++;
                    }
                }
            }
        }

        // 인간 사망 → 일반 좀비로 리스폰 (좀비가 인간을 죽인 경우)
        if (currentTeam.Value == Team.Human)
        {
            currentTeam.Value = Team.NormalZombie;
            // 팔라딘 충전량 초기화
            var paladin = GetComponentInChildren<PaladinSkillExecutor>();
            if (paladin != null) paladin.ResetShieldEnergy();
        }
        else
        {
            // 좀비가 죽은 경우 체력만 복구하여 제자리 리스폰
            currentHealth.Value = maxHealth.Value;
        }
        
        // 스폰 장소로 이동 (좀비 리스폰 위치)
        var movement = GetComponentInParent<PlayerMovement>();
        if (movement != null)
        {
            var charCtrl = movement.GetComponent<UnityEngine.CharacterController>();
            if (charCtrl != null) charCtrl.enabled = false;
            movement.transform.position = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
            if (charCtrl != null) charCtrl.enabled = true;
        }
    }
    
    private void OnTeamChanged(Team previous, Team current)
    {
        Debug.Log($"Player {OwnerClientId} changed team to {current}");
       

        if (IsServer)
        {
            if (current == Team.HostZombie)
            {
                maxHealth.Value = 100000;
                currentHealth.Value = 100000;
            }
            else if (current == Team.NormalZombie)
            {
                maxHealth.Value = 50000;
                currentHealth.Value = 50000;
            }
        }
    }


    private void OnZombieTypeChanged(ZombieType previous, ZombieType current)
    {
        Debug.Log($"Player {OwnerClientId} evolved into {current} Zombie!");
        // TODO: Apply evolution specific stat bumps locally or visually
    }
}
