
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace InterfaceTweaks
{
    public class DamagePreview
    {

        static GameObject previewObject = null;

        public void OnDestroy()
        {
            RemoveLabel();
        }

        [HarmonyPatch(typeof(MouseController), nameof(MouseController.targetTile))]
        [HarmonyPostfix]
        public static void TargetTile(BattleTile t, MouseController __instance)
        {
            if (!Plugin.showDamagePreview.Value)
            {
                return;
            }

            if (__instance.mode == MouseController.InputMode.characterSelected)
            {
                if (__instance.SelectedCharacter != null && t.character != null && t.character != __instance.SelectedCharacter)
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
                    text.fontSize += 10;
                    text.text = GetAttackDamage(__instance.SelectedCharacter, t.character).ToString();

                    text.color = new Color(1, 1, 1);
                    text.fontSharedMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
                    text.outlineWidth = 0.125f;
                    // text.outlineColor = new Color(0.5f, 0.5f, 0.5f);
                    text.outlineColor = new Color(1f, 1f, 1f);
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


        private static DamageInfo GetAttackDamage(Character attacker, Character target)
        {
            var info = new DamageInfo();
            DamageType damageType = attacker.stats.ModifiedDamageType;
            if (!attacker.canAttack())
            {
                return info;
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
                GetRecievedDamage(info, target, num2, damageType, ignoreArmor, false);
            }
            return info;
        }

        private static void GetRecievedDamage(DamageInfo info, Character target, int val, DamageType type, bool ignoreArmor, bool ignoreShield)
        {
            if (val <= 0)
            {
                return;
            }
            if (GameController.instance.hasGlobalPassive("GreenAgility", target.IsPlayerChar) && target.stats.hasTrait(SpecialTraits.GreenSkin))
            {
                info.dodgeChance += 25;
            }
            else if (GameController.instance.hasGlobalPassive("DodgeMastery", target.IsPlayerChar))
            {
                info.dodgeChance += 10;
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

            if (!ignoreShield && target.currentShield > 0)
            {
                if (target.currentShield >= val)
                {
                    info.shieldDamage += val;
                    return;
                }
                if (target.currentShield > 0)
                {
                    val -= target.currentShield;
                    info.shieldDamage += target.currentShield;
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
            GetRecievedDamageLoseHp(info, target, num3, ignoreArmor);
        }

        private static void GetRecievedDamageLoseHp(DamageInfo info, Character target, int i, bool ignoreArmor = false)
        {
            if (target.stats.Invuln)
            {
                info.hpDamage = 0;
                return;
            }
            int j = ignoreArmor ? i : (i - target.stats.getModifiedArmor());
            j = Mathf.Max(1, j);
            if (GameController.instance.hasTeamPerk(Species.ShadowGhost, 4, target.IsPlayerChar))
            {
                int shadowTiles = 0;
                foreach (KeyValuePair<Point, BattleTile> keyValuePair in WorldController.instance.World.tiles)
                {
                    if (keyValuePair.Value.Type == BattleTile.TileType.shadow && keyValuePair.Value.character == null)
                    {
                        shadowTiles++;
                    }
                }
                if (shadowTiles > j)
                {
                    info.shadowsDamage = j;
                    j = 0;
                }
                else
                {
                    info.shadowsDamage = shadowTiles;
                    j -= shadowTiles;
                }
            }
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
            if (target.effectIsAlreadyActive(global::Effect.Type.ShadowForm))
            {
                if (j < target.stats.CurrMana)
                {
                    info.mpDamage += j;
                    j = 0;
                }
                else
                {
                    info.mpDamage += target.stats.CurrMana;
                    j -= target.stats.CurrMana;
                }
            }
            else if (target.stats.hasTrait(GeneticTraitType.Ethereal))
            {
                if (Mathf.Max(1, j / 2) < target.stats.CurrMana)
                {
                    j = Mathf.Max(1, j / 2);
                    info.mpDamage += j;
                }
                else
                {
                    info.mpDamage += target.stats.CurrMana;
                    j -= target.stats.CurrMana;
                }
            }
            info.hpDamage += j;
        }

        private class DamageInfo
        {
            public int shieldDamage = 0;
            public int shadowsDamage = 0;
            public int mpDamage = 0;
            public int hpDamage = 0;

            public int dodgeChance = 0;

            public override string ToString()
            {
                var strings = new List<string>();
                if (shieldDamage > 0)
                {
                    strings.Add("<color=#FFFFFF>" + shieldDamage + "</color>");
                }
                if (shadowsDamage > 0)
                {
                    strings.Add("<color=#000000>" + shadowsDamage + "</color>");
                }
                if (mpDamage > 0)
                {
                    strings.Add(Colors.getColor("mana") + mpDamage + "</color>");
                }
                if (hpDamage > 0)
                {
                    strings.Add(Colors.getColor("red") + hpDamage + "</color>");
                }

                string dodgeString = dodgeChance > 0 ? " <size=50%>(" + dodgeChance + "% dodge)</size>" : "";
                return string.Join(" + ", strings) + dodgeString;
            }
        }
    }
}
