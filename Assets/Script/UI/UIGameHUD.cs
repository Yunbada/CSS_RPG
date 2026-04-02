using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class UIGameHUD : MonoBehaviour
{
    [Header("Match Info")]
    public Text timerText;
    public Text humanCountText;
    public Text zombieCountText;

    [Header("Player Info")]
    public Text hpText;
    public Text ammoText;
    public Text[] skillTexts;
    public Text classNameText; // 추가된 전직 이름 텍스트

    [Header("Right Info")]
    public Text expText; // 레벨 및 경험치 표시
    public GameObject inventoryPanel; // N키로 열리는 인벤토리 컨테이너
    public Text[] inventoryTexts; // 우측 번호 선택창 (임시)

    [Header("Center Info")]
    public Text totalDamageText; // 크로스헤어 아래 누적 데미지 표시



    public void UpdateClassName(string className)
    {
        if (classNameText != null)
        {
            classNameText.text = $"< {className} >";
            classNameText.color = new Color(0.5f, 1f, 0.5f); // 밝은 연두색
        }
    }

    private void UpdateMatchInfo()
    {
        if (RoundManager.Instance == null) return;

        float timer = RoundManager.Instance.roundTimer.Value;
        int min = Mathf.FloorToInt(timer / 60f);
        int sec = Mathf.FloorToInt(timer % 60f);

        if (timerText != null)
            timerText.text = $"{min:D2}:{sec:D2}";

        int humanCount = 0;
        int zombieCount = 0;
        
        foreach (var p in PlayerState.AllPlayersList)
        {
            if (p != null)
            {
                if (p.currentTeam.Value == Team.Human) humanCount++;
                else zombieCount++;
            }
        }

        if (humanCountText != null) humanCountText.text = $"Humans: {humanCount}";
        if (zombieCountText != null) zombieCountText.text = $"Zombies: {zombieCount}";
    }

    private void UpdatePlayerInfo()
    {
        var localObj = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localObj == null) return;

        var state = localObj.GetComponentInChildren<PlayerState>();
        var skillCtrl = localObj.GetComponentInChildren<SkillController>();
        var pClass = localObj.GetComponentInChildren<PlayerClass>();
        var pExp = localObj.GetComponentInChildren<PlayerExperience>();

        // HP Update
        if (hpText != null && state != null)
        {
            hpText.text = $"HP: {state.currentHealth.Value} / {state.maxHealth.Value}";
        }

        // Exp / Level Update
        if (expText != null && pExp != null)
        {
            int maxExp = pExp.Level.Value * 100;
            expText.text = $"<Lv. {pExp.Level.Value}>\n<Exp: {pExp.CurrentExp.Value} / {maxExp}>";
        }

        // 누적 데미지 Update
        if (totalDamageText != null)
        {
            var combat = localObj.GetComponentInChildren<CombatSystem>();
            if (combat != null)
            {
                totalDamageText.text = $"DMG: {combat.TotalDamageDealt}";
            }
        }

        // 스킬 UI 업데이트는 이제 SkillSystem.cs에서 UpdateSkillUI()를 직접 호출하여 처리하므로 
        // 이곳에 있던 구형 폴링 업데이트 루프는 삭제되었습니다.

        // Ammo / Shield Energy Update
        if (ammoText != null)
        {
            if (pClass != null && pClass.currentClass.Value == PlayerClassType.Gunner)
            {
                ammoText.gameObject.SetActive(true);
                ammoText.text = "Ammo: 30 / ∞";
            }
            else if (pClass != null && pClass.currentClass.Value == PlayerClassType.Paladin)
            {
                ammoText.gameObject.SetActive(true);
                int currentEnergy = 0;
                var executor = localObj.GetComponentInChildren<PaladinSkillExecutor>();
                if (executor != null)
                {
                    currentEnergy = executor.ShieldEnergy;
                }
                ammoText.text = $"충전량: {currentEnergy}%";
            }
            else
            {
                ammoText.gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient || NetworkManager.Singleton.LocalClient == null)
            return;

        UpdateMatchInfo();
        UpdatePlayerInfo();

        var localObj = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localObj != null)
        {
            var inputHandle = localObj.GetComponent<InputHandle>();
            // N 키로 인벤토리(목록) 패널 토글
            if (inputHandle != null && inputHandle.toggleDebugMenuInput && inventoryPanel != null)
            {
                inventoryPanel.SetActive(!inventoryPanel.activeSelf);
            }
        }
    }

    public void UpdateSkillUI(int index, string skillName, float currentCooldown, float maxCooldown)
    {
        if (skillTexts == null || index < 0 || index >= skillTexts.Length || skillTexts[index] == null)
            return;

        Text targetText = skillTexts[index];

        if (string.IsNullOrEmpty(skillName))
        {
            targetText.text = ""; // 스킬이 없는 빈 칸은 텍스트를 숨김
            return;
        }

        if (currentCooldown > 0f)
        {
            // 스킬 쿨다운 중 (회색 표시 및 남은 시간 표기 - 정수형 카운트다운)
            targetText.color = Color.gray;
            targetText.text = $"{index + 1}. {skillName} [{Mathf.CeilToInt(currentCooldown)}초]";
        }
        else
        {
            // 스킬 사용 가능 (노란색 표시)
            targetText.color = new Color(1f, 0.8f, 0f); // 빛나는 노란색
            targetText.text = $"{index + 1}. {skillName}";
        }
    }
}
