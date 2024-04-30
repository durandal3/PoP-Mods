
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace InterfaceTweaks
{
    public class DamagePreview
    {

        static GameObject previewObject = null;

        [HarmonyPatch(typeof(MouseController), nameof(MouseController.targetTile))]
        [HarmonyPostfix]
        public static void TargetTile(BattleTile t, MouseController __instance)
        {
            
            if (__instance.mode == MouseController.InputMode.characterSelected)
            {
                if (__instance.SelectedCharacter != null && t.character != null && t.character != __instance.SelectedCharacter && t.character.IsPlayerChar != __instance.SelectedCharacter.IsPlayerChar && __instance.SelectedCharacter.isValidAttackTarget(t.character))
                {
                    RemoveLabel();
                    previewObject = Object.Instantiate(TileHighlighter.instance.TileHighlightPrefab, WorldController.instance.getWorldCoordinates(t.X, t.Y), Quaternion.identity);
                    previewObject.name = "Damage Preview";
                    previewObject.GetComponent<SpriteRenderer>().sprite = null;

                    var p = previewObject.transform.position;
                    previewObject.transform.position = new Vector3(p.x, p.y + 0.8f, 50);


                    var text = previewObject.GetComponentInChildren<TMP_Text>();
                    text.enableWordWrapping = false;
                    text.alignment = TextAlignmentOptions.Center;
                    text.fontStyle = FontStyles.Bold;
                    text.text = GetAttackDamage(__instance.SelectedCharacter, t.character);
                    text.color = new Color(1, 0, 0);
                }
            }
        }

        [HarmonyPatch(typeof(TileHighlighter), nameof(TileHighlighter.unhighlightEffect))]
        [HarmonyPatch(typeof(TileHighlighter), nameof(TileHighlighter.unHighlightAll))]
        [HarmonyPostfix]
        public static void RemoveLabel()
        {
            if (previewObject != null)
            {
                Object.Destroy(previewObject);
                previewObject = null;
            }
        }


        private static string GetAttackDamage(Character attacker, Character target)
        {
            DamageType damageType = attacker.stats.ModifiedDamageType;
            if (!attacker.canAttack())
            {
                return "0";
            }
            if (!attacker.stats.MaxLust && !attacker.willBeDestroyed && !attacker.isStunned())
            {
                bool ignoreArmor = false;
                int num2 = attacker.stats.getModifiedStrength();
                if (attacker.stats.hasTrait(GeneticTraitType.Telekinetic))
                {
                    num2 = Mathf.Max(attacker.stats.getModifiedStrength(), attacker.stats.getModifiedMagicStrength());
                }
                if (!target.stats.isHumanoid && GameController.instance.hasTeamPerk(Species.Succubus, 5, attacker.IsPlayerChar))
                {
                    num2 = Mathf.Max(num2, attacker.stats.getModifiedLDmg());
                }
                if (attacker.stats.HalfLust)
                {
                    num2 = Mathf.Max(1, num2 / 2);
                }
                if (attacker.stats.hasTrait(GeneticTraitType.PiercingClaws))
                {
                    ignoreArmor = true;
                }
                return GetRecievedDamage(target, num2, damageType, ignoreArmor, false);
            }
            return "0";
        }

        private static string GetRecievedDamage(Character target, int val, DamageType type, bool ignoreArmor, bool ignoreShield)
        {
            if (val <= 0)
            {
                return "0";
            }
            float num = (float)val;
            float num2 = 1f;
            if (target.stats.HalfLust)
            {
                if (GameController.instance.hasTeamPerk(Species.Succubus, 4, !target.IsPlayerChar))
                {
                    num2 += 1f;
                }
                else
                {
                    num2 += 0.5f;
                }
            }
            if (target.effectIsAlreadyActive(global::Effect.Type.Cursed) && GameController.instance.hasTeamPerk(Species.ShadowGhost, 5, !target.IsPlayerChar))
            {
                num2 += 0.25f;
            }
            if (target.effectIsAlreadyActive(global::Effect.Type.Vulnerable))
            {
                num2 += 0.5f;
            }
            if (GameController.instance.hasGlobalPassive("Sensitivity", !target.IsPlayerChar) && (target.effectIsAlreadyActive(global::Effect.Type.Stunned) || target.effectIsAlreadyActive(global::Effect.Type.Frozen)))
            {
                num2 += 0.5f;
            }
            if (target.effectIsAlreadyActive(global::Effect.Type.Defending))
            {
                num2 -= 0.5f;
            }
            val = (int)(num * num2);
            if (target.stats.CurrMana == 0 && GameController.instance.hasTeamPerk(Species.Lymean, 5, !target.IsPlayerChar))
            {
                val *= 2;
            }

            string ret = "";
            if (!ignoreShield && target.currentShield > 0)
            {
                if (target.currentShield >= val)
                {
                    return val + "(s)";
                }
                if (target.currentShield > 0)
                {
                    var baseDamage = val;
                    val -= target.currentShield;
                    ret += (baseDamage - val) + "(s) + ";
                }
            }
            if (type == DamageType.Fire)
            {
                if (target.stats.OnTile.Type == BattleTile.TileType.water)
                {
                    val /= 2;
                }
                if (SaveController.instance.ChosenUnique == UniqueCharacter.Ember && target.IsPlayerChar && val > 1)
                {
                    val /= 2;
                }
            }
            else if (type == DamageType.Electric)
            {
                if (target.stats.OnTile.Type == BattleTile.TileType.water)
                {
                    val *= 2;
                }
                if (GameController.instance.hasGlobalPassive("LightningRod", target.IsPlayerChar))
                {
                    val /= 2;
                }
            }
            else if (type == DamageType.True)
            {
                ignoreArmor = true;
            }

            float elementalDefMod = target.stats.getElementalDefMod(type);
            int num3 = (int)((float)val * elementalDefMod);
            int num4 = GetRecievedDamageLoseHp(target, num3, ignoreArmor);

            ret += num4;
            if (GameController.instance.hasGlobalPassive("GreenAgility", target.IsPlayerChar) && target.stats.hasTrait(SpecialTraits.GreenSkin) && global::UnityEngine.Random.Range(0, 4) == 0)
            {
                LogController.instance.addMessage(target.stats.CharName + " avoided dmg");
                return ret + " (may dodge)";
            }
            else if (GameController.instance.hasGlobalPassive("DodgeMastery", target.IsPlayerChar) && global::UnityEngine.Random.Range(0, 10) == 0)
            {
                LogController.instance.addMessage(target.stats.CharName + " avoided dmg");
                return ret + " (may dodge)";
            }
            return ret;
        }

        private static int GetRecievedDamageLoseHp(Character target, int i, bool ignoreArmor = false)
        {
            if (target.stats.Invuln)
            {
                LogController.instance.addMessage(target.stats.CharName + " is invulnerable");
                return 0;
            }
            int j = ignoreArmor ? i : (i - target.stats.getModifiedArmor());
            j = Mathf.Max(1, j);
            // TODO note shadows will be removed to reduce damage?
            // if (GameController.instance.hasTeamPerk(Species.ShadowGhost, 4, target.IsPlayerChar))
            // {
            // }
            if (j > 0 && GameController.instance.hasGlobalPassive("LightBarrier", target.IsPlayerChar) && target.getEffectDuration(global::Effect.Type.Enlightened) > 0)
            {
                j /= 2;
            }
            if (target.stats.hasTrait(GeneticTraitType.Robust) && j > target.stats.getModifiedMaxHp() / 4)
            {
                j = target.stats.getModifiedMaxHp() / 4;
            }
            if (target.stats.hasTrait(SpecialTraits.CrystalArmor) && j > 0)
            {
                j = 1;
            }
            // TODO note mana absorption?
            if (target.effectIsAlreadyActive(global::Effect.Type.ShadowForm))
            {
            //     if (j < target.stats.CurrMana)
            //     {
            //         j = 0;
            //     }
            //     else
            //     {
            //         j -= target.stats.CurrMana;
            //     }
            }
            // else if (target.stats.hasTrait(GeneticTraitType.Ethereal))
            // {
                // if (Mathf.Max(1, j / 2) < target.stats.CurrMana)
                // {
                //     j = Mathf.Max(1, j / 2);
                // }
                // else
                // {
                //     j -= target.stats.CurrMana;
                // }
            // }
            return j;
        }
    }
}
