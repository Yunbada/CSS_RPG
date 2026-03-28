using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;

public class UISettingsManager : MonoBehaviour
{
    public static UISettingsManager Instance;

    public GameObject settingsPanel;
    public Slider fontSizeSlider;
    public Text fontSizeLabel;

    [Header("All Text Components in UI")]
    public List<Text> allUITexts = new List<Text>();

    private float _globalFontSizeMultiplier = 1.0f;
    private Dictionary<Text, int> _originalFontSizes = new Dictionary<Text, int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (settingsPanel != null) settingsPanel.SetActive(false); // เริ่มต้น 끄기

        // 슬라이더 초기화
        if (fontSizeSlider != null)
        {
            fontSizeSlider.minValue = 0.5f;
            fontSizeSlider.maxValue = 2.0f;
            fontSizeSlider.value = 1.0f;
            fontSizeSlider.onValueChanged.AddListener(OnFontSizeChanged);
        }

        // 초기 폰트 사이즈 저장
        foreach (var t in allUITexts)
        {
            if (t != null) _originalFontSizes[t] = t.fontSize;
        }
    }

    private void Update()
    {
        // 최상단 메뉴나 다른 상태가 없을 때 ESC로 설정창 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel != null)
            {
                bool isActive = settingsPanel.activeSelf;
                settingsPanel.SetActive(!isActive);

                // 마우스 잠금 해제 (FPS 게임이므로 설정창이 열리면 마우스를 보이게 함)
                if (!isActive)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
    }

    private void OnFontSizeChanged(float value)
    {
        _globalFontSizeMultiplier = value;
        if (fontSizeLabel != null) fontSizeLabel.text = $"Font Scale: {(value * 100):0}%";

        foreach (var kvp in _originalFontSizes)
        {
            if (kvp.Key != null)
            {
                kvp.Key.fontSize = Mathf.RoundToInt(kvp.Value * _globalFontSizeMultiplier);
            }
        }
    }
}
