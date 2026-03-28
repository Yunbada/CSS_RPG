using UnityEngine;
using Unity.Netcode;

public enum PlayerClassType
{
    None,
    Fighter,    // 무투가
    Swordsman,  // 검사
    Gunner,     // 거너 (유일한 총기 사용)
    Mage        // 마법사
}

public class PlayerClass : NetworkBehaviour
{
    public NetworkVariable<PlayerClassType> currentClass = new NetworkVariable<PlayerClassType>(
        PlayerClassType.Fighter,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        // 런타임 무결성 보장: SkillSystem 컴포넌트 자동 부착
        if (GetComponent<SkillSystem>() == null)
            gameObject.AddComponent<SkillSystem>();

        // 런타임 무결성 보장: CombatSystem 컴포넌트 자동 부착
        if (GetComponent<CombatSystem>() == null)
            gameObject.AddComponent<CombatSystem>();

        if (IsOwner)
        {
            var skillSys = GetComponent<SkillSystem>();
            if (skillSys != null) 
            {
                skillSys.InitializeSkillSystem(this);
            }

            var combatSys = GetComponent<CombatSystem>();
            if (combatSys != null)
            {
                combatSys.InitializeCombatSystem();
            }
            
            if (LocalUserData.Current != null)
            {
                ChangeClassServerRpc((PlayerClassType)LocalUserData.Current.ClassIndex);
            }
        }
    }

    public void ChangeClass(PlayerClassType newClass)
    {
        if (IsServer)
        {
            currentClass.Value = newClass;
            ApplyClassBaseStats(newClass);
            SaveDataClientRpc((int)newClass); // Server also needs to trigger save for itself
        }
        else
        {
            ChangeClassServerRpc(newClass); // Renamed from RequestChangeClassServerRpc
        }
    }

    [ServerRpc]
    public void ChangeClassServerRpc(PlayerClassType newClass)
    {
        currentClass.Value = newClass;
        ApplyClassBaseStats(newClass);
        SaveDataClientRpc((int)newClass);
    }

    [ClientRpc]
    private void SaveDataClientRpc(int classIdx)
    {
        if (IsOwner && LocalUserData.Current != null)
        {
            LocalUserData.Current.ClassIndex = classIdx;
            CsvDatabase.Instance.SaveUser(LocalUserData.Current);
        }
    }

    private void ApplyClassBaseStats(PlayerClassType targetClass)
    {
        // 전직 시 특정 보너스 스탯을 주고 싶다면 여기에 StatSystem 연동
        // 기획상 레벨업 시 스탯 증가 요소는 없고 장비와 스킬로만 성장
    }

    // 기본공격 호출
    public void PerformBasicAttack()
    {
        switch (currentClass.Value)
        {
            case PlayerClassType.Fighter:
                Debug.Log("무투가 기본 공격! (근접)");
                break;
            case PlayerClassType.Swordsman:
                Debug.Log("검사 기본 공격! (근접)");
                break;
            case PlayerClassType.Gunner:
                Debug.Log("거너 총기 발사! (원거리)");
                break;
            case PlayerClassType.Mage:
                Debug.Log("마법사 마법 투척! (원거리)");
                break;
            case PlayerClassType.None:
            default:
                Debug.Log("전직 전 기본 공격!");
                break;
        }
    }
}
