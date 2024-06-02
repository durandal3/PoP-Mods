using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace InterfaceTweaks
{
    public class CreateTransformMenuSkillSorter
    {

        private static Util.SortOrder sortOrder = Util.SortOrder.ORIGINAL;

        public static void OnDestroy()
        {
            RemoveButtons();
        }

        static void RemoveButtons()
        {
            if (MaincharacterScreenManager.instance?.skillSelectionRoster == null)
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
            return MaincharacterScreenManager.instance.skillSelectionRoster.transform.parent.parent.parent;
        }

        [HarmonyPatch(typeof(MainCharacter), nameof(MainCharacter.getAvailableSkills))]
        [HarmonyPostfix]
        private static void GetAvailableSkillsSorted(ref string[] __result)
        {
            if (sortOrder == Util.SortOrder.ALPHABETIC)
            {
                var list = __result.ToList();
                list.Sort();
                __result = list.ToArray();
                return;
            }
        }

        [HarmonyPatch(typeof(MaincharacterScreenManager), nameof(MaincharacterScreenManager.updateGeneralContent))]
        [HarmonyPostfix]
        public static void UpdateGeneralContent(MaincharacterScreenManager __instance)
        {
            var parent = GetParentTransform();
            if (parent.GetComponentInChildren<Marker>() == null)
            {
                if (Plugin.addSortButtons.Value)
                {
                    var skillsLabel = parent.GetChild(0);

                    var p = skillsLabel.localPosition;
                    Util.MakeSortButtons(typeof(Marker), parent, p.x - 300, p.y - 10, (s) =>
                    {
                        sortOrder = s;
                        __instance.updateGeneralContent();
                    }, [
                        Util.SortOrder.ORIGINAL,
                        Util.SortOrder.ALPHABETIC,
                    ]);
                }
                sortOrder = Util.SortOrder.ORIGINAL;
            }
        }

        public class Marker : MonoBehaviour { }
    }
}
