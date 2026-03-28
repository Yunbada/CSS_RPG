using Unity.Netcode.Components;

/// <summary>
/// Owner 권한으로 Transform을 동기화하는 NetworkTransform.
/// 기본 NetworkTransform은 Server Authority인데, 
/// FPS에서 Owner(클라이언트)가 직접 이동하므로 Owner Authority가 필요함.
/// </summary>
public class OwnerNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false; // Owner가 권한을 가짐
    }
}
