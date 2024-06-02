using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine;

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
            if (!Plugin.addSortButtons.Value)
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
                Util.MakeSortButtons(typeof(Marker), headerObj.transform, 0, 30, ResortMenu, [
                    Util.SortOrder.ORIGINAL,
                        Util.SortOrder.ALPHABETIC,
                        Util.SortOrder.RARITY,
                        Util.SortOrder.RARITY_ALPHABETIC,
                    ]);
            }
        }

        private static void ResortMenu(Util.SortOrder order)
        {
            var options = new List<CharacterManager.ActionButton>(lastOptions);
            switch (order)
            {
                case Util.SortOrder.ORIGINAL:
                    break;
                case Util.SortOrder.ALPHABETIC:
                    options.Sort((a, b) =>
                    {
                        return StripString(a.name).CompareTo(StripString(b.name));
                    });
                    break;
                case Util.SortOrder.RARITY:
                    options = Enumerable.OrderBy(options, (e) => GetRaritySort(e.name)).ToList();
                    break;
                case Util.SortOrder.RARITY_ALPHABETIC:
                    options = Enumerable
                            .OrderBy(options, (e) => StripString(e.name))
                            .OrderBy((e) => GetRaritySort(e.name))
                            .ToList();
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

        public class Marker : MonoBehaviour { }
    }
}
