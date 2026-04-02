using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// 무투가 전용 스킬 실행기
/// CombatSystem.ExecuteSkillAttack()에서 무투가 전용 스킬일 때 이 클래스로 위임합니다.
/// 각 스킬은 코루틴으로 이동/타격을 시간에 걸쳐 수행합니다.
/// </summary>
public class FighterSkillExecutor : MonoBehaviour, ISkillExecutor
{
    private CombatSystem combatSystem;
    private PlayerState playerState;
    private CharacterController charCtrl;
    private Camera playerCamera;

    public void Initialize(CombatSystem combat, PlayerState state)
    {
        combatSystem = combat;
        playerState = state;
        charCtrl = GetComponentInParent<CharacterController>();
        
        var movement = GetComponentInParent<PlayerMovement>();
        if (movement != null)
        {
            playerCamera = movement.GetComponentInChildren<Camera>(true);
        }

        // 혹시라도 자식 객체가 루트에서 분리된 채 저장/동기화됐을 경우를 대비하여 원점 복귀
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    // =========================================================================
    // 스킬 실행 및 FSM 상태 연동
    // =========================================================================

    public void ExecuteSkill(int skillIndex, SkillData skill)
    {
        switch (skillIndex)
        {
            case 0: Skill_JeongGwon(); break;
            case 1: Skill_SeungCheon(); break;
            case 2: Skill_BackStep(); break;
            case 3: Skill_PaSweGwon(); break;
            case 4: Skill_PungGak(); break;
            case 5: Skill_YeonPa(); break;
            case 6: Skill_BunGaeChum(); break;
            case 7: Skill_YeonTan(); break;
            case 8: Skill_JinGongNanMu(); break;
        }
    }

    private void SetInvincible(bool value)
    {
        if (combatSystem != null)
        {
            combatSystem.ChangeState(value ? CombatState.SkillExecuting : CombatState.Idle);
        }

        if (playerState != null)
            playerState.SetInvincibleServerRpc(value);
    }

    private void SpawnVFX(int vfxType, Vector3 position, Quaternion rotation)
    {
        if (playerState != null)
        {
            playerState.SpawnSkillVFXServerRpc(vfxType, position, rotation);
        }
    }

    // =========================================================================
    // 1번: 정권 찌르기 — 전방 2m 단일 타격
    // =========================================================================
    public void Skill_JeongGwon()
    {
        StartCoroutine(JeongGwonCoroutine());
    }

    private IEnumerator JeongGwonCoroutine()
    {
        SetInvincible(true);
        Transform rootTransform = charCtrl.transform;
        // 전방 1.5m 지점에 충격파 이펙트 (Index 0)
        SpawnVFX(0, rootTransform.position + rootTransform.forward * 1.5f + Vector3.up * 1.2f, rootTransform.rotation);
        RaycastAttack(2f, 1.2f, "정권 찌르기");
        yield return new WaitForSeconds(0.3f); 
        SetInvincible(false);
    }

    // =========================================================================
    // 2번: 승천권 — 전방 2m 타격 + 적과 함께 2m 상승
    // =========================================================================
    public void Skill_SeungCheon()
    {
        StartCoroutine(SeungCheonCoroutine());
    }

    private IEnumerator SeungCheonCoroutine()
    {
        SetInvincible(true);
        Transform rootTransform = charCtrl.transform;
        // 전방 1.5m 지점에 상승 에너지 이펙트 (Index 1)
        SpawnVFX(1, rootTransform.position + rootTransform.forward * 1.5f + Vector3.up * 0.5f, Quaternion.identity);
        // 전방 2m 다수 타격
        var targets = RaycastAttack(2f, 2.0f, "승천권");

        // 자신이 위로 뜀
        float elapsed = 0f;
        float duration = 0.4f;
        float liftHeight = 2f;
        float liftSpeed = liftHeight / duration;

        // 적중한 다수 모든 대상도 함께 위로 띄움
        foreach (var targetItem in targets)
        {
            if (targetItem is PlayerState pTargetLift)
            {
                pTargetLift.KnockUpServerRpc(Vector3.up * liftSpeed, duration);
            }
        }

        while (elapsed < duration)
        {
            if (charCtrl != null)
                charCtrl.Move(Vector3.up * liftSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetInvincible(false);
    }

    // =========================================================================
    // 3번: 백스텝 — 후방 2m 회피
    // =========================================================================
    public void Skill_BackStep()
    {
        StartCoroutine(BackStepCoroutine());
    }

    private IEnumerator BackStepCoroutine()
    {
        SetInvincible(true);
        var movement = GetComponentInParent<PlayerMovement>();
        if (movement != null) movement.ResetGravity();

        float elapsed = 0f;
        float duration = 0.2f;
        float distance = 2f;
        float speed = distance / duration;
        Transform rootTransform = charCtrl.transform;
        Vector3 backDir = -rootTransform.forward;

        while (elapsed < duration)
        {
            if (charCtrl != null)
                charCtrl.Move(backDir * speed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Debug.Log("백스텝 완료!");
        SetInvincible(false);
    }

    // =========================================================================
    // 4번: 파쇄권 — 전방 2m 대시 후 도착 지점 주변 1m AoE 타격
    // =========================================================================
    public void Skill_PaSweGwon()
    {
        StartCoroutine(PaSweGwonCoroutine());
    }

    private IEnumerator PaSweGwonCoroutine()
    {
        SetInvincible(true);
        var movement = GetComponentInParent<PlayerMovement>();
        if (movement != null) movement.ResetGravity();

        // 전방 2m 대시
        float elapsed = 0f;
        float duration = 0.15f;
        float distance = 2f;
        float speed = distance / duration;
        Transform rootTransform = charCtrl.transform;
        Vector3 fwd = rootTransform.forward;

        while (elapsed < duration)
        {
            if (charCtrl != null)
                charCtrl.Move(fwd * speed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 도착 지점 주변 1m AoE + 지면 파열 이펙트 (Index 3)
        SpawnVFX(3, rootTransform.position + Vector3.up * 0.1f, Quaternion.identity);
        AreaAttack(rootTransform.position, 1f, 2.5f, "파쇄권");
        SetInvincible(false);
    }

    // =========================================================================
    // 5번: 풍각 — 회전 킥, 주변 1m 타격 + 전방 3m 1.5초간 전진
    // =========================================================================
    public void Skill_PungGak()
    {
        StartCoroutine(PungGakCoroutine());
    }

    private IEnumerator PungGakCoroutine()
    {
        SetInvincible(true);
        float elapsed = 0f;
        float duration = 1.5f;
        float distance = 3f;
        float speed = distance / duration;
        float tickInterval = 0.3f;
        float tickTimer = 0f;
        Transform rootTransform = charCtrl.transform;

        while (elapsed < duration)
        {
            if (charCtrl != null)
                charCtrl.Move(rootTransform.forward * speed * Time.deltaTime);

            tickTimer += Time.deltaTime;
            if (tickTimer >= tickInterval)
            {
                // 회전할 때마다 태풍의 눈 이펙트 발생 (Index 2)
                SpawnVFX(2, rootTransform.position + Vector3.up * 0.2f, Quaternion.identity);
                AreaAttack(rootTransform.position, 1f, 1.5f, "풍각");
                tickTimer = 0f;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        SetInvincible(false);
    }

    // =========================================================================
    // 6번: 연파 — 전방 2m 적을 1.5초간 빠르게 연속 타격
    // =========================================================================
    public void Skill_YeonPa()
    {
        StartCoroutine(YeonPaCoroutine());
    }

    private IEnumerator YeonPaCoroutine()
    {
        SetInvincible(true);
        float elapsed = 0f;
        float duration = 1.5f;
        float tickInterval = 0.15f;
        float tickTimer = 0f;
        int hitCount = 0;
        Transform rootTransform = charCtrl.transform;

        while (elapsed < duration)
        {
            tickTimer += Time.deltaTime;
            if (tickTimer >= tickInterval)
            {
                // 공격 시마다 고속 잔상 이펙트 발생 (Index 6)
                SpawnVFX(6, rootTransform.position + rootTransform.forward * 1.0f + Vector3.up * 1.0f, rootTransform.rotation);
                RaycastAttack(2f, 0.5f, "연파");
                hitCount++;
                tickTimer = 0f;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        Debug.Log($"연파 완료! 총 {hitCount}회 타격");
        SetInvincible(false);
    }

    // =========================================================================
    // 7번: 번개춤 — 주변 3m 적들에게 2초간 텔레포트 타격
    // =========================================================================
    public void Skill_BunGaeChum()
    {
        StartCoroutine(BunGaeChumCoroutine());
    }

    private IEnumerator BunGaeChumCoroutine()
    {
        SetInvincible(true);
        Transform rootTransform = charCtrl.transform;
        
        var model = rootTransform.Find("PlayerModel");
        if (model != null) model.gameObject.SetActive(false); // 모델 숨기기

        float elapsed = 0f;
        float duration = 2f;
        float tickInterval = 0.25f;
        float tickTimer = 0f;

        while (elapsed < duration)
        {
            tickTimer += Time.deltaTime;
            if (tickTimer >= tickInterval)
            {
                // 주변 5m 범위 내의 적 찾기
                Collider[] hits = Physics.OverlapSphere(rootTransform.position, 5f);
                System.Collections.Generic.List<IDamageable> targets = new System.Collections.Generic.List<IDamageable>();
                foreach (var col in hits)
                {
                    var target = CombatSystem.FindDamageable(col.gameObject);
                    if (target != null && (Object)target != (Object)playerState && playerState.IsEnemy(target.CurrentTeam))
                    {
                        targets.Add(target);
                    }
                }

                if (targets.Count > 0)
                {
                    // 무작위 타겟 하나 선택하여 순간이동
                    IDamageable randomTarget = targets[Random.Range(0, targets.Count)];
                    
                    if (charCtrl != null) charCtrl.enabled = false;
                    
                    Vector3 newPos = randomTarget.EntityTransform.position;
                    // 적의 머리위나 발밑으로 순간이동하지 않도록 높이 보정 가능성 유지
                    newPos.x += Random.Range(-0.5f, 0.5f);
                    newPos.z += Random.Range(-0.5f, 0.5f);
                    rootTransform.position = newPos; // 자식(RPG_System)이 아닌 루트 플레이어를 이동

                    // Y축(상하) 회전을 방지하기 위해 바라보는 높이를 플레이어와 일치시킴
                    Vector3 lookPos = randomTarget.EntityTransform.position;
                    lookPos.y = rootTransform.position.y;
                    rootTransform.LookAt(lookPos); // 회전도 루트 플레이어를 회전
                    
                    if (charCtrl != null) charCtrl.enabled = true;

                    // 이동한 타겟 위치에서 전격 폭발 이펙트 발생 (Index 4)
                    SpawnVFX(4, rootTransform.position + Vector3.up * 1.0f, Quaternion.identity);
                    AreaAttack(rootTransform.position, 2f, 1.0f, "번개춤");
                }

                tickTimer = 0f;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (model != null) model.gameObject.SetActive(true); // 모델 보이기
        Debug.Log("번개춤 완료!");
        SetInvincible(false);
    }

    // =========================================================================
    // 8번: 연탄 — 장풍을 연속적으로 빠르게 발사 (전방 Raycast)
    // =========================================================================
    public void Skill_YeonTan()
    {
        StartCoroutine(YeonTanCoroutine());
    }

    private IEnumerator YeonTanCoroutine()
    {
        SetInvincible(true);
        float elapsed = 0f;
        float duration = 1.5f;
        float tickInterval = 0.12f;
        float tickTimer = 0f;
        int hitCount = 0;
        Transform rootTransform = charCtrl.transform;

        while (elapsed < duration)
        {
            tickTimer += Time.deltaTime;
            if (tickTimer >= tickInterval)
            {
                // 장풍 발사 시 에너지 구체 이펙트 발생 (Index 5)
                SpawnVFX(5, rootTransform.position + rootTransform.forward * 1.5f + Vector3.up * 1.2f, rootTransform.rotation);
                RaycastAttack(10f, 0.6f, "연탄"); // 장풍이므로 사거리 10m
                hitCount++;
                tickTimer = 0f;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        Debug.Log($"연탄 완료! 총 {hitCount}발 발사");
        SetInvincible(false);
    }

    // =========================================================================
    // 9번: 진공난무 — 주변 3~4m 적을 2초간 끌어당긴 후 강력한 일격
    // =========================================================================
    public void Skill_JinGongNanMu()
    {
        StartCoroutine(JinGongNanMuCoroutine());
    }

    private IEnumerator JinGongNanMuCoroutine()
    {
        SetInvincible(true);
        Transform rootTransform = charCtrl.transform;
        // 2초간 주변 5m 적들에게 틱 데미지 + 끌어당기기
        float elapsed = 0f;
        float pullDuration = 2f;
        float tickInterval = 0.3f;
        float tickTimer = 0f;

        while (elapsed < pullDuration)
        {
            tickTimer += Time.deltaTime;
            if (tickTimer >= tickInterval)
            {
                // 주변 적 검색 시 태풍의 눈(흡입) 효과 (Index 2)
                SpawnVFX(2, rootTransform.position + Vector3.up * 0.5f, Quaternion.identity);
                
                // 주변 5m 적 검색 및 끌어당기기
                Collider[] hits = Physics.OverlapSphere(rootTransform.position, 5f);
                foreach (var col in hits)
                {
                    var target = CombatSystem.FindDamageable(col.gameObject);
                    if (target != null && (Object)target != (Object)playerState && target.CurrentTeam != playerState.currentTeam.Value)
                    {
                        // 플레이어 방향으로 끌어당기는 벡터 계산
                        Vector3 pullDir = (rootTransform.position - target.EntityTransform.position).normalized;
                        // y축(위아래)은 유지
                        pullDir.y = 0; 
                        
                        // 넉업 함수를 재활용하여 강제 이동(끌어당기기) 적용
                        if (target is PlayerState pTargetP) pTargetP.KnockUpServerRpc(pullDir * 6f, tickInterval);
                        
                        // 틱 데미지
                        if (combatSystem != null)
                        {
                            // 당겨지는 대상의 몸통 부분을 타격 지점으로 전달
                            combatSystem.DealDamageToTarget(target, 0.3f, "진공난무(흡인)", col.ClosestPoint(rootTransform.position));
                        }
                    }
                }
                tickTimer = 0f;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 최종 강력한 타격
        AreaAttack(rootTransform.position, 4f, 4.0f, "진공난무(폭발)");
        Debug.Log("진공난무 최종 타격!");
        SetInvincible(false);
    }

    // =========================================================================
    // 공용 공격 유틸리티
    // =========================================================================
    private System.Collections.Generic.List<IDamageable> RaycastAttack(float reqRange, float multiplier, string skillName)
    {
        var damagedTargets = new System.Collections.Generic.List<IDamageable>();
        if (playerCamera == null) return damagedTargets;

        float range = reqRange + 1f; // 전체적으로 범위 1m 증가
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        
        // 시각적 디버그 표시 (1초 유지)
        StartCoroutine(DrawDebugLineCoroutine(ray.origin, ray.origin + ray.direction * range, Color.red, 1f));

        var hits = Physics.SphereCastAll(ray, 1.5f, range); // 범위 1.5m로 늘리고 다중타겟 활성화
        foreach (var hit in hits)
        {
            var target = CombatSystem.FindDamageable(hit.collider.gameObject);
            if (target != null && (Object)target != (Object)playerState && playerState.IsEnemy(target.CurrentTeam))
            {
                if (!damagedTargets.Contains(target) && combatSystem != null)
                {
                    combatSystem.DealDamageToTarget(target, multiplier, skillName, hit.point);
                    damagedTargets.Add(target); // 타겟 전체 공격
                }
            }
        }
        return damagedTargets;
    }

    private void AreaAttack(Vector3 center, float reqRadius, float multiplier, string skillName)
    {
        float radius = reqRadius + 1f; // 전체적으로 범위 1m 증가
        // 시각적 디버그 구형 표시 (1초 유지)
        StartCoroutine(DrawDebugSphereCoroutine(center, radius, Color.red, 1f));

        Collider[] hits = Physics.OverlapSphere(center, radius);
        foreach (var col in hits)
        {
            var target = CombatSystem.FindDamageable(col.gameObject);
            if (target != null && (Object)target != (Object)playerState && playerState.IsEnemy(target.CurrentTeam))
            {
                if (combatSystem != null)
                {
                    combatSystem.DealDamageToTarget(target, multiplier, skillName, col.ClosestPoint(center));
                }
            }
        }
    }

    // =========================================================================
    // 시각적 공격 범위 표시 헬퍼 (테스트용)
    // =========================================================================
    private IEnumerator DrawDebugLineCoroutine(Vector3 start, Vector3 end, Color color, float duration)
    {
        GameObject lineObj = new GameObject("DebugAttackLine");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.SetPositions(new Vector3[] { start, end });
        
        // URP 및 기본 빌트인 모두 작동하는 기본 스프라이트 셰이더
        var mat = new Material(Shader.Find("Sprites/Default"));
        lr.material = mat;
        lr.startColor = color;
        lr.endColor = color;
        
        yield return new WaitForSeconds(duration);
        Destroy(lineObj);
    }

    private IEnumerator DrawDebugSphereCoroutine(Vector3 center, float radius, Color color, float duration)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(sphere.GetComponent<Collider>()); // 물리 충돌 방지
        sphere.transform.position = center;
        sphere.transform.localScale = new Vector3(radius * 2, radius * 2, radius * 2);
        
        Renderer rend = sphere.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(color.r, color.g, color.b, 0.4f); // 반투명
        rend.material = mat;
        
        yield return new WaitForSeconds(duration);
        Destroy(sphere);
    }
}
