using Harmony12;
using Kingmaker.Blueprints.Classes;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using ModMaker.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using static ModMaker.Utils.ReflectionCache;
using static Multiclass.Main;

namespace Multiclass.HarmonyPatches
{
    internal static class TakeHighestSkillPoints
    {
        public static bool IsAvailable()
        {
            return MultipleClasses.IsAvailable() &&
                Core.Settings.toggleTakeHighestSkillPoints;
        }

        public static bool Enabled {
            get => Core.Settings.toggleTakeHighestSkillPoints;
            set => Core.Settings.toggleTakeHighestSkillPoints = value;
        }

        [HarmonyPatch(typeof(LevelUpHelper), nameof(LevelUpHelper.CalcClassSkillPoints))]
        static class LevelUpHelper_CalcClassSkillPoints_Patch
        {
            [HarmonyPostfix]
            static void Postfix(UnitDescriptor unit, ref int __result)
            {
                if (IsAvailable())
                {
                    foreach (BlueprintCharacterClass characterClass in Core.Mod.AppliedMulticlassSet)
                        __result = Math.Max(__result, unit.Progression.GetClassData(characterClass)?.CalcSkillPoints() ?? 0);
                }
            }
        }
    }
}
