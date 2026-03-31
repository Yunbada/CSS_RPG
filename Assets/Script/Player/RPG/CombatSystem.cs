using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 전투 시스템 (MonoBehaviour)
/// NetworkBehaviour가 아니므로 런타임 AddComponent 후에도 정상 작동합니다.
/// ServerRpc는 PlayerState.AttackTargetServerRpc()를 통해 위임합니다.
/// </summary>
public class CombatSystem : MonoBehaviour
{
    [Header("기본 공격 설정")]
    public float basicAttackRange = 4f;
    public float basicAttackCooldown = 0.5f;
    public float basicAttackMultiplier = 1.0f;

    private float basicAttackTimer = 0f;

    private InputHandle inputHandle;
    private PlayerClass playerClass;
    private PlayerState playerState;
    private StatSystem statSystem;
    private SkillSystem skillSystem;
    private Camera playerCamera;

    // 전직별 스킬 실행기
    private FighterSkillExecutor fighterExecutor;

    private bool isInitialized = false;

    private void Awake()
    {
        inputHandle = GetComponentInParent<InputHandle>();
        playerClass = GetComponent<PlayerClass>();
        playerState = GetComponentInParent<PlayerState>();
        statSystem = GetComponent<StatSystem>();
    }

    /// <summary>
    /// PlayerClass.OnNetworkSpawn()에서 명시적으로 호출됩니다.
    /// </summary>
    public void InitializeCombatSystem()
    {
        skillSystem = GetComponent<SkillSystem>();

        var cam = GetComponentInParent<PlayerMovement>()?.GetComponentInChildren<Camera>(true);
        if (cam != null) playerCamera = cam;

        // 무투가 스킬 실행기 자동 부착 및 초기화
        fighterExecutor = GetComponent<FighterSkillExecutor>();
        if (fighterExecutor == null)
            fighterExecutor = gameObject.AddComponent<FighterSkillExecutor>();
        fighterExecutor.Initialize(this, playerState);

        isInitialized = true;
        Debug.Log("[CombatSystem] 초기화 완료!");
    }

    // 스킬 사용 중 여부 (FighterSkillExecutor에서 제어)
    public bool IsUsingSkill => fighterExecutor != null && fighterExecutor.isUsingSkill;

    private void Update()
    {
        if (!isInitialized) return;

        // 기본 공격 쿨타임 감소
        if (basicAttackTimer > 0f)
            basicAttackTimer -= Time.deltaTime;

        // 전직 메뉴 열려있으면 공격 차단
        var classCtrl = FindObjectOfType<ClassSelectionController>();
        if (classCtrl != null && classCtrl.panel != null && classCtrl.panel.activeSelf)
            return;

        // 스킬 사용 중이면 기본 공격 차단
        if (IsUsingSkill) return;

        // 마우스 좌클릭 기본 공격
        if (inputHandle != null && inputHandle.attackInput && basicAttackTimer <= 0f)
        {
            PerformBasicAttack();
            basicAttackTimer = basicAttackCooldown;
        }
    }

    // =========================================================================
    // 기본 공격 (마우스 좌클릭)
    // =========================================================================
    private void PerformBasicAttack()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        
        // SphereCastAll을 사용하여 타격 범위를 후하게 판정 (반경 1m 캡슐 형태 전방 투사)
        var hits = Physics.SphereCastAll(ray, 1.0f, basicAttackRange);
        foreach (var hit in hits)
        {
            var targetState = FindPlayerState(hit.collider.gameObject);
            if (targetState != null && targetState != playerState)
            {
                if (targetState.currentTeam.Value == playerState.currentTeam.Value) continue;

                int damage = CalculateDamage(basicAttackMultiplier, targetState);
                SendDamage(targetState, damage, hit.point);
                Debug.Log($"기본 공격! {damage} 데미지 적중!");
                return; // 최초의 유효한 대상 하나만 타격
            }
        }
    }

    // =========================================================================
    // 스킬 공격 (SkillSystem에서 호출됨)
    // =========================================================================
    public void ExecuteSkillAttack(int skillIndex)
    {
        if (skillSystem == null) return;

        SkillData skill = skillSystem.currentSkills[skillIndex];
        if (skill == null) return;

        // 스킬 사용 중이면 추가 스킬 사용 차단
        if (IsUsingSkill) return;

        // ====== 전직별 전용 스킬 실행기 위임 ======
        // 좀비가 아닌 인간 무투가일 때만 무투가 스킬 실행
        if (playerState != null && playerState.currentTeam.Value == Team.Human && 
            playerClass != null && playerClass.currentClass.Value == PlayerClassType.Fighter && fighterExecutor != null)
        {
            ExecuteFighterSkill(skillIndex);
            return;
        }

        // ====== 범용 스킬 처리 (다른 직업용 및 좀비용) ======
        if (playerCamera == null) return;

        if (skill.isSelfBuff)
        {
            Debug.Log($"버프 스킬 [{skill.skillName}] 발동!");
            return;
        }

        float range = skill.range > 0 ? skill.range : basicAttackRange;

        if (skill.areaRadius > 0)
            PerformAreaAttack(skill, range);
        else
            PerformSingleAttack(skill, range);
    }

    private void PerformSingleAttack(SkillData skill, float range)
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        
        var hits = Physics.SphereCastAll(ray, 1.0f, range);
        foreach (var hit in hits)
        {
            var targetState = FindPlayerState(hit.collider.gameObject);
            if (targetState != null && targetState != playerState)
            {
                if (targetState.currentTeam.Value == playerState.currentTeam.Value) continue;

                int damage = CalculateDamage(skill.damageMultiplier, targetState);
                SendDamage(targetState, damage, hit.point);
                Debug.Log($"[{skill.skillName}] 단일 공격! {damage} 데미지!");
                return; // 최초의 유효한 대상 하나만 타격
            }
        }
    }

    private void PerformAreaAttack(SkillData skill, float range)
    {
        Vector3 center = transform.position + playerCamera.transform.forward * Mathf.Min(range, 5f);
        Collider[] hits = Physics.OverlapSphere(center, skill.areaRadius);

        int hitCount = 0;
        foreach (var col in hits)
        {
            var targetState = FindPlayerState(col.gameObject);
            if (targetState != null && targetState != playerState)
            {
                if (targetState.currentTeam.Value == playerState.currentTeam.Value) continue;

                int damage = CalculateDamage(skill.damageMultiplier, targetState);
                SendDamage(targetState, damage, col.ClosestPoint(center)); // 가장 가까운 표면 지점을 타격 지점으로 전달
                hitCount++;
            }
        }

        if (hitCount > 0)
            Debug.Log($"[{skill.skillName}] 범위 공격! {hitCount}명에게 적중!");
    }

    // =========================================================================
    // 데미지 공식
    // =========================================================================
    public int CalculateDamage(float skillMultiplier, PlayerState target)
    {
        float attack = statSystem != null ? statSystem.GetStat(StatType.Attack) : 100f;
        float critRate = statSystem != null ? statSystem.GetStat(StatType.CritRate) : 5f;
        float critDmg = statSystem != null ? statSystem.GetStat(StatType.CritDamage) : 150f;

        var targetStat = target.GetComponent<StatSystem>();
        float defense = targetStat != null ? targetStat.GetStat(StatType.Defense) : 100f;

        float baseDamage = attack * skillMultiplier;
        float afterDefense = baseDamage * (100f / (100f + defense));

        bool isCrit = Random.Range(0f, 100f) < critRate;
        if (isCrit)
        {
            afterDefense *= (critDmg / 100f);
            Debug.Log("★ 크리티컬 히트! ★");
        }

        return Mathf.Max(1, Mathf.RoundToInt(afterDefense));
    }

    // =========================================================================
    // 데미지 전송 (PlayerState의 ServerRpc를 통해)
    // =========================================================================
    // 서버 RPC 호출 위임 (타격 지점 포함)
    private void SendDamage(PlayerState target, int damage, Vector3 hitPosition)
    {
        if (playerState != null && target != null)
        {
            playerState.AttackTargetServerRpc(target.NetworkObject, damage, hitPosition);
        }
    }

    // =========================================================================
    // 외부에서 호출 가능한 데미지 적용 (FighterSkillExecutor 등에서 사용)
    // =========================================================================
    // 외부에서 호출 가능한 데미지 적용 (FighterSkillExecutor 등에서 사용)
    public void DealDamageToTarget(PlayerState target, float skillMultiplier, string skillName, Vector3 hitPosition = default)
    {
        if (target == null) return;

        int damage = CalculateDamage(skillMultiplier, target);
        // hitPosition이 기본값이면 대상의 몸통(Vector3.up) 지점 사용
        Vector3 finalHitPos = hitPosition == default ? target.transform.position + Vector3.up : hitPosition;
        SendDamage(target, damage, finalHitPos);
        Debug.Log($"[{skillName}] {damage} 데미지!");
    }

    // =========================================================================
    // 무투가 전용 스킬 라우터
    // =========================================================================
    private void ExecuteFighterSkill(int skillIndex)
    {
        switch (skillIndex)
        {
            case 0: fighterExecutor.Skill_JeongGwon(); break;
            case 1: fighterExecutor.Skill_SeungCheon(); break;
            case 2: fighterExecutor.Skill_BackStep(); break;
            case 3: fighterExecutor.Skill_PaSweGwon(); break;
            case 4: fighterExecutor.Skill_PungGak(); break;
            case 5: fighterExecutor.Skill_YeonPa(); break;
            case 6: fighterExecutor.Skill_BunGaeChum(); break;
            case 7: fighterExecutor.Skill_YeonTan(); break;
            case 8: fighterExecutor.Skill_JinGongNanMu(); break;
        }
    }

    // =========================================================================
    // 유틸리티: Collider 소유자의 PlayerState 찾기
    // PlayerState가 루트가 아닌 자식 객체(RPG_Systems)에 있으므로
    // GetComponentInParent만으로는 찾을 수 없음 → 위+아래 모두 검색
    // =========================================================================
    public static PlayerState FindPlayerState(GameObject obj)
    {
        // 1) 자기 자신이나 부모에서 찾기
        var state = obj.GetComponentInParent<PlayerState>();
        if (state != null) return state;

        // 2) 루트를 거쳐 자식에서 찾기
        var root = obj.transform.root;
        state = root.GetComponentInChildren<PlayerState>();
        return state;
    }
}
