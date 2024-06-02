using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InterfaceTweaks
{
    public class Util
    {

        static readonly Sprite panelBackgroundSprite = CreateSprite(new Color(0.2f, 0.2f, 0.2f));
        static readonly Sprite buttonBackgroundSprite = CreateSprite(new Color(0.3f, 0.3f, 0.3f));

        private static Sprite CreateSprite(Color color)
        {
            var texture = new Texture2D(30, 20, TextureFormat.RGB24, false);

            // set the pixel values
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
            // Apply all SetPixel calls
            texture.Apply();
            return Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height),
                    new Vector2(texture.width / 2, texture.height / 2));
        }

        public static void MakeSortButtons(Type markerType, Transform parent, float startX, float startY, Action<SortOrder> sorter, List<SortOrder> sortOrders)
        {
            int buttonNum = 0;

            GameObject panelFitter = new("Sort Panel", markerType, typeof(ContentSizeFitter));
            // GameObject panelFitter = new("Sort Panel", typeof(Marker));
            panelFitter.transform.SetParent(parent, false);
            panelFitter.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 30);
            // panelFitter.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            // panelFitter.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;


            GameObject panelObj = new("Sort Panel", markerType, typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            panelObj.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            panelObj.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            panelObj.transform.SetParent(panelFitter.transform, false);
            panelObj.transform.localPosition = new Vector2(startX, startY);
            // panelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 30);


            var layout = panelObj.GetComponent<HorizontalLayoutGroup>();

            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.enabled = true;
            layout.childControlHeight = true;
            layout.spacing = 5;
            layout.padding = new RectOffset(5, 5, 5, 5);

            panelObj.GetComponent<Image>().sprite = panelBackgroundSprite;
            // panelObj.GetComponent<Image>().rectTransform.sizeDelta = new Vector2(20, 20);

            GameObject sortLabelObj = new("Sort Label", typeof(TextMeshProUGUI));
            // sortLabelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 30);
            sortLabelObj.GetComponent<TMP_Text>().fontSize = 14;
            sortLabelObj.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
            sortLabelObj.GetComponent<TMP_Text>().text = "Sort:";
            sortLabelObj.transform.SetParent(panelObj.transform, false);

            // startX += 30;

            foreach (var sortOrder in sortOrders)
            {
                GameObject buttonObj = new("Sort Button", typeof(Button), typeof(Image), typeof(EventTrigger));
                buttonObj.transform.SetParent(panelObj.transform, false);
                // buttonObj.transform.localPosition = new Vector2(startX + (25 * buttonNum), startY);

                buttonObj.GetComponent<Image>().sprite = buttonBackgroundSprite;
                buttonObj.GetComponent<Button>().targetGraphic = buttonObj.GetComponent<Image>();
                // buttonObj.GetComponent<Image>().rectTransform.sizeDelta = new Vector2(30, 20);

                var textObj = new GameObject("Sort Button Label", typeof(TextMeshProUGUI), typeof(ContentSizeFitter));
                textObj.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                textObj.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                textObj.transform.SetParent(buttonObj.transform, false);

                // textObj.GetComponent<RectTransform>().sizeDelta = buttonObj.GetComponent<Image>().rectTransform.sizeDelta;
                var tmpText = textObj.GetComponent<TMP_Text>();
                tmpText.fontSize = 14;
                tmpText.alignment = TextAlignmentOptions.Center;
                switch (sortOrder)
                {
                    default:
                    case SortOrder.ORIGINAL:
                        tmpText.text = "D";
                        ToolTipManager.instance.addTooltipTo(buttonObj, "Default order",
                                ToolTipManager.Position.TopRight);
                        break;
                    case SortOrder.ALPHABETIC:
                        tmpText.text = "A";
                        ToolTipManager.instance.addTooltipTo(buttonObj, "Alphabetic order",
                                ToolTipManager.Position.TopRight);
                        break;
                    case SortOrder.RARITY:
                        tmpText.text = "R";
                        ToolTipManager.instance.addTooltipTo(buttonObj, "Rarity order",
                                ToolTipManager.Position.TopRight);
                        break;
                    case SortOrder.RARITY_ALPHABETIC:
                        tmpText.text = "RA";
                        ToolTipManager.instance.addTooltipTo(buttonObj, "Rarity order, alphabetic order within same rarity",
                                ToolTipManager.Position.TopRight);
                        break;
                    case SortOrder.COST:
                        tmpText.text = "C";
                        ToolTipManager.instance.addTooltipTo(buttonObj, "Cost order",
                                ToolTipManager.Position.TopRight);
                        break;
                    case SortOrder.PASSIVE_TYPE:
                        tmpText.text = "T";
                        ToolTipManager.instance.addTooltipTo(buttonObj, "Passive type (onAttack/onMove/etc...)",
                                ToolTipManager.Position.TopRight);
                        break;
                }

                Button button = buttonObj.GetComponentInChildren<Button>();
                button.onClick.AddListener(() => sorter.Invoke(sortOrder));

                buttonNum++;
            }
        }

        public enum SortOrder
        {
            ORIGINAL, ALPHABETIC, RARITY, RARITY_ALPHABETIC, COST, PASSIVE_TYPE,
        }

        public static void ChangePosition(Transform transform, int dx, int dy)
        {
            var p = transform.localPosition;
            transform.localPosition = new Vector3(p.x + dx, p.y + dy, p.z);
        }

        public static void PrintAllChildren(Transform transform)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Plugin.Log.LogWarning(transform.GetChild(i));
            }
        }
    }
}
