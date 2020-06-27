using UnityEngine;
using UnityModManagerNet;
using static ModMaker.Extensions.RichText;
//using static Multiclass.Main;

namespace Multiclass.Menus
{
    public class TopMessage : ModBase.Menu.IPage
    {
        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.Label(" * Most of the mod features should be toggled before selecting any class in the level up screen.".Color(RGBA.silver));

            //if (Core.Storage.LibraryObject == null)
            //{
            //    GUILayout.Space(10);
            //    GUILayout.Label(" * Game Loading...".Bold().Color(NamedRGBA.yellow));
            //}
        }
    }
}
