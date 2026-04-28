using UnityEngine;
using UnityEngine.UI;

namespace CSS_RPG.UI
{
    public class InventoryView
    {
        private GameObject inventoryCanvasObj;
        private GameObject inventoryPanel;
        private Text[] inventoryTexts;

        public InventoryView()
        {
            EnsureUI();
        }

        private void EnsureUI()
        {
            if (inventoryCanvasObj != null) return;

            inventoryCanvasObj = new GameObject("Inventory_Canvas_Runtime");
            Canvas canvas = inventoryCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900; // 서버 콘솔 및 에러로그 밑에 뜨도록
            
            var scaler = inventoryCanvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f; // 창모드 해상도 대응
            inventoryCanvasObj.AddComponent<GraphicRaycaster>();

            inventoryPanel = new GameObject("InventoryPanel");
            inventoryPanel.transform.SetParent(inventoryCanvasObj.transform, false);
            RectTransform invRt = inventoryPanel.AddComponent<RectTransform>();
            invRt.anchorMin = new Vector2(1f, 0.5f);
            invRt.anchorMax = new Vector2(1f, 0.5f);
            invRt.pivot = new Vector2(1f, 0.5f);
            invRt.anchoredPosition = new Vector2(-10, 0);
            invRt.sizeDelta = new Vector2(330, 580);

            Image invBg = inventoryPanel.AddComponent<Image>();
            invBg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            inventoryTexts = new Text[10];
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            for (int i = 0; i < 10; i++)
            {
                GameObject txtObj = new GameObject($"InvText_{i}");
                txtObj.transform.SetParent(inventoryPanel.transform, false);
                RectTransform rt = txtObj.AddComponent<RectTransform>();
                // 패널 내부 좌우를 stretch하고 상단 기준으로 배치
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.sizeDelta = new Vector2(-20, 48); // 좌우 10px 여백 확보, 높이 48
                rt.anchoredPosition = new Vector2(0, -10 - (i * 55)); // 10px 상단 여백, 55px 간격

                Text txt = txtObj.AddComponent<Text>();
                txt.font = font;
                txt.fontSize = 22;
                txt.color = Color.white;
                txt.alignment = TextAnchor.MiddleLeft;
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;

                // 그림자
                Shadow shadow = txtObj.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.8f);
                shadow.effectDistance = new Vector2(2, -2);

                inventoryTexts[i] = txt;
            }

            inventoryCanvasObj.SetActive(false); // 기본은 꺼짐 상태
        }

        public void TogglePanel(bool isVisible)
        {
            if (inventoryCanvasObj != null)
            {
                inventoryCanvasObj.SetActive(isVisible);
            }
        }

        public void DestroyUI()
        {
            if (inventoryCanvasObj != null)
            {
                Object.Destroy(inventoryCanvasObj);
            }
        }

        public void ClearTexts()
        {
            if (inventoryTexts == null) return;
            for (int i = 0; i < inventoryTexts.Length; i++)
            {
                if (inventoryTexts[i] != null)
                {
                    inventoryTexts[i].text = "";
                    inventoryTexts[i].color = Color.white;
                }
            }
        }

        public void SetText(int index, string text, Color color)
        {
            if (inventoryTexts == null || index < 0 || index >= inventoryTexts.Length) return;
            if (inventoryTexts[index] == null) return;
            inventoryTexts[index].text = text;
            inventoryTexts[index].color = color;
        }

        public Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return Color.white;
                case ItemRarity.Uncommon: return new Color(0.3f, 1f, 0.3f);     // 초록
                case ItemRarity.Rare: return new Color(0.3f, 0.5f, 1f);         // 파랑
                case ItemRarity.Epic: return new Color(0.7f, 0.3f, 1f);         // 보라
                case ItemRarity.Legendary: return new Color(1f, 0.65f, 0f);     // 주황
                default: return Color.white;
            }
        }
    }
}
