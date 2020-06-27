using Harmony12;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.UnitLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Multiclass.Main;

namespace Multiclass.HarmonyPatches
{
    internal static class IgnoreEquipmentRestrictions
    {
        public static bool IsAvailable(Type type, UnitDescriptor unit)
        {
            return Core.Enabled &&
                Core.Settings.toggleIgnoreEquipmentRestrictions &&
                Core.Settings.ignoredEquipmentRestrictionSet.Contains(type.FullName) &&
                unit.IsPlayerFaction;
        }

        public static bool Enabled {
            get => Core.Settings.toggleIgnoreEquipmentRestrictions;
            set => Core.Settings.toggleIgnoreEquipmentRestrictions = value;
        }

        public static HashSet<string> IgnoredTypes => Core.Settings.ignoredEquipmentRestrictionSet;

        [HarmonyPatch]
        static class EquipmentRestriction_Check_Patch
        {
            static IEnumerable<MethodBase> TargetMethods(HarmonyInstance instance)
            {
                return Core.Mod.EquipmentRestrictionTypes.Select(type => type.GetMethod(nameof(EquipmentRestriction.CanBeEquippedBy)));
            }

            [HarmonyPrefix]
            static bool Prefix(EquipmentRestriction __instance, UnitDescriptor unit, ref bool? __state)
            {
                if ((__state = IsAvailable(__instance.GetType(), unit)).Value)
                {
                    return false;
                }
                return true;
            }

            [HarmonyPostfix]
            static void Postfix(EquipmentRestriction __instance, UnitDescriptor unit, ref bool __result, ref bool? __state)
            {
                if (__state ?? IsAvailable(__instance.GetType(), unit))
                {
                    __result = true;
                }
            }
        }
    }
}
