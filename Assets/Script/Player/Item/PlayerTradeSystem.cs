using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// 플레이어 간 1:1 아이템 거래 시스템
/// - 근거리 플레이어 탐색
/// - 거래 요청 및 수락
/// - 단일 슬롯 아이템 거래 등록 및 교환
/// </summary>
public class PlayerTradeSystem : NetworkBehaviour
{
    // =========================================================================
    // 거래 상태 정의
    // =========================================================================
    public NetworkVariable<ulong> TradePartnerId = new NetworkVariable<ulong>(ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsTrading = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // 내가 올린 아이템
    public NetworkVariable<int> OfferedItemId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> OfferedItemCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // 나에게 거래를 요청한 플레이어 목록 (로컬 전용)
    public List<ulong> PendingRequests { get; private set; } = new List<ulong>();

    private InventorySystem inventory;

    private void Awake()
    {
        inventory = GetComponent<InventorySystem>();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && IsTrading.Value)
        {
            CancelTradeServerRpc(); // 연결 끊김 시 자동 취소
        }
    }

    public List<ulong> GetAllPlayers()
    {
        List<ulong> players = new List<ulong>();
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) 
            return players;

        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = kvp.Key;
            if (clientId == OwnerClientId) continue; // 나 자신 제외

            var clientObj = kvp.Value.PlayerObject;
            if (clientObj != null && clientObj.IsSpawned)
            {
                var otherTrade = clientObj.GetComponentInChildren<PlayerTradeSystem>();
                if (otherTrade != null)
                {
                    players.Add(clientId);
                }
            }
        }
        return players;
    }

    // =========================================================================
    // 거래 요청
    // =========================================================================
    [ServerRpc]
    public void RequestTradeServerRpc(ulong targetClientId)
    {
        if (IsTrading.Value) return;

        // 상대방에게 RPC 전달
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(targetClientId, out var targetClient))
        {
            var targetTrade = targetClient.PlayerObject.GetComponentInChildren<PlayerTradeSystem>();
            if (targetTrade != null && !targetTrade.IsTrading.Value)
            {
                targetTrade.ReceiveTradeRequestClientRpc(OwnerClientId);
            }
        }
    }

    [ClientRpc]
    private void ReceiveTradeRequestClientRpc(ulong sourceClientId)
    {
        if (IsOwner)
        {
            if (!PendingRequests.Contains(sourceClientId))
            {
                PendingRequests.Add(sourceClientId);
                Debug.Log($"[Trade] Player {sourceClientId} 님으로부터 거래 요청이 왔습니다.");
            }
        }
    }

    // =========================================================================
    // 거래 수락 / 시작
    // =========================================================================
    [ServerRpc]
    public void AcceptTradeServerRpc(ulong requesterId)
    {
        if (IsTrading.Value) return;
        
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(requesterId, out var requesterObj))
        {
            var requesterTrade = requesterObj.PlayerObject.GetComponentInChildren<PlayerTradeSystem>();
            if (requesterTrade != null && !requesterTrade.IsTrading.Value)
            {
                // 양쪽 상태 Trading으로 변경
                StartTradeSession(requesterTrade);
                StartTradeSession(this);

                // 파트너 ID 설정
                requesterTrade.TradePartnerId.Value = this.OwnerClientId;
                this.TradePartnerId.Value = requesterId;
                
                // 요청 목록 정리
                ClearRequestsClientRpc(requesterId);
                requesterTrade.ClearRequestsClientRpc(this.OwnerClientId);
            }
        }
    }

    private void StartTradeSession(PlayerTradeSystem p)
    {
        p.IsTrading.Value = true;
        p.IsReady.Value = false;
        p.OfferedItemId.Value = 0;
        p.OfferedItemCount.Value = 0;
    }

    [ClientRpc]
    private void ClearRequestsClientRpc(ulong acceptedId)
    {
        if (IsOwner)
        {
            PendingRequests.Remove(acceptedId);
            // 거래 시작 시 모든 요청 초기화
            if (IsTrading.Value)
                PendingRequests.Clear();
        }
    }

    // =========================================================================
    // 거래 아이템 등록/해제
    // =========================================================================
    [ServerRpc]
    public void OfferItemServerRpc(int itemId, int count)
    {
        if (!IsTrading.Value || IsReady.Value) return;

        // 기존에 올린게 있다면 다시 인벤토리로 반환
        if (OfferedItemId.Value > 0 && OfferedItemCount.Value > 0)
        {
            inventory.AddItem(OfferedItemId.Value, OfferedItemCount.Value);
        }

        if (itemId > 0 && count > 0)
        {
            // 인벤토리에서 제거 후 등록
            if (inventory.RemoveItemById(itemId, count))
            {
                OfferedItemId.Value = itemId;
                OfferedItemCount.Value = count;
            }
            else
            {
                OfferedItemId.Value = 0;
                OfferedItemCount.Value = 0;
            }
        }
        else
        {
            OfferedItemId.Value = 0;
            OfferedItemCount.Value = 0;
        }
        
        CancelReadyForBoth();
    }

    private void CancelReadyForBoth()
    {
        // 아이템이 변경되면 양쪽의 Ready 상태 해제
        IsReady.Value = false;
        if (TryGetPartner(out var partner))
        {
            partner.IsReady.Value = false;
        }
    }

    // =========================================================================
    // 준비 및 교환 실행
    // =========================================================================
    [ServerRpc]
    public void SetReadyServerRpc(bool ready)
    {
        if (!IsTrading.Value) return;
        IsReady.Value = ready;

        if (ready && TryGetPartner(out var partner))
        {
            if (partner.IsReady.Value)
            {
                ExecuteTrade(this, partner);
            }
        }
    }

    private void ExecuteTrade(PlayerTradeSystem p1, PlayerTradeSystem p2)
    {
        // p1에게 p2 아이템 지급
        if (p2.OfferedItemId.Value > 0)
            p1.inventory.AddItem(p2.OfferedItemId.Value, p2.OfferedItemCount.Value);

        // p2에게 p1 아이템 지급
        if (p1.OfferedItemId.Value > 0)
            p2.inventory.AddItem(p1.OfferedItemId.Value, p1.OfferedItemCount.Value);

        // 종료
        FinishSession(p1);
        FinishSession(p2);
    }

    // =========================================================================
    // 취소
    // =========================================================================
    [ServerRpc]
    public void CancelTradeServerRpc()
    {
        if (!IsTrading.Value) return;

        if (TryGetPartner(out var partner))
        {
            ReturnOfferedItem(partner);
            FinishSession(partner);
        }

        ReturnOfferedItem(this);
        FinishSession(this);
    }

    private void ReturnOfferedItem(PlayerTradeSystem p)
    {
        if (p.OfferedItemId.Value > 0 && p.OfferedItemCount.Value > 0)
        {
            p.inventory.AddItem(p.OfferedItemId.Value, p.OfferedItemCount.Value);
        }
    }

    private void FinishSession(PlayerTradeSystem p)
    {
        p.IsTrading.Value = false;
        p.IsReady.Value = false;
        p.TradePartnerId.Value = ulong.MaxValue;
        p.OfferedItemId.Value = 0;
        p.OfferedItemCount.Value = 0;
    }

    private bool TryGetPartner(out PlayerTradeSystem partner)
    {
        partner = null;
        if (TradePartnerId.Value != ulong.MaxValue && 
            NetworkManager.Singleton.ConnectedClients.TryGetValue(TradePartnerId.Value, out var client))
        {
            partner = client.PlayerObject.GetComponentInChildren<PlayerTradeSystem>();
            return partner != null;
        }
        return false;
    }
}
