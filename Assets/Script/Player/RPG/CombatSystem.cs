using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 전투 시스템 (MonoBehaviour)
/// NetworkBehaviour가 아니므로 런타임 AddComponent 후에도 정상 작동합니다.
/// ServerRpc는 PlayerHealth.AttackTargetServerRpc()를 통해 위임합니다.
/// </summary>
public class CombatSystem : MonoBehaviour
{
    [Header("기본 공격 설정")]
    [SerializeField] private float basicAttackRange = 1.5f;
    [SerializeField] private float basicAttackCooldown = 0.5f;
    [SerializeField] private float basicAttackMultiplier = 1.0f;
    [SerializeField] private float basicAttackRadius = 0.5f;
    [SerializeField] private float basicAttackAngle = 120f;  // 전방 판정 각도 (도)

    private float basicAttackTimer = 0f;

    private InputHandle inputHandle;
    private PlayerClass playerClass;
    private PlayerState playerState;
    private PlayerHealth playerHealth;
    private StatSystem statSystem;
    private SkillSystem skillSystem;
    private Camera playerCamera;

    // 전직별 스킬 실행기 (인터페이스를 통한 추상화 OCP/DIP 적용)
    private ISkillExecutor currentSkillExecutor;

    private bool isInitialized = false;

    // 총 데미지 카운터 (내가 때린 누적 데미지)
    public int TotalDamageDealt { get; private set; } = 0;

    /// <summary>DoT 데미지를 누적 데미지에 합산 (서버에서 호출)</summary>
    public void AddDotDamage(int damage)
    {
        TotalDamageDealt += damage;
    }

    /// <summary>라운드 종료 시 데미지 카운터 초기화</summary>
    public void ResetDamageCounter()
    {
        TotalDamageDealt = 0;
    }

    private void Awake()
    {
        inputHandle = GetComponentInParent<InputHandle>();
        playerClass = GetComponent<PlayerClass>();
        playerState = GetComponentInParent<PlayerState>();
        playerHealth = GetComponentInParent<PlayerHealth>();
        statSystem = GetComponent<StatSystem>();

        // 핵심 버그 수정: RPG_System이 루트에서 이탈하는 것을 원천 차단 (로컬 좌표 0 락)
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private void OnEnable()
    {
        if (playerClass != null) 
            playerClass.currentClass.OnValueChanged += OnClassChanged;
    }

    private void OnDisable()
    {
        if (playerClass != null) 
            playerClass.currentClass.OnValueChanged -= OnClassChanged;
    }

    private void OnClassChanged(PlayerClassType oldClass, PlayerClassType newClass)
    {
        // 로컬 플레이어 등 소유권이 있는 클라이언트만 이펙터 교체
        if (playerClass.IsOwner)
        {
            UpdateSkillExecutor();
        }
    }

    public void InitializeCombatSystem()
    {
        skillSystem = GetComponent<SkillSystem>();

        var cam = GetComponentInParent<PlayerMovement>()?.GetComponentInChildren<Camera>(true);
        if (cam != null) playerCamera = cam;

        UpdateSkillExecutor();

        isInitialized = true;
        Debug.Log("[CombatSystem] 초기화 완료!");
    }

    public void UpdateSkillExecutor()
    {
        // 기존 실행기 제거 (새로운 직업군으로 전직/접속 시)
        var oldExecutors = GetComponents<ISkillExecutor>();
        foreach (var exec in oldExecutors) 
        {
            Destroy((MonoBehaviour)exec);
        }

        PlayerClassType classType = playerClass.currentClass.Value;
        switch (classType)
        {
            case PlayerClassType.Fighter: currentSkillExecutor = gameObject.AddComponent<FighterSkillExecutor>(); break;
            case PlayerClassType.Swordsman: currentSkillExecutor = gameObject.AddComponent<SwordsmanSkillExecutor>(); break;
            case PlayerClassType.Mage: currentSkillExecutor = gameObject.AddComponent<MageSkillExecutor>(); break;
            case PlayerClassType.Paladin: currentSkillExecutor = gameObject.AddComponent<PaladinSkillExecutor>(); break;
            // 미구현 직업들은 임시로 무투가 혹은 파이터가 작동하도록 처리
            case PlayerClassType.Gunner: currentSkillExecutor = gameObject.AddComponent<FighterSkillExecutor>(); break;
            case PlayerClassType.None: default: break; 
        }

        if (currentSkillExecutor != null)
        {
            currentSkillExecutor.Initialize(this, playerState, playerHealth);
            Debug.Log($"[CombatSystem] 스킬 실행기 {currentSkillExecutor.GetType().Name} 할당 완료.");
        }
    }

    // 전투 상태 머신 (FSM)
    public CombatState CurrentState { get; private set; } = CombatState.Idle;

    public void ChangeState(CombatState newState)
    {
        if (CurrentState == CombatState.Dead) return;
        CurrentState = newState;
        Debug.Log($"[CombatSystem] 상태 변경: {newState}");
    }

    // 스킬 사용 중 여부 (FSM 기반 하위 호환성)
    public bool IsUsingSkill => CurrentState == CombatState.SkillExecuting || CurrentState == CombatState.SkillCasting;

    private void Update()
    {
        if (!isInitialized) return;

        // 기본 공격 쿨타임 감소
        if (basicAttackTimer > 0f)
            basicAttackTimer -= Time.deltaTime;

        // TODO: 전직 UI가 열려있을 때 공격 차단 로직은 새 UI 시스템 구현 시 재연결

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
        StartCoroutine(BasicAttackCoroutine());
    }

    private System.Collections.IEnumerator BasicAttackCoroutine()
    {
        ChangeState(CombatState.BasicAttacking);

        // 플레이어 루트 트랜스폼 기준 전방 판정
        Transform root = transform.root;
        Vector3 origin = root.position + Vector3.up * 1.0f; // 허리 높이
        Vector3 forward = root.forward;
        
        // OverlapSphere로 반경 내 모든 콜라이더 탐지
        var colliders = Physics.OverlapSphere(origin, basicAttackRange);
        var damaged = new System.Collections.Generic.HashSet<IDamageable>();
        
        foreach (var col in colliders)
        {
            // 전방 각도 체크: 플레이어 전방 방향과 적 방향의 각도가 허용 범위 내인지
            Vector3 dirToTarget = (col.transform.position - origin).normalized;
            float angle = Vector3.Angle(forward, dirToTarget);
            if (angle > basicAttackAngle * 0.5f) continue; // 전방 범위 밖이면 무시
            
            var targetState = FindDamageable(col.gameObject);
            if (targetState != null && (Object)targetState != (Object)playerHealth)
            {
                if (!playerState.IsEnemy(targetState.CurrentTeam)) continue;

                if (damaged.Add(targetState)) // 중복 타격 방지
                {
                    int damage = CalculateDamage(basicAttackMultiplier, targetState);
                    SendDamage(targetState, damage, "기본 공격", col.ClosestPoint(origin));
                }
            }
        }

        // 추후 애니메이션 이벤트(Animation Event)로 대체될 하드코딩 딜레이
        yield return new WaitForSeconds(0.25f);
        
        // 만약 피격(Stun) 등으로 상태가 변형되지 않았다면 Idle로 복구
        if (CurrentState == CombatState.BasicAttacking)
        {
            ChangeState(CombatState.Idle);
        }
    }

    // =========================================================================
    // 기본 공격 범위 시각화 (에디터 전용 기즈모)
    // =========================================================================
    private void OnDrawGizmosSelected()
    {
        Transform root = transform.root;
        Vector3 origin = root.position + Vector3.up * 1.0f;
        Vector3 forward = root.forward;

        // 공격 범위 원 (반투명 빨강)
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Gizmos.DrawSphere(origin, basicAttackRange);

        // 전방 판정 원뿔 경계선 (노란색)
        Gizmos.color = Color.yellow;
        float halfAngle = basicAttackAngle * 0.5f;
        
        // 원뿔 좌측/우측 경계 벡터
        Vector3 leftBoundary = Quaternion.Euler(0, -halfAngle, 0) * forward * basicAttackRange;
        Vector3 rightBoundary = Quaternion.Euler(0, halfAngle, 0) * forward * basicAttackRange;
        
        Gizmos.DrawLine(origin, origin + leftBoundary);
        Gizmos.DrawLine(origin, origin + rightBoundary);
        
        // 전방 방향 중심선 (녹색)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + forward * basicAttackRange);
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
        // 좀비가 아닌 인간일 때 인터페이스를 통한 스킬 실행
        if (playerState != null && playerState.currentTeam.Value == Team.Human && currentSkillExecutor != null)
        {
            currentSkillExecutor.ExecuteSkill(skillIndex, skill);
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
            var targetState = FindDamageable(hit.collider.gameObject);
            if (targetState != null && (Object)targetState != (Object)playerHealth)
            {
                if (targetState.CurrentTeam == playerState.currentTeam.Value) continue;

                int damage = CalculateDamage(skill.damageMultiplier, targetState);
                SendDamage(targetState, damage, skill.skillName, hit.point);
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
            var targetState = FindDamageable(col.gameObject);
            if (targetState != null && (Object)targetState != (Object)playerHealth)
            {
                if (targetState.CurrentTeam == playerState.currentTeam.Value) continue;

                int damage = CalculateDamage(skill.damageMultiplier, targetState);
                SendDamage(targetState, damage, skill.skillName, col.ClosestPoint(center)); // 가장 가까운 표면 지점을 타격 지점으로 전달
                hitCount++;
            }
        }

        if (hitCount > 0)
            Debug.Log($"[{skill.skillName}] 범위 공격! {hitCount}명에게 적중!");
    }

    // =========================================================================
    // 데미지 공식
    // =========================================================================
    public int CalculateDamage(float skillMultiplier, IDamageable target)
    {
        float attack = statSystem != null ? statSystem.GetStat(StatType.Attack) : 100f;
        float critRate = statSystem != null ? statSystem.GetStat(StatType.CritRate) : 5f;
        float critDmg = statSystem != null ? statSystem.GetStat(StatType.CritDamage) : 150f;

        var targetStat = target.EntityTransform.GetComponent<StatSystem>();
        float defense = targetStat != null ? targetStat.GetStat(StatType.Defense) : 100f;

        float baseDamage = attack * skillMultiplier;
        float afterDefense = baseDamage * (100f / (100f + defense));

        bool isCrit = Random.Range(0f, 100f) < critRate;
        if (isCrit)
        {
            afterDefense *= (critDmg / 100f);
            Debug.Log("★ 크리티컬 히트! ★");
        }

        // 방어구 관통력 (추가 데미지 계수)
        float armorPen = statSystem != null ? statSystem.GetStat(StatType.ArmorPenetration) : 0f;
        float bonusDamage = afterDefense * (armorPen / 100f); // 수치의 % 비율만큼 타격 시 고정 추가 데미지
        afterDefense += bonusDamage;

        return Mathf.Max(1, Mathf.RoundToInt(afterDefense));
    }

    // =========================================================================
    // 데미지 전송 (PlayerState의 ServerRpc를 통해)
    // =========================================================================
    // 서버 RPC 호출 위임 (타격 지점 포함)
    private void SendDamage(IDamageable target, int damage, string skillName, Vector3 hitPosition)
    {
        if (playerHealth != null && target != null)
        {
            playerHealth.AttackTargetServerRpc(target.GetNetworkObject(), damage, skillName, hitPosition);
            TotalDamageDealt += damage; // 누적 데미지 기록
            
            // OCP: 직업 고유 게이지 충전 등은 각 실행기의 OnDamageDealt에서 처리
            if (currentSkillExecutor != null)
            {
                currentSkillExecutor.OnDamageDealt(damage);
            }
        }
    }

    // =========================================================================
    // 외부에서 호출 가능한 데미지 적용 (FighterSkillExecutor 등에서 사용)
    // =========================================================================
    public void DealDamageToTarget(IDamageable target, float skillMultiplier, string skillName, Vector3 hitPosition = default)
    {
        if (target == null) return;

        int damage = CalculateDamage(skillMultiplier, target);
        // hitPosition이 기본값이면 대상의 몸통(Vector3.up) 지점 사용
        Vector3 finalHitPos = hitPosition == default ? target.EntityTransform.position + Vector3.up : hitPosition;
        SendDamage(target, damage, skillName, finalHitPos);
    }

    public void ResetTotalDamage()
    {
        TotalDamageDealt = 0;
    }

    // =========================================================================
    // 디버그 범위 시각화 헬퍼 (임시 구체 생성 후 지우기)
    // =========================================================================
    public static void DrawDebugSphere(Vector3 center, float radius, float duration = 1.0f, Color? color = null)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = center;
        sphere.transform.localScale = Vector3.one * radius * 2f;

        // Collider 제거 (타격 판정에 영향 X)
        var col = sphere.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // 반투명 색상 설정
        var renderer = sphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color c = color ?? new Color(1f, 0.3f, 0f, 0.25f);
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = c;
            renderer.material = mat;
        }

        Destroy(sphere, duration);
    }

    public static void DrawDebugLine(Vector3 start, Vector3 end, float duration = 1.0f)
    {
        Debug.DrawLine(start, end, Color.red, duration);
    }



    // =========================================================================
    // 유틸리티: Collider 소유자의 IDamageable 찾기
    // =========================================================================
    public static IDamageable FindDamageable(GameObject obj)
    {
        var damageable = obj.GetComponentInParent<IDamageable>();
        if (damageable != null) return damageable;

        var root = obj.transform.root;
        damageable = root.GetComponentInChildren<IDamageable>();
        return damageable;
    }
}
