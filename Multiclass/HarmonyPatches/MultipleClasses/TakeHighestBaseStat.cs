using Harmony12;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
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
    internal static class TakeHighestBaseStat
    {
        internal static bool IsAvailable()
        {
            return MultipleClasses.IsAvailable();
        }

        public static bool EnabledBAB {
            get => Core.Settings.toggleTakeHighestBAB;
            set => Core.Settings.toggleTakeHighestBAB = value;
        }

        public static bool EnabledSaveRecalculation {
            get => Core.Settings.toggleTakeHighestSaveByRecalculation;
            set => Core.Settings.toggleTakeHighestSaveByRecalculation = value;
        }

        public static bool EnabledSaveAlwaysHigh {
            get => Core.Settings.toggleTakeHighestSaveByAlways;
            set => Core.Settings.toggleTakeHighestSaveByAlways = value;
        }

        private static int CalcGainingBaseStat(int value, UnitDescriptor unit, StatType statType)
        {
            // take Highest BAB
            if (EnabledBAB && statType == StatType.BaseAttackBonus)
            {
                foreach (BlueprintCharacterClass characterClass in Core.Mod.AppliedMulticlassSet)
                {
                    ClassData classData =
                        GetMethodDel<UnitProgressionData, Func<UnitProgressionData, BlueprintCharacterClass, ClassData>>
                        ("SureClassData")(unit.Progression, characterClass);
                    int newClassLevel = classData.Level;
                    int oldClassLevel = newClassLevel - 1;
                    BlueprintStatProgression BAB = classData.BaseAttackBonus;
                    value = Math.Max(value, BAB.GetBonus(newClassLevel) - BAB.GetBonus(oldClassLevel));
                }
                return value;
            }

            // recalculate save
            else if (EnabledSaveRecalculation && statType != StatType.BaseAttackBonus)
            {
                Func<ClassData, BlueprintStatProgression> getStatProgression;
                switch (statType)
                {
                    case StatType.SaveFortitude:
                        getStatProgression = (ClassData c) => c.FortitudeSave;
                        break;
                    case StatType.SaveReflex:
                        getStatProgression = (ClassData c) => c.ReflexSave;
                        break;
                    case StatType.SaveWill:
                        getStatProgression = (ClassData c) => c.WillSave;
                        break;
                    default:
                        return value;
                }

                BlueprintStatProgression savesHigh =
                    Core.Mod.LibraryObject.BlueprintsByAssetId["ff4662bde9e75f145853417313842751"] as BlueprintStatProgression;
                BlueprintStatProgression savesLow =
                    Core.Mod.LibraryObject.BlueprintsByAssetId["dc0c7c1aba755c54f96c089cdf7d14a3"] as BlueprintStatProgression;

                int saveHighLevel = 0;
                foreach (ClassData classData in unit.Progression.Classes)
                {
                    if (getStatProgression(classData) == savesHigh)
                        saveHighLevel = Math.Max(saveHighLevel, classData.Level);
                }

                int characterLevel = unit.Progression.CharacterLevel;
                if (saveHighLevel > characterLevel)
                    saveHighLevel = characterLevel;

                Core.Debug($"   - Recalculated {statType} has {saveHighLevel} high saves level(s).");
                return (int)Math.Floor((saveHighLevel > 0 ? 2d : 0d) + saveHighLevel / 2d + (characterLevel - saveHighLevel) / 3d)
                    - unit.Stats.GetStat(statType).BaseValue;
            }

            // taking save from high save table by character level
            else if (EnabledSaveAlwaysHigh && statType != StatType.BaseAttackBonus)
            {
                int newCharacterLevel = unit.Progression.CharacterLevel;
                int oldCharacterLevel = newCharacterLevel - 1;
                BlueprintStatProgression savesHigh =
                    Core.Mod.LibraryObject.BlueprintsByAssetId["ff4662bde9e75f145853417313842751"] as BlueprintStatProgression;
                return savesHigh.GetBonus(newCharacterLevel) - savesHigh.GetBonus(oldCharacterLevel);
            }
            return value;
        }

        [HarmonyPatch(typeof(ApplyClassMechanics), "AddBaseStat")]
        static class ApplyClassMechanics_AddBaseStat_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // int value = progression.GetBonus(newClassLevel) - progression.GetBonus(oldClassLevel);
                // ---------------- after  ----------------
                // int value = progression.GetBonus(newClassLevel) - progression.GetBonus(oldClassLevel);
                // if (IsAvailable())
                //     value = TakeHighestBaseStat(value);
                List<CodeInstruction> findingCodes = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_S),
                    new CodeInstruction(OpCodes.Ldarg_2),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetMethodInfo<BlueprintStatProgression, Func<BlueprintStatProgression, int, int>>
                        (nameof(BlueprintStatProgression.GetBonus))),
                    new CodeInstruction(OpCodes.Ldarg_S),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetMethodInfo<BlueprintStatProgression, Func<BlueprintStatProgression, int, int>>
                        (nameof(BlueprintStatProgression.GetBonus))),
                    new CodeInstruction(OpCodes.Sub),
                    new CodeInstruction(OpCodes.Stloc_0)
                };
                int startIndex = codes.FindCodes(findingCodes);
                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brfalse, codes.NewLabel(startIndex + findingCodes.Count, il)),
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldarg_3),
                        new CodeInstruction(OpCodes.Call,
                            new Func<int, UnitDescriptor, StatType, int>(CalcGainingBaseStat).Method),
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
