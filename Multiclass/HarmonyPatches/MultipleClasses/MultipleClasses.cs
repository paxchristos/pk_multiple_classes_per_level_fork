using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using ModMaker.Utils;
using Multiclass.Extensions;
using Multiclass.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using static ModMaker.Utils.ReflectionCache;
using static Multiclass.Main;

namespace Multiclass.HarmonyPatches
{
    internal static class MultipleClasses
    {
        public static bool IsAvailable()
        {
            return Core.Enabled && 
                Core.Settings.toggleMulticlass &&
                Core.Mod.LevelUpController.IsManualPlayerUnit();
        }

        public static bool Enabled {
            get => Core.Settings.toggleMulticlass;
            set => Core.Settings.toggleMulticlass = value;
        }

        public static HashSet<string> MainCharSet => Core.Settings.selectedMulticlassSet;

        public static HashSet<string> CharGenSet => Core.Settings.selectedCharGenMulticlassSet;

        public static SerializableDictionary<string, HashSet<string>> CompanionSets => Core.Settings.selectedCompanionMulticlassSet;

        public static HashSet<string> DemandCompanionSet(string name)
        {
            if (!Core.Settings.selectedCompanionMulticlassSet.TryGetValue(name, out HashSet<string> classes))
            {
                Core.Settings.selectedCompanionMulticlassSet.Add(name, classes = new HashSet<string>());
            }
            return classes;
        }

        #region Utils

        private static void ForEachAppliedMulticlass(LevelUpState state, UnitDescriptor unit, Action action)
        {
            StateReplacer stateReplacer = new StateReplacer(state);
            foreach (BlueprintCharacterClass characterClass in Core.Mod.AppliedMulticlassSet)
            {
                stateReplacer.Replace(characterClass, unit.Progression.GetClassLevel(characterClass));
                action();
            }
            stateReplacer.Restore();
        }

        #endregion

        #region Class Level & Archetype

        [HarmonyPatch(typeof(SelectClass), nameof(SelectClass.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class SelectClass_Apply_Patch
        {
            [HarmonyPostfix]
            static void Postfix(LevelUpState state, UnitDescriptor unit)
            {
                if (IsAvailable())
                {
                    Core.Mod.AppliedMulticlassSet.Clear();
                    Core.Mod.UpdatedProgressions.Clear();

                    // get multiclass setting
                    HashSet<string> selectedMulticlassSet =
                        Core.Mod.LevelUpController.Unit.IsMainCharacter ? MainCharSet :
                        (state.IsCharGen() && unit.IsCustomCompanion()) ? CharGenSet :
                        CompanionSets.ContainsKey(unit.CharacterName) ? CompanionSets[unit.CharacterName] : null;
                    if (selectedMulticlassSet == null || selectedMulticlassSet.Count == 0)
                        return;

                    // applying classes
                    StateReplacer stateReplacer = new StateReplacer(state);
                    foreach (BlueprintCharacterClass characterClass in Core.Mod.CharacterClasses)
                    {
                        if (characterClass != stateReplacer.SelectedClass && selectedMulticlassSet.Contains(characterClass.AssetGuid))
                        {
                            stateReplacer.Replace(null, 0);
                            if (new SelectClass(characterClass).Check(state, unit))
                            {
                                Core.Debug($" - {nameof(SelectClass)}.{nameof(SelectClass.Apply)}*({characterClass}, {unit})");

                                unit.Progression.AddClassLevel_NotCharacterLevel(characterClass);
                                //state.NextClassLevel = unit.Progression.GetClassLevel(characterClass);
                                //state.SelectedClass = characterClass;
                                characterClass.RestrictPrerequisites(unit, state);
                                //EventBus.RaiseEvent<ILevelUpSelectClassHandler>(h => h.HandleSelectClass(unit, state));

                                Core.Mod.AppliedMulticlassSet.Add(characterClass);
                            }
                        }
                    }
                    stateReplacer.Restore();

                    // applying archetypes
                    ForEachAppliedMulticlass(state, unit, () =>
                    {
                        foreach (BlueprintArchetype archetype in state.SelectedClass.Archetypes)
                        {
                            if (selectedMulticlassSet.Contains(archetype.AssetGuid))
                            {
                                AddArchetype addArchetype = new AddArchetype(state.SelectedClass, archetype);
                                if (addArchetype.Check(state, unit))
                                {
                                    addArchetype.Apply(state, unit);
                                }
                            }
                        }
                    });
                }
            }
        }

        #endregion

        #region Skills & Features

        [HarmonyPatch(typeof(ApplyClassMechanics), nameof(ApplyClassMechanics.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class ApplyClassMechanics_Apply_Patch
        {
            [HarmonyPostfix]
            static void Postfix(ApplyClassMechanics __instance, LevelUpState state, UnitDescriptor unit)
            {
                if (IsAvailable())
                {
                    if (state.SelectedClass != null)
                    {
                        ForEachAppliedMulticlass(state, unit, () =>
                        {
                            Core.Debug($" - {nameof(ApplyClassMechanics)}.{nameof(ApplyClassMechanics.Apply)}*({state.SelectedClass}[{state.NextClassLevel}], {unit})");

                            __instance.Apply_NoStatsAndHitPoints(state, unit);
                        });
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SelectFeature), nameof(SelectFeature.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class SelectFeature_Apply_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(SelectFeature __instance, LevelUpState state, UnitDescriptor unit, ref StateReplacer __state)
            {
                if (IsAvailable())
                {
                    if (__instance.Item != null)
                    {
                        FeatureSelectionState selectionState = 
                            GetMethodDel<SelectFeature, Func<SelectFeature, LevelUpState, FeatureSelectionState>>
                            ("GetSelectionState")(__instance, state);
                        if (selectionState != null)
                        {
                            BlueprintCharacterClass sourceClass = (selectionState.Source as BlueprintFeature)?.GetSourceClass(unit);
                            if (sourceClass != null)
                            {
                                __state = new StateReplacer(state);
                                __state.Replace(sourceClass, unit.Progression.GetClassLevel(sourceClass));
                            }
                        }
                    }
                }
            }

            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            static void Postfix(SelectFeature __instance, ref StateReplacer __state)
            {
                if (__state != null)
                {
                    __state.Restore();
                }
            }
        }

        [HarmonyPatch(typeof(SelectFeature), nameof(SelectFeature.Check), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class SelectFeature_Check_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(SelectFeature __instance, LevelUpState state, UnitDescriptor unit, ref StateReplacer __state)
            {
                if (IsAvailable())
                {
                    if (__instance.Item != null)
                    {
                        FeatureSelectionState selectionState = 
                            GetMethodDel<SelectFeature, Func<SelectFeature, LevelUpState, FeatureSelectionState>>
                            ("GetSelectionState")(__instance, state);
                        if (selectionState != null)
                        {
                            BlueprintCharacterClass sourceClass = (selectionState.Source as BlueprintFeature)?.GetSourceClass(unit);
                            if (sourceClass != null)
                            {
                                __state = new StateReplacer(state);
                                __state.Replace(sourceClass, unit.Progression.GetClassLevel(sourceClass));
                            }
                        }
                    }
                }
            }

            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            static void Postfix(SelectFeature __instance, ref StateReplacer __state)
            {
                if (__state != null)
                {
                    __state.Restore();
                }
            }
        }

        #endregion

        #region Spellbook

        [HarmonyPatch(typeof(ApplySpellbook), nameof(ApplySpellbook.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class ApplySpellbook_Apply_Patch
        {
            [HarmonyPostfix]
            static void Postfix(MethodBase __originalMethod, ApplySpellbook __instance, LevelUpState state, UnitDescriptor unit)
            {
                if (IsAvailable() && !Core.Mod.LockedPatchedMethods.Contains(__originalMethod))
                {
                    Core.Mod.LockedPatchedMethods.Add(__originalMethod);
                    ForEachAppliedMulticlass(state, unit, () =>
                    {
                        __instance.Apply(state, unit);
                    });
                    Core.Mod.LockedPatchedMethods.Remove(__originalMethod);
                }
            }
        }

        #endregion

        #region Commit

        [HarmonyPatch(typeof(LevelUpController), nameof(LevelUpController.Commit))]
        static class LevelUpController_Commit_Patch
        {
            [HarmonyPostfix]
            static void Postfix(LevelUpController __instance)
            {
                if (IsAvailable())
                {
                    if (__instance.State.IsCharGen() && __instance.Unit.IsCustomCompanion() && CharGenSet.Count > 0)
                    {
                        CompanionSets.Add(__instance.Unit.CharacterName, new HashSet<string>(CharGenSet));
                        CharGenSet.Clear();
                    }
                }
            }
        }

        #endregion
    }
}
