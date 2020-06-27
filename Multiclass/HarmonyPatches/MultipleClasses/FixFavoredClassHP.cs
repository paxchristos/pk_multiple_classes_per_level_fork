using Harmony12;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
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
    internal static class FixFavoredClassHP
    {
        public static bool IsAvailable()
        {
            return Core.Enabled &&
                Core.Settings.toggleFixFavoredClassHP;
        }

        public static bool Enabled {
            get => Core.Settings.toggleFixFavoredClassHP;
            set => Core.Settings.toggleFixFavoredClassHP = value;
        }

        [HarmonyPatch(typeof(ApplyClassMechanics), "ApplyHitPoints")]
        static class ApplyClassMechanics_ApplyHitPoints_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // @class.Level >= nextLevel && ...
                // ---------------- after  ----------------
                // @class.Level >= (IsAvailable() ? state.NextClassLevel : nextLevel) && ...
                List<CodeInstruction> findingCodes = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldloc_S),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<ClassData, int>(nameof(ClassData.Level))),
                    new CodeInstruction(OpCodes.Ldloc_2),
                    new CodeInstruction(OpCodes.Blt)
                };
                int startIndex = codes.FindCodes(findingCodes);
                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brfalse, codes.NewLabel(startIndex + 2, il)),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld,
                            GetFieldInfo<LevelUpState, int>(nameof(LevelUpState.NextClassLevel))),
                        new CodeInstruction(OpCodes.Br, codes.NewLabel(startIndex + 3, il))
                   };
                    return codes.InsertRange(startIndex + 2, patchingCodes, true).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }
    }
}
