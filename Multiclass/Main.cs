using ModBase;
using ModMaker.Utils;
using System.Reflection;
using UnityEngine.SceneManagement;
using UnityModManagerNet;

namespace Multiclass
{
#if (DEBUG)
    [EnableReloading]
#endif
    static class Main
    {
        public static Core<Mod, Settings> Core;
        public static Menu Menu;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            Core = new Core<Mod, Settings>(modEntry, Assembly.GetExecutingAssembly());
            Menu = new Menu(modEntry, Assembly.GetExecutingAssembly());
            modEntry.OnToggle = OnToggle;
#if (DEBUG)
            modEntry.OnToggle = OnToggle;
            modEntry.OnUnload = Unload;
            return true;
        }

        static bool Unload(UnityModManager.ModEntry modEntry)
        {
            Menu = null;
            Core.Disable(modEntry, true);
            Core = null;
            return true;
        }
#else
            return true;
        }
#endif

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value)
            {
                Core.Enable(modEntry);
                Menu.Enable(modEntry, new Menus.TopMessage());
            }
            else
            {
                Menu.Disable(modEntry);
                Core.Disable(modEntry, false);
                ReflectionCache.Clear();
            }
            return value == Core.Enabled;
        }

#if DEBUG
        static private Scene _scene;

        static void OnUpdate(UnityModManager.ModEntry modEntry, float value)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene != _scene)
            {
                _scene = activeScene;
                Core.Debug($"[Scene] {_scene.name}");
            }
        }
#endif
    }
}
