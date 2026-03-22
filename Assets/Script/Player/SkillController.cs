using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class SkillController : MonoBehaviour
{
    private InputHandle Inputhandle;
    private ISkill Iskill;
    

    void Start()
    {
        ISkill[] Iskill = new ISkill[9];
        Inputhandle = GetComponent<InputHandle>();
    }
    void Update()
    {
        UseSkill(Inputhandle.numInput);
    }

    public void UseSkill(int num)
    {
        Iskill.Skill();
    }

}
