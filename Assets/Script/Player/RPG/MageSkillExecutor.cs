using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 마법사(Mage) 클래스의 스킬 실행기
/// ISkillExecutor를 상속받아 OCP와 다형성을 유지합니다.
/// </summary>
public class MageSkillExecutor : MonoBehaviour, ISkillExecutor
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
        if (skillIndex == 20) StartCoroutine(FireballCoroutine(skill));    // 파이어볼
        else if (skillIndex == 21) StartCoroutine(BlizzardCoroutine(skill)); // 블리자드
        else
        {
            Debug.LogWarning($"[MageSkillExecutor] 매칭되는 스킬 로직이 없습니다! Index: {skillIndex}");
        }
    }

    // =========================================================================
    // 스킬 20: 파이어볼 (원거리 폭발 - 에임 위치로)
    // =========================================================================
    private IEnumerator FireballCoroutine(SkillData skill)
    {
        if (combatSystem != null) combatSystem.ChangeState(CombatState.SkillCasting);
        
        // 캐스팅 시간 0.5초
        yield return new WaitForSeconds(0.5f);

        if (combatSystem != null) combatSystem.ChangeState(CombatState.SkillExecuting);

        // 폭발 중심점 탐색 (카메라 에임 10m 앞 기준)
        Vector3 impactPoint = playerCamera.transform.position + playerCamera.transform.forward * 10f;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 15f))
        {
            impactPoint = hit.point;
        }

        // 폭발 반경 4m 데미지
        AreaAttack(impactPoint, 4f, skill.damageMultiplier, skill.skillName);

        // 후딜레이 0.3초
        yield return new WaitForSeconds(0.3f);
        if (combatSystem != null && combatSystem.CurrentState == CombatState.SkillExecuting)
            combatSystem.ChangeState(CombatState.Idle);
    }

    // =========================================================================
    // 스킬 21: 블리자드 (다단 장판기)
    // =========================================================================
    private IEnumerator BlizzardCoroutine(SkillData skill)
    {
        if (combatSystem != null) combatSystem.ChangeState(CombatState.SkillCasting);

        // 캐스팅 시간 1초 (위험 노출)
        yield return new WaitForSeconds(1.0f);
        if (combatSystem != null && combatSystem.CurrentState != CombatState.SkillCasting) yield break; // 중간에 스턴 당했으면 파기

        if (combatSystem != null) combatSystem.ChangeState(CombatState.SkillExecuting);

        Transform rootTransform = charCtrl != null ? charCtrl.transform : playerState.transform;
        // 시전자 주변 6m 구역 다단 히트 (2초간 4번 틱 데미지)
        for (int i = 0; i < 4; i++)
        {
            // 광역 데미지 오라 판정 처리
            AreaAttack(rootTransform.position, 6f, skill.damageMultiplier * 0.25f, skill.skillName + " (Tick)");
            yield return new WaitForSeconds(0.5f);
        }

        if (combatSystem != null && combatSystem.CurrentState == CombatState.SkillExecuting)
            combatSystem.ChangeState(CombatState.Idle);
    }

    // =========================================================================
    // 공용 공격 유틸리티 
    // =========================================================================
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
