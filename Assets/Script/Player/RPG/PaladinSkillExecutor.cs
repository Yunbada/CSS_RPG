using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaladinSkillExecutor : BaseSkillExecutor
{
    // 축복의 방패 에너지
    public int ShieldEnergy { get; private set; } = 0;
    private const int MAX_ENERGY = 100;

    public void AddShieldEnergy(int amount)
    {
        ShieldEnergy = Mathf.Clamp(ShieldEnergy + amount, 0, MAX_ENERGY);
    }

    /// <summary>라운드 종료 또는 좀비 전환 시 충전량 초기화</summary>
    public void ResetShieldEnergy()
    {
        ShieldEnergy = 0;
    }

    /// <summary>
    /// CombatSystem에서 데미지를 입힐 때마다 호출됩니다.
    /// 성기사 고유 메카닉: 공격 시 방패 에너지 5 충전.
    /// </summary>
    public override void OnDamageDealt(int damage)
    {
        AddShieldEnergy(5);
    }

    public override void ExecuteSkill(int skillIndex, SkillData skill)
    {
        switch (skillIndex)
        {
            case 0: StartCoroutine(Skill_ForwardSlash(skill)); break;
            case 1: StartCoroutine(Skill_BlessedShield(skill)); break;
            case 2: StartCoroutine(Skill_LightPunishment(skill)); break;
            case 3: StartCoroutine(Skill_HeavenlyWings(skill)); break;
            case 4: StartCoroutine(Skill_Judgment(skill)); break;
            case 5: StartCoroutine(Skill_HolySpear(skill)); break;
            case 6: StartCoroutine(Skill_LawOfLight(skill)); break;
            case 7: StartCoroutine(Skill_IndomitableWill(skill)); break;
            case 8: StartCoroutine(Skill_ApostleOfLight(skill)); break;
        }
    }

    // =========================================================================
    // 스킬 0: 전진베기 
    // 바라보는 방향 이동 후 바라보는 곳 타격해 주변 적 넉백 (1.5m 이동)
    // =========================================================================
    private IEnumerator Skill_ForwardSlash(SkillData skill)
    {
        SetInvincible(true);
        Transform root = charCtrl.transform;

        float distance = 1.5f;
        float duration = 0.2f;
        float speed = distance / duration;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            charCtrl.Move(root.forward * speed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SpawnVFX(6, root.position + root.forward * 1f + Vector3.up, root.rotation);
        
        // 전방 타격 및 넉백
        Vector3 hitCenter = root.position + root.forward * 1.5f;
        CombatSystem.DrawDebugSphere(hitCenter, 2f, 0.8f, new Color(1f, 0.5f, 0f, 0.2f));
        
        var targets = GetEnemiesInSphere(hitCenter, 2f);
        foreach (var target in targets)
        {
            combatSystem.DealDamageToTarget(target, skill.damageMultiplier, skill.skillName, target.EntityTransform.position);
            if (target is PlayerHealth pH)
            {
                Vector3 pushDir = (pH.EntityTransform.position - root.position).normalized;
                pushDir.y = 0;
                pH.KnockUpServerRpc(pushDir * 5f, 0.3f);
            }
        }

        SetInvincible(false);
    }

    // =========================================================================
    // 스킬 1: 축복의 방패
    // =========================================================================
    private IEnumerator Skill_BlessedShield(SkillData skill)
    {
        SetInvincible(true);
        Transform root = charCtrl.transform;
        int usedEnergy = ShieldEnergy;

        if (usedEnergy >= 100) usedEnergy = 100;
        else if (usedEnergy >= 80) usedEnergy = 80;
        else if (usedEnergy >= 60) usedEnergy = 60;
        else if (usedEnergy >= 40) usedEnergy = 40;
        else usedEnergy = 0;

        ShieldEnergy -= usedEnergy; // 소모

        if (usedEnergy == 0 || usedEnergy == 40 || usedEnergy == 60)
        {
            float dist = usedEnergy == 60 ? 2.5f : (usedEnergy == 40 ? 2.0f : 1.5f);
            float dur = 0.2f;

            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                charCtrl.Move(root.forward * (dist / dur) * Time.deltaTime);
                yield return null;
            }

            if (usedEnergy > 0)
            {
                SpawnVFX(3, root.position, Quaternion.identity); // 내려찍기 파열
                Vector3 center = root.position + root.forward;
                CombatSystem.DrawDebugSphere(center, 2.5f, 0.8f, new Color(0f, 0.5f, 1f, 0.2f));
                var targets = GetEnemiesInSphere(center, 2.5f);
                foreach (var t in targets)
                {
                    combatSystem.DealDamageToTarget(t, skill.damageMultiplier, "축복의 방패(강타)");
                    if (t is PlayerHealth pH)
                    {
                        pH.KnockUpServerRpc(Vector3.up * 5f, 0.5f); // 에어본
                    }
                }
            }
        }
        else // 80 or 100
        {
            SpawnVFX(5, root.position + Vector3.up, root.rotation); // 방패 투척 (임시 투사체)
            
            // 무한사거리 Raycast로 투척 충돌 판정
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            var hits = Physics.SphereCastAll(ray, 1.5f, 100f);
            
            var damagedTargets = new List<IDamageable>();
            foreach (var hit in hits)
            {
                var target = CombatSystem.FindDamageable(hit.collider.gameObject);
                if (target != null && (Object)target != (Object)playerHealth && playerState.IsEnemy(target.CurrentTeam))
                {
                    if (!damagedTargets.Contains(target))
                    {
                        combatSystem.DealDamageToTarget(target, skill.damageMultiplier, "축복의 방패(투척)");
                        // 100이면 장시간 스턴
                        float stunDur = usedEnergy == 100 ? 5.0f : 2.0f;
                        if (target is PlayerHealth pH)
                        {
                            pH.KnockUpServerRpc(root.forward * 5f, 0.2f); // 넉백
                            // 스턴
                            pH.KnockUpServerRpc(Vector3.zero, stunDur); 
                        }
                        damagedTargets.Add(target);
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.2f);
        SetInvincible(false);
    }

    // =========================================================================
    // 스킬 2: 빛의 징벌 (+ 화상 DoT)
    // =========================================================================
    private IEnumerator Skill_LightPunishment(SkillData skill)
    {
        SetInvincible(true);
        Transform root = charCtrl.transform;

        yield return new WaitForSeconds(0.3f);
        SpawnVFX(1, root.position, Quaternion.identity); // 빛기둥 (임시 상승 이펙트)
        
        CombatSystem.DrawDebugSphere(root.position, 4f, 1.0f, new Color(1f, 1f, 0f, 0.15f));
        var targets = GetEnemiesInSphere(root.position, 4f);
        foreach (var t in targets)
        {
            combatSystem.DealDamageToTarget(t, skill.damageMultiplier, skill.skillName);
            if (t is PlayerHealth pH)
            {
                pH.KnockUpServerRpc(Vector3.up * 6f, 0.5f); // 에어본
                float tickDmg = (statSystem != null ? statSystem.GetStat(StatType.Attack) : 100f) * 0.2f;
                pH.ApplyDoTWithAttackerServerRpc(0, 5, 1f, tickDmg, playerState.OwnerClientId); // 공격자 추적 DoT
            }
        }

        SetInvincible(false);
    }

    // =========================================================================
    // 스킬 3: 천상의 날개
    // =========================================================================
    private IEnumerator Skill_HeavenlyWings(SkillData skill)
    {
        SetInvincible(true);
        Transform root = charCtrl.transform;

        // 위로 4m 이동
        for (float t = 0; t < 0.3f; t += Time.deltaTime)
        {
            charCtrl.Move(Vector3.up * (4f / 0.3f) * Time.deltaTime);
            yield return null;
        }

        // 약 2초간 유저 자율 조작 허용 (PlayerMovement에서 강제 비활성화를 피해야 하지만 여기선 임시 오버라이드)
        float hoverTime = 2.0f;
        float elapsed = 0f;
        var inputHandle = GetComponentInParent<InputHandle>();
        
        while (elapsed < hoverTime && !inputHandle.attackInput) // 클릭 시 즉시 하강
        {
            Vector3 move = root.right * inputHandle.horizontalInput + root.forward * inputHandle.verticalInput;
            charCtrl.Move(move * 8f * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 내리찍기
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            charCtrl.Move(Vector3.down * (20f) * Time.deltaTime);
            yield return null;
        }

        SpawnVFX(3, root.position, Quaternion.identity); // 지면 파열
        CombatSystem.DrawDebugSphere(root.position, 5f, 1.0f, new Color(1f, 0f, 0f, 0.15f));
        var targets = GetEnemiesInSphere(root.position, 5f);
        foreach (var t in targets)
        {
            combatSystem.DealDamageToTarget(t, skill.damageMultiplier, skill.skillName);
            if (t is PlayerHealth pH)
            {
                pH.KnockUpServerRpc((pH.EntityTransform.position - root.position).normalized * 5f + Vector3.up * 3f, 0.5f);
            }
        }

        SetInvincible(false);
    }

    // =========================================================================
    // 스킬 4: 심판 (공중 콤보)
    // =========================================================================
    private IEnumerator Skill_Judgment(SkillData skill)
    {
        SetInvincible(true);
        Transform root = charCtrl.transform;

        SpawnVFX(2, root.position + Vector3.up * 0.1f, Quaternion.identity); // 태풍의 눈(광역 강타 임시)
        
        CombatSystem.DrawDebugSphere(root.position, 7f, 1.0f, new Color(0.8f, 0f, 1f, 0.15f));
        var targets = GetEnemiesInSphere(root.position, 7f);
        foreach (var t in targets)
        {
            combatSystem.DealDamageToTarget(t, skill.damageMultiplier, skill.skillName);
            // 공중에 뜬 적(대상이 지면에서 1m 이상 떨어져있는지 야매 체크)
            if (t is PlayerHealth pH && !Physics.Raycast(pH.EntityTransform.position, Vector3.down, 1.5f))
            {
                pH.KnockUpServerRpc(root.forward * 10f, 0.4f); // 강제 베어 날림
                combatSystem.DealDamageToTarget(t, skill.damageMultiplier * 1.5f, "심판(공중추가타)");
            }
        }
        
        yield return new WaitForSeconds(0.4f);
        SetInvincible(false);
    }

    // =========================================================================
    // 스킬 5: 빛의 성창 (슬로우)
    // =========================================================================
    private IEnumerator Skill_HolySpear(SkillData skill)
    {
        SetInvincible(true);
        Transform root = charCtrl.transform;
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        SpawnVFX(5, ray.origin + ray.direction * 2f, root.rotation);

        var damagedTargets = new List<IDamageable>();
        var hits = Physics.SphereCastAll(ray, 1.5f, 100f);
        foreach (var hit in hits)
        {
            var target = CombatSystem.FindDamageable(hit.collider.gameObject);
            if (target != null && (Object)target != (Object)playerHealth && playerState.IsEnemy(target.CurrentTeam))
            {
                if (!damagedTargets.Contains(target))
                {
                    combatSystem.DealDamageToTarget(target, skill.damageMultiplier, skill.skillName);
                    var pmInfo = target.EntityTransform.GetComponent<PlayerMovement>();
                    if (pmInfo != null)
                    {
                        pmInfo.ApplySlowClientRpc(0.5f, 3.0f); // 3초간 50% 이속 감소
                    }
                    damagedTargets.Add(target);
                }
            }
        }
        
        yield return new WaitForSeconds(0.3f);
        SetInvincible(false);
    }

    // =========================================================================
    // 스킬 6: 빛의 율법 (장판)
    // =========================================================================
    private IEnumerator Skill_LawOfLight(SkillData skill)
    {
        SetInvincible(true);
        Transform root = charCtrl.transform;
        
        Vector3 poolCenter = root.position + root.forward * 5f;
        SpawnVFX(2, poolCenter + Vector3.up * 0.1f, Quaternion.identity); // 장판 이펙트 
        CombatSystem.DrawDebugSphere(poolCenter, 10f, 5.5f, new Color(0f, 1f, 0.5f, 0.1f));
        
        // 5초간 지속 데미지 및 슬로우 
        // 편의상 1초에 1번씩 5번 판정하는 코루틴을 돌립니다.
        StartCoroutine(LawOfLightPoolCoroutine(poolCenter, 10f, 5, 1f, skill.damageMultiplier));
        
        yield return new WaitForSeconds(0.5f);
        SetInvincible(false);
    }

    private IEnumerator LawOfLightPoolCoroutine(Vector3 center, float radius, int ticks, float interval, float mult)
    {
        for (int i = 0; i < ticks; i++)
        {
            var targets = GetEnemiesInSphere(center, radius);
            foreach (var t in targets)
            {
                combatSystem.DealDamageToTarget(t, mult * 0.2f, "빛의 율법(장판)");
                var pmInfo = t.EntityTransform.GetComponent<PlayerMovement>();
                if (pmInfo != null) pmInfo.ApplySlowClientRpc(0.3f, 1.5f);
            }
            yield return new WaitForSeconds(interval);
        }
    }

    // =========================================================================
    // 스킬 7: 불굴의 의지 (버프)
    // =========================================================================
    private IEnumerator Skill_IndomitableWill(SkillData skill)
    {
        SetInvincible(true);
        SpawnVFX(1, charCtrl.transform.position, Quaternion.identity);

        if (statSystem != null)
        {
            Debug.Log("[불굴의 의지] 스탯 버프 부여!");
            
            var atkMod = new StatModifier(30f, false, this); // +30% 가산치
            var critMod = new StatModifier(20f, false, this); // +20% 가산치
            var defMod = new StatModifier(10f, false, this); // +10% 가산치
            var penMod = new StatModifier(12f, false, this); // +12% 가산치 (방관)

            statSystem.AddModifier(StatType.Attack, atkMod);
            statSystem.AddModifier(StatType.CritDamage, critMod);
            statSystem.AddModifier(StatType.Defense, defMod);
            statSystem.AddModifier(StatType.ArmorPenetration, penMod);
            
            // 15초 뒤에 버프 해제
            StartCoroutine(RemoveBuffAfterDelay(15f, this));
        }
        
        yield return new WaitForSeconds(0.5f);
        SetInvincible(false);
    }
    
    private IEnumerator RemoveBuffAfterDelay(float delay, object source)
    {
        yield return new WaitForSeconds(delay);
        if (statSystem != null)
        {
            statSystem.RemoveAllModifiersFromSource(StatType.Attack, source);
            statSystem.RemoveAllModifiersFromSource(StatType.CritDamage, source);
            statSystem.RemoveAllModifiersFromSource(StatType.Defense, source);
            statSystem.RemoveAllModifiersFromSource(StatType.ArmorPenetration, source);
            Debug.Log("[불굴의 의지] 스탯 버프가 해제되었습니다.");
        }
    }

    // =========================================================================
    // 스킬 8: 빛의 사도 (폭격)
    // =========================================================================
    private IEnumerator Skill_ApostleOfLight(SkillData skill)
    {
        SetInvincible(true);
        Transform root = charCtrl.transform;

        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            charCtrl.Move(Vector3.up * (10f / 0.5f) * Time.deltaTime);
            yield return null;
        }

        Vector3 groundCenter = root.position;
        groundCenter.y = 0; // 대충 초기 높이 가정

        for (int i = 0; i < 20; i++)
        {
            Vector3 randomSpot = groundCenter + new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f));
            SpawnVFX(5, randomSpot + Vector3.up * 10f, Quaternion.LookRotation(Vector3.down));
            SpawnVFX(3, randomSpot, Quaternion.identity);

            CombatSystem.DrawDebugSphere(randomSpot, 3f, 0.5f, new Color(1f, 0f, 0f, 0.15f));
            var targets = GetEnemiesInSphere(randomSpot, 3f);
            foreach (var t in targets)
            {
                combatSystem.DealDamageToTarget(t, skill.damageMultiplier * 0.3f, "빛의 사도(성창폭격)");
                if (t is PlayerHealth pH) pH.KnockUpServerRpc(Vector3.zero, 1.0f); // 1초 스턴
            }
            yield return new WaitForSeconds(0.15f);
        }

        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            charCtrl.Move(Vector3.down * (10f / 0.5f) * Time.deltaTime);
            yield return null;
        }

        SetInvincible(false);
    }
}
