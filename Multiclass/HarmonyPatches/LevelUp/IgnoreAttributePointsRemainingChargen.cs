using Harmony12;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UI.LevelUp;
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
    internal class IgnoreAttributePointsRemainingChargen
    {
        public static bool IsAvailable()
        {
            return Core.Enabled &&
                Core.Settings.toggleIgnoreAttributePointsRemainingChargen &&
                Core.Mod.LevelUpController.IsManualPlayerUnit();
        }

        public static bool Enabled {
            get => Core.Settings.toggleIgnoreAttributePointsRemainingChargen;
            set => Core.Settings.toggleIgnoreAttributePointsRemainingChargen = value;
        }

        // 關於屬性點花費大於剩餘屬性點時能否繼續提升屬性點
        [HarmonyPatch(typeof(StatsDistribution), nameof(StatsDistribution.CanAdd), typeof(StatType))]
        static class StatsDistribution_CanAdd_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // GetAddCost(attribute) <= Points
                // ---------------- after  ----------------
                // IsAvailable() || GetAddCost(attribute) <= Points
                List<CodeInstruction> findingCodes = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call,
                        GetMethodInfo<StatsDistribution, Func<StatsDistribution, StatType, int>>(nameof(StatsDistribution.GetAddCost))),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call,
                        GetPropertyInfo<StatsDistribution, int>(nameof(StatsDistribution.Points)).GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Cgt),
                    new CodeInstruction(OpCodes.Ldc_I4_0),
                    new CodeInstruction(OpCodes.Ceq)
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
                        new CodeInstruction(OpCodes.Pop),
                    };
                    return codes.InsertRange(startIndex, patchingCodes, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }

        // 關於剩餘屬性點不為 0 時能否繼續 / 完成升級
        [HarmonyPatch(typeof(StatsDistribution), nameof(StatsDistribution.IsComplete))]
        static class StatsDistribution_IsComplete_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // if (IsAvailable())
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

        // 關於剩餘屬性點不為 0 時標題是否醒目提示
        [HarmonyPatch(typeof(CharBAbilityScoresAllocator), "SetupVisualMarks")]
        static class CharBAbilityScoresAllocator_SetupVisualMarks_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // state.StatsDistribution.IsComplete()
                // ---------------- after  ----------------
                // if (IsAvailable)
                // {
                //     Enabled = false
                //     state.StatsDistribution.IsComplete()
                //     Enabled = true
                // }
                // else
                // {
                //     state.StatsDistribution.IsComplete()
                // }
                List<CodeInstruction> findingCodes = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<LevelUpState, StatsDistribution>(nameof(LevelUpState.StatsDistribution))),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetMethodInfo<StatsDistribution, Func<StatsDistribution, bool>>(nameof(StatsDistribution.IsComplete))),
                    new CodeInstruction(OpCodes.Br_S)
                };
                int startIndex = codes.FindCodes(findingCodes);
                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brfalse, codes.NewLabel(startIndex, il)),
                        new CodeInstruction(OpCodes.Ldc_I4_0),
                        new CodeInstruction(OpCodes.Call,
                            new Action<bool>(Toggle).Method),
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Ldfld,
                            GetFieldInfo<LevelUpState, StatsDistribution>(nameof(LevelUpState.StatsDistribution))),
                        new CodeInstruction(OpCodes.Callvirt,
                            GetMethodInfo<StatsDistribution, Func<StatsDistribution, bool>>(nameof(StatsDistribution.IsComplete))),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Call,
                            new Action<bool>(Toggle).Method),
                        new CodeInstruction(OpCodes.Br, codes.Item(startIndex + findingCodes.Count - 1).operand),
                    };
                    return codes.InsertRange(startIndex, patchingCodes, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }

            private static void Toggle(bool value)
            {
                Enabled = value;
            }
        }
    }
}
