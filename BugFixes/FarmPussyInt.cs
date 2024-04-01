using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BugFixes
{
    public class Patches
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FarmPairing))]
        [HarmonyPatch(nameof(FarmPairing.breedCharacters))]
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


            var hasCockMethod = new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Stats), "hasCock"));
            return new CodeMatcher(instructions)
                    .MatchForward(false, hasCockMethod)
                    .Advance(1)
                    .MatchForward(false, hasCockMethod)
                    .SetOperandAndAdvance(AccessTools.Method(typeof(Stats), "hasPussy"))
                    .MatchForward(false, hasCockMethod)
                    .Advance(1)
                    .MatchForward(false, hasCockMethod)
                    .SetOperandAndAdvance(AccessTools.Method(typeof(Stats), "hasPussy"))
                    .InstructionEnumeration();
        }
    }
}
