using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InterfaceTweaks
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private static List<Harmony> _harmony = [];

        public static ConfigEntry<bool> showUnlockableTraits;
        public static ConfigEntry<bool> highlightUnlockableSpecies;
        public static ConfigEntry<bool> showExtraAp;

        private void Awake()
        {
            Log = base.Logger;

            showExtraAp = Config.Bind("General", "ShowExtraAP", true,
                    "On will add extra AP icons for characters with over 2 AP");
            showUnlockableTraits = Config.Bind("General.Unlockables", "ShowUnlockableTraits", false,
                    "Whether to show unlockable traits in a tooltip in the new game screen (\"GeneticTraits\" on starter selection screen)");
            highlightUnlockableSpecies = Config.Bind("General.Unlockables", "HighlightUnlockableSpecies", false,
                    "Whether to highlight unlockable species (in portals, combats, tavern hires, and character overview)");


            _harmony.Add(Harmony.CreateAndPatchAll(typeof(Plugin)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(SexTrainingFilter)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(UnlockIndicator)));
        }

        private void OnDestroy()
        {
            foreach (var item in _harmony)
            {
                item.UnpatchSelf();
            }
        }


        [HarmonyPatch(typeof(ToolTipManager), nameof(ToolTipManager.showTooltip))]
        [HarmonyPrefix]
        public static void MakeTavernCharTooltipsEasierToGetTo(ref bool __runOriginal)
        {
            // Don't show other tooltips while the character tooltip is open and on the tavern hire screen (and not on the character tooltip)
            if (ToolTipManager.instance.characterTooltipOpen &&
                    TownInterfaceController.instance != null &&
                    TownInterfaceController.instance.hireCharacterRoster.activeInHierarchy &&
                    ToolTipManager.instance.inactivityTimerStarted)
            {
                __runOriginal = false;
            }
        }

        [HarmonyPatch(typeof(QuickCharacterInfo), nameof(QuickCharacterInfo.setFilteredInfo))]
        [HarmonyPostfix]
        public static void AddInjuryToName(int v, QuickCharacterInfo __instance)
        {
            if (v != 0)
            {
                return;
            }

            if (__instance.character.isInjured)
            {
                __instance.GetComponentInChildren<TMP_Text>().text = "<color=red>(I)</color> " + __instance.GetComponentInChildren<TMP_Text>().text;
            }
        }

        [HarmonyPatch(typeof(InterfaceController), nameof(InterfaceController.showCharacterInfo))]
        [HarmonyPostfix]
        public static void ShowExtraAp(Character c, InterfaceController __instance)
        {
            if (!showExtraAp.Value)
            {
                return;
            }
            for (int j = 2; j < c.stats.CurrAp; j++)
            {
                GameObject gameObject = Instantiate(__instance.actionPointPrefab, __instance.actionPointRoster.transform);
                gameObject.transform.localScale = Vector3.one;
                gameObject.GetComponent<Image>().sprite = __instance.fullAp;
            }
        }

        [HarmonyPatch(typeof(InterfaceController), nameof(InterfaceController.updatePartyCharacter))]
        [HarmonyPostfix]
        public static void ShowExtraApInRoster(int id, InterfaceController __instance)
        {
            if (!showExtraAp.Value)
            {
                return;
            }
            for (int j = 0; j < __instance.partyCharacters.Count; j++)
            {
                if (__instance.partyCharacters[j].stats.genetics.id == id)
                {
                    Stats stats = __instance.partyCharacters[j].stats;
                    GameObject partyPanel = __instance.partyCharacters[j].partyPanel;
                    Transform transform = partyPanel.GetComponentsInChildren<GridLayoutGroup>()[0].transform;
                    for (int l = 2; l < stats.CurrAp; l++)
                    {
                        GameObject gameObject = Instantiate(__instance.actionPointPrefab, transform.transform);
                        gameObject.transform.localScale = Vector3.one;
                        gameObject.GetComponent<Image>().sprite = __instance.fullAp;
                    }
                }
            }
        }
    }
}
