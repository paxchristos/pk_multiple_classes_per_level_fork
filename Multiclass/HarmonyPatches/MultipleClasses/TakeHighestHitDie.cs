using Harmony12;
using Kingmaker.Blueprints.Classes;
using Kingmaker.RuleSystem;
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
    internal static class TakeHighestHitDie
    {
        public static bool IsAvailable()
        {
            return MultipleClasses.IsAvailable() &&
                Core.Settings.toggleTakeHighestHitDie;
        }

        public static bool Enabled {
            get => Core.Settings.toggleTakeHighestHitDie;
            set => Core.Settings.toggleTakeHighestHitDie = value;
        }

        private static int CalcHighestHitDie(int hitDie)
        {
            foreach (BlueprintCharacterClass characterClass in Core.Mod.AppliedMulticlassSet)
                hitDie = Math.Max(hitDie, (int)characterClass.HitDie);
            return hitDie;
        }

        [HarmonyPatch(typeof(ApplyClassMechanics), "ApplyHitPoints")]
        static class ApplyClassMechanics_ApplyHitPoints_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // int hitDie = (int)classData.CharacterClass.HitDie;
                // ---------------- after  ----------------
                // int hitDie = (int)classData.CharacterClass.HitDie;
                // if (IsAvailable())
                //     hitDie = TakeHighestHitDie(hitDie);
                List<CodeInstruction> findingCodes = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<ClassData, BlueprintCharacterClass>(nameof(ClassData.CharacterClass))),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<BlueprintCharacterClass, DiceType>(nameof(BlueprintCharacterClass.HitDie))),
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
                        new CodeInstruction(OpCodes.Call,
                            new Func<int, int>(CalcHighestHitDie).Method),
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
