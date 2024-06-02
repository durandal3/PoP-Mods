using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace InterfaceTweaks
{
    public class McProfessionSorter
    {

        private static List<Profession> sortedProfessions = [];

        public static void OnDestroy()
        {
            RemoveButtons();
        }

        static void RemoveButtons()
        {
            if (CharacterCreationManager.instance?.mainCharacterProfessionRoster == null)
            {
                return;
            }
            foreach (var c in GetParentTransform().GetComponentsInChildren<Marker>())
            {
                UnityEngine.Object.Destroy(c.gameObject);
            }
        }

        private static Transform GetParentTransform()
        {
            return CharacterCreationManager.instance.mainCharacterProfessionRoster.transform.parent.parent.parent;
        }

        private static void DoMcProfessionSort(Util.SortOrder traitOrder)
        {
            if (!sortedProfessions.Any())
            {
                sortedProfessions = new List<Profession>(SaveController.instance.permanentInfo.availableProfessions);
            }
            var roster = CharacterCreationManager.instance.mainCharacterProfessionRoster.transform;

            Dictionary<Profession, Transform> previousButtons = [];
            foreach (var trait in sortedProfessions)
            {
                var child = roster.GetChild(0);
                previousButtons.Add(trait, child);
                child.SetParent(null, true);
            }
            switch (traitOrder)
            {
                case Util.SortOrder.ORIGINAL:
                    sortedProfessions = new List<Profession>(SaveController.instance.permanentInfo.availableProfessions);
                    break;
                case Util.SortOrder.ALPHABETIC:
                    sortedProfessions = Enumerable.OrderBy(sortedProfessions, (e) => e.ToString()).ToList();
                    break;
                case Util.SortOrder.RARITY:
                    FieldInfo specialBonusProfessionField = typeof(CharacterCreationManager).GetField("specialBonusProfession", BindingFlags.NonPublic | BindingFlags.Instance);
                    var specialBonusProfession = (Profession)specialBonusProfessionField.GetValue(CharacterCreationManager.instance);
                    FieldInfo bonusProfessionsField = typeof(CharacterCreationManager).GetField("bonusProfessions", BindingFlags.NonPublic | BindingFlags.Instance);
                    var bonusProfessions = (List<Profession>)bonusProfessionsField.GetValue(CharacterCreationManager.instance);
                    sortedProfessions = Enumerable.OrderBy(sortedProfessions, (e) =>
                    {
                        if (e == specialBonusProfession)
                        {
                            return -1;
                        }
                        return bonusProfessions.Contains(e) ? 0 : 1;
                    }).ToList();
                    break;
                case Util.SortOrder.COST:
                    sortedProfessions = Enumerable.OrderBy(sortedProfessions, CharacterCreationManager.getCostForMcProfession).ToList();
                    break;
            }

            foreach (var trait in sortedProfessions)
            {
                var child = previousButtons[trait];
                child.SetParent(roster, true);
            }
        }

        [HarmonyPatch(typeof(CharacterCreationManager), nameof(CharacterCreationManager.showTab))]
        [HarmonyPrefix]
        public static void CreateButtons()
        {
            var parent = GetParentTransform();
            if (parent.GetComponentInChildren<Marker>() == null)
            {
                if (Plugin.addSortButtons.Value)
                {
                    var label = parent.GetChild(7);
                    var p = label.localPosition;
                    Util.MakeSortButtons(typeof(Marker), parent, p.x + 210, p.y, DoMcProfessionSort, [
                        Util.SortOrder.ORIGINAL,
                        Util.SortOrder.ALPHABETIC,
                        Util.SortOrder.RARITY,
                        Util.SortOrder.COST,
                    ]);
                }
                sortedProfessions = new List<Profession>(SaveController.instance.permanentInfo.availableProfessions);
            }
        }


        [HarmonyPatch(typeof(CharacterCreationManager), nameof(CharacterCreationManager.selectProfession))]
        [HarmonyPrefix]
        public static void SelectProfession(int i, CharacterCreationManager __instance, ref bool __runOriginal)
        {
            __runOriginal = false;
            FieldInfo professionIndexField = typeof(CharacterCreationManager).GetField("professionIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            var professionIndex = (int)professionIndexField.GetValue(CharacterCreationManager.instance);

            if (professionIndex != i)
            {
                var newInd = sortedProfessions.IndexOf(SaveController.instance.permanentInfo.availableProfessions[i]);
                var oldInd = sortedProfessions.IndexOf(SaveController.instance.permanentInfo.availableProfessions[professionIndex]);

                var notSelectedColor = (Color)typeof(CharacterCreationManager).GetField("notSelectedColor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(CharacterCreationManager.instance);
                var selectedColor = (Color)typeof(CharacterCreationManager).GetField("selectedColor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(CharacterCreationManager.instance);
                __instance.mainCharacterProfessionRoster.GetComponentsInChildren<Button>()[oldInd].gameObject.GetComponent<Image>().color = notSelectedColor;
                professionIndexField.SetValue(CharacterCreationManager.instance, i);
                __instance.mainCharacterProfessionRoster.GetComponentsInChildren<Button>()[newInd].gameObject.GetComponent<Image>().color = selectedColor;
                typeof(CharacterCreationManager).GetMethod("updatePoints", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, []);
                typeof(CharacterCreationManager).GetMethod("updateBonusText", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, []);
            }
        }

        [HarmonyPatch(typeof(CharacterCreationManager), nameof(CharacterCreationManager.showTab))]
        [HarmonyPostfix]
        public static void ShowTab(int i, CharacterCreationManager __instance)
        {
            if (i != 2)
            {
                return;
            }

            FieldInfo professionIndexField = typeof(CharacterCreationManager).GetField("professionIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            var professionIndex = (int)professionIndexField.GetValue(CharacterCreationManager.instance);
            var selectedIndex = sortedProfessions.IndexOf(SaveController.instance.permanentInfo.availableProfessions[professionIndex]);

            var notSelectedColor = (Color)typeof(CharacterCreationManager).GetField("notSelectedColor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(CharacterCreationManager.instance);
            var selectedColor = (Color)typeof(CharacterCreationManager).GetField("selectedColor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(CharacterCreationManager.instance);
            for (int n = 0; n < __instance.mainCharacterProfessionRoster.transform.childCount; n++)
            {
                if (selectedIndex == n)
                {
                    __instance.mainCharacterProfessionRoster.transform.GetChild(n).GetComponentInChildren<Image>().color = selectedColor;
                }
                else
                {
                    __instance.mainCharacterProfessionRoster.transform.GetChild(n).GetComponentInChildren<Image>().color = notSelectedColor;
                }
            }
        }

        public class Marker : MonoBehaviour { }
    }
}
