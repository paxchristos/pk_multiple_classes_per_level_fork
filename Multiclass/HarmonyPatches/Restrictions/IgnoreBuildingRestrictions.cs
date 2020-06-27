using Harmony12;
using Kingmaker.Kingdom.Settlements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Multiclass.Main;

namespace Multiclass.HarmonyPatches
{
    internal static class IgnoreBuildingRestrictions
    {
        public static bool IsAvailable(Type type)
        {
            return Core.Enabled &&
                Core.Settings.toggleIgnoreBuildingRestrictions &&
                Core.Settings.ignoredBuildingRestrictionSet.Contains(type.FullName);
        }

        public static bool Enabled {
            get => Core.Settings.toggleIgnoreBuildingRestrictions;
            set => Core.Settings.toggleIgnoreBuildingRestrictions = value;
        }

        public static HashSet<string> IgnoredTypes => Core.Settings.ignoredBuildingRestrictionSet;

        [HarmonyPatch]
        static class BuildingRestriction_Check_Patch
        {
            static IEnumerable<MethodBase> TargetMethods(HarmonyInstance instance)
            {
                return Core.Mod.BuildingRestrictionTypes.Select(type => type.GetMethod(nameof(BuildingRestriction.CanBuild)))
                    .Concat(Core.Mod.BuildingRestrictionTypes.Select(type => type.GetMethod(nameof(BuildingRestriction.CanBuildHere))));
            }

            [HarmonyPrefix]
            static bool Prefix(BuildingRestriction __instance, ref bool? __state)
            {
                if ((__state = IsAvailable(__instance.GetType())).Value)
                {
                    return false;
                }
                return true;
            }

            [HarmonyPostfix]
            static void Postfix(BuildingRestriction __instance, ref bool __result, ref bool? __state)
            {
                if (__state ?? IsAvailable(__instance.GetType()))
                {
                    __result = true;
                }
            }
        }
    }
}
