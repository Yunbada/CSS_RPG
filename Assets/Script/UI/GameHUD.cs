using UnityEngine;
using Unity.Netcode;

public class GameHUD : MonoBehaviour
{
    private GUIStyle labelStyle;
    private GUIStyle skillReadyStyle;
    private GUIStyle skillCooldownStyle;

    private void OnGUI()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient || NetworkManager.Singleton.LocalClient == null)
            return;

        // 1920x1080 해상도 기준으로 UI 자동 스케일링
        Vector3 scale = new Vector3((float)Screen.width / 1920f, (float)Screen.height / 1080f, 1f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold };
            labelStyle.normal.textColor = Color.white;
            
            skillReadyStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
            skillReadyStyle.normal.textColor = Color.yellow;
            
            skillCooldownStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
            skillCooldownStyle.normal.textColor = Color.gray;
        }

        DrawMatchInfo();
        DrawPlayerInfo();
    }

    private void DrawMatchInfo()
    {
        if (RoundManager.Instance == null) return;

        float timer = RoundManager.Instance.roundTimer.Value;
        int min = Mathf.FloorToInt(timer / 60f);
        int sec = Mathf.FloorToInt(timer % 60f);

        // 상단 중앙: 시간
        GUI.Label(new Rect(1920 / 2 - 50, 20, 200, 50), $"{min:D2}:{sec:D2}", labelStyle);

        int humanCount = 0;
        int zombieCount = 0;
        
        // FindObjectsOfType is fine for prototype MVP. To optimize later, maintain a list in RoundManager.
        foreach (var p in FindObjectsOfType<PlayerState>())
        {
            if (p.currentTeam.Value == Team.Human) humanCount++;
            else zombieCount++;
        }

        // 상단 좌측: 인간 수
        GUI.Label(new Rect(20, 20, 200, 50), $"Humans: {humanCount}", labelStyle);
        
        // 상단 우측: 좀비 수
        GUI.Label(new Rect(1920 - 250, 20, 200, 50), $"Zombies: {zombieCount}", labelStyle);
    }

    private void DrawPlayerInfo()
    {
        var localObj = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localObj == null) return;

        var state = localObj.GetComponent<PlayerState>();
        var skillCtrl = localObj.GetComponent<SkillController>();
        var pClass = localObj.GetComponent<PlayerClass>();

        // 좌측 하단: 체력
        if (state != null)
        {
            GUI.Label(new Rect(20, 1080 - 80, 400, 50), $"HP: {state.currentHealth.Value} / {state.maxHealth.Value}", labelStyle);
        }

        // 좌측 상단 (미니맵 아래쪽 공간 확보) - 미니맵은 추후 RenderTexture 카메라 오버레이로 구현 예정
        GUI.Label(new Rect(20, 80, 200, 40), "[Minimap Area]", labelStyle);

        // 좌측 중앙: 스킬 
        if (skillCtrl != null)
        {
            float startY = 1080 / 2 - 150;
            // Hack for MVP: Currently equippedSkills array is private, so we just show placeholders or you can expose them.
            // For now, I'll print generic skill slots.
            for (int i = 0; i < 9; i++)
            {
                // UI 기획: 사용 가능 노란색 / 쿨타임 회색
                GUI.Label(new Rect(20, startY + (i*40), 300, 40), $"Skill {i+1} [ Ready ]", skillReadyStyle);
            }
        }

        // 우측 하단: 탄약 (거너)
        if (pClass != null && pClass.currentClass.Value == PlayerClassType.Gunner)
        {
            GUI.Label(new Rect(1920 - 250, 1080 - 80, 300, 50), "Ammo: 30 / ∞", labelStyle);
        }
    }
}
