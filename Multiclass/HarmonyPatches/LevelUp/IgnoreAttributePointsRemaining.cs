using Harmony12;
using Kingmaker.UI.LevelUp;
using Kingmaker.UI.LevelUp.Phase;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using ModMaker.Extensions;
using Multiclass.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using static ModMaker.Utils.ReflectionCache;
using static Multiclass.Main;

namespace Multiclass.HarmonyPatches
{
    internal class IgnoreAttributePointsRemaining
    {
        public static bool IsAvailable()
        {
            return Core.Enabled &&
                Core.Settings.toggleIgnoreAttributePointsRemaining &&
                Core.Mod.LevelUpController.IsManualPlayerUnit();
        }

        public static bool Enabled {
            get => Core.Settings.toggleIgnoreAttributePointsRemaining;
            set => Core.Settings.toggleIgnoreAttributePointsRemaining = value;
        }

        // 關於屬性點分配介面是否顯示
        [HarmonyPatch(typeof(CharBPhaseSkills), nameof(CharBPhaseSkills.CheckAvailible))]
        static class CharBPhaseSkills_CheckAvailible_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // m_State.AttributePoints > 0 ||
                // ---------------- after  ----------------
                // IsAvailable() || m_State.AttributePoints > 0 ||
                List<CodeInstruction> findingCodes = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call,
                        GetPropertyInfo<CharBPhaseSkills, LevelUpState>("m_State").GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<LevelUpState, int>(nameof(LevelUpState.AttributePoints))),
                    new CodeInstruction(OpCodes.Ldc_I4_0),
                    new CodeInstruction(OpCodes.Bgt)
                };
                int startIndex = codes.FindLastCodes(findingCodes);
                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brtrue, codes.Item(startIndex + findingCodes.Count - 1).operand),
                    };
                    return codes.InsertRange(startIndex, patchingCodes, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }

        // 關於剩餘屬性點小於等於 0 時能否繼續分配屬性點 (內部邏輯)
        [HarmonyPatch(typeof(SpendAttributePoint), nameof(SpendAttributePoint.Check), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class SpendAttributePoint_Check_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // state.AttributePoints > 0
                // ---------------- after  ----------------
                // IsAvailable() || state.AttributePoints > 0
                List<CodeInstruction> findingCodes = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<LevelUpState, int>(nameof(LevelUpState.AttributePoints))),
                    new CodeInstruction(OpCodes.Ldc_I4_0),
                    new CodeInstruction(OpCodes.Cgt)
                };
                int startIndex = codes.FindLastCodes(findingCodes);
                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brtrue, codes.NewLabel(startIndex + findingCodes.Count, il)),
                        new CodeInstruction(OpCodes.Pop)
                    };
                    return codes.InsertRange(startIndex, patchingCodes, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }

        // 關於剩餘屬性點小於等於 0 時能否繼續分配屬性點 (介面按鈕)
        [HarmonyPatch(typeof(CharBAbilityScoresAllocator), "SetupCommonPoints")]
        static class CharBAbilityScoresAllocator_SetupCommonPoints_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // int attributePoints = m_CharacterBuildController.LevelUpController.State.AttributePoints;
                // ---------------- before ----------------
                // attributePoints > 0 &&
                // ---------------- after  ----------------
                // (IsAvailable() || attributePoints > 0) &&
                List<CodeInstruction> findingCodes_1 = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call,
                        GetPropertyInfo<CharBAbilityScoresAllocator, CharacterBuildController>("m_" + nameof(CharacterBuildController)).GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetPropertyInfo<CharacterBuildController, LevelUpController>(nameof(CharacterBuildController.LevelUpController)).GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<LevelUpController, LevelUpState>(nameof(LevelUpController.State))),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<LevelUpState, int>(nameof(LevelUpState.AttributePoints))),
                    new CodeInstruction(OpCodes.Stloc_0)
                };
                List<CodeInstruction> findingCodes_2 = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldc_I4_0),
                    new CodeInstruction(OpCodes.Ble)
                };
                int startIndex_1 = codes.FindCodes(findingCodes_1);
                int startIndex_2 = codes.FindLastCodes(findingCodes_2);
                if (startIndex_1 >= 0 && startIndex_2 >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brtrue, codes.NewLabel(startIndex_2 + findingCodes_2.Count, il))
                    };
                    return codes.InsertRange(startIndex_2, patchingCodes, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }

        // 關於剩餘屬性點不為 0 時能否繼續升級
        [HarmonyPatch(typeof(CharBAbilityScoresAllocator), nameof(CharBAbilityScoresAllocator.ScoresAllocated))]
        static class CharBAbilityScoresAllocator_ScoresAllocated_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // state.AttributePoints == 0 &&
                // ---------------- after  ----------------
                // IsAvailable() || state.AttributePoints == 0 &&
                List<CodeInstruction> findingCodes = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<LevelUpState, int>(nameof(LevelUpState.AttributePoints))),
                    new CodeInstruction(OpCodes.Brtrue)
                };
                int startIndex = codes.FindCodes(findingCodes);
                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brtrue, codes.NewLabel(startIndex + findingCodes.Count, il))
                    };
                    return codes.InsertRange(startIndex, patchingCodes, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }

        // 關於剩餘屬性點不為 0 時能否完成升級
        [HarmonyPatch(typeof(LevelUpState), nameof(LevelUpState.IsComplete))]
        static class LevelUpState_IsComplete_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // if (AttributePoints > 0)
                // ---------------- after  ----------------
                // if (!IsAvailable() && AttributePoints > 0)
                List<CodeInstruction> findingCodes = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<LevelUpState, int>(nameof(LevelUpState.AttributePoints))),
                    new CodeInstruction(OpCodes.Ldc_I4_0),
                    new CodeInstruction(OpCodes.Ble)
                };
                int startIndex = codes.FindLastCodes(findingCodes);
                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brtrue, codes.Item(startIndex + findingCodes.Count -1).operand)
                    };
                    return codes.InsertRange(startIndex, patchingCodes, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }

        // 關於剩餘屬性點不為 0 時標題是否醒目提示
        //[HarmonyPatch(typeof(CharBSkillsAllocator), "SetupVisualMarks")]
        //static class CharBSkillsAllocator_SetupVisualMarks_Patch
        //{
        //    [HarmonyTranspiler]
        //    static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
        //    {
        //        return codes;
        //    }
        //}
    }
}
