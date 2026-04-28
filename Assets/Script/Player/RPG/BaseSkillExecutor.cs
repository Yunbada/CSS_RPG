using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 각 직업별 스킬 실행기의 공통 로직을 담는 추상 클래스.
/// 중복 코드(물리 판정, 이펙트 생성, 무적 처리 등)를 제거하여 SOLID(DRY) 원칙을 준수합니다.
/// </summary>
public abstract class BaseSkillExecutor : MonoBehaviour, ISkillExecutor
{
    protected CombatSystem combatSystem;
    protected PlayerState playerState;
    protected PlayerHealth playerHealth;
    protected PlayerVFXController playerVfx;
    protected CharacterController charCtrl;
    protected Camera playerCamera;
    protected StatSystem statSystem;

    public virtual void Initialize(CombatSystem combat, PlayerState state, PlayerHealth health)
    {
        combatSystem = combat;
        playerState = state;
        playerHealth = health;
        
        playerVfx = GetComponent<PlayerVFXController>();
        charCtrl = GetComponentInParent<CharacterController>();
        statSystem = GetComponentInParent<StatSystem>();

        var movement = GetComponentInParent<PlayerMovement>();
        if (movement != null)
        {
            playerCamera = movement.GetComponentInChildren<Camera>(true);
        }

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public abstract void ExecuteSkill(int skillIndex, SkillData skill);

    protected void SetInvincible(bool value)
    {
        if (combatSystem != null)
        {
            combatSystem.ChangeState(value ? CombatState.SkillExecuting : CombatState.Idle);
        }

        if (playerHealth != null)
        {
            playerHealth.SetInvincibleServerRpc(value);
        }
    }

    protected void SpawnVFX(int vfxType, Vector3 position, Quaternion rotation)
    {
        if (playerVfx != null)
        {
            playerVfx.SpawnSkillVFXServerRpc(vfxType, position, rotation);
        }
    }

    protected List<IDamageable> RaycastAttack(float reqRange, float multiplier, string skillName, int maxHits = -1)
    {
        var damagedTargets = new List<IDamageable>();
        if (playerCamera == null) return damagedTargets;

        float range = reqRange + 1f; // 약간의 여유 보정
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        
        var hits = Physics.SphereCastAll(ray, 1.5f, range);
        foreach (var hit in hits)
        {
            var target = CombatSystem.FindDamageable(hit.collider.gameObject);
            if (target != null && (Object)target != (Object)playerHealth && playerState.IsEnemy(target.CurrentTeam))
            {
                if (!damagedTargets.Contains(target))
                {
                    if (combatSystem != null)
                    {
                        combatSystem.DealDamageToTarget(target, multiplier, skillName, hit.point);
                    }
                    damagedTargets.Add(target);
                    if (maxHits > 0 && damagedTargets.Count >= maxHits) break;
                }
            }
        }
        return damagedTargets;
    }

    protected void AreaAttack(Vector3 center, float reqRadius, float multiplier, string skillName)
    {
        float radius = reqRadius + 1f; 

        Collider[] hits = Physics.OverlapSphere(center, radius);
        foreach (var col in hits)
        {
            var target = CombatSystem.FindDamageable(col.gameObject);
            if (target != null && (Object)target != (Object)playerHealth && playerState.IsEnemy(target.CurrentTeam))
            {
                if (combatSystem != null)
                {
                    combatSystem.DealDamageToTarget(target, multiplier, skillName, col.ClosestPoint(center));
                }
            }
        }
    }

    protected List<IDamageable> GetEnemiesInSphere(Vector3 center, float radius)
    {
        List<IDamageable> result = new List<IDamageable>();
        Collider[] hits = Physics.OverlapSphere(center, radius);
        foreach (var col in hits)
        {
            var target = CombatSystem.FindDamageable(col.gameObject);
            if (target != null && (Object)target != (Object)playerHealth && playerState.IsEnemy(target.CurrentTeam))
            {
                if (!result.Contains(target)) result.Add(target);
            }
        }
        return result;
    }
}
