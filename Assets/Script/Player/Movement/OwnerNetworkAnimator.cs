using Unity.Netcode.Components;

/// <summary>
/// Owner 권한으로 Animator를 동기화하는 NetworkAnimator.
/// 기본 NetworkAnimator는 Server Authority인데,
/// 클라이언트가 직접 애니메이션 파라미터를 설정하므로 Owner Authority가 필요함.
/// </summary>
public class OwnerNetworkAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false; // Owner가 권한을 가짐
    }
}
