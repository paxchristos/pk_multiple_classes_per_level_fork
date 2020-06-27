using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.FactLogic;
using ModMaker.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using static ModMaker.Utils.ReflectionCache;
using static Multiclass.Main;

namespace Multiclass.HarmonyPatches
{
    internal static class IgnoreSpellbookAlignmentRestriction
    {
        internal static bool IsAvailable(UnitDescriptor unit)
        {
            return Core != null && 
                Core.Settings.toggleIgnoreSpellbookAlignmentRestriction &&
                unit.IsPlayerFaction;
        }

        public static bool Enabled {
            get => Core.Settings.toggleIgnoreSpellbookAlignmentRestriction;
            set => Core.Settings.toggleIgnoreSpellbookAlignmentRestriction = value;
        }

        [HarmonyPatch(typeof(ForbidSpellbookOnAlignmentDeviation), nameof(ForbidSpellbookOnAlignmentDeviation.CheckAlignment))]
        static class ForbidSpellbookOnAlignmentDeviation_CheckAlignment_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // Alignment
                // ---------------- after  ----------------
                // IsAvailable() ? AlignmentMaskType.Any : Alignment
                List<CodeInstruction> findingCodes = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, 
                        GetFieldInfo<ForbidSpellbookOnAlignmentDeviation, AlignmentMaskType>(nameof(ForbidSpellbookOnAlignmentDeviation.Alignment)))
                };
                int startIndex = codes.FindCodes(findingCodes);
                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, 
                            GetPropertyInfo<OwnedGameLogicComponent<UnitDescriptor>, UnitDescriptor>(nameof(OwnedGameLogicComponent<UnitDescriptor>.Owner)).GetGetMethod(true)),
                        new CodeInstruction(OpCodes.Call,
                            new Func<UnitDescriptor, bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brfalse, codes.NewLabel(startIndex, il)),
                        new CodeInstruction(OpCodes.Ldc_I4, (int)AlignmentMaskType.Any),
                        new CodeInstruction(OpCodes.Br, codes.NewLabel(startIndex + findingCodes.Count, il)),
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
