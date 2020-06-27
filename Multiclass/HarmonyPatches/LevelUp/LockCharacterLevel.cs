using Harmony12;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Multiclass.Extensions;
using Newtonsoft.Json.Linq;
using System;
using static Multiclass.Main;

namespace Multiclass.HarmonyPatches
{
    internal static class LockCharacterLevel
    {
        public static bool IsAvailable()
        {
            return Core.Enabled &&
                Core.Settings.toggleLockCharacterLevel;
        }

        public static bool Enabled {
            get => Core.Settings.toggleLockCharacterLevel;
            set => Core.Settings.toggleLockCharacterLevel = value;
        }

        [HarmonyPatch(typeof(LevelUpController), MethodType.Constructor, typeof(UnitDescriptor), typeof(bool), typeof(JToken), typeof(LevelUpState.CharBuildMode))]
        static class LevelUpController_ctor_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(LevelUpController __instance, UnitDescriptor unit, bool autoCommit, LevelUpState.CharBuildMode mode)
            {
                if (IsAvailable() &&
                    unit.Progression.CharacterLevel > 0 &&
                    unit.IsPlayerFaction &&
                    !unit.IsPet &&
                    !autoCommit &&
                    mode != LevelUpState.CharBuildMode.PreGen)
                {
                    Core.Mod.IsLevelLocked = true;
                }
                else if (Core.Enabled)
                {
                    Core.Mod.IsLevelLocked = false;
                }
            }
        }

        // Do not gain attribute points
        [HarmonyPatch(typeof(LevelUpState), MethodType.Constructor, typeof(UnitDescriptor), typeof(LevelUpState.CharBuildMode))]
        static class LevelUpState_ctor_Patch
        {
            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            static void Postfix(LevelUpState __instance, UnitDescriptor unit, LevelUpState.CharBuildMode mode)
            {
                if (IsAvailable() && Core.Mod.IsLevelLocked)
                {
                    //__instance.SetFieldValue(nameof(LevelUpState.NextLevel), unit.Progression.CharacterLevel);
                    __instance.AttributePoints = 0;
                }
            }
        }

        // Do not gain character level
        [HarmonyPatch(typeof(UnitProgressionData), nameof(UnitProgressionData.AddClassLevel), new Type[] { typeof(BlueprintCharacterClass) })]
        static class UnitProgressionData_AddClassLevel_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static bool Prefix(UnitProgressionData __instance, BlueprintCharacterClass characterClass)
            {
                if (IsAvailable() && Core.Mod.IsLevelLocked)
                {
                    __instance.AddClassLevel_NotCharacterLevel(characterClass);
                    return false;
                }
                return true;
            }
        }

        // Do not gain skill points
        [HarmonyPatch(typeof(LevelUpState), nameof(LevelUpState.OnApplyAction))]
        static class LevelUpState_OnApplyAction_Patch
        {
            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            static void Postfix(LevelUpState __instance, UnitDescriptor ___m_Unit)
            {
                if (IsAvailable() && Core.Mod.IsLevelLocked)
                {
                    __instance.TotalSkillPoints = 0;
                }
            }
        }

        // Do not gain BAB, hit points, saves
        [HarmonyPatch(typeof(ApplyClassMechanics), nameof(ApplyClassMechanics.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class ApplyClassMechanics_Apply_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static bool Prefix(ApplyClassMechanics __instance, LevelUpState state, UnitDescriptor unit)
            {
                if (IsAvailable() && Core.Mod.IsLevelLocked)
                {
                    __instance.Apply_NoStatsAndHitPoints(state, unit);
                    return false;
                }
                return true;
            }
        }
    }
}
