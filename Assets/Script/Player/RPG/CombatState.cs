/// <summary>
/// 전투 시스템 상태 머신(FSM)에 쓰이는 열거형.
/// 단순 boolean 대신 명확한 상태 전이를 위해 사용합니다.
/// </summary>
public enum CombatState
{
    Idle,           // 대기 및 이동 가능
    BasicAttacking, // 기본 공격 애니메이션 재생 중
    SkillCasting,   // 스킬 캐스팅(선딜레이) 중
    SkillExecuting, // 스킬 발동/돌진 중 (주로 무적 상태 동반)
    Stunned,        // 피격/넉백/스턴 상태
    Dead            // 사망
}
