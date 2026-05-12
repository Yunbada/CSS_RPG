using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 물리적인 3D 아이템 드롭을 담당하는 네트워크 객체입니다.
/// 누구나(Team.Human) 다가가면 획득할 수 있습니다.
/// </summary>
public class LootDrop : NetworkBehaviour
{
    public NetworkVariable<int> ItemID = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> Count = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        // Add Rigidbody for physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.mass = 1f;
        rb.linearDamping = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Keep existing BoxCollider for physical collision, ensure it's not a trigger
        BoxCollider boxCol = GetComponent<BoxCollider>();
        if (boxCol != null)
        {
            boxCol.isTrigger = false;
        }

        // Add a SphereCollider as a trigger for pickup
        SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.5f; // Pick up range
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // 서버에서만 충돌(획득) 판정 처리

        // 트리거된 객체가 플레이어인지 확인
        PlayerState playerState = other.GetComponentInParent<PlayerState>();
        if (playerState != null && playerState.CurrentTeam == Team.Human)
        {
            // 인간 팀인 플레이어만 획득 가능
            InventorySystem inventory = playerState.GetComponent<InventorySystem>();
            if (inventory != null)
            {
                // 인벤토리에 아이템 추가
                bool added = inventory.AddItem(ItemID.Value, Count.Value);
                if (added)
                {
                    Debug.Log($"[LootDrop] Player {playerState.OwnerClientId} picked up ItemID {ItemID.Value} (Count: {Count.Value})");
                    // 획득 성공 시 네트워크에서 아이템 제거
                    NetworkObject.Despawn();
                }
            }
        }
    }
}
