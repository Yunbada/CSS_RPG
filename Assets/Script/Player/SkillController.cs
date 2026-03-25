using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(InputHandle))]
public class SkillController : NetworkBehaviour
{
    private InputHandle inputHandle;
    private PlayerExperience playerExp;
    private PlayerState playerState;

    // 최대 9개 스킬 (인간), 좀비는 최대 5개 스킬
    private ISkill[] equippedSkills = new ISkill[9];

    private void Awake()
    {
        inputHandle = GetComponent<InputHandle>();
        playerExp = GetComponent<PlayerExperience>();
        playerState = GetComponent<PlayerState>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        // 스킬 쿨타임 감소
        for (int i = 0; i < equippedSkills.Length; i++)
        {
            if (equippedSkills[i] != null && equippedSkills[i].CurrentCooldown > 0)
            {
                equippedSkills[i].CurrentCooldown -= Time.deltaTime;
            }
        }

        // 입력 처리 (1~9번 키)
        int numInput = inputHandle.numInput;
        if (numInput != -1) // -1 means no input
        {
            TryExecuteSkill(numInput);
        }
    }

    private void TryExecuteSkill(int index)
    {
        if (index < 0 || index >= equippedSkills.Length) return;

        // 좀비일 경우 5번 이후 스킬은 사용 불가
        if (playerState.currentTeam.Value != Team.Human && index >= 5) 
        {
            return; 
        }

        ISkill skill = equippedSkills[index];
        if (skill != null)
        {
            int currentLevel = playerExp != null ? playerExp.Level.Value : 1;
            
            // 해금 레벨 및 쿨타임 체크
            if (skill.CanUse(currentLevel) && skill.CurrentCooldown <= 0)
            {
                // 서버로 스킬 실행 알림 (RPC)
                ExecuteSkillServerRpc(index);
                // 쿨타임 시작
                skill.CurrentCooldown = skill.Cooldown;
            }
        }
    }

    [ServerRpc]
    private void ExecuteSkillServerRpc(int index)
    {
        // 서버에서 검증 루틴 추가 가능
        ExecuteSkillClientRpc(index);
    }

    [ClientRpc]
    private void ExecuteSkillClientRpc(int index)
    {
        ISkill skill = equippedSkills[index];
        if (skill != null)
        {
            skill.Execute(gameObject);
        }
    }
}
