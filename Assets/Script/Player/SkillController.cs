using UnityEngine;

public class SkillController : MonoBehaviour
{
    private InputHandle Inputhandle;
    private ISkill[] skill;


    void Start()
    {
        skill = new ISkill[9];
        Inputhandle = GetComponent<InputHandle>();
    }
    void Update()
    {
        UseSkill(Inputhandle.numInput);
    }

    void UseSkill(int num)
    {
        skill[num].UseSkill();
    }

}
