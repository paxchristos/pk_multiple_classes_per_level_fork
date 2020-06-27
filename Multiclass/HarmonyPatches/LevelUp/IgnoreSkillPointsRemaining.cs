using Harmony12;
using Kingmaker.UI.LevelUp;
using Kingmaker.UI.LevelUp.Phase;
using Kingmaker.UnitLogic.Class.LevelUp;
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
    internal static class IgnoreSkillPointsRemaining
    {
        public static bool IsAvailable()
        {
            return Core.Enabled &&
                Core.Settings.toggleIgnoreSkillPointsRemaining &&
                Core.Mod.LevelUpController.IsManualPlayerUnit();
        }

        public static bool Enabled {
            get => Core.Settings.toggleIgnoreSkillPointsRemaining;
            set => Core.Settings.toggleIgnoreSkillPointsRemaining = value;
        }

        // 關於技能點分配介面是否顯示
        [HarmonyPatch(typeof(CharBPhaseSkills), nameof(CharBPhaseSkills.CheckAvailible))]
        static class CharBPhaseSkills_CheckAvailible_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // m_State.TotalSkillPoints != 0 ||
                // ---------------- after  ----------------
                // IgnoreSkillPointsRemaining.IsAvailable() || m_State.TotalSkillPoints != 0 ||
                List<CodeInstruction> findingCodes = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call,
                        GetPropertyInfo<CharBPhaseSkills, LevelUpState>("m_State").GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<LevelUpState, int>(nameof(LevelUpState.TotalSkillPoints))),
                    new CodeInstruction(OpCodes.Brtrue)
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

        // 關於剩餘技能點小於等於 0 時能否繼續分配技能點
        [HarmonyPatch(typeof(CharBSkillsAllocator), nameof(CharBSkillsAllocator.FillLevelUpData))]
        static class CharBSkillsAllocator_FillLevelUpData_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // m_CharacterBuildController.LevelUpController.State.SkillPointsRemaining > 0 &&
                // ---------------- after  ----------------
                // (IgnoreSkillPointsRemaining.IsAvailable() || m_CharacterBuildController.LevelUpController.State.SkillPointsRemaining > 0) &&
                List<CodeInstruction> findingCodes = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call,
                        GetPropertyInfo<CharBSkillsAllocator, CharacterBuildController>("m_" + nameof(CharacterBuildController)).GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetPropertyInfo<CharacterBuildController, LevelUpController>(nameof(CharacterBuildController.LevelUpController)).GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<LevelUpController, LevelUpState>(nameof(LevelUpController.State))),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetPropertyInfo<LevelUpState, int>(nameof(LevelUpState.SkillPointsRemaining)).GetGetMethod(true)),
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
                        new CodeInstruction(OpCodes.Brtrue, codes.NewLabel(startIndex + findingCodes.Count, il)),
                    };
                    return codes.InsertRange(startIndex, patchingCodes, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }

        // 關於剩餘技能點不為 0 時能否繼續 / 完成升級
        [HarmonyPatch(typeof(LevelUpState), nameof(LevelUpState.IsSkillPointsComplete))]
        static class LevelUpState_IsSkillPointsComplete_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- after  ----------------
                // if (IgnoreSkillPointsRemaining.IsAvailable())
                //     return true;
                // ... original codes ...
                List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Call,
                        new Func<bool>(IsAvailable).Method),
                    new CodeInstruction(OpCodes.Brfalse, codes.NewLabel(0, il)),
                    new CodeInstruction(OpCodes.Ldc_I4_1),
                    new CodeInstruction(OpCodes.Ret)
                };
                return codes.InsertRange(0, patchingCodes, true).Complete();
            }
        }

        // 關於剩餘技能點不為 0 時能否繼續升級
        //[HarmonyPatch(typeof(CharBSkillsAllocator), nameof(CharBSkillsAllocator.SkillsAllocated))]
        //static class CharBSkillsAllocator_SkillsAllocated_Patch
        //{
        //    [HarmonyTranspiler]
        //    static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
        //    {
        //        // ---------------- before ----------------
        //        // m_CharacterBuildController.LevelUpController.State.IsSkillPointsComplete();
        //        // ---------------- after  ----------------
        //        // IsAvailable() || m_CharacterBuildController.LevelUpController.State.IsSkillPointsComplete();
        //        List<CodeInstruction> findingCodes = new List<CodeInstruction>
        //        {
        //            new CodeInstruction(OpCodes.Ldarg_0),
        //            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CharBSkillsAllocator), "get_m_" + nameof(CharacterBuildController))),
        //            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(CharacterBuildController), "get_" + nameof(CharacterBuildController.LevelUpController))),
        //            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(LevelUpController), nameof(LevelUpController.State))),
        //            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(LevelUpState), nameof(LevelUpState.IsSkillPointsComplete)))
        //        };
        //        int startIndex = codes.FindCodes(findingCodes);
        //        if (startIndex >= 0)
        //        {
        //            List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
        //            {
        //                new CodeInstruction(OpCodes.Ldc_I4_1),
        //                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(IgnoreSkillPointsRemaining), nameof(IsAvailable))),
        //                new CodeInstruction(OpCodes.Brtrue, codes.NewLabel(startIndex + findingCodes.Count, il)),
        //                new CodeInstruction(OpCodes.Pop)
        //            };
        //            return codes.InsertRange(startIndex, patchingCodes, true).AsEnumerable();
        //        }
        //        else
        //        {
        //            throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
        //        }
        //    }
        //}

        // 關於剩餘技能點不為 0 時能否完成升級
        //[HarmonyPatch(typeof(LevelUpState), nameof(LevelUpState.IsComplete))]
        //static class LevelUpState_IsComplete_Patch
        //{
        //    [HarmonyTranspiler]
        //    static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
        //    {
        //        // ---------------- before ----------------
        //        // !IsSkillPointsComplete()
        //        // ---------------- after  ----------------
        //        // !(IsAvailable() || IsSkillPointsComplete())
        //        List<CodeInstruction> findingCodes = new List<CodeInstruction>
        //        {
        //            new CodeInstruction(OpCodes.Ldarg_0),
        //            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LevelUpState), nameof(LevelUpState.IsSkillPointsComplete))),
        //            new CodeInstruction(OpCodes.Brtrue)
        //        };
        //        int startIndex = codes.FindLastCodes(findingCodes);
        //        if (startIndex >= 0)
        //        {
        //            List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
        //            {
        //                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(IgnoreSkillPointsRemaining), nameof(IsAvailable))),
        //                new CodeInstruction(OpCodes.Brtrue, codes.Item(startIndex + findingCodes.Count -1).operand)
        //            };
        //            return codes.InsertRange(startIndex, patchingCodes, true).AsEnumerable();
        //        }
        //        else
        //        {
        //            throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
        //        }
        //    }
        //}

        // 關於剩餘技能點不為 0 時標題是否醒目提示
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
