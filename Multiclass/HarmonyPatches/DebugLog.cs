#if DEBUG
using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Multiclass.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using static ModMaker.Utils.ReflectionCache;
using static Multiclass.Main;

namespace Multiclass.HarmonyPatches
{
    internal static class DebugLog
    {
        public static bool IsAvailable()
        {
            return MultipleClasses.IsAvailable();
        }

        [HarmonyPatch(typeof(LevelUpController), MethodType.Constructor, typeof(UnitDescriptor), typeof(bool), typeof(JToken), typeof(LevelUpState.CharBuildMode))]
        static class LevelUpController_ctor_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(LevelUpController __instance, UnitDescriptor unit, bool autoCommit, JToken unitJson, LevelUpState.CharBuildMode mode)
            {
                if (Core.Enabled)
                    Core.Debug($"{nameof(LevelUpController)}..ctor({unit}, {(autoCommit ? "isAutoCommit" : "notAutoCommit")}, {unitJson?.Type}, {mode})");
            }
        }

        [HarmonyPatch(typeof(LevelUpController), nameof(LevelUpController.SelectClass), new Type[] { typeof(BlueprintCharacterClass), typeof(bool) })]
        static class LevelUpController_SelectClass_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(LevelUpController __instance, BlueprintCharacterClass characterClass, bool applyMechanics)
            {
                if (IsAvailable())
                    Core.Debug($"{nameof(LevelUpController)}.{nameof(LevelUpController.SelectClass)}({characterClass}, {applyMechanics})");
            }
        }

        [HarmonyPatch(typeof(SelectClass), nameof(SelectClass.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class SelectClass_Apply_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(SelectClass __instance, LevelUpState state, UnitDescriptor unit, BlueprintCharacterClass ___m_CharacterClass)
            {
                if (IsAvailable())
                    Core.Debug($" - {nameof(SelectClass)}.{nameof(SelectClass.Apply)}({___m_CharacterClass}, {unit})");
            }
        }

        [HarmonyPatch(typeof(AddArchetype), nameof(AddArchetype.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class AddArchetype_Apply_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(AddArchetype __instance, LevelUpState state, UnitDescriptor unit)
            {
                if (IsAvailable())
                    Core.Debug($" - {nameof(AddArchetype)}.{nameof(AddArchetype.Apply)}({state.SelectedClass}[{state.NextClassLevel}], {unit})");
            }
        }

        [HarmonyPatch(typeof(ApplyClassMechanics), nameof(ApplyClassMechanics.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class ApplyClassMechanics_Apply_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(ApplyClassMechanics __instance, LevelUpState state, UnitDescriptor unit)
            {
                if (IsAvailable())
                    Core.Debug($" - {nameof(ApplyClassMechanics)}.{nameof(ApplyClassMechanics.Apply)}({state.SelectedClass}[{state.NextClassLevel}], {unit})");
            }
        }

        [HarmonyPatch(typeof(LevelUpHelper), nameof(LevelUpHelper.UpdateProgression), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor), typeof(BlueprintProgression) })]
        static class LevelUpHelper_UpdateProgression_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(LevelUpState state, UnitDescriptor unit, BlueprintProgression progression)
            {
                if (IsAvailable())
                {
                    ProgressionData progressionData = unit.Progression.SureProgressionData(progression);
                    int nextLevel = progressionData.Blueprint.CalcLevel(unit);
                    Core.Debug($" - {nameof(LevelUpHelper)}.{nameof(LevelUpHelper.UpdateProgression)}({state.SelectedClass}[{state.NextClassLevel}], {unit}, {progression}) - NextLevel: {nextLevel}");
                }
            }
        }

        [HarmonyPatch(typeof(LevelUpHelper), nameof(LevelUpHelper.AddFeatures), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor), typeof(IList<BlueprintFeatureBase>), typeof(BlueprintScriptableObject), typeof(int) })]
        static class LevelUpHelper_AddFeatures_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(LevelUpState state, UnitDescriptor unit, IList<BlueprintFeatureBase> features, BlueprintScriptableObject source, int level)
            {
                if (IsAvailable())
                    Core.Debug($"   - {nameof(LevelUpHelper)}.{nameof(LevelUpHelper.AddFeatures)}({state.SelectedClass}[{state.NextClassLevel}], {unit}, {features.Count}, {source}, {level})");
            }
        }

        [HarmonyPatch(typeof(SelectFeature), nameof(SelectFeature.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class SelectFeature_Apply_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(SelectFeature __instance, LevelUpState state, UnitDescriptor unit)
            {
                if (IsAvailable())
                {
                    BlueprintCharacterClass sourceClass = (GetMethodDel<SelectFeature, Func<SelectFeature, LevelUpState, FeatureSelectionState>>
                        ("GetSelectionState")(__instance, state)?.Source as BlueprintProgression)?.GetSourceClass(unit);
                    if (sourceClass == null)
                    {
                        Core.Debug($" - {nameof(SelectFeature)}.{nameof(SelectFeature.Apply)}({state.SelectedClass}[{state.NextClassLevel}], {unit}) - {__instance.Selection}, {__instance.Item}");
                    }
                    else
                    {
                        Core.Debug($" - {nameof(SelectFeature)}.{nameof(SelectFeature.Apply)}({sourceClass}[{unit.Progression.GetClassLevel(sourceClass)}], {unit}) - {__instance.Selection}, {__instance.Item}");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ApplySpellbook), nameof(ApplySpellbook.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class ApplySpellbook_Apply_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(ApplySpellbook __instance, LevelUpState state, UnitDescriptor unit)
            {
                if (IsAvailable())
                    Core.Debug($" - {nameof(ApplySpellbook)}.{nameof(ApplySpellbook.Apply)}({state.SelectedClass}[{state.NextClassLevel}], {unit})");
            }
        }

        [HarmonyPatch(typeof(SelectSpell), nameof(SelectSpell.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class SelectSpell_Apply_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(SelectSpell __instance, LevelUpState state, UnitDescriptor unit)
            {
                if (IsAvailable())
                    Core.Debug($" - {nameof(SelectSpell)}.{nameof(SelectSpell.Apply)}({state.SelectedClass}[{state.NextClassLevel}], {unit}) - { __instance.Spellbook}[{__instance.SpellLevel}], {__instance.Spell}");
            }
        }
    }
}
#endif