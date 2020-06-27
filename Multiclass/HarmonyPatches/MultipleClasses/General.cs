using Harmony12;
using Kingmaker.Assets.UI.LevelUp;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UI.LevelUp;
using Kingmaker.UI.LevelUp.Phase;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using ModMaker.Extensions;
using Multiclass.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using static ModMaker.Utils.ReflectionCache;
using static Multiclass.Main;

namespace Multiclass.HarmonyPatches
{
    internal static class General
    {
        [HarmonyPatch(typeof(LevelUpController), MethodType.Constructor, typeof(UnitDescriptor), typeof(bool), typeof(JToken), typeof(LevelUpState.CharBuildMode))]
        static class LevelUpController_ctor_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(LevelUpController __instance)
            {
                if (Core.Enabled)
                {
                    Core.Mod.LevelUpController = __instance;
                }
            }
        }

        // 修復升級時從 LevelEntry 添加 Features 的流程，使其能適應同時獲得複數職業或複數等級的情況，正確添加 Features
        [HarmonyPatch(typeof(LevelUpHelper), nameof(LevelUpHelper.UpdateProgression), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor), typeof(BlueprintProgression) })]
        static class LevelUpHelper_UpdateProgression_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before 1 ----------------
                // int nextLevel = progressionData.Blueprint.CalcLevel(unit);
                // progressionData.Level = nextLevel;
                // ---------------- after  1 ----------------
                // int nextLevel = progressionData.Blueprint.CalcLevel(unit);
                // int maxLevel = 20 // unit.Progression.CharacterLevel;
                // if (nextLevel > maxLevel)
                //     nextLevel = maxLevel;
                // ---------------- before 2 ----------------
                // LevelEntry levelEntry = progressionData.GetLevelEntry(nextLevel);
                // AddFeatures(state, unit, levelEntry.Features, progression, nextLevel);
                // ---------------- after  2 ----------------
                // for (int i = level + 1; i <= nextLevel; i++)
                // {
                //     if (!AllowProceed(progression))
                //         break;
                //     progressionData.Level = i;
                //     LevelEntry levelEntry = progressionData.GetLevelEntry(i);
                //     AddFeatures(state, unit, levelEntry.Features, progression, i);
                //     if (!AllowMultiLevel())
                //         break;
                // }
                List<CodeInstruction> findingCodes_1 = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldfld, 
                        GetFieldInfo<ProgressionData, BlueprintProgression>(nameof(ProgressionData.Blueprint))),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Callvirt, 
                        GetMethodInfo<BlueprintProgression, Func<BlueprintProgression, UnitDescriptor, int>>
                        (nameof(BlueprintProgression.CalcLevel))),
                    new CodeInstruction(OpCodes.Stloc_2),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldloc_2),
                    new CodeInstruction(OpCodes.Stfld, 
                        GetFieldInfo<ProgressionData, int>(nameof(ProgressionData.Level)))
                };
                List<CodeInstruction> findingCodes_2 = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldloc_2),
                    new CodeInstruction(OpCodes.Callvirt, 
                        GetMethodInfo<ProgressionData, Func<ProgressionData, int, LevelEntry>>
                        (nameof(ProgressionData.GetLevelEntry))),
                    new CodeInstruction(OpCodes.Stloc_3),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldloc_3),
                    new CodeInstruction(OpCodes.Ldfld, 
                        GetFieldInfo<LevelEntry, List<BlueprintFeatureBase>>(nameof(LevelEntry.Features))),
                    new CodeInstruction(OpCodes.Ldarg_2),
                    new CodeInstruction(OpCodes.Ldloc_2),
                    new CodeInstruction(OpCodes.Call, 
                        GetMethodInfo<Action<LevelUpState, UnitDescriptor, IList<BlueprintFeatureBase>, BlueprintScriptableObject, int>>
                        (typeof(LevelUpHelper), nameof(LevelUpHelper.AddFeatures))),
                };
                int startIndex_1 = codes.FindCodes(findingCodes_1);
                int startIndex_2 = codes.FindLastCodes(findingCodes_2);
                if (startIndex_1 >= 0 && startIndex_2 >= 0)
                {
                    //LocalBuilder maxLevel = il.DeclareLocal(typeof(int));
                    List<CodeInstruction> patchingCodes_1 = new List<CodeInstruction>()
                    {
                        // int maxLevel = 20;
                        new CodeInstruction(OpCodes.Ldloc_2),
                        new CodeInstruction(OpCodes.Ldc_I4, 20),
                        new CodeInstruction(OpCodes.Ble, codes.NewLabel(startIndex_1 + findingCodes_1.Count, il)),
                        new CodeInstruction(OpCodes.Ldc_I4, 20),
                        new CodeInstruction(OpCodes.Stloc_2)

                        //// int maxLevel = unit.Progression.CharacterLevel;
                        //new CodeInstruction(OpCodes.Ldloc_2),
                        //new CodeInstruction(OpCodes.Ldarg_1),
                        //new CodeInstruction(OpCodes.Ldfld,
                        //    GetFieldInfo<UnitDescriptor, UnitProgressionData>(nameof(UnitDescriptor.Progression))),
                        //new CodeInstruction(OpCodes.Callvirt, 
                        //    GetPropertyInfo<UnitProgressionData, int>(nameof(UnitProgressionData.CharacterLevel)).GetGetMethod(true)),
                        //new CodeInstruction(OpCodes.Dup),
                        //new CodeInstruction(OpCodes.Stloc, maxLevel),
                        //new CodeInstruction(OpCodes.Ble, codes.NewLabel(startIndex_1 + findingCodes_1.Count, il)),
                        //new CodeInstruction(OpCodes.Ldloc, maxLevel),
                        //new CodeInstruction(OpCodes.Stloc_2)
                    };
                    Label loopStart = il.DefineLabel();
                    Label loopEnd = codes.NewLabel(startIndex_2 + findingCodes_2.Count, il);
                    LocalBuilder i = il.DeclareLocal(typeof(int));
                    List<CodeInstruction> patchingCodes_2_1 = new List<CodeInstruction>()
                    {
                        //// progressionData.Level = nextLevel;
                        //new CodeInstruction(OpCodes.Ldloc_0),
                        //new CodeInstruction(OpCodes.Ldloc_2),
                        //new CodeInstruction(OpCodes.Stfld,
                        //    GetFieldInfo<ProgressionData, int>(nameof(ProgressionData.Level))),

                        // int i = level
                        new CodeInstruction(OpCodes.Ldloc_1),
                        new CodeInstruction(OpCodes.Stloc, i),
                        // i++
                        new CodeInstruction(OpCodes.Ldloc, i) { labels = { loopStart } },
                        new CodeInstruction(OpCodes.Ldc_I4_1) ,
                        new CodeInstruction(OpCodes.Add),
                        new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Stloc, i),
                        // i <= nextLevel
                        new CodeInstruction(OpCodes.Ldloc_2),
                        new CodeInstruction(OpCodes.Bgt, loopEnd),
                        // if (!AllowProceed(progression)
                        //     break;
                        new CodeInstruction(OpCodes.Ldarg_2),
                        new CodeInstruction(OpCodes.Call,
                            new Func<BlueprintProgression, bool>(AllowProceed).Method),
                        new CodeInstruction(OpCodes.Brfalse, loopEnd),
                        // progressionData.Level = i;
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Ldloc, i),
                        new CodeInstruction(OpCodes.Stfld,
                            GetFieldInfo<ProgressionData, int>(nameof(ProgressionData.Level)))
                    };
                    List<CodeInstruction> patchingCodes_2_2 = new List<CodeInstruction>()
                    {
                        // if (!AllowMultiLevel())
                        //     break;
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(AllowMultiLevel).Method),
                        new CodeInstruction(OpCodes.Brtrue, loopStart)
                    };
                    return codes
                        .InsertRange(startIndex_2 + findingCodes_2.Count, patchingCodes_2_2, false)
                        .Replace(startIndex_2 + 9, new CodeInstruction(OpCodes.Ldloc, i), true)
                        .Replace(startIndex_2 + 1, new CodeInstruction(OpCodes.Ldloc, i), true)
                        .InsertRange(startIndex_2, patchingCodes_2_1, true)
                        .ReplaceRange(startIndex_1 + 5, 3, patchingCodes_1, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }

            private static bool AllowProceed(BlueprintProgression progression)
            {
                // SpellSpecializationProgression << shouldn't be applied more than once per character level
                return !Core.Enabled || 
                    Core.Mod.UpdatedProgressions.Add(progression) || 
                    progression.AssetGuid != "fe9220cdc16e5f444a84d85d5fa8e3d5";
            }

            private static bool AllowMultiLevel()
            {
                return Core.Enabled;
            }
        }

        // 修復升級時的法術選擇介面，使其能適應同時從複數施法職業 (且包含至少一個非吟遊詩人自發施法職業) 獲得新法術的情況，正確計算法術選擇欄位數量
        [HarmonyPatch(typeof(CharBSelectionSwitchSpells), "ParseSpellSelection")]
        static class CharBSelectionSwitchSpells_ParseSpellSelection_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // lastIndex = TryParseMemorizersCollections(showedSpellsCollection, lastIndex);
                // if (lastIndex <= 0)
                // {
                //     lastIndex = TryParseSpontaneuosCastersCollections(showedSpellsCollection, lastIndex);
                // }
                // ---------------- after  ----------------
                // int newIndex = TryParseMemorizersCollections(showedSpellsCollection, lastIndex);
                // lastIndex = ((newIndex == lastIndex) ? TryParseSpontaneuosCastersCollections(showedSpellsCollection, lastIndex) : newIndex);
                List<CodeInstruction> findingCodes = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Call, 
                        GetMethodInfo<CharBSelectionSwitchSpells, Func<CharBSelectionSwitchSpells, SpellSelectionData, int, int>>
                        ("TryParseMemorizersCollections")),
                    new CodeInstruction(OpCodes.Stloc_0),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldc_I4_0),
                    new CodeInstruction(OpCodes.Bgt),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldloc_1),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Call,
                        GetMethodInfo<CharBSelectionSwitchSpells, Func<CharBSelectionSwitchSpells, SpellSelectionData, int, int>>
                        ("TryParseSpontaneuosCastersCollections")),
                    new CodeInstruction(OpCodes.Stloc_0)
                };
                int startIndex = codes.FindCodes(findingCodes);
                if (startIndex >= 0)
                {
                    Label callTryParseSpontaneuosCastersCollections = il.DefineLabel();
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Beq, callTryParseSpontaneuosCastersCollections),
                        new CodeInstruction(OpCodes.Stloc_0),
                        new CodeInstruction(OpCodes.Br, codes.Item(startIndex + 4).operand),
                        new CodeInstruction(OpCodes.Pop) { labels = { callTryParseSpontaneuosCastersCollections } }
                    };
                    return codes.ReplaceRange(startIndex + 1, 4, patchingCodes, false).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }

        // Do not proceed the spell selection if the caster level was not changed
        [HarmonyPatch(typeof(ApplySpellbook), nameof(ApplySpellbook.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class ApplySpellbook_Apply_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // int casterLevelBefore = spellbook.CasterLevel;
                // spellbook.AddCasterLevel();
                // int casterLevelAfter = spellbook.CasterLevel;
                // ---------------- after  ----------------
                // int casterLevelBefore = spellbook.CasterLevel;
                // spellbook.AddCasterLevel();
                // int casterLevelAfter = spellbook.CasterLevel;
                // if (casterLevelBefore == casterLevelAfter)
                //     return;
                List<CodeInstruction> findingCodes = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldloc_3),
                    new CodeInstruction(OpCodes.Callvirt, 
                        GetPropertyInfo<Spellbook, int>(nameof(Spellbook.CasterLevel)).GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Stloc_S),
                    new CodeInstruction(OpCodes.Ldloc_3),
                    new CodeInstruction(OpCodes.Callvirt, 
                        GetMethodInfo<Spellbook, Action<Spellbook>>(nameof(Spellbook.AddCasterLevel))),
                    new CodeInstruction(OpCodes.Ldloc_3),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetPropertyInfo<Spellbook, int>(nameof(Spellbook.CasterLevel)).GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Stloc_S)
                };
                int startIndex = codes.FindCodes(findingCodes);
                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Ldloc_S, codes.Item(startIndex + 2).operand),
                        new CodeInstruction(OpCodes.Ldloc_S, codes.Item(startIndex + 7).operand),
                        new CodeInstruction(OpCodes.Ceq),
                        new CodeInstruction(OpCodes.Brfalse, codes.NewLabel(startIndex + findingCodes.Count, il)),
                        new CodeInstruction(OpCodes.Ret)
                    };
                    return codes.InsertRange(startIndex + findingCodes.Count, patchingCodes, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }

        // Fixed new spell slots (to be calculated not only from the highest caster level when gaining more than one level of a spontaneous caster at a time)
        [HarmonyPatch(typeof(SpellSelectionData), nameof(SpellSelectionData.SetLevelSpells), new Type[] { typeof(int), typeof(int) })]
        static class SpellSelectionData_SetLevelSpells_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(SpellSelectionData __instance, int level, ref int count)
            {
                if (__instance.LevelCount[level] != null)
                {
                    count += __instance.LevelCount[level].SpellSelections.Length;
                }
            }
        }

        // Fixed new spell slots (to be calculated not only from the highest caster level when gaining more than one level of a memorizer at a time)
        [HarmonyPatch(typeof(SpellSelectionData), nameof(SpellSelectionData.SetExtraSpells), new Type[] { typeof(int), typeof(int) })]
        static class SpellSelectionData_SetExtraSpells_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(SpellSelectionData __instance, ref int count, ref int maxLevel)
            {
                if (__instance.ExtraSelected != null)
                {
                    __instance.ExtraMaxLevel = maxLevel = Math.Max(__instance.ExtraMaxLevel, maxLevel);
                    count += __instance.ExtraSelected.Length;
                }
            }
        }

        // Fixed the UI for selecting new spells (to refresh the level tabs of the spellbook correctly on toggling the spellbook)
        [HarmonyPatch(typeof(CharBPhaseSpells), "RefreshSpelbookView")]
        static class CharBPhaseSpells_RefreshSpelbookView_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static bool Prefix()
            {
                return false;
            }

            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            static void Postfix(CharBPhaseSpells __instance)
            {
                __instance.HandleSelectLevel(__instance.ChooseLevelForBook());
            }
        }

        // Fixed the UI for selecting new spells (to switch the spellbook correctly on selecting a spell)
        [HarmonyPatch(typeof(CharacterBuildController), nameof(CharacterBuildController.SetSpell), new Type[] { typeof(BlueprintAbility), typeof(int), typeof(bool) })]
        static class CharacterBuildController_SetSpell_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- check  0 ----------------
                // BlueprintSpellbook spellbook = Spells.CurrentSpellSelectionData.Spellbook;
                // ---------------- before 1 ----------------
                // DefineAvailibleData();
                // ---------------- after  1 ----------------
                // BlueprintCharacterClass selectedClass = this.LevelUpController.State.SelectedClass;
                // this.LevelUpController.State.SelectedClass = spellbook.CharacterClass;
                // DefineAvailibleData();
                // ---------------- before 2 ----------------
                // SetupUI();
                // ---------------- after  2 ----------------
                // SetupUI();
                // this.LevelUpController.State.SelectedClass = selectedClass;
                List<CodeInstruction> findingCodes_0 = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, 
                        GetFieldInfo<CharacterBuildController, CharBPhaseSpells>(nameof(CharacterBuildController.Spells))),
                    new CodeInstruction(OpCodes.Callvirt, 
                        GetPropertyInfo<CharBPhaseSpells, SpellSelectionData>(nameof(CharBPhaseSpells.CurrentSpellSelectionData)).GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Ldfld, 
                        GetFieldInfo<SpellSelectionData, BlueprintSpellbook>(nameof(SpellSelectionData.Spellbook))),
                    new CodeInstruction(OpCodes.Stloc_0)
                };
                List<CodeInstruction> findingCodes_1 = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, 
                        GetMethodInfo<CharacterBuildController, Action<CharacterBuildController>>("DefineAvailibleData"))
                };
                List<CodeInstruction> findingCodes_2 = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, 
                        GetMethodInfo<CharacterBuildController, Action<CharacterBuildController>>("SetupUI"))
                };
                int startIndex_1 = codes.FindLastCodes(findingCodes_1);
                int startIndex_2 = codes.FindLastCodes(findingCodes_2);
                if (codes.FindCodes(findingCodes_0) >= 0 && startIndex_1 >= 0 && startIndex_2 >= 0)
                {
                    List<CodeInstruction> patchingCodes_1 = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Callvirt, 
                            GetPropertyInfo<CharacterBuildController, LevelUpController>(nameof(CharacterBuildController.LevelUpController)).GetGetMethod(true)),
                        new CodeInstruction(OpCodes.Ldfld, 
                            GetFieldInfo<LevelUpController, LevelUpState>(nameof(LevelUpController.State))),
                        new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Ldfld, 
                            GetFieldInfo<LevelUpState, BlueprintCharacterClass>(nameof(LevelUpState.SelectedClass))),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Callvirt, 
                            GetPropertyInfo<CharacterBuildController, LevelUpController>(nameof(CharacterBuildController.LevelUpController)).GetGetMethod(true)),
                        new CodeInstruction(OpCodes.Ldfld, 
                            GetFieldInfo<LevelUpController, LevelUpState>(nameof(LevelUpController.State))),
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Ldfld, 
                            GetFieldInfo<BlueprintSpellbook, BlueprintCharacterClass>(nameof(BlueprintSpellbook.CharacterClass))),
                        new CodeInstruction(OpCodes.Stfld, 
                            GetFieldInfo<LevelUpState, BlueprintCharacterClass>(nameof(LevelUpState.SelectedClass))),
                    };
                    List<CodeInstruction> patchingCodes_2 = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Stfld, 
                            GetFieldInfo<LevelUpState, BlueprintCharacterClass>(nameof(LevelUpState.SelectedClass))),
                    };
                    return codes
                        .InsertRange(startIndex_2 + findingCodes_2.Count, patchingCodes_2, false)
                        .InsertRange(startIndex_1, patchingCodes_1, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }

        // Fixed the UI for selecting new spells (to switch the spellbook correctly on clicking a slot)
        [HarmonyPatch(typeof(CharBPhaseSpells), nameof(CharBPhaseSpells.OnChangeCurrentCollection))]
        static class CharBPhaseSpells_OnChangeCurrentCollection_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(CharBPhaseSpells __instance)
            {
                if (__instance.CurrentSpellSelectionData != null)
                {
                    __instance.SetSpellbook(__instance.CurrentSpellSelectionData.Spellbook.CharacterClass);
                }
            }
        }

        // Fixed the UI for selecting new spells (to refresh the spell list correctly on clicking a slot when the selections have the same spell list) - memorizer
        [HarmonyPatch(typeof(CharBFeatureSelector), nameof(CharBFeatureSelector.FillDataAllSpells), new Type[] { typeof(SpellSelectionData), typeof(int) })]
        static class CharBFeatureSelector_FillDataAllSpells_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // spellSelectionData.SpellList == selectorLayerBody.CurrentSpellSelectionData.SpellList
                // ---------------- after  ----------------
                // spellSelectionData.SpellList == selectorLayerBody.CurrentSpellSelectionData.SpellList && 
                //     spellSelectionData.Spellbook == selectorLayerBody.CurrentSpellSelectionData.Spellbook &&
                List<CodeInstruction> findingCodes = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldfld, 
                        GetFieldInfo<SpellSelectionData, BlueprintSpellList>(nameof(SpellSelectionData.SpellList))),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<CharBSelectorLayer, SpellSelectionData>(nameof(CharBSelectorLayer.CurrentSpellSelectionData))),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<SpellSelectionData, BlueprintSpellList>(nameof(SpellSelectionData.SpellList))),
                    new CodeInstruction(OpCodes.Call),
                    new CodeInstruction(OpCodes.Brfalse)
                };
                int startIndex = codes.FindCodes(findingCodes);
                if (startIndex >= 0)
                {
                    Label callTryParseSpontaneuosCastersCollections = il.DefineLabel();
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldfld,
                            GetFieldInfo<SpellSelectionData, BlueprintSpellbook>(nameof(SpellSelectionData.Spellbook))),
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<CharBSelectorLayer, SpellSelectionData>(nameof(CharBSelectorLayer.CurrentSpellSelectionData))),
                        new CodeInstruction(OpCodes.Ldfld,
                            GetFieldInfo<SpellSelectionData, BlueprintSpellbook>(nameof(SpellSelectionData.Spellbook))),
                        new CodeInstruction(OpCodes.Call, codes.Item(startIndex + 5).operand),
                        new CodeInstruction(OpCodes.Brfalse, codes.Item(startIndex + 6).operand),
                    };
                    return codes.InsertRange(startIndex + findingCodes.Count, patchingCodes, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }

        // Fixed the UI for selecting new spells (to refresh the spell list correctly on clicking a slot when the selections have the same spell list) - spontaneous caster
        [HarmonyPatch(typeof(CharBFeatureSelector), nameof(CharBFeatureSelector.FillDataSpellLevel), new Type[] { typeof(SpellSelectionData), typeof(int) })]
        static class CharBFeatureSelector_FillDataSpellLevel_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // spellSelectionData.SpellList == selectorLayerBody.CurrentSpellSelectionData.SpellList
                // ---------------- after  ----------------
                // spellSelectionData.SpellList == selectorLayerBody.CurrentSpellSelectionData.SpellList && 
                //     spellSelectionData.Spellbook == selectorLayerBody.CurrentSpellSelectionData.Spellbook
                List<CodeInstruction> findingCodes = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<SpellSelectionData, BlueprintSpellList>(nameof(SpellSelectionData.SpellList))),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<CharBSelectorLayer, SpellSelectionData>(nameof(CharBSelectorLayer.CurrentSpellSelectionData))),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<SpellSelectionData, BlueprintSpellList>(nameof(SpellSelectionData.SpellList))),
                    new CodeInstruction(OpCodes.Call),
                    new CodeInstruction(OpCodes.Brfalse)
                };
                int startIndex = codes.FindCodes(findingCodes);
                if (startIndex >= 0)
                {
                    Label callTryParseSpontaneuosCastersCollections = il.DefineLabel();
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldfld, 
                            GetFieldInfo<SpellSelectionData, BlueprintSpellbook>(nameof(SpellSelectionData.Spellbook))),
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Ldfld,
                            GetFieldInfo<CharBSelectorLayer, SpellSelectionData>(nameof(CharBSelectorLayer.CurrentSpellSelectionData))),
                        new CodeInstruction(OpCodes.Ldfld,
                            GetFieldInfo<SpellSelectionData, BlueprintSpellbook>(nameof(SpellSelectionData.Spellbook))),
                        new CodeInstruction(OpCodes.Call, codes.Item(startIndex + 5).operand),
                        new CodeInstruction(OpCodes.Brfalse, codes.Item(startIndex + 6).operand)
                    };
                    return codes.InsertRange(startIndex + findingCodes.Count, patchingCodes, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }

        // Fixed a vanilla PFK bug that caused dragon bloodline to be displayed in Magus' feats tree
        [HarmonyPatch(typeof(ApplyClassMechanics), "ApplyProgressions")]
        static class ApplyClassMechanics_ApplyProgressions_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // progression.Classes.Contains(blueprintCharacterClass)
                // ---------------- after  ----------------
                // progression.IsChildProgressionOf(unit, blueprintCharacterClass)
                List<CodeInstruction> findingCodes = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldloc_3),
                    new CodeInstruction(OpCodes.Ldfld),
                    new CodeInstruction(OpCodes.Ldfld, 
                        GetFieldInfo<BlueprintProgression, BlueprintCharacterClass[]>(nameof(BlueprintProgression.Classes))),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Call)
                };
                int startIndex = codes.FindCodes(findingCodes);
                if (startIndex >= 0)
                {
                    Label callTryParseSpontaneuosCastersCollections = il.DefineLabel();
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Call, new Func<BlueprintProgression, UnitDescriptor, BlueprintCharacterClass, bool>(Extensions.Kingmaker.IsChildProgressionOf).Method),
                    };
                    return codes.ReplaceRange(startIndex + 2, 3, patchingCodes, false).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }
    }
}
