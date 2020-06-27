using Harmony12;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
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
    internal static class RestrictClassLevelForPrerequisitesToCharacterLevel
    {
        public static bool IsAvailable()
        {
            return Core.Enabled &&
                Core.Settings.toggleRestrictClassLevelForPrerequisitesToCharacterLevel &&
                Core.Mod.LevelUpController.IsManualPlayerUnit();
        }

        public static bool Enabled {
            get => Core.Settings.toggleRestrictClassLevelForPrerequisitesToCharacterLevel;
            set => Core.Settings.toggleRestrictClassLevelForPrerequisitesToCharacterLevel = value;
        }

        // 限制用於滿足專長的參考角色等級不得大於實際角色等級
        [HarmonyPatch(typeof(PrerequisiteClassLevel), nameof(PrerequisiteClassLevel.Check))]
        static class PrerequisiteClassLevel_Check_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator il)
            {
                // ---------------- before ----------------
                // unit.Progression.GetClassLevel(CharacterClass) + fakeLevels;
                // ---------------- after  ----------------
                // IsAvailable() ? 
                //     Math.Min(unit.Progression.GetClassLevel(CharacterClass) + fakeLevels, unit.Progression.CharacterLevel);
                //     unit.Progression.GetClassLevel(CharacterClass) + fakeLevels :
                List<CodeInstruction> findingCodes = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_2),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<UnitDescriptor, UnitProgressionData>(nameof(UnitDescriptor.Progression))),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld,
                        GetFieldInfo<PrerequisiteClassLevel, BlueprintCharacterClass>(nameof(PrerequisiteClassLevel.CharacterClass))),
                    new CodeInstruction(OpCodes.Callvirt,
                        GetMethodInfo<UnitProgressionData, Func<UnitProgressionData, BlueprintCharacterClass, int>>(nameof(UnitProgressionData.GetClassLevel))),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Add)
                };
                int startIndex = codes.FindLastCodes(findingCodes);
                if (startIndex >= 0)
                {
                    List<CodeInstruction> patchingCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call,
                            new Func<bool>(IsAvailable).Method),
                        new CodeInstruction(OpCodes.Brfalse, codes.NewLabel(startIndex + findingCodes.Count, il)),
                        new CodeInstruction(OpCodes.Ldarg_2),
                        new CodeInstruction(OpCodes.Ldfld,
                            GetFieldInfo<UnitDescriptor, UnitProgressionData>(nameof(UnitDescriptor.Progression))),
                        new CodeInstruction(OpCodes.Callvirt,
                            GetPropertyInfo<UnitProgressionData, int>(nameof(UnitProgressionData.CharacterLevel)).GetGetMethod(true)),
                        new CodeInstruction(OpCodes.Call,
                            new Func<int, int, int>(Math.Min).Method)
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
