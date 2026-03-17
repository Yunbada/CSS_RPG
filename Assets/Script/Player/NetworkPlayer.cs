using System;
using System.Runtime;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private InputHandle m_InputHandle;
    [SerializeField] private PlayerMove m_PlayerMove;
    [SerializeField] private SkillController m_SkillController;
    
    private void Awake()
    {
        m_InputHandle.enabled = false;
        m_PlayerMove.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            m_InputHandle.enabled = true;
            m_PlayerMove.enabled = true;
        }
    }

    [Rpc(target: SendTo.Server)]
    private void UpdateToServerRpc()
    {
        m_SkillController.UseSkill(m_InputHandle.numInput);
    }

    private void LateUpdate()
    {
        if(!IsOwner) return;

        UpdateToServerRpc();
    }


}
