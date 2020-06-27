using ModMaker.Utils;
using Multiclass.HarmonyPatches;
using UnityEngine;
using UnityModManagerNet;
using static ModMaker.Extensions.RichText;
using static Multiclass.Main;

namespace Multiclass.Menus
{
    public class LevelUpOptions : ModBase.Menu.IToggleablePage
    {
        public string Name => "Level Up";

        public int Priority => 300;

        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (Core == null || !Core.Enabled)
                return;

            GUIStyle style = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };

            LockCharacterLevel.Enabled =
                GUIHelper.ToggleButton(LockCharacterLevel.Enabled,
                "Lock Character Level" + " (Unavailable on CharGen)".Color(RGBA.silver), style);

            GUILayout.Space(10);

            IgnoreAlignmentRestriction.Enabled =
                GUIHelper.ToggleButton(IgnoreAlignmentRestriction.Enabled,
                "Ignore Alignment Restriction" + " (CharGen)".Color(RGBA.silver), style);
            IgnoreAttributeStatCapChargen.Enabled =
                GUIHelper.ToggleButton(IgnoreAttributeStatCapChargen.Enabled,
                "Ignore Attribute Cap" + " (CharGen)".Color(RGBA.silver), style);
            IgnoreAttributePointsRemainingChargen.Enabled =
                GUIHelper.ToggleButton(IgnoreAttributePointsRemainingChargen.Enabled,
                "Ignore Attribute Points Remaining" + " (CharGen)".Color(RGBA.silver), style);

            GUILayout.Space(10);

            IgnoreAttributePointsRemaining.Enabled =
                GUIHelper.ToggleButton(IgnoreAttributePointsRemaining.Enabled,
                "Ignore Attribute Points Remaining", style);
            IgnoreSkillCap.Enabled =
                GUIHelper.ToggleButton(IgnoreSkillCap.Enabled,
                "Ignore Skill Cap", style);
            IgnoreSkillPointsRemaining.Enabled =
                GUIHelper.ToggleButton(IgnoreSkillPointsRemaining.Enabled,
                "Ignore Skill Points Remaining", style);

            GUILayout.Label(" * Only available for the player faction.".Color(RGBA.silver));
        }
    }
}
