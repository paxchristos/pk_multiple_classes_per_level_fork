using Harmony12;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Designers.Mechanics.Prerequisites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Multiclass.Main;

namespace Multiclass.HarmonyPatches
{
    internal static class IgnorePrerequisites
    {
        public static bool IsAvailable(Type type)
        {
            return Core.Enabled &&
                Core.Settings.toggleIgnorePrerequisites &&
                Core.Settings.ignoredPrerequisiteSet.Contains(type.FullName);
        }

        public static bool Enabled {
            get => Core.Settings.toggleIgnorePrerequisites;
            set => Core.Settings.toggleIgnorePrerequisites = value;
        }

        public static HashSet<string> IgnoredTypes => Core.Settings.ignoredPrerequisiteSet;

        [HarmonyPatch]
        static class Prerequisite_Check_Patch
        {
            [HarmonyTargetMethods]
            static IEnumerable<MethodBase> TargetMethods(HarmonyInstance instance)
            {
                foreach (MethodBase method in Core.Mod.PrerequisiteTypes.Select(type => type.GetMethod(nameof(Prerequisite.Check))))
                    yield return method;
                /*in Kingmaker.Blueprints.Classes.Preqrequisites.PrerequisiteLoreMaster CheckStat is an int, Check is a Bool.
                ORIGINAL yield return typeof(PrerequisiteLoreMaster).GetMethod(nameof(PrerequisiteLoreMaster.CheckStat)); 
                Fixed Below*/
                yield return typeof(PrerequisiteLoreMaster).GetMethod(nameof(PrerequisiteLoreMaster.Check));
                yield return typeof(PrerequisiteParametrizedFeature).GetMethod(nameof(PrerequisiteParametrizedFeature.CheckFeature));
                yield return typeof(PrerequisiteParametrizedWeaponSubcategory).GetMethod(nameof(PrerequisiteParametrizedWeaponSubcategory.CheckFeature));
                yield return typeof(PrerequisiteStatValue).GetMethod(nameof(PrerequisiteStatValue.CheckUnit));
                yield return typeof(PrerequisiteFullStatValue).GetMethod(nameof(PrerequisiteFullStatValue.CheckUnit));
            }

            [HarmonyPrefix]
            static bool Prefix(Prerequisite __instance, ref bool? __state)
            {
                if ((__state = IsAvailable(__instance.GetType())).Value)
                {
                    return false;
                }
                return true;
            }

            [HarmonyPostfix]
            static void Postfix(Prerequisite __instance, ref bool __result, ref bool? __state)
            {
                if (__state ?? IsAvailable(__instance.GetType()))
                {
                    __result = true;
                }
            }
        }
    }
}
