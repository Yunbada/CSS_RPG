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

    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> maxHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public NetworkVariable<ZombieType> currentZombieType = new NetworkVariable<ZombieType>(ZombieType.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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
    public GameObject vfxStraight;        // 정권/충격파
    public GameObject vfxRising;          // 승천권/상승기둥
    public GameObject vfxTyphoon;         // 풍각/태풍의눈
    public GameObject vfxRupture;         // 파쇄권/지면파열
    public GameObject vfxLightning;       // 번개춤/전격폭발
    public GameObject vfxOrb;             // 연탄/에너지구체
    public GameObject vfxSmear;           // 연파/고속잔상
    public GameObject vfxAbstractFlash;   // 공용/타격섬광

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
        
        if (IsOwner)
        {
            // 오직 C# 코드로만 런타임에 완벽한 UI 캔버스를 창조 (프리팹/인스펙터 연결이 전혀 필요 없음)
            UIBuilder.CreateGameHUD(GetComponent<PlayerClass>());

            var mainMenu = GameObject.Find("MainMenu_Canvas"); //플레이어 + Canvas 얘는 MainmenuCanvas 오브젝트로 만들어 놓고 플레이어 화면이랑 연동시키는 형태?
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
        if (AllPlayersList.Contains(this))
            AllPlayersList.Remove(this);

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
    public void AttackTargetServerRpc(NetworkObjectReference targetRef, int damage, Vector3 hitPosition)
    {
        if (targetRef.TryGet(out NetworkObject targetObj))
        {
            // 대상이 IDamageable을 구현했는지 검사 (PlayerState 외에도 MonsterState 등 타격 가능)
            var damageable = targetObj.GetComponentInChildren<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, OwnerClientId);
                
                // 모든 클라이언트에게 타격 이펙트 재생 요청
                PlayHitEffectClientRpc(hitPosition);
            }
            else
            {
                Debug.LogWarning($"[AttackTargetServerRpc] targetObj '{targetObj.name}'에서 IDamageable을 찾을 수 없음!");
            }
        }
    }

    // =========================================================================
    // 복합 상태이상 지속 피해 (DoT) 처리
    // =========================================================================
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ApplyDoTServerRpc(int damageType, int totalTicks, float tickInterval, float damagePerTick)
    {
        StartCoroutine(DoTCoroutine(damageType, totalTicks, tickInterval, damagePerTick));
    }

    private System.Collections.IEnumerator DoTCoroutine(int damageType, int totalTicks, float tickInterval, float damagePerTick)
    {
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
                Debug.Log($"[DoT] 타입 {damageType} 기반으로 {computedDamage} 의 화상/출혈 피해 발생!");
                
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
        }

        // 인간이든 숙주 좀비든 사망 시 무조건 일반 좀비 50,000 체력으로 리스폰
        currentTeam.Value = Team.NormalZombie;
        maxHealth.Value = 50000;
        currentHealth.Value = 50000;
        
        // 스폰 장소로 이동시킴
        var movement = GetComponentInParent<PlayerMovement>();
        if (movement != null)
        {
            var charCtrl = movement.GetComponent<UnityEngine.CharacterController>();
            if (charCtrl != null) charCtrl.enabled = false;
            movement.transform.position = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f)); // 임시 무작위 좌표 스폰
            if (charCtrl != null) charCtrl.enabled = true;
        }
    }
    
    private void OnTeamChanged(Team previous, Team current)
    {
        Debug.Log($"Player {OwnerClientId} changed team to {current}");
        ApplyTeamColor(current);

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
