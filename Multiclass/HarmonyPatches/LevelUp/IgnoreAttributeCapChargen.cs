using Harmony12;
using Kingmaker.EntitySystem.Stats;
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
    internal class IgnoreAttributeStatCapChargen
    {
        public static bool IsAvailable()
        {
            return Core.Enabled &&
                Core.Settings.toggleIgnoreAttributeStatCapChargen &&
                Core.Mod.LevelUpController.IsManualPlayerUnit();
        }

        public static bool Enabled {
            get => Core.Settings.toggleIgnoreAttributeStatCapChargen;
            set => Core.Settings.toggleIgnoreAttributeStatCapChargen = value;
        }

        // 關於屬性點大於等於屬性點上限時能否繼續提升屬性點
        [HarmonyPatch(typeof(StatsDistribution), nameof(StatsDistribution.CanAdd), typeof(StatType))]
        static class StatsDistribution_CanAdd_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // int statValue = StatValues[attribute];
                // if (statValue >= 18)
                // ---------------- after  ----------------
                // int statValue = StatValues[attribute];
                // if (!IsAvailable() && statValue >= 18)
                List<CodeInstruction> findingCodes = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<StatsDistribution, Dictionary<StatType, int>>(nameof(StatsDistribution.StatValues))),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetPropertyInfo<Dictionary<StatType, int>, int>("Item").GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Stloc_0),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)18),
                    new CodeInstruction(OpCodes.Blt)
                };
                int startIndex = codes.FindCodes(findingCodes);

                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brtrue, codes.Item(startIndex + findingCodes.Count - 1).operand)
                    };
                    return codes.InsertRange(startIndex + 5, patchingCodes, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }

        // 關於屬性點小於等於屬性點下限時能否繼續降低屬性點
        [HarmonyPatch(typeof(StatsDistribution), nameof(StatsDistribution.CanRemove), typeof(StatType))]
        static class StatsDistribution_CanRemove_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // int statValue = StatValues[attribute];
                // if (statValue <= 7)
                // ---------------- after  ----------------
                // int statValue = StatValues[attribute];
                // if (statValue <= (IsAvailable() ? 1 : 7))
                List<CodeInstruction> findingCodes = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<StatsDistribution, Dictionary<StatType, int>>(nameof(StatsDistribution.StatValues))),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetPropertyInfo<Dictionary<StatType, int>, int>("Item").GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Stloc_0),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldc_I4_7),
                    new CodeInstruction(OpCodes.Bgt)
                };
                int startIndex = codes.FindCodes(findingCodes);
                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brfalse, codes.NewLabel(startIndex + findingCodes.Count - 2, il)),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Br, codes.NewLabel(startIndex + findingCodes.Count - 1, il))
                   };
                    return codes.InsertRange(startIndex + findingCodes.Count - 2, patchingCodes, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }

        // 關於提升屬性點所需要的點數
        [HarmonyPatch(typeof(StatsDistribution), nameof(StatsDistribution.GetAddCost), typeof(StatType))]
        static class StatsDistribution_GetAddCost_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // int statValue = StatValues[attribute];
                // ---------------- after  ----------------
                // int statValue = StatValues[attribute];
                // if (IsAvailable())
                //     if (statValue >= 18)
                //         statValue = 17;
                //     else if (statValue < 7)
                //         statValue = 7;
                List<CodeInstruction> findingCodes = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<StatsDistribution, Dictionary<StatType, int>>(nameof(StatsDistribution.StatValues))),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetPropertyInfo<Dictionary<StatType, int>, int>("Item").GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Stloc_0)
                };
                int startIndex = codes.FindCodes(findingCodes);

                Label skip = codes.NewLabel(startIndex + findingCodes.Count, il);
                Label nextCheck = il.DefineLabel();
                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brfalse, skip),
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)18),
                        new CodeInstruction(OpCodes.Blt, nextCheck),
                        new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)17),
                        new CodeInstruction(OpCodes.Stloc_0),
                        new CodeInstruction(OpCodes.Br, skip),
                        new CodeInstruction(OpCodes.Ldloc_0) { labels = { nextCheck } },
                        new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)7),
                        new CodeInstruction(OpCodes.Bge, skip),
                        new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)7),
                        new CodeInstruction(OpCodes.Stloc_0)
                    };
                    return codes.InsertRange(startIndex + findingCodes.Count, patchingCodes, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }

        // 關於降低屬性點能獲得的點數
        [HarmonyPatch(typeof(StatsDistribution), nameof(StatsDistribution.GetRemoveCost), typeof(StatType))]
        static class StatsDistribution_GetRemoveCost_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // int statValue = StatValues[attribute];
                // ---------------- after  ----------------
                // int statValue = StatValues[attribute];
                // if (IsAvailable())
                //     if (statValue > 18)
                //         statValue = 18;
                //     else if (statValue <= 7)
                //         statValue = 8;
                List<CodeInstruction> findingCodes = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<StatsDistribution, Dictionary<StatType, int>>(nameof(StatsDistribution.StatValues))),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetPropertyInfo<Dictionary<StatType, int>, int>("Item").GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Stloc_0)
                };
                int startIndex = codes.FindCodes(findingCodes);

                Label skip = codes.NewLabel(startIndex + findingCodes.Count, il);
                Label nextCheck = il.DefineLabel();
                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brfalse, skip),
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)18),
                        new CodeInstruction(OpCodes.Ble, nextCheck),
                        new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)18),
                        new CodeInstruction(OpCodes.Stloc_0),
                        new CodeInstruction(OpCodes.Br, skip),
                        new CodeInstruction(OpCodes.Ldloc_0) { labels = { nextCheck } },
                        new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)7),
                        new CodeInstruction(OpCodes.Bgt, skip),
                        new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)8),
                        new CodeInstruction(OpCodes.Stloc_0)
                    };
                    return codes.InsertRange(startIndex + findingCodes.Count, patchingCodes, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }
    }
}
