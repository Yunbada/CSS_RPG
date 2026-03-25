using UnityEngine;
using Unity.Netcode;

public class NetworkManagerUI : MonoBehaviour
{
    private bool isLoggedIn = false;
    private bool isRegisterMode = false;
    
    // Auth variables
    private string inputId = "";
    private string inputPw = "";
    private string authMessage = "";
    
    private GUIStyle headerStyle;

    private void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;
        
        // 1920x1080 해상도 기준으로 UI 자동 스케일링
        Vector3 scale = new Vector3((float)Screen.width / 1920f, (float)Screen.height / 1080f, 1f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);

        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 36, fontStyle = FontStyle.Bold };
            headerStyle.normal.textColor = Color.white;
            headerStyle.alignment = TextAnchor.MiddleCenter;
        }

        // 로그인되지 않았으면 인증 화면 렌더링
        if (!isLoggedIn)
        {
            DrawAuthUI();
            return;
        }
        
        // 로그인되었고 아직 서버/클라이언트에 접속하지 않았으면 접속 버튼 렌더링
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            DrawConnectionUI();
        }
    }

    private void DrawAuthUI()
    {
        // 화면 중앙에 인증 창 배치
        float boxWidth = 400;
        float boxHeight = 350;
        Rect boxRect = new Rect((1920 - boxWidth)/2, (1080 - boxHeight)/2, boxWidth, boxHeight);
        
        GUI.Box(boxRect, "");

        GUILayout.BeginArea(boxRect);
        
        GUILayout.Space(20);
        GUILayout.Label(isRegisterMode ? "Sign Up" : "Login", headerStyle);
        GUILayout.Space(30);

        GUILayout.Label("ID:");
        inputId = GUILayout.TextField(inputId, 20); // 최대 20자
        
        GUILayout.Space(10);
        
        GUILayout.Label("Password:");
        inputPw = GUILayout.PasswordField(inputPw, '*', 20);

        GUILayout.Space(20);

        if (isRegisterMode)
        {
            if (GUILayout.Button("Create Account", GUILayout.Height(40)))
            {
                TryRegister();
            }
            if (GUILayout.Button("Back to Login", GUILayout.Height(40)))
            {
                isRegisterMode = false;
                authMessage = "";
            }
        }
        else
        {
            if (GUILayout.Button("Login", GUILayout.Height(40)))
            {
                TryLogin();
            }
            if (GUILayout.Button("Sign Up", GUILayout.Height(40)))
            {
                isRegisterMode = true;
                authMessage = "";
            }
        }

        GUILayout.Space(20);
        if (!string.IsNullOrEmpty(authMessage))
        {
            GUIStyle msgStyle = new GUIStyle(GUI.skin.label);
            msgStyle.normal.textColor = authMessage.Contains("Success") ? Color.green : Color.red;
            msgStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label(authMessage, msgStyle);
        }

        GUILayout.EndArea();
    }

    private void TryRegister()
    {
        if (string.IsNullOrEmpty(inputId) || string.IsNullOrEmpty(inputPw))
        {
            authMessage = "ID and Password cannot be empty.";
            return;
        }

        string key = "CSS_RPG_PW_" + inputId;
        if (PlayerPrefs.HasKey(key))
        {
            authMessage = "ID already exists.";
        }
        else
        {
            PlayerPrefs.SetString(key, inputPw);
            PlayerPrefs.Save();
            authMessage = "Registration Success! Please Login.";
            isRegisterMode = false;
        }
    }

    private void TryLogin()
    {
        if (string.IsNullOrEmpty(inputId) || string.IsNullOrEmpty(inputPw))
        {
            authMessage = "ID and Password cannot be empty.";
            return;
        }

        string key = "CSS_RPG_PW_" + inputId;
        if (!PlayerPrefs.HasKey(key))
        {
            authMessage = "Account does not exist.";
            return;
        }

        string savedPw = PlayerPrefs.GetString(key);
        if (savedPw == inputPw)
        {
            // 로그인 성공
            isLoggedIn = true;
            // Note: In a real game, you would pass the ID to the server or NetworkVariable to show the player's name.
        }
        else
        {
            authMessage = "Incorrect Password.";
        }
    }

    private void DrawConnectionUI()
    {
        GUILayout.BeginArea(new Rect(20, 20, 300, 300));
        
        GUILayout.Label($"Welcome, {inputId}!", new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold });
        GUILayout.Space(20);

        if (GUILayout.Button("Host (Server + Client)", GUILayout.Height(50)))
        {
            NetworkManager.Singleton.StartHost();
        }
        if (GUILayout.Button("Client", GUILayout.Height(50)))
        {
            NetworkManager.Singleton.StartClient();
        }
        if (GUILayout.Button("Server Only", GUILayout.Height(50)))
        {
            NetworkManager.Singleton.StartServer();
        }
        
        GUILayout.EndArea();
    }
}
