using Harmony12;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Multiclass.Main;

namespace Multiclass.HarmonyPatches
{
    internal static class IgnoreActivatableAbilityRestrictions
    {
        public static bool IsAvailable(Type type, UnitDescriptor unit)
        {
            return Core.Enabled &&
                Core.Settings.toggleIgnoreActivatableAbilityRestrictions &&
                Core.Settings.ignoredActivatableAbilityRestrictionSet.Contains(type.FullName) &&
                unit.IsPlayerFaction;
        }

        public static bool Enabled {
            get => Core.Settings.toggleIgnoreActivatableAbilityRestrictions;
            set => Core.Settings.toggleIgnoreActivatableAbilityRestrictions = value;
        }

        public static HashSet<string> IgnoredTypes => Core.Settings.ignoredActivatableAbilityRestrictionSet;

        [HarmonyPatch]
        static class ActivatableAbilityRestriction_Check_Patch
        {
            static IEnumerable<MethodBase> TargetMethods(HarmonyInstance instance)
            {
                return Core.Mod.ActivatableAbilityRestrictionTypes.Select(type => type.GetMethod(nameof(ActivatableAbilityRestriction.IsAvailable)));
            }

            [HarmonyPrefix]
            static bool Prefix(ActivatableAbilityRestriction __instance, ref bool? __state)
            {
                if ((__state = IsAvailable(__instance.GetType(), __instance.Owner)).Value)
                {
                    return false;
                }
                return true;
            }

            [HarmonyPostfix]
            static void Postfix(ActivatableAbilityRestriction __instance, ref bool __result, ref bool? __state)
            {
                if (__state ?? IsAvailable(__instance.GetType(), __instance.Owner))
                {
                    __result = true;
                }
            }
        }
    }
}
