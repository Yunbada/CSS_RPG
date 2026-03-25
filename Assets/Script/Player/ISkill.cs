using UnityEngine;
using Unity.Netcode;

public interface ISkill
{
    string SkillName { get; }
    float Cooldown { get; }
    int RequiredLevel { get; }
    float CurrentCooldown { get; set; }

    bool CanUse(int playerLevel);
    void Execute(GameObject caster);
}
