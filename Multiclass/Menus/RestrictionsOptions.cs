using ModMaker.Utils;
using Multiclass.HarmonyPatches;
using UnityEngine;
using UnityModManagerNet;
using static ModMaker.Extensions.RichText;
using static Multiclass.Main;

namespace Multiclass.Menus
{
    public class RestrictionsOptions : ModBase.Menu.IToggleablePage
    {
        public string Name => "Restrictions";

        public int Priority => 500;

        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (Core == null || !Core.Enabled)
                return;

            GUIStyle style = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };

            IgnoreSpellbookAlignmentRestriction.Enabled =
                GUIHelper.ToggleButton(IgnoreSpellbookAlignmentRestriction.Enabled,
                "Ignore Spellbook Alignment Restriction", style);

            GUILayout.Space(10);

            IgnoreAbilityCasterCheckers.Enabled =
                GUIHelper.ToggleTypeList(IgnoreAbilityCasterCheckers.Enabled,
                "Ignore Ability Caster Checkers",
                IgnoreAbilityCasterCheckers.IgnoredTypes,
                Core.Mod.AbilityCasterCheckerTypes,
                style);

            GUILayout.Space(10);

            IgnoreActivatableAbilityRestrictions.Enabled =
                GUIHelper.ToggleTypeList(IgnoreActivatableAbilityRestrictions.Enabled,
                "Ignore Activatable Ability Restrictions",
                IgnoreActivatableAbilityRestrictions.IgnoredTypes,
                Core.Mod.ActivatableAbilityRestrictionTypes,
                style);

            GUILayout.Space(10);

            IgnoreEquipmentRestrictions.Enabled =
                GUIHelper.ToggleTypeList(IgnoreEquipmentRestrictions.Enabled,
                "Ignore Equipment Restrictions",
                IgnoreEquipmentRestrictions.IgnoredTypes,
                Core.Mod.EquipmentRestrictionTypes,
                style);

            GUILayout.Space(10);

            IgnoreBuildingRestrictions.Enabled =
                GUIHelper.ToggleTypeList(IgnoreBuildingRestrictions.Enabled,
                "Ignore Building Restrictions",
                IgnoreBuildingRestrictions.IgnoredTypes,
                Core.Mod.BuildingRestrictionTypes,
                style);

            GUILayout.Label(" * Only available for the player faction.".Color(RGBA.silver));
        }
    }
}
