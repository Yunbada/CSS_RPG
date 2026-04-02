using UnityEngine;
using Unity.Netcode;

/// <summary>
/// S.O.L.I.D 구조 확장을 위한 타격 가능 객체 공통 인터페이스입니다.
/// 플레이어 외에도 몬스터, 파괴 가능한 바리케이드 등이 이 인터페이스를 구현하게 됩니다.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 현재 소속된 팀 정보 (피아식별용)
    /// </summary>
    Team CurrentTeam { get; }

    /// <summary>
    /// 데미지 수용 함수 (네트워크 환경에서는 서버에서만 호출됨)
    /// </summary>
    void TakeDamage(int amount, ulong attackerId = 0);
    
    /// <summary>
    /// 대상의 Transform 반환 (VFX 스폰, 타격 위치 계산 등)
    /// </summary>
    Transform EntityTransform { get; }

    /// <summary>
    /// 서버 검증을 위한 NetworkObject 반환
    /// </summary>
    NetworkObject GetNetworkObject();
}
