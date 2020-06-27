using Harmony12;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Multiclass.Main;

namespace Multiclass.HarmonyPatches
{
    internal static class IgnoreAbilityCasterCheckers
    {
        public static bool IsAvailable(Type type, UnitEntityData unit)
        {
            return Core.Enabled &&
                Core.Settings.toggleIgnoreAbilityCasterCheckers &&
                Core.Settings.ignoredAbilityCasterCheckerSet.Contains(type.FullName) &&
                unit.IsPlayerFaction;
        }

        public static bool Enabled {
            get => Core.Settings.toggleIgnoreAbilityCasterCheckers;
            set => Core.Settings.toggleIgnoreAbilityCasterCheckers = value;
        }

        public static HashSet<string> IgnoredTypes => Core.Settings.ignoredAbilityCasterCheckerSet;

        [HarmonyPatch]
        static class AbilityCasterChecker_Check_Patch
        {
            static IEnumerable<MethodBase> TargetMethods(HarmonyInstance instance)
            {
                return Core.Mod.AbilityCasterCheckerTypes.Select(type => type.GetMethod(nameof(IAbilityCasterChecker.CorrectCaster)));
            }

            [HarmonyPrefix]
            static bool Prefix(IAbilityCasterChecker __instance, UnitEntityData caster, ref bool? __state)
            {
                if ((__state = IsAvailable(__instance.GetType(), caster)).Value)
                {
                    return false;
                }
                return true;
            }

            [HarmonyPostfix]
            static void Postfix(IAbilityCasterChecker __instance, UnitEntityData caster, ref bool __result, ref bool? __state)
            {
                if (__state ?? IsAvailable(__instance.GetType(), caster))
                {
                    __result = true;
                }
            }
        }
    }
}
