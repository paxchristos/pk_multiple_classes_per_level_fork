using Harmony12;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using ModMaker.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using static Multiclass.Main;

namespace Multiclass.HarmonyPatches
{
    internal static class AlwaysReceiveFavoredClassHPExceptPrestige
    {
        public static bool IsAvailable()
        {
            return Core.Enabled &&
                Core.Settings.toggleAlwaysReceiveFavoredClassHPExceptPrestige;
        }

        public static bool Enabled {
            get => Core.Settings.toggleAlwaysReceiveFavoredClassHPExceptPrestige;
            set => Core.Settings.toggleAlwaysReceiveFavoredClassHPExceptPrestige = value;
        }

        [HarmonyPatch(typeof(ApplyClassMechanics), "ApplyHitPoints")]
        static class ApplyClassMechanics_ApplyHitPoints_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // favoredClassBonus = 0;
                // ---------------- after  ----------------
                // if (IsAvailable())
                //     favoredClassBonus = 0;
                List<CodeInstruction> findingCodes = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldc_I4_0),
                    new CodeInstruction(OpCodes.Stloc_3)
                };
                int startIndex = codes.FindCodes(findingCodes);
                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brfalse, codes.NewLabel(startIndex, il)),
                        new CodeInstruction(OpCodes.Br, codes.NewLabel(startIndex + findingCodes.Count, il))
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
