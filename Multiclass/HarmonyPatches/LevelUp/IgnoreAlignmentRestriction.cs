using Harmony12;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Class.LevelUp;
using ModMaker.Extensions;
using Multiclass.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using static Multiclass.Main;

namespace Multiclass.HarmonyPatches
{
    internal static class IgnoreAlignmentRestriction
    {
        public static bool IsAvailable()
        {
            return Core.Enabled &&
                Core.Settings.toggleIgnoreAlignmentRestriction &&
                Core.Mod.LevelUpController.IsManualPlayerUnit();
        }

        public static bool Enabled {
            get => Core.Settings.toggleIgnoreAlignmentRestriction;
            set => Core.Settings.toggleIgnoreAlignmentRestriction = value;
        }

        // 關於能否忽略角色既有的陣營限制來選擇陣營 (角色創建介面)
        [HarmonyPatch(typeof(LevelUpState), nameof(LevelUpState.IsAlignmentRestricted), new Type[] { typeof(Alignment) })]
        static class LevelUpState_IsAlignmentRestricted_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- after  ----------------
                // if (IgnoreAlignmentRestriction.IsAvailable())
                //     return true;
                // ... original codes ...
                List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Call,
                        new Func<bool>(IsAvailable).Method),
                    new CodeInstruction(OpCodes.Brfalse, codes.NewLabel(0, il)),
                    new CodeInstruction(OpCodes.Ldc_I4_0),
                    new CodeInstruction(OpCodes.Ret),
                };
                return codes.InsertRange(0, patchingCodes, true).Complete();
            }
        }
    }
}
