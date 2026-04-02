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

    // 전직별 스킬 실행기 (인터페이스를 통한 추상화 OCP/DIP 적용)
    private ISkillExecutor currentSkillExecutor;

    private bool isInitialized = false;

    // 총 데미지 카운터 (내가 때린 누적 데미지)
    public int TotalDamageDealt { get; private set; } = 0;

    private void Awake()
    {
        inputHandle = GetComponentInParent<InputHandle>();
        playerClass = GetComponent<PlayerClass>();
        playerState = GetComponentInParent<PlayerState>();
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
            currentSkillExecutor.Initialize(this, playerState);
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

        // 전직 메뉴 열려있으면 공격 차단
        var classCtrl = FindFirstObjectByType<ClassSelectionController>();
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
        StartCoroutine(BasicAttackCoroutine());
    }

    private System.Collections.IEnumerator BasicAttackCoroutine()
    {
        ChangeState(CombatState.BasicAttacking);

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        
        // SphereCastAll을 사용하여 타격 범위를 후하게 판정 (반경 1.5m 캡슐 형태 전방 투사)
        var hits = Physics.SphereCastAll(ray, 1.5f, basicAttackRange);
        var damaged = new System.Collections.Generic.HashSet<IDamageable>();
        
        foreach (var hit in hits)
        {
            var targetState = FindDamageable(hit.collider.gameObject);
            if (targetState != null && (Object)targetState != (Object)playerState)
            {
                if (!playerState.IsEnemy(targetState.CurrentTeam)) continue;

                if (damaged.Add(targetState)) // 중복 타격 방지
                {
                    int damage = CalculateDamage(basicAttackMultiplier, targetState);
                    SendDamage(targetState, damage, hit.point);
                    Debug.Log($"기본 공격! 다중 {damage} 데미지 적중!");
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
            if (targetState != null && (Object)targetState != (Object)playerState)
            {
                if (targetState.CurrentTeam == playerState.currentTeam.Value) continue;

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
            var targetState = FindDamageable(col.gameObject);
            if (targetState != null && (Object)targetState != (Object)playerState)
            {
                if (targetState.CurrentTeam == playerState.currentTeam.Value) continue;

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
    private void SendDamage(IDamageable target, int damage, Vector3 hitPosition)
    {
        if (playerState != null && target != null)
        {
            playerState.AttackTargetServerRpc(target.GetNetworkObject(), damage, hitPosition);
            TotalDamageDealt += damage; // 누적 데미지 기록
            
            // 성기사 방패 에너지 후킹 (리플렉션 또는 빠른 캐스팅)
            if (currentSkillExecutor is PaladinSkillExecutor paladin)
            {
                paladin.AddShieldEnergy(5);
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
        SendDamage(target, damage, finalHitPos);
        Debug.Log($"[{skillName}] {damage} 데미지!");
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
