using Harmony12;
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
    internal static class RestrictCasterLevelToCharacterLevelTemporary
    {
        public static bool IsAvailable()
        {
            return Core.Enabled &&
                Core.Settings.toggleRestrictCasterLevelToCharacterLevelTemporary &&
                Core.Mod.LevelUpController.IsManualPlayerUnit();
        }

        public static bool Enabled {
            get => Core.Settings.toggleRestrictCasterLevelToCharacterLevelTemporary;
            set => Core.Settings.toggleRestrictCasterLevelToCharacterLevelTemporary = value;
        }

        // 限制施法者等級不得大於角色等級
        [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.CasterLevel), MethodType.Getter)]
        static class Spellbook_CasterLevel_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // m_CasterLevelInternal
                // ---------------- after  ----------------
                // IsAvailable() ? Math.Min(m_CasterLevelInternal, Owner.Progression.CharacterLevel) : m_CasterLevelInternal
                List<CodeInstruction> findingCodes = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<Spellbook, int>("m_CasterLevelInternal"))
                };
                int startIndex = codes.FindLastCodes(findingCodes);
                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brfalse, codes.NewLabel(startIndex + findingCodes.Count, il)),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld,
                            GetFieldInfo<Spellbook, UnitDescriptor>(nameof(Spellbook.Owner))),
                        new CodeInstruction(OpCodes.Ldfld,
                            GetFieldInfo<UnitDescriptor, UnitProgressionData>(nameof(UnitDescriptor.Progression))),
                        new CodeInstruction(OpCodes.Callvirt,
                            GetPropertyInfo<UnitProgressionData, int>(nameof(UnitProgressionData.CharacterLevel)).GetGetMethod(true)),
                        new CodeInstruction(OpCodes.Call,
                            new Func<int, int, int>(Math.Min).Method)
                    };
                    return codes.InsertRange(startIndex + findingCodes.Count, patchingCodes, false).Complete();
                }
                else
                {
                    throw new Exception($"Failed to patch '{MethodBase.GetCurrentMethod().DeclaringType}'");
                }
            }
        }

        // toggle off on applying spellbook (if not, the spontaneous casters cannot acquire new spells)
        [HarmonyPatch(typeof(ApplySpellbook), nameof(ApplySpellbook.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class ApplySpellbook_Apply_Patch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(ApplySpellbook __instance, ref bool __state)
            {
                if (IsAvailable())
                {
                    __state = true;
                    Enabled = false;
                }
            }

            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            static void Postfix(ApplySpellbook __instance, ref bool __state)
            {
                if (__state)
                    Enabled = true;
            }
        }
    }
}
