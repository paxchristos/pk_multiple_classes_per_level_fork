using ModMaker.Utils;
using Multiclass.HarmonyPatches;
using UnityEngine;
using UnityModManagerNet;
using static ModMaker.Extensions.RichText;
using static Multiclass.Main;

namespace Multiclass.Menus
{
    public class PrerequisitesOptions : ModBase.Menu.IToggleablePage
    {
        public string Name => "Prerequisites";

        public int Priority => 400;

        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (Core == null || !Core.Enabled)
                return;

            GUIStyle style = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };

            IgnorePrerequisites.Enabled =
                GUIHelper.ToggleTypeList(IgnorePrerequisites.Enabled,
                "Ignore Prerequisites",
                IgnorePrerequisites.IgnoredTypes,
                Core.Mod.PrerequisiteTypes,
                style);

            GUILayout.Label(" * Available for all units.".Color(RGBA.silver));
        }
    }
}
