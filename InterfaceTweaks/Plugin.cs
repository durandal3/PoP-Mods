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

        public static void PrintAllComponents(GameObject gameObject)
        {
            // Debug util
            foreach (var c in gameObject.GetComponentsInChildren<object>())
            {
                Log.LogWarning(c.GetType() + ": " + c);
            }
        }

        private static readonly List<Harmony> _harmony = [];

        public static ConfigEntry<bool> addSortButtons;
        public static ConfigEntry<int> defaultChallengeRank;
        public static ConfigEntry<bool> allowChangingWorldMod;
        public static ConfigEntry<bool> allowChangingWorldModToAnything;
        public static ConfigEntry<bool> showDamagePreview;
        public static ConfigEntry<bool> showUnlockableTraits;
        public static ConfigEntry<bool> highlightUnlockableSpecies;

        private void Awake()
        {
            Log = base.Logger;

            addSortButtons = Config.Bind("General", "AddSortButtons", true,
                    "Add sort buttons to various lists");
            defaultChallengeRank = Config.Bind("General", "DefaultChallengeRank", -1,
                    "Default Challenge rank to set when making a new game. -1 for default behaviour (1 less than max). If higher than your allowed max, will set to max (so set to e.g. 1000 to always set to max possible)");
            allowChangingWorldMod = Config.Bind("General", "AllowChangingWorldMod", true,
                    "Allowing changing the World Modifier during new game setup");
            allowChangingWorldModToAnything = Config.Bind("General", "AllowChangingWorldModToAnything", false,
                    "Allows setting the World Modifier to anything, not just those normally available through the Creation ending");
            showDamagePreview = Config.Bind("General.Combat", "ShowDamagePreview", true,
                    "Shows a preview of damage dealt when attacking (only for basic attacks). White number = shield, Black number = shadow tiles, Blue number = mana, Red number = health");
            showUnlockableTraits = Config.Bind("General.Unlockables", "ShowUnlockableTraits", false,
                    "Whether to show unlockable traits in a tooltip (Top-left of gallery and new game \"GeneticTraits\" on starter selection screen)");
            highlightUnlockableSpecies = Config.Bind("General.Unlockables", "HighlightUnlockableSpecies", false,
                    "Whether to highlight unlockable species (in portals, combats, tavern hires, and character overview)");


            _harmony.Add(Harmony.CreateAndPatchAll(typeof(Plugin)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(DamagePreview)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(ExtraApDisplay)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(NewGameTweaks)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(ScrollingFixes)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(SexTrainingFilter)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(UnlockIndicator)));

            _harmony.Add(Harmony.CreateAndPatchAll(typeof(SelectionMenuSorter)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(McTraitSorter)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(McProfessionSorter)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(StarterTraitSorter)));
        }

        private void OnDestroy()
        {
            foreach (var item in _harmony)
            {
                item.UnpatchSelf();
            }
            DamagePreview.OnDestroy();
            SelectionMenuSorter.OnDestroy();
            McTraitSorter.OnDestroy();
            McProfessionSorter.OnDestroy();
            StarterTraitSorter.OnDestroy();
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


    }
}
