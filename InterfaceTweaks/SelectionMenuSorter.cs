using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InterfaceTweaks
{
    public class SelectionMenuSorter
    {

        private static List<CharacterManager.ActionButton> lastOptions = null;
        private static string lastHeader = null;
        private static bool fromButton = false;

        public static void OnDestroy()
        {
            RemoveButtons();
        }

        static void RemoveButtons()
        {
            if (CharacterManager.instance == null)
            {
                return;
            }
            var headerObj = CharacterManager.instance.genericChoiceWindow.transform.GetChild(1).gameObject;
            for (int i = headerObj.transform.childCount - 1; i >= 0; i--)
            {
                Plugin.Log.LogWarning(headerObj.transform.GetChild(i));
                UnityEngine.Object.Destroy(headerObj.transform.GetChild(i).gameObject);
            }
        }

        [HarmonyPatch(typeof(CharacterManager), nameof(CharacterManager.showSelectionMenu))]
        [HarmonyPostfix]
        public static void ShowSelectionMenu(CharacterManager __instance, string header, List<CharacterManager.ActionButton> options)
        {
            if (fromButton)
            {
                return;
            }


            lastOptions = options;
            lastHeader = header;

            var headerObj = __instance.genericChoiceWindow.transform.GetChild(1).gameObject;
            if (headerObj.transform.childCount > 0)
            {
                return;
            }
            else
            {
                GameObject labelObj = UnityEngine.Object.Instantiate(__instance.genericChoicePrefab, headerObj.transform);
                labelObj.name = "Sort Label";

                var p = labelObj.transform.localPosition;
                labelObj.transform.localPosition = new Vector2(-220, 55);
                labelObj.GetComponentInChildren<Image>().rectTransform.sizeDelta = new Vector2(40 + (Enum.GetValues(typeof(SortOrder)).Length * 50), 40);
                labelObj.GetComponentInChildren<Image>().color = new Color(100, 100, 50);
                labelObj.GetComponentInChildren<TMP_Text>().text = " Sort:";
                labelObj.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Left;

                UnityEngine.Object.Destroy(labelObj.GetComponent<Button>());
                UnityEngine.Object.Destroy(labelObj.GetComponent<EventTrigger>());
                UnityEngine.Object.Destroy(labelObj.GetComponent<Scaling>());


                foreach (var order in (SortOrder[])Enum.GetValues(typeof(SortOrder)))
                {
                    MakeButton(__instance, headerObj, order);
                }
            }
        }

        private static void MakeButton(CharacterManager __instance, GameObject headerObj, SortOrder sortOrder)
        {
            GameObject buttonObj = UnityEngine.Object.Instantiate(__instance.genericChoicePrefab, headerObj.transform);
            buttonObj.name = "Sort Button";

            var p = buttonObj.transform.localPosition;
            buttonObj.transform.localPosition = new Vector2(-180 + (50 * (int)sortOrder), 50);
            var r = buttonObj.GetComponent<RectTransform>();

            buttonObj.GetComponentInChildren<UnityEngine.UI.Image>().rectTransform.sizeDelta = new Vector2(40, 30);
            var tmpText = buttonObj.GetComponentInChildren<TMP_Text>();
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
                    ToolTipManager.instance.addTooltipTo(buttonObj, "Rarity order, default order within same rarity",
                            ToolTipManager.Position.TopRight);
                    break;
                case SortOrder.RARITY_ALPHABETIC:
                    tmpText.text = "RA";
                    ToolTipManager.instance.addTooltipTo(buttonObj, "Rarity order, alphabetic order within same rarity",
                            ToolTipManager.Position.TopRight);
                    break;
            }

            Button button = buttonObj.GetComponentInChildren<Button>();
            button.onClick.AddListener(() => ResortMenu(sortOrder));
        }

        private static void ResortMenu(SortOrder order)
        {
            var options = new List<CharacterManager.ActionButton>(lastOptions);
            switch (order)
            {
                case SortOrder.ORIGINAL:
                    break;
                case SortOrder.ALPHABETIC:
                    options.Sort((a, b) =>
                    {
                        return StripString(a.name).CompareTo(StripString(b.name));
                    });
                    break;
                case SortOrder.RARITY:
                    options.Sort((a, b) =>
                    {
                        var ar = GetRaritySort(a.name);
                        var br = GetRaritySort(b.name);
                        return ar - br;
                    });
                    break;
                case SortOrder.RARITY_ALPHABETIC:
                    options.Sort((a, b) =>
                    {
                        var ar = GetRaritySort(a.name);
                        var br = GetRaritySort(b.name);
                        if (ar != br)
                        {
                            return ar - br;
                        }
                        return StripString(a.name).CompareTo(StripString(b.name));
                    });
                    break;
            }

            CharacterManager.instance.closeSelectionMenu();
            fromButton = true;
            CharacterManager.instance.showSelectionMenu(lastHeader, options);
            fromButton = false;
        }

        public static int GetRaritySort(string str)
        {
            var pattern = @"<color=([^>]*)";

            var result = Regex.Match(str, pattern).Groups[1].Value;

            return result switch
            {
                // mythic
                "#ff00ba" => -40,
                // special
                "green" => -30,
                "#1EFF00FF" => -30,
                // legendary
                "orange" => -20,
                // rare
                "#69C1FFFF" => -10,
                "#00ffff" => -10,
                // common
                _ => 0,
            };
        }

        public static string StripString(string str)
        {
            return Regex.Replace(str, "<.*?>", String.Empty);
        }

        private enum SortOrder
        {
            ORIGINAL, ALPHABETIC, RARITY, RARITY_ALPHABETIC
        }
    }
}
