using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 검사(Swordsman) 클래스의 스킬 실행기
/// ISkillExecutor를 상속받아 OCP와 다형성을 유지합니다.
/// </summary>
public class SwordsmanSkillExecutor : MonoBehaviour, ISkillExecutor
{
    private CombatSystem combatSystem;
    private PlayerState playerState;
    private Camera playerCamera;

    private CharacterController charCtrl;

    public void Initialize(CombatSystem combat, PlayerState state)
    {
        combatSystem = combat;
        playerState = state;
        charCtrl = GetComponentInParent<CharacterController>();
        var pm = GetComponentInParent<PlayerMovement>();
        if (pm != null) playerCamera = pm.GetComponentInChildren<Camera>(true);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void ExecuteSkill(int skillIndex, SkillData skill)
    {
        if (skillIndex == 10) StartCoroutine(DrawSwordCoroutine(skill));      // 발도술
        else if (skillIndex == 11) StartCoroutine(SpinSlashCoroutine(skill)); // 회전 베기
        else
        {
            Debug.LogWarning($"[SwordsmanSkillExecutor] 매칭되는 스킬 로직이 없습니다! Index: {skillIndex}");
        }
    }

    // =========================================================================
    // 스킬 10: 발도술 (전방 빠른 찌르기)
    // =========================================================================
    private IEnumerator DrawSwordCoroutine(SkillData skill)
    {
        if (combatSystem != null) combatSystem.ChangeState(CombatState.SkillCasting);
        
        // 집중 (선딜레이 0.3초)
        yield return new WaitForSeconds(0.3f);

        if (combatSystem != null) combatSystem.ChangeState(CombatState.SkillExecuting);

        // 일직선 데미지 판정
        RaycastAttack(5f, skill.damageMultiplier, skill.skillName);

        // 후딜레이
        yield return new WaitForSeconds(0.2f);
        if (combatSystem != null && combatSystem.CurrentState == CombatState.SkillExecuting)
            combatSystem.ChangeState(CombatState.Idle);
    }

    // =========================================================================
    // 스킬 11: 회전 베기 (광역 공격)
    // =========================================================================
    private IEnumerator SpinSlashCoroutine(SkillData skill)
    {
        if (combatSystem != null) combatSystem.ChangeState(CombatState.SkillExecuting);
        
        // 주변 3.5m 범위 판정 및 이펙트 소환 (PlayerState.Rpc 사용 권장)
        Transform rootTransform = charCtrl != null ? charCtrl.transform : playerState.transform;
        AreaAttack(rootTransform.position, 3.5f, skill.damageMultiplier, skill.skillName);

        yield return new WaitForSeconds(0.4f); // 모션 길이
        
        if (combatSystem != null && combatSystem.CurrentState == CombatState.SkillExecuting)
            combatSystem.ChangeState(CombatState.Idle);
    }

    // =========================================================================
    // 공용 공격 유틸리티 (FighterSkillExecutor와 동일)
    // =========================================================================
    private void RaycastAttack(float reqRange, float multiplier, string skillName)
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        var hits = Physics.SphereCastAll(ray, 1.0f, reqRange);
        foreach (var hit in hits)
        {
            var target = CombatSystem.FindDamageable(hit.collider.gameObject);
            if (target != null && (Object)target != (Object)playerState && playerState.IsEnemy(target.CurrentTeam))
            {
                if (combatSystem != null)
                {
                    combatSystem.DealDamageToTarget(target, multiplier, skillName, hit.point);
                    return; // 관통 불가능 (1명만 타격)
                }
            }
        }
    }

    private void AreaAttack(Vector3 center, float reqRadius, float multiplier, string skillName)
    {
        Collider[] hits = Physics.OverlapSphere(center, reqRadius);
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
}
