using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

[System.Serializable]
public class SkillData
{
    public string skillName;
    public string description;
    public float cooldownTime;
    
    [HideInInspector] 
    public float currentCooldown;

    // =========== 전투 속성 ===========
    public float damageMultiplier;  // 데미지 배율 (예: 1.5 = Attack의 150%)
    public float range;             // 사거리 (미터, 0이면 기본 공격 사거리 사용)
    public float areaRadius;        // 범위 공격 반경 (0이면 단일 타격)
    public bool isSelfBuff;         // 자신에게 적용하는 버프인 경우 true
    
    public bool IsReady => currentCooldown <= 0f;
}

public class SkillSystem : MonoBehaviour
{
    [Header("Runtime Info - Do not edit manually")]
    public SkillData[] currentSkills = new SkillData[9];
    
    private InputHandle inputHandle;
    private PlayerClass playerClass;
    private PlayerState playerState;

    private bool isInitialized = false;
    private bool isZombie = false;

    private void Awake()
    {
        inputHandle = GetComponent<InputHandle>();
        // playerClass is now explicitly injected by InitializeSkillSystem
    }

    public void InitializeSkillSystem(PlayerClass pClass)
    {
        playerClass = pClass;
        playerState = GetComponent<PlayerState>();
        
        // 직업 변경 이벤트를 구독하여 스킬 목록을 재구성
        if (playerClass != null)
        {
            playerClass.currentClass.OnValueChanged += OnClassChanged;
        }

        // 팀 변경 이벤트를 구독하여 좀비/인간 스킬 전환
        if (playerState != null)
        {
            playerState.currentTeam.OnValueChanged += OnTeamChanged;
        }

        // 초기 로드 (현재 팀 상태에 따라 분기)
        RefreshSkillsForCurrentState();
        isInitialized = true;
    }

    private void OnClassChanged(PlayerClassType oldClass, PlayerClassType newClass)
    {
        // 좀비 상태가 아닐 때만 인간 스킬을 새로고침
        if (!isZombie)
        {
            LoadSkillsForClass(newClass);
        }
    }

    private void OnTeamChanged(Team oldTeam, Team newTeam)
    {
        RefreshSkillsForCurrentState();
    }

    private void RefreshSkillsForCurrentState()
    {
        if (playerState != null && playerState.currentTeam.Value != Team.Human)
        {
            isZombie = true;
            LoadZombieSkills(playerState.currentTeam.Value);
        }
        else
        {
            isZombie = false;
            if (playerClass != null)
                LoadSkillsForClass(playerClass.currentClass.Value);
        }
    }

    private void OnDestroy()
    {
        if (playerClass != null)
            playerClass.currentClass.OnValueChanged -= OnClassChanged;
        if (playerState != null)
            playerState.currentTeam.OnValueChanged -= OnTeamChanged;
    }

    private void Update()
    {
        if (!isInitialized) return;

        // 쿨타임 감소 로직
        for (int i = 0; i < currentSkills.Length; i++)
        {
            if (currentSkills[i] != null && currentSkills[i].currentCooldown > 0f)
            {
                currentSkills[i].currentCooldown -= Time.deltaTime;
                
                // 쿨타임이 끝났을 경우 0으로 보정 (음수 방지)
                if (currentSkills[i].currentCooldown <= 0f)
                {
                    currentSkills[i].currentCooldown = 0f;
                }
                
                // 시간이 줄어드는 것을 UI에 실시간 표기하기 위해 매 프레임 업데이트
                UpdateHUD(i);
            }
        }

        // 전직 UI가 켜져있을 땐 스킬 단축키 입력을 무시
        var classCtrl = FindFirstObjectByType<ClassSelectionController>();
        bool isUIOpen = classCtrl != null && classCtrl.panel != null && classCtrl.panel.activeSelf;
        
        var hud = FindFirstObjectByType<UIGameHUD>();
        bool isInvOpen = hud != null && hud.inventoryPanel != null && hud.inventoryPanel.activeSelf;

        if (inputHandle != null && !isUIOpen)
        {
            int pressedKey = inputHandle.numInput;
            if (pressedKey >= 0 && pressedKey < 9)
            {
                if (isInvOpen)
                {
                    // 인벤토리용 더미 디버그 조작
                    if (pressedKey == 0) // 숫자 1번
                    {
                        var exp = GetComponent<PlayerExperience>();
                        if (exp != null) exp.SetCheatLevelServerRpc(100);
                        Debug.Log("디버그: 인벤토리 열림 상태에서 1번 단축키로 레벨 100 설정!");
                    }
                    else if (pressedKey == 1) // 숫자 2번
                    {
                        playerClass.SetAwakeningServerRpc(1);
                        Debug.Log("디버그: 인벤토리 열림 상태에서 2번 단축키로 1차 각성 설정!");
                    }
                    else if (pressedKey == 2) // 숫자 3번
                    {
                        playerClass.SetAwakeningServerRpc(2);
                        Debug.Log("디버그: 인벤토리 열림 상태에서 3번 단축키로 2차 각성 설정!");
                    }
                    else if (pressedKey == 3) // 숫자 4번
                    {
                        // 전직, 레벨, 경험치, 각성 모두 초기화
                        playerClass.ChangeClassServerRpc(PlayerClassType.None);
                        playerClass.SetAwakeningServerRpc(0);
                        var exp = GetComponent<PlayerExperience>();
                        if (exp != null) exp.SetCheatLevelServerRpc(1);
                        Debug.Log("디버그: 전직/레벨/경험치/각성 모두 초기화!");
                    }
                }
                else
                {
                    TryUseSkill(pressedKey);
                }
            }
        }
    }

    private void TryUseSkill(int index)
    {
        if (currentSkills == null || index >= currentSkills.Length || currentSkills[index] == null) return;
        
        // 등록된 스킬이 없으면 리턴 (이름이 없는 경우)
        if (string.IsNullOrEmpty(currentSkills[index].skillName)) return;

        // --- 각성 레벨에 따른 스킬 덱 차단 ---
        int awkLevel = playerClass.awakeningLevel.Value;
        var pExp = GetComponent<PlayerExperience>();
        if (pExp != null)
        {
            if (pExp.Level.Value >= 90) awkLevel = Mathf.Max(awkLevel, 2);
            else if (pExp.Level.Value >= 60) awkLevel = Mathf.Max(awkLevel, 1);
        }

        if ((index == 6 || index == 7) && awkLevel < 1)
        {
            Debug.Log("해당 스킬은 1차 각성(Lv.60) 이상 달성 시 해금됩니다.");
            return;
        }
        if (index == 8 && awkLevel < 2)
        {
            Debug.Log("해당 스킬은 2차 각성(Lv.90) 이상 달성 시 해금됩니다.");
            return;
        }

        // 스킬 사용 중이면 추가 스킬 사용 차단
        var combat = GetComponent<CombatSystem>();
        if (combat != null && combat.IsUsingSkill) return;

        SkillData skill = currentSkills[index];

        if (skill.IsReady)
        {
            // 스킬 사용
            Debug.Log($"[{playerClass.currentClass.Value}] {index + 1}번 스킬 사용 ({skill.skillName}), 쿨타임 {skill.cooldownTime}초 적용!");
            
            // 힐 스킬 처리 (응급 치료 / 흡혈 등 — 스킬 이름으로 판별)
            if (skill.skillName.Contains("응급 치료") || skill.skillName.Contains("흡혈"))
            {
                var pState = GetComponent<PlayerState>();
                if (pState != null)
                {
                    int healAmount = 50;
                    if (isZombie && pState.currentTeam.Value == Team.HostZombie)
                        healAmount = 80;

                    pState.HealServerRpc(healAmount);
                    Debug.Log($"회복 스킬 발동! HP가 {healAmount} 회복되었습니다.");
                }
            }
            else
            {
                // 실제 전투 스킬 실행 (CombatSystem 연동)
                var combatSys = GetComponent<CombatSystem>();
                if (combatSys != null)
                {
                    combatSys.ExecuteSkillAttack(index);
                }
            }
            
            skill.currentCooldown = skill.cooldownTime;
            UpdateHUD(index);
        }
        else
        {
            // 아직 쿨타임 중 (원한다면 콘솔 출력 가능)
        }
    }

    private void UpdateHUD(int index)
    {
        if (playerState != null && !playerState.IsOwner) return;

        // 최적화를 위해 FindFirstObjectByType 사용 구조 유지 (로컬 클라이언트엔 1개만 존재함)
        var hud = FindFirstObjectByType<UIGameHUD>();
        if (hud != null && currentSkills[index] != null)
        {
            hud.UpdateSkillUI(index, currentSkills[index].skillName, currentSkills[index].currentCooldown, currentSkills[index].cooldownTime);
        }
    }

    // =========================================================================
    // ✔️ 여기서부터 유저님이 원하시는 대로 직업별 스킬을 마음껏 수정하시면 됩니다! ✔️
    // =========================================================================
    public void LoadSkillsForClass(PlayerClassType classType)
    {
        // 처음 9칸 비어있는 상태로 덮어씌움 (초기화)
        for (int i = 0; i < 9; i++) currentSkills[i] = new SkillData { skillName = "", cooldownTime = 1f };

        string classNameKor = "미전직 (단축키 C)";

        switch (classType)
        {
            case PlayerClassType.Fighter: // 무투가
                classNameKor = "무투가 (Fighter)";
                SetSkill(0, "정권 찌르기", 1.0f, "전방 2m 이내의 적을 타격", 1.2f, 2f, 0f, false);
                SetSkill(1, "승천권", 1.0f, "전방 2m 이내의 적과 함께 위로 2m 상승하며 타격", 2.0f, 2f, 0f, false);
                SetSkill(2, "백스텝", 1.0f, "후방 2m으로 빠르게 회피", 0f, 0f, 0f, true);
                SetSkill(3, "파쇄권", 1.0f, "전방 2m으로 돌진 후 도착 지점 주변 1m 적 타격", 2.5f, 2f, 1f, false);
                SetSkill(4, "풍각", 1.0f, "회전 킥으로 주변 1m 적 타격 + 전방 3m 1.5초간 전진", 1.5f, 0f, 1f, false);
                SetSkill(5, "연파", 1.0f, "전방 2m 이내의 적을 1.5초간 빠르게 연속 타격", 0.5f, 2f, 0f, false);
                SetSkill(6, "번개춤", 1.0f, "주변 3m 이내 적들에게 2초간 보이지 않는 속도로 타격", 1.0f, 0f, 3f, false);
                SetSkill(7, "연탄", 1.0f, "장풍을 연속적으로 빠르게 발사", 0.6f, 10f, 0f, false);
                SetSkill(8, "진공난무 (궁극기)", 1.0f, "주변 3~4m의 적들을 2초간 끌어당긴 후 강력한 타격", 4.0f, 0f, 4f, false);
                break;
                
            case PlayerClassType.Swordsman: // 검사
                classNameKor = "검사 (Swordsman)";
                SetSkill(0, "⭐ 응급 치료", 2.0f, "체력을 50 회복합니다."); // 전 직업 공통
                SetSkill(1, "회전 베기", 4.0f, "주변 360도 범위 타격");
                SetSkill(2, "검풍", 6.0f, "전방으로 날아가는 검기");
                SetSkill(3, "내려찍기", 5.0f, "강하게 도약 후 내려찍음");
                SetSkill(4, "반격 자세", 8.0f, "피격 시 반격");
                SetSkill(5, "돌진 찌르기", 7.0f, "적을 관통하며 돌진");
                SetSkill(6, "검의 춤", 14.0f, "주변 범위 연속 타격");
                SetSkill(7, "발도술", 18.0f, "순식간에 단일 적에게 큰 피해");
                SetSkill(8, "진공참 (궁극기)", 40.0f, "화면 끝까지 닿는 거대 검풍");
                break;

            case PlayerClassType.Gunner: // 거너
                classNameKor = "거너 (Gunner)";
                SetSkill(0, "⭐ 응급 치료", 2.0f, "체력을 50 회복합니다."); // 전 직업 공통
                SetSkill(1, "수류탄 투척", 6.0f, "폭발하는 수류탄 투척");
                SetSkill(2, "난사", 8.0f, "주변 모든 적에게 총알 난사");
                SetSkill(3, "구르기", 3.0f, "무적 회피기");
                SetSkill(4, "산탄총", 5.0f, "근거리 부채꼴 타격");
                SetSkill(5, "화염방사", 12.0f, "지속적인 화염 데미지");
                SetSkill(6, "지뢰 매설", 10.0f, "밟으면 폭발하는 지뢰");
                SetSkill(7, "헤드샷", 15.0f, "엄청난 화력의 저격");
                SetSkill(8, "위성 폭격 (궁극기)", 50.0f, "지정된 넓은 범위에 레이저 폭격");
                break;

            case PlayerClassType.Mage: // 마법사
                classNameKor = "마법사 (Mage)";
                SetSkill(0, "⭐ 응급 치료", 2.0f, "체력을 50 회복합니다."); // 전 직업 공통
                SetSkill(1, "파이어볼", 4.0f, "폭발하는 화염구");
                SetSkill(2, "아이스 스피어", 5.0f, "적을 관통하고 둔화시키는 얼음창");
                SetSkill(3, "텔레포트", 6.0f, "단거리 순간이동");
                SetSkill(4, "라이트닝 체인", 8.0f, "여러 적에게 전이되는 번개");
                SetSkill(5, "매직 쉴드", 15.0f, "피해를 흡수하는 방어막");
                SetSkill(6, "블리자드", 20.0f, "지속적인 범위 둔화 및 피해");
                SetSkill(7, "블랙홀", 25.0f, "주변 적을 한 곳으로 모음");
                SetSkill(8, "메테오 스트라이크 (궁극기)", 60.0f, "화면을 뒤덮는 운석 낙하");
                break;
                
            case PlayerClassType.Paladin: // 성기사
                classNameKor = "성기사 (Paladin)";
                SetSkill(0, "전진베기", 1.0f, "바라보는 방향 이동 후 넉백 타격", 1.0f, 1.5f, 0f, false);
                SetSkill(1, "축복의 방패", 3.0f, "에너지량에 비례해 돌진강타 혹은 방패투척 스턴", 1.5f, 2.5f, 0f, false);
                SetSkill(2, "빛의 징벌", 6.0f, "주변을 내리쳐 적을 띄우고 화상을 입힘", 2.0f, 4f, 0f, false);
                SetSkill(3, "천상의 날개", 8.0f, "수직 비행 후 원하는 곳을 강하게 내리찍음", 2.5f, 5f, 0f, false);
                SetSkill(4, "심판", 10.0f, "공중에 뜬 적을 베어버리는 추가 콤보", 2.5f, 7f, 0f, false);
                SetSkill(5, "빛의 성창", 12.0f, "적을 관통해 이속을 감소시키는 빛의 창", 2.0f, 0f, 0f, false);
                SetSkill(6, "빛의 율법 (1차 각성)", 15.0f, "장판을 소환해 지속 피해와 둔화 부여", 1.0f, 5f, 0f, false);
                SetSkill(7, "불굴의 의지 (1차 각성)", 30.0f, "15초간 공격/방어/치피/방관 자가 버프", 0f, 0f, 0f, true);
                SetSkill(8, "빛의 사도 (궁극기)", 60.0f, "상공에서 20m 공간에 무수한 창을 꽂아 징벌", 3.0f, 20f, 0f, false);
                break;
                
            default: // 직업이 없을 때 (또는 초기 상태)
                classNameKor = "미전직 (단축키 C)";
                SetSkill(0, "⭐ 응급 치료", 2.0f, "체력을 50 회복합니다."); // 전 직업 공통
                break;
        }

        // 스킬셋이 변경되었으므로 전체 UI를 한 번 초기화 갱신합니다.
        if (playerState != null && playerState.IsOwner)
        {
            var hud = FindFirstObjectByType<UIGameHUD>();
            if (hud != null)
            {
                hud.UpdateClassName(classNameKor);
                for (int i = 0; i < 9; i++) UpdateHUD(i);
            }
        }
    }

    // =========================================================================
    // 🧟 좀비 스킬 데이터 (좀비로 변신 시 자동 적용)
    // =========================================================================
    public void LoadZombieSkills(Team zombieTeam)
    {
        // 쿨타임 초기화
        for (int i = 0; i < 9; i++) currentSkills[i] = new SkillData { skillName = "", cooldownTime = 1f };

        string classNameKor;

        if (zombieTeam == Team.HostZombie)
        {
            classNameKor = "🧟 숙주";
            SetSkill(0, "⭐ 흡혈", 3.0f, "적을 물어 HP 80 회복");
            SetSkill(1, "감염 할퀴기", 2.0f, "빠른 근접 공격 + 감염 효과");
            SetSkill(2, "공포의 울부짖음", 10.0f, "주변 인간의 이동속도 감소");
            SetSkill(3, "좀비 대돌진 (궁극기)", 45.0f, "엄청난 속도로 질주하며 닿는 적 모두 감염");
        }
        else // NormalZombie
        {
            classNameKor = "🧟 좀비";
            // 일반 좀비는 스킬이 없음
        }

        // UI 갱신
        if (playerState != null && playerState.IsOwner)
        {
            var hud = FindFirstObjectByType<UIGameHUD>();
            if (hud != null)
            {
                hud.UpdateClassName(classNameKor);
                for (int i = 0; i < 9; i++) UpdateHUD(i);
            }
        }
    }

    // 스킬 데이터 입력을 편리하게 하기 위한 헬퍼 함수 (전투 참수 포함)
    private void SetSkill(int slotIndex, string name, float cooldown, string desc,
        float dmgMult = 0f, float range = 0f, float areaRadius = 0f, bool isSelfBuff = false)
    {
        if (slotIndex >= 0 && slotIndex < 9)
        {
            currentSkills[slotIndex] = new SkillData 
            { 
                skillName = name, 
                cooldownTime = cooldown, 
                description = desc,
                currentCooldown = 0f,
                damageMultiplier = dmgMult,
                range = range,
                areaRadius = areaRadius,
                isSelfBuff = isSelfBuff
            };
        }
    }
}
