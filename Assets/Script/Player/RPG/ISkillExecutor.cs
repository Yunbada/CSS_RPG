using UnityEngine;

/// <summary>
/// 모든 클래스(직업)의 스킬 실행기가 공통으로 구현해야 하는 인터페이스 (OCP 적용)
/// CombatSystem은 이 인터페이스에만 의존합니다. (DIP 적용)
/// </summary>
public interface ISkillExecutor
{
    /// <summary>
    /// 초기화 (CombatSystem에서 자동 호출)
    /// </summary>
    void Initialize(CombatSystem combat, PlayerState state, PlayerHealth health);

    /// <summary>
    /// 스킬 발동 라우터
    /// </summary>
    void ExecuteSkill(int skillIndex, SkillData skill);

    /// <summary>
    /// 데미지를 입혔을 때 호출되는 콜백 (OCP: 직업 고유 게이지 충전 등)
    /// CombatSystem은 이 메서드만 호출하며, 구체적인 구현은 각 직업이 결정합니다.
    /// </summary>
    void OnDamageDealt(int damage);
}
