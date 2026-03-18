using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class SkillController : MonoBehaviour
{
    private InputHandle Inputhandle;
    [SerializeField] private ISkill Iskill;
    

    void Start()
    {
        Inputhandle = GetComponent<InputHandle>();
    }
    void Update()
    {
        UseSkill(Inputhandle.numInput);
    }

    public void UseSkill(int num)
    {
        if (num != -1) Debug.Log(num);
    }

}
