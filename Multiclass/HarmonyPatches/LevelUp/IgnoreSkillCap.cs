using Harmony12;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UI.LevelUp;
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
    internal static class IgnoreSkillCap
    {
        public static bool IsAvailable()
        {
            return Core.Enabled &&
                Core.Settings.toggleIgnoreSkillCap &&
                Core.Mod.LevelUpController.IsManualPlayerUnit();
        }

        public static bool Enabled {
            get => Core.Settings.toggleIgnoreSkillCap;
            set => Core.Settings.toggleIgnoreSkillCap = value;
        }

        // 關於技能數值大於等於角色等級時能否繼續分配技能點 (內部邏輯)
        [HarmonyPatch(typeof(SpendSkillPoint), nameof(SpendSkillPoint.Check), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class SpendSkillPoint_Check_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // if (unit.Stats.GetStat(Skill).BaseValue >= state.NextLevel)
                // ---------------- after  ----------------
                // if (!IgnoreSkillCap.IsAvailable() && unit.Stats.GetStat(Skill).BaseValue >= state.NextLevel)
                List<CodeInstruction> findingCodes = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_2),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<UnitDescriptor, CharacterStats>(nameof(UnitDescriptor.Stats))),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld,
                         GetFieldInfo<SpendSkillPoint, StatType>(nameof(SpendSkillPoint.Skill))),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetMethodInfo<CharacterStats, Func<CharacterStats, StatType, ModifiableValue>>(nameof(CharacterStats.GetStat))),
                        //typeof(CharacterStats).GetMethods().Single(m =>
                        //m.Name == nameof(CharacterStats.GetStat) &&
                        //!m.IsGenericMethodDefinition &&
                        //m.GetParameters().Length == 1 &&
                        //m.GetParameters().First().ParameterType == typeof(StatType))),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetPropertyInfo<ModifiableValue, int>(nameof(ModifiableValue.BaseValue)).GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<LevelUpState, int>(nameof(LevelUpState.NextLevel))),
                    new CodeInstruction(OpCodes.Blt)
                };
                int startIndex = codes.FindLastCodes(findingCodes);
                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brtrue, codes.Item(startIndex + findingCodes.Count - 1).operand)
                    };
                    return codes.InsertRange(startIndex, patchingCodes, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }

        // 關於技能數值大於等於角色等級時能否繼續分配技能點 (介面按鈕)
        [HarmonyPatch(typeof(CharBSkillsAllocator), nameof(CharBSkillsAllocator.FillLevelUpData))]
        static class CharBSkillsAllocator_FillLevelUpData_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // bool flag = m_CharacterBuildController.LevelUpController.Preview.Unit.Descriptor.Progression.CharacterLevel > stat2.BaseValue;
                // ---------------- after  ----------------
                // bool flag = IgnoreSkillCap.IsAvailable() || m_CharacterBuildController.LevelUpController.Preview.Unit.Descriptor.Progression.CharacterLevel > stat2.BaseValue;
                List<CodeInstruction> findingCodes = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call,
                        GetPropertyInfo<CharBSkillsAllocator, CharacterBuildController>("m_" + nameof(CharacterBuildController)).GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetPropertyInfo<CharacterBuildController, LevelUpController>(nameof(CharacterBuildController.LevelUpController)).GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<LevelUpController, UnitDescriptor>(nameof(LevelUpController.Preview))),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetPropertyInfo<UnitDescriptor, UnitEntityData>(nameof(UnitDescriptor.Unit)).GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetPropertyInfo<UnitEntityData, UnitDescriptor>(nameof(UnitEntityData.Descriptor)).GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<UnitDescriptor, UnitProgressionData>(nameof(UnitDescriptor.Progression))),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetPropertyInfo<UnitProgressionData, int>(nameof(UnitProgressionData.CharacterLevel)).GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Ldloc_S),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetPropertyInfo<ModifiableValue, int>(nameof(ModifiableValue.BaseValue)).GetGetMethod(true)),
                    new CodeInstruction(OpCodes.Cgt),
                    new CodeInstruction(OpCodes.Stloc_S)
                };
                int startIndex = codes.FindLastCodes(findingCodes);
                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brfalse, codes.NewLabel(startIndex, il)),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Br, codes.NewLabel(startIndex + findingCodes.Count - 1, il)),
                    };
                    return codes.InsertRange(startIndex, patchingCodes, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }
    }
}
