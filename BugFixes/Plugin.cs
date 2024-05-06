using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BugFixes
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        private static Harmony _harmony;

        private void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FarmPairing), nameof(FarmPairing.breedCharacters))]
        static IEnumerable<CodeInstruction> BreedCharactersPussyInterest(IEnumerable<CodeInstruction> instructions)
        {
            // FarmPairing.breedCharacters has a couple places like:
            // if (this.a.character.hasCock())
            // {
            //     this.b.character.changeCockInterest(global::UnityEngine.Random.Range(num8 - 3, num8 + 3));
            // }
            // if (this.a.character.hasCock())
            // {
            //     this.b.character.changePussyInterest(global::UnityEngine.Random.Range(num8 - 3, num8 + 3));
            // }
            // Fix the second check for raising pussy interest to check for pussy, instead of cock


            var hasCockMethodCall = new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Stats), "hasCock"));
            var hasPussyMethod = AccessTools.Method(typeof(Stats), "hasPussy");
            return new CodeMatcher(instructions)
                    .MatchForward(false, hasCockMethodCall)
                    .Advance(1)
                    .MatchForward(false, hasCockMethodCall)
                    .SetOperandAndAdvance(hasPussyMethod)
                    .MatchForward(false, hasCockMethodCall)
                    .Advance(1)
                    .MatchForward(false, hasCockMethodCall)
                    .SetOperandAndAdvance(hasPussyMethod)
                    .InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Brothel), nameof(Brothel.getRandomRichness))]
        static IEnumerable<CodeInstruction> GetRandomRichness(IEnumerable<CodeInstruction> instructions)
        {
            // Brothel.getRandomRichness has a couple places like:
            // int num2 = this.customers.poor.getTotal() * 100 / num;
            // int num3 = this.customers.poor.getTotal() * 100 / num;
            // Fix the second check for to get average customers


            var poorCustomerFieldLoad = new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PopulationGroup), "poor"));
            var averageCustomerField = AccessTools.Field(typeof(PopulationGroup), "average");
            return new CodeMatcher(instructions)
                    .MatchForward(false, poorCustomerFieldLoad)
                    .Advance(1)
                    .MatchForward(false, poorCustomerFieldLoad)
                    .Advance(1)
                    .MatchForward(false, poorCustomerFieldLoad)
                    .SetOperandAndAdvance(averageCustomerField)
                    .MatchForward(false, poorCustomerFieldLoad)
                    .Advance(1)
                    .MatchForward(false, poorCustomerFieldLoad)
                    .Advance(1)
                    .MatchForward(false, poorCustomerFieldLoad)
                    .SetOperandAndAdvance(averageCustomerField)
                    .InstructionEnumeration();
        }
    }
}
