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
    void Initialize(CombatSystem combat, PlayerState state);

    /// <summary>
    /// 스킬 발동 라우터
    /// </summary>
    void ExecuteSkill(int skillIndex, SkillData skill);
}
