using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TMPro;
using UnityEngine.UI;

namespace InterfaceTweaks
{
    public class NewGameTweaks
    {
        [HarmonyPatch(typeof(CharacterCreationManager), "Start")]
        [HarmonyPostfix]
        public static void DefaultChallengeLevel(CharacterCreationManager __instance)
        {
            int level = Plugin.defaultChallengeRank.Value;

            if (level >= 0)
            {
                var getHighestPossibleAscensionMethod = typeof(CharacterCreationManager).GetMethod("getHighestPossibleAscension", BindingFlags.NonPublic | BindingFlags.Instance);
                typeof(CharacterCreationManager).GetField("ascensionSelected", BindingFlags.NonPublic | BindingFlags.Instance)
                        .SetValue(__instance, Math.Min(level, (int)getHighestPossibleAscensionMethod.Invoke(__instance, [])));

                __instance.updateAscension();
            }
        }

        [HarmonyPatch(typeof(CharacterCreationManager), "Start")]
        [HarmonyPostfix]
        public static void AllowChangingWorldMod(CharacterCreationManager __instance)
        {
            if (!Plugin.allowChangingWorldMod.Value)
            {
                return;
            }
            if (!__instance.worldModifier.activeSelf)
            {
                return; // Not unlocked TODO also the case when world mod is none...
            }
            if (__instance.worldModifier.GetComponent<Button>() != null)
            {
                return; // Already added
            }
            var button = __instance.worldModifier.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                switch (SaveController.instance.WorldModifier)
                {
                    case PermanentWorldModifier.None:
                    default:
                        SaveController.instance.WorldModifier = PermanentWorldModifier.Growth;
                        break;
                    case PermanentWorldModifier.Growth:
                        SaveController.instance.WorldModifier = PermanentWorldModifier.Diversity;
                        break;
                    case PermanentWorldModifier.Diversity:
                        SaveController.instance.WorldModifier = PermanentWorldModifier.Intelligence;
                        break;
                    case PermanentWorldModifier.Intelligence:
                        SaveController.instance.WorldModifier = PermanentWorldModifier.Plentiful;
                        break;
                    case PermanentWorldModifier.Plentiful:
                        SaveController.instance.WorldModifier = PermanentWorldModifier.Equality;
                        break;
                    case PermanentWorldModifier.Equality:
                        SaveController.instance.WorldModifier = PermanentWorldModifier.Historic;
                        break;
                    case PermanentWorldModifier.Historic:
                        if (Plugin.allowChangingWorldModToAnything.Value)
                        {
                            SaveController.instance.WorldModifier = PermanentWorldModifier.TwinFusion;
                        }
                        else
                        {
                            SaveController.instance.WorldModifier = PermanentWorldModifier.None;
                        }
                        break;

                    // Not normally available:
                    case PermanentWorldModifier.TwinFusion:
                        SaveController.instance.WorldModifier = PermanentWorldModifier.Empowering;
                        break;
                    case PermanentWorldModifier.Empowering:
                        SaveController.instance.WorldModifier = PermanentWorldModifier.SexBound;
                        break;
                    case PermanentWorldModifier.SexBound:
                        SaveController.instance.WorldModifier = PermanentWorldModifier.None;
                        break;
                    case PermanentWorldModifier.FarmFocus: // Skipping this one - it doesn't do anything
                        SaveController.instance.WorldModifier = PermanentWorldModifier.None;
                        break;
                }

                __instance.worldModifier.GetComponentInChildren<TMP_Text>().text = "WorldModifier (click to change):\n<color=orange>" + SaveController.instance.WorldModifier + "</color>";
                ToolTipManager.instance.hideToolTip();
                __instance.showWorldModifierTooltip();
            });
            __instance.worldModifier.GetComponentInChildren<TMP_Text>().text = "WorldModifier (click to change):\n<color=orange>" + SaveController.instance.WorldModifier + "</color>";
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CharacterCreationManager), nameof(CharacterCreationManager.confirm))]
        public static IEnumerable<CodeInstruction> ConfirmDontResetWorldMod(IEnumerable<CodeInstruction> instructions)
        {
            // Change CharacterCreationManager.confirm to not reset the world mod back to the one in permanentInfo
            // Make it just set it to itself again (removing everything doesn't work because labels?)

            return new CodeMatcher(instructions)
                    .MatchForward(false, 
                            new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(SaveController), nameof(SaveController.instance))),
                            new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(SaveController), nameof(SaveController.instance))),
                            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(SaveController), nameof(SaveController.instance.permanentInfo))),
                            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PermanentInfo), nameof(SaveController.instance.permanentInfo.permanentWorldModifier))),
                            new CodeMatch(OpCodes.Callvirt)
                    )
                    .Advance(2)
                    .SetOperandAndAdvance(AccessTools.Field(typeof(SaveController), "worldModifier"))
                    .RemoveInstruction()
                    .InstructionEnumeration();
        }
    }
}
