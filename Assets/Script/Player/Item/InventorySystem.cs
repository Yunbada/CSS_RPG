using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public enum MaterialType
{
    RottenLeather, // 썩은 가죽
    RottenTooth,   // 썩은 이빨
    BrokenSkull    // 깨진 두개골
}

public class InventorySystem : NetworkBehaviour
{
    public NetworkVariable<int> LeatherCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> ToothCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> SkullCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsOwner && LocalUserData.Current != null)
        {
            SyncInventoryServerRpc(LocalUserData.Current.Leather, LocalUserData.Current.Tooth, LocalUserData.Current.Skull);
        }

        if (IsServer)
        {
            PlayerState.OnAnyZombieDied += HandleZombieDied;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            PlayerState.OnAnyZombieDied -= HandleZombieDied;
        }
    }

    private void HandleZombieDied(ulong killerId)
    {
        if (killerId == OwnerClientId)
        {
            // 10% 확률로 아이템 드랍
            if (Random.value <= 0.1f)
            {
                MaterialType randomMat = (MaterialType)Random.Range(0, 3);
                AddMaterial(randomMat, 1);
                Debug.Log($"Player {OwnerClientId} earned a {randomMat} from killing a zombie.");
            }
        }
    }

    [ServerRpc]
    private void SyncInventoryServerRpc(int l, int t, int s)
    {
        LeatherCount.Value = l;
        ToothCount.Value = t;
        SkullCount.Value = s;
    }

    // 서버 전용 로직
    public void AddMaterial(MaterialType type, int amount = 1)
    {
        if (!IsServer) return;

        switch (type)
        {
            case MaterialType.RottenLeather: LeatherCount.Value += amount; break;
            case MaterialType.RottenTooth: ToothCount.Value += amount; break;
            case MaterialType.BrokenSkull: SkullCount.Value += amount; break;
        }

        SaveDataClientRpc(LeatherCount.Value, ToothCount.Value, SkullCount.Value);
    }

    [ClientRpc]
    private void SaveDataClientRpc(int l, int t, int s)
    {
        if (IsOwner && LocalUserData.Current != null)
        {
            LocalUserData.Current.Leather = l;
            LocalUserData.Current.Tooth = t;
            LocalUserData.Current.Skull = s;
            CsvDatabase.Instance.SaveUser(LocalUserData.Current);
        }
    }

    public void RequestCraft(MaterialType type)
    {
        if (IsOwner)
        {
            CraftEquipmentServerRpc(type);
        }
    }

    [ServerRpc]
    private void CraftEquipmentServerRpc(MaterialType type)
    {
        int cost = 10;
        bool canCraft = false;

        switch (type)
        {
            case MaterialType.RottenLeather:
                if (LeatherCount.Value >= cost) { LeatherCount.Value -= cost; canCraft = true; } break;
            case MaterialType.RottenTooth:
                if (ToothCount.Value >= cost) { ToothCount.Value -= cost; canCraft = true; } break;
            case MaterialType.BrokenSkull:
                if (SkullCount.Value >= cost) { SkullCount.Value -= cost; canCraft = true; } break;
        }

        if (canCraft)
        {
            SaveDataClientRpc(LeatherCount.Value, ToothCount.Value, SkullCount.Value);
            GetComponent<EquipmentSystem>()?.GenerateAndEquipReward(type);
            Debug.Log($"Player {OwnerClientId} crafted a new equipment using {type}");
        }
    }
}
