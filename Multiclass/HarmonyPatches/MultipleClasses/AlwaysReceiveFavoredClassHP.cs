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
    internal static class AlwaysReceiveFavoredClassHP
    {
        public static bool IsAvailable()
        {
            return Core.Enabled &&
                Core.Settings.toggleAlwaysReceiveFavoredClassHP;
        }

        public static bool Enabled {
            get => Core.Settings.toggleAlwaysReceiveFavoredClassHP;
            set => Core.Settings.toggleAlwaysReceiveFavoredClassHP = value;
        }

        [HarmonyPatch(typeof(ApplyClassMechanics), "ApplyHitPoints")]
        static class ApplyClassMechanics_ApplyHitPoints_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // ... + favoredClassBonus;
                // ---------------- after  ----------------
                // ... + (IsAvailable() ? 1 : favoredClassBonus);
                List<CodeInstruction> findingCodes = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldloc_3),
                    new CodeInstruction(OpCodes.Add)
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
                        new CodeInstruction(OpCodes.Br, codes.NewLabel(startIndex + 1, il))
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
