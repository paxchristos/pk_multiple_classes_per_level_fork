using ModMaker.Utils;
using Multiclass.HarmonyPatches;
using UnityEngine;
using UnityModManagerNet;
using static ModMaker.Extensions.RichText;
using static Multiclass.Main;

namespace Multiclass.Menus
{
    public class MultipleClassesOptions : ModBase.Menu.IToggleablePage
    {
        public string Name => "Options";

        public int Priority => 200;

        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (Core == null || !Core.Enabled)
                return;

            GUIStyle style = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };

            TakeHighestHitDie.Enabled =
                GUIHelper.ToggleButton(TakeHighestHitDie.Enabled,
                "Take Highest Hit Die Of The Selected Classes" +
                " (Excluding favored class HP bonus)".Color(RGBA.silver) +
                " (Recommended)".Color(RGBA.cyan), style);
            TakeHighestSkillPoints.Enabled =
                GUIHelper.ToggleButton(TakeHighestSkillPoints.Enabled,
                "Take Highest Skill Points Of The Selected Classes" +
                " (Recommended)".Color(RGBA.cyan), style);
            TakeHighestBaseStat.EnabledBAB =
                GUIHelper.ToggleButton(TakeHighestBaseStat.EnabledBAB,
                "Take Highest BAB Of The Selected Classes" +
                " (Recommended)".Color(RGBA.cyan), style);
            TakeHighestBaseStat.EnabledSaveRecalculation =
                GUIHelper.ToggleButton(TakeHighestBaseStat.EnabledSaveRecalculation,
                "Take Highest Saves By Recalculation" +
                " (Similar to but different from the Gestalt rules)".Color(RGBA.silver) +
                " (Recommended)".Color(RGBA.cyan),
                () => TakeHighestBaseStat.EnabledSaveAlwaysHigh = false, null, style);
            TakeHighestBaseStat.EnabledSaveAlwaysHigh =
                GUIHelper.ToggleButton(TakeHighestBaseStat.EnabledSaveAlwaysHigh,
                "Take Highest Saves By Character Level" +
                " (Regardless of the selected classes)".Color(RGBA.silver),
                () => TakeHighestBaseStat.EnabledSaveRecalculation = false, null, style);

            GUILayout.Label(" * Only available with multiple classes on.".Color(RGBA.silver));

            GUILayout.Space(10);

            RecalculateCasterLevelOnLevelingUp.Enabled =
                GUIHelper.ToggleButton(RecalculateCasterLevelOnLevelingUp.Enabled,
                "Recalculate Caster Level On Leveling Up" +
                " (Even if you didn't pick any specific caster class at the new character level)".Color(RGBA.silver) +
                " (Recommended)".Color(RGBA.cyan), style);
            RestrictCasterLevelToCharacterLevel.Enabled =
                GUIHelper.ToggleButton(RestrictCasterLevelToCharacterLevel.Enabled,
                "Restrict Caster Level To Current Character Level" +
                " (It takes effect on leveling up, affecting spells aquired, not immediate or temporary)".Color(RGBA.silver) +
                " (Recommended)".Color(RGBA.cyan), style);
            RestrictCasterLevelToCharacterLevelTemporary.Enabled =
                GUIHelper.ToggleButton(RestrictCasterLevelToCharacterLevelTemporary.Enabled,
                "Restrict Caster Level To Current Character Level Temporary" +
                " (Does not affect the number of new spells known through leveling up)".Color(RGBA.silver) +
                " (Recommended)".Color(RGBA.cyan), style);
            RestrictClassLevelForPrerequisitesToCharacterLevel.Enabled =
                GUIHelper.ToggleButton(RestrictClassLevelForPrerequisitesToCharacterLevel.Enabled,
                "Restrict Class Level For Prerequisites To Current Character Level" +
                " (Recommended)".Color(RGBA.cyan), style);

            GUILayout.Label(" * Only available for the player faction.".Color(RGBA.silver));

            GUILayout.Space(10);

            FixFavoredClassHP.Enabled =
                GUIHelper.ToggleButton(FixFavoredClassHP.Enabled,
                "Fix Favored Class HP Bonus" +
                " (Define the highest level class as favored class)".Color(RGBA.silver), () =>
                {
                    AlwaysReceiveFavoredClassHPExceptPrestige.Enabled = false;
                    AlwaysReceiveFavoredClassHP.Enabled = false;
                }, null, style);
            AlwaysReceiveFavoredClassHPExceptPrestige.Enabled =
                GUIHelper.ToggleButton(AlwaysReceiveFavoredClassHPExceptPrestige.Enabled,
                "Always Receive Favored Class HP Bonus" +
                " (Excluding prestige classes)".Color(RGBA.silver) +
                " (Compatible with Eldritch Arcana﻿)".Color(RGBA.silver) +
                " (Recommended) ".Color(RGBA.cyan), () =>
                {
                    FixFavoredClassHP.Enabled = false;
                    AlwaysReceiveFavoredClassHP.Enabled = false;
                }, null, style);
            AlwaysReceiveFavoredClassHP.Enabled =
                GUIHelper.ToggleButton(AlwaysReceiveFavoredClassHP.Enabled,
                "Always Receive Favored Class HP Bonus" +
                " (Including prestige classes)".Color(RGBA.silver), () =>
                {
                    FixFavoredClassHP.Enabled = false;
                    AlwaysReceiveFavoredClassHPExceptPrestige.Enabled = false;
                }, null, style);

            GUILayout.Label(" * Available for all units.".Color(RGBA.silver));
        }
    }
}
