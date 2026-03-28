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

    [Header("UI Panels")]
    public GameObject authPanel;
    public GameObject connectPanel;

    [Header("Auth UI")]
    public UnityEngine.UI.InputField idInput;
    public UnityEngine.UI.InputField pwInput;
    public UnityEngine.UI.Text authMessageText;

    private void Awake()
    {
        // CsvDatabase가 없다면 자동 추가 (서버 연동 전 MVP 용)
        if (FindObjectOfType<CsvDatabase>() == null)
        {
            gameObject.AddComponent<CsvDatabase>();
        }

        if (authPanel != null) authPanel.SetActive(true);
        if (connectPanel != null) connectPanel.SetActive(false);
    }

    public void OnLoginClicked()
    {
        string id = idInput != null ? idInput.text : "";
        string pw = pwInput != null ? pwInput.text : "";

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
        {
            ShowMessage("ID and Password cannot be empty.");
            return;
        }

        var userData = CsvDatabase.Instance.LoginUser(id, pw);
        if (userData != null)
        {
            LocalUserData.Current = userData;
            ShowMessage("Login Success!", Color.green);
            ShowConnectPanel();
        }
        else
        {
            ShowMessage("Incorrect ID or Password.", Color.red);
        }
    }

    public void OnRegisterClicked()
    {
        string id = idInput != null ? idInput.text : "";
        string pw = pwInput != null ? pwInput.text : "";

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
        {
            ShowMessage("ID and Password cannot be empty.");
            return;
        }

        if (CsvDatabase.Instance.RegisterUser(id, pw))
        {
            ShowMessage("Registration Success! Please Login.", Color.green);
        }
        else
        {
            ShowMessage("ID already exists.", Color.red);
        }
    }

    private void ShowMessage(string msg, Color? color = null)
    {
        if (authMessageText != null)
        {
            authMessageText.text = msg;
            authMessageText.color = color ?? Color.red;
        }
    }

    private void ShowConnectPanel()
    {
        if (authPanel != null) authPanel.SetActive(false);
        if (connectPanel != null) connectPanel.SetActive(true);
    }

    public void OnStartHostClicked()
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient) return;
        NetworkManager.Singleton.StartHost();
        HideCanvas();
    }

    public void OnStartClientClicked()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;
        NetworkManager.Singleton.StartClient();
        HideCanvas();
    }

    public void OnStartServerClicked()
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient) return;
        NetworkManager.Singleton.StartServer();
        HideCanvas();
    }

    private void HideCanvas()
    {
        var mainMenu = GameObject.Find("MainMenu_Canvas");
        if (mainMenu != null)
        {
            mainMenu.SetActive(false);
        }
        else
        {
            if (authPanel != null) authPanel.SetActive(false);
            if (connectPanel != null) connectPanel.SetActive(false);
        }
    }
}
