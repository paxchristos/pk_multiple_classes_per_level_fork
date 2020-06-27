using Harmony12;
using Kingmaker.UnitLogic;
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
    internal static class RestrictCasterLevelToCharacterLevel
    {
        public static bool IsAvailable()
        {
            return Core.Enabled &&
                Core.Settings.toggleRestrictCasterLevelToCharacterLevel &&
                Core.Mod.LevelUpController.IsManualPlayerUnit();
        }

        public static bool Enabled {
            get => Core.Settings.toggleRestrictCasterLevelToCharacterLevel;
            set => Core.Settings.toggleRestrictCasterLevelToCharacterLevel = value;
        }

        // 限制施法者等級不得大於角色等級
        [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.AddCasterLevel))]
        static class Spellbook_AddCasterLevel_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // m_CasterLevelInternal++;
                // ---------------- after  ----------------
                // m_CasterLevelInternal++;
                // if (IsAvailable())
                //     m_CasterLevelInternal = Math.Min(m_CasterLevelInternal, Owner.Progression.CharacterLevel);
                List<CodeInstruction> findingCodes = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Dup),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<Spellbook, int>("m_CasterLevelInternal")),
                    new CodeInstruction(OpCodes.Ldc_I4_1),
                    new CodeInstruction(OpCodes.Add),
                    new CodeInstruction(OpCodes.Stfld,
                        GetFieldInfo<Spellbook, int>("m_CasterLevelInternal"))
                };
                int startIndex = codes.FindCodes(findingCodes);
                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brfalse, codes.NewLabel(startIndex + findingCodes.Count, il)),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Ldfld,
                            GetFieldInfo<Spellbook, int>("m_CasterLevelInternal")),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld,
                            GetFieldInfo<Spellbook, UnitDescriptor>(nameof(Spellbook.Owner))),
                        new CodeInstruction(OpCodes.Ldfld,
                            GetFieldInfo<UnitDescriptor, UnitProgressionData>(nameof(UnitDescriptor.Progression))),
                        new CodeInstruction(OpCodes.Callvirt,
                            GetPropertyInfo<UnitProgressionData, int>(nameof(UnitProgressionData.CharacterLevel)).GetGetMethod(true)),
                        new CodeInstruction(OpCodes.Call,
                            new Func<int, int, int>(Math.Min).Method),
                        new CodeInstruction(OpCodes.Stfld,
                            GetFieldInfo<Spellbook, int>("m_CasterLevelInternal"))
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
