using System;
using System.Collections.Generic;
using System.Reflection;
using Effect;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InterfaceTweaks
{
    public class UnlockIndicator
    {

        static void OnDestroy()
        {
        }

        static bool IsUnlockableTrait(GeneticTrait trait)
        {
            return !trait.unlocked && trait.unlockInfo != "Can't be unlocked.";
        }

        static bool HasUnlockableTrait(List<GeneticTrait> traits)
        {
            foreach (var trait in traits)
            {
                if (!trait.unlocked && trait.unlockInfo != "Can't be unlocked.")
                {
                    return true;
                }
            }
            return false;
        }

        static bool HasUnlocks(Stats stats)
        {
            // Locked species, but don't highlight tokens (vines/drones/etc...)
            if (!SaveController.instance.hasSpeciesInGallery(stats.species) && !stats.IsToken)
            {
                return true;
            }
            return false;
        }

        [HarmonyPatch(typeof(OWParty), nameof(OWParty.act2))]
        [HarmonyPostfix]
        public static void RefreshAura(OWParty __instance)
        {
            __instance.updateAura();
        }

        [HarmonyPatch(typeof(OWParty), nameof(OWParty.updateAura))]
        [HarmonyPrefix]
        public static void UpdateAura(OWParty __instance, ref bool __runOriginal)
        {
            if (!Plugin.highlightUnlockableSpecies.Value)
            {
                return;
            }
            bool hasUnlocks = false;
            foreach (var character in __instance.info.characters)
            {
                if (!SaveController.instance.hasSpeciesInGallery(character.species))
                {
                    hasUnlocks = true;
                    break;
                }
            }

            if (hasUnlocks)
            {
                __instance.sprites[1].color = new(0.8f, 0f, 0.8f, 0.6f);
                __runOriginal = false;
            }
        }

        private static void UpdateBattleAura(Character character)
        {
            if (!Plugin.highlightUnlockableSpecies.Value)
            {
                return;
            }
            if (character.IsPlayerChar)
            {
                return;
            }

            var renderer = character.GetComponentsInChildren<SpriteRenderer>()[1];
            if (HasUnlocks(character.stats))
            {
                renderer.sprite = TileHighlighter.instance.TileHighlightPrefab.GetComponent<SpriteRenderer>().sprite;
                renderer.color = new(0.8f, 0f, 0.8f, 0.6f);
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.setUI))]
        [HarmonyPostfix]
        public static void UpdateBattleAuraAtStart(Character __instance)
        {
            UpdateBattleAura(__instance);
        }

        [HarmonyPatch(typeof(Defending), nameof(Defending.removeEffect))]
        [HarmonyPostfix]
        public static void UpdateBattleAuraOnDefendRemove(Defending __instance)
        {
            FieldInfo characterField = typeof(Defending).GetField("character", BindingFlags.NonPublic | BindingFlags.Instance);
            UpdateBattleAura((Character)characterField.GetValue(__instance));
        }

        [HarmonyPatch(typeof(TownInterfaceController), nameof(TownInterfaceController.showNoticeBoardTab))]
        [HarmonyPostfix]
        public static void ShowNoticeBoardTab(int index, TownInterfaceController __instance)
        {
            if (!Plugin.highlightUnlockableSpecies.Value)
            {
                return;
            }
            // Highlight characters in the tavern that have unlockables
            if (index == 8) // 8 is the index of the hire tab
            {
                int count = __instance.hireCharacterRoster.transform.childCount;
                int charCount = SaveController.instance.tavernCharacterOffers.Count;
                for (int m = 0; m < charCount; m++)
                {
                    var panel = __instance.hireCharacterRoster.transform.GetChild(count - charCount + m).gameObject;
                    var image = panel.GetComponentsInChildren<Image>()[1];
                    Stats s = SaveController.instance.tavernCharacterOffers[m].stats;

                    if (HasUnlocks(s))
                    {
                        image.color = new(0.8f, 0f, 0.8f, 1.0f);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(QuickCharacterInfo), nameof(QuickCharacterInfo.setFilteredInfo))]
        [HarmonyPostfix]
        public static void ShowLockedSpeciesInCharacterOverview(int v, QuickCharacterInfo __instance)
        {
            if (!Plugin.highlightUnlockableSpecies.Value)
            {
                return;
            }
            if (v != 0)
            {
                return;
            }

            if (HasUnlocks(__instance.character))
            {
                __instance.GetComponentInChildren<TMP_Text>().text = "<color=green>Gal</color> " + __instance.GetComponentInChildren<TMP_Text>().text;
            }
        }


        static bool showingTraitTooltip = false;

        [HarmonyPatch(typeof(ToolTipManager), nameof(ToolTipManager.showSetTooltip))]
        [HarmonyPrefix]
        public static void SetShowingTraitTooltip(string s)
        {
            if (s == "genetictraits")
            {
                showingTraitTooltip = true;
                return;
            }
        }

        [HarmonyPatch(typeof(ToolTipManager), nameof(ToolTipManager.showTooltip))]
        [HarmonyPrefix]
        public static void ShowTraitTooltip(ref ToolTipManager.Position pos, ref string text)
        {
            if (showingTraitTooltip)
            {
                showingTraitTooltip = false;
                if (!Plugin.showUnlockableTraits.Value)
                {
                    return;
                }
                pos = ToolTipManager.Position.BotLeft;
                int count = 0;
                foreach (var t in Enum.GetValues(typeof(GeneticTraitType)))
                {
                    var gt = GeneticTrait.getTrait((GeneticTraitType) t);
                    if (IsUnlockableTrait(gt))
                    {
                        text += "\n" + gt.getName() + ": " + gt.unlockInfo;
                        count++;
                        if (count > 15)
                        {
                            text += "\n...";
                            return;
                        }
                    }
                }
            }
        }
    }
}
