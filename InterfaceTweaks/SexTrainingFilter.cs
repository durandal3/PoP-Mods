using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace InterfaceTweaks
{
    public class SexTrainingFilter
    {
        private static int currentFilter = -1;

        private class FilterClickListener : MonoBehaviour, IPointerClickHandler
        {
            public int setting = -1;

            public void OnPointerClick(PointerEventData eventData)
            {
                if (setting == currentFilter)
                {
                    currentFilter = -1;
                }
                else
                {
                    currentFilter = setting;
                }
                UpdateSexMainCharacter();
                UpdateRosters();
            }
        }

        static void UpdateRosters()
        {
            MethodInfo LeftSexRosterMethodInfo = typeof(TownInterfaceController).GetMethod("updateLeftSexRoster", BindingFlags.NonPublic | BindingFlags.Instance);
            LeftSexRosterMethodInfo.Invoke(TownInterfaceController.instance, new object[] { });
            MethodInfo RightSexRosterMethodInfo = typeof(TownInterfaceController).GetMethod("updateRightSexRoster", BindingFlags.NonPublic | BindingFlags.Instance);
            RightSexRosterMethodInfo.Invoke(TownInterfaceController.instance, new object[] { });
        }

        [HarmonyPatch(typeof(TownInterfaceController), nameof(TownInterfaceController.showHomeTab))]
        [HarmonyPrefix]
        static void ShowHomeTab(int index)
        {
            // Clear the filter
            currentFilter = -1;
            if (index == 1) // Sex-Training tab
            {
                var smcs = TownInterfaceController.instance.sexMainCharacterStats;
                if (smcs[0].GetComponent<FilterClickListener>() == null)
                {
                    for (int i = 0; i < smcs.Length; i++)
                    {
                        FilterClickListener test = smcs[i].AddComponent<FilterClickListener>();
                        test.setting = i;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(TownInterfaceController), nameof(TownInterfaceController.updateSexMainCharacter))]
        [HarmonyPostfix]
        static void UpdateSexMainCharacter()
        {
            var smcs = TownInterfaceController.instance.sexMainCharacterStats;
            for (int i = 0; i < smcs.Length; i++)
            {
                if (smcs[i].activeSelf)
                {
                    var label = smcs[i].GetComponentsInChildren<TMP_Text>()[2];
                    label.gameObject.SetActive(false);
                    if (i == currentFilter)
                    {
                        label.outlineColor = Color.white;
                    }
                    else
                    {
                        label.outlineColor = Color.black;
                    }
                    label.gameObject.SetActive(true);
                }
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TownInterfaceController), "updateLeftSexRoster")]
        [HarmonyPatch(typeof(TownInterfaceController), "updateRightSexRoster")]
        static IEnumerable<CodeInstruction> UpdateSexRoster(IEnumerable<CodeInstruction> instructions)
        {
            // Add a filter to the character list between getting it and using it.
            return new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Brfalse))
                    .Advance(1)
                    .Insert(new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Plugin), nameof(Plugin.FilterList))),
                        new CodeInstruction(OpCodes.Stloc_0)
                    })
                    .InstructionEnumeration();
        }

        static List<Stats> FilterList(List<Stats> list)
        {
            if (currentFilter == -1 || list == null)
            {
                return list;
            }
            List<Stats> newList = new List<Stats>();
            List<Species> alreadyFucked = SaveController.instance.mainCharacter.sexSkills[currentFilter].fuckedAtCurrentSkillLevel;
            foreach (var item in list)
            {
                if (!alreadyFucked.Contains(item.species))
                {
                    newList.Add(item);
                }
            }
            return newList;
        }
    }
}
