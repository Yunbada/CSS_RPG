using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 플레이어 VFX(시각 효과) 관리 (SRP 분리)
/// 스킬 이펙트 및 타격 이펙트 프리팹의 네트워크 동기화 생성/파괴를 전담합니다.
/// </summary>
public class PlayerVFXController : NetworkBehaviour
{
    [Header("Skill VFX Prefabs (Abstract Energy)")]
    [SerializeField] private GameObject vfxStraight;
    [SerializeField] private GameObject vfxRising;
    [SerializeField] private GameObject vfxTyphoon;
    [SerializeField] private GameObject vfxRupture;
    [SerializeField] private GameObject vfxBuff;
    [SerializeField] private GameObject vfxLightning;
    [SerializeField] private GameObject vfxOrb;
    [SerializeField] private GameObject vfxSmear;
    [SerializeField] private GameObject vfxAbstractFlash;

    [Header("Hit Impact VFX (Networked)")]
    [SerializeField] private GameObject hitImpactNormal;
    [SerializeField] private GameObject hitImpactCritical;
    [SerializeField] private GameObject hitImpactDoT;

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SpawnSkillVFXServerRpc(int vfxType, Vector3 position, Quaternion rotation)
    {
        SpawnSkillVFXClientRpc(vfxType, position, rotation);
    }

    [ClientRpc]
    public void SpawnSkillVFXClientRpc(int vfxType, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = null;
        switch (vfxType)
        {
            case 0: prefab = vfxStraight; break;
            case 1: prefab = vfxRising; break;
            case 2: prefab = vfxTyphoon; break;
            case 3: prefab = vfxRupture; break;
            case 4: prefab = vfxLightning; break;
            case 5: prefab = vfxOrb; break;
            case 6: prefab = vfxSmear; break;
            case 7: prefab = vfxAbstractFlash; break;
        }

        if (prefab != null)
        {
            GameObject vfx = Instantiate(prefab, position, rotation);
            Destroy(vfx, 1.5f);
        }
    }
}
