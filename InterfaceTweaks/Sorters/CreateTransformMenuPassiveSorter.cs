using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace InterfaceTweaks
{
    public class CreateTransformMenuPassiveSorter
    {

        private static Util.SortOrder sortOrder = Util.SortOrder.ORIGINAL;

        public static void OnDestroy()
        {
            RemoveButtons();
        }

        static void RemoveButtons()
        {
            if (MaincharacterScreenManager.instance?.passiveSelectionRoster == null)
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
            return MaincharacterScreenManager.instance.passiveSelectionRoster.transform.parent.parent.parent;
        }

        [HarmonyPatch(typeof(MainCharacter), nameof(MainCharacter.getAvailablePassives))]
        [HarmonyPostfix]
        private static void GetAvailablePassivesSorted(ref Passives[] __result)
        {
            switch (sortOrder)
            {
                case Util.SortOrder.ORIGINAL:
                default:
                    return;
                case Util.SortOrder.ALPHABETIC:
                    {
                        var list = __result.ToList();
                        list.Sort((a, b) => a.name.CompareTo(b.name));
                        __result = list.ToArray();
                        return;
                    }
                case Util.SortOrder.PASSIVE_TYPE:
                    {
                        var list = __result.ToList();
                        list.Sort((a, b) => a.type.CompareTo(b.type));
                        __result = list.ToArray();
                        return;
                    }
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
                    var skillsLabel = parent.GetChild(2);

                    var p = skillsLabel.localPosition;
                    Util.MakeSortButtons(typeof(Marker), parent, p.x - 235, p.y - 10, (s) =>
                    {
                        sortOrder = s;
                        __instance.updateGeneralContent();
                    }, [
                        Util.SortOrder.ORIGINAL,
                        Util.SortOrder.ALPHABETIC,
                        Util.SortOrder.PASSIVE_TYPE,
                    ]);
                }
                sortOrder = Util.SortOrder.ORIGINAL;
            }
        }

        public class Marker : MonoBehaviour { }
    }
}
