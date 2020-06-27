using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Multiclass.Extensions;
using Multiclass.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Multiclass.Main;

namespace Multiclass.HarmonyPatches
{
    internal static class RecalculateCasterLevelOnLevelingUp
    {
        public static bool IsAvailable()
        {
            return Core.Enabled &&
                Core.Settings.toggleRecalculateCasterLevelOnLevelingUp &&
                Core.Mod.LevelUpController.IsManualPlayerUnit();
        }

        public static bool Enabled {
            get => Core.Settings.toggleRecalculateCasterLevelOnLevelingUp;
            set => Core.Settings.toggleRecalculateCasterLevelOnLevelingUp = value;
        }

        [HarmonyPatch(typeof(ApplySpellbook), nameof(ApplySpellbook.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class ApplySpellbook_Apply_Patch
        {
            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            static void Postfix(MethodBase __originalMethod, ApplySpellbook __instance, LevelUpState state, UnitDescriptor unit)
            {
                if (!IsAvailable())
                    return;

                if (!Core.Mod.LockedPatchedMethods.Contains(__originalMethod))
                {
                    Core.Mod.LockedPatchedMethods.Add(__originalMethod);

                    Dictionary<Spellbook, int> spellbookCasterLevels = new Dictionary<Spellbook, int>();

                    // simulate the internal caster level
                    foreach (ClassData classData in unit.Progression.Classes)
                    {
                        if (classData != null && classData.Spellbook != null)
                        {
                            int[] skipLevelList = classData.CharacterClass.GetComponent<SkipLevelsForSpellProgression>()?.Levels;
                            int skipLevels = 0;
                            // Eldritch Knight || Dragon Disciple
                            if (classData.CharacterClass.AssetGuid == "de52b73972f0ed74c87f8f6a8e20b542" ||
                                classData.CharacterClass.AssetGuid == "72051275b1dbb2d42ba9118237794f7c")
                                skipLevels = 1;

                            Spellbook spellbook = unit.DemandSpellbook(classData.Spellbook);
                            if (!spellbookCasterLevels.TryGetValue(spellbook, out int casterLevel))
                                casterLevel = 0;
                            for (int i = 1 + skipLevels; i <= classData.Level; i++)
                            {
                                if (skipLevelList == null || !skipLevelList.Contains(i))
                                {
                                    casterLevel++;
                                }
                            }
                            spellbookCasterLevels[spellbook] = casterLevel;
                        }
                    }

                    // fucking Mystic
                    foreach (Feature feature in unit.Progression.Features.Enumerable
                        .Where(feature => feature.Components.Any(item => item is AddSpellbookLevel)))
                    {
                        foreach (AddSpellbookLevel fact in feature.Components
                            .Where(item => item is AddSpellbookLevel))
                        {
                            ClassData classData = unit.Progression.GetClassData(feature.GetSourceClass());
                            int[] skipLevelList = classData.CharacterClass.GetComponent<SkipLevelsForSpellProgression>()?.Levels;

                            Spellbook spellbook = fact.Owner.DemandSpellbook(fact.Spellbook);
                            if (!spellbookCasterLevels.TryGetValue(spellbook, out int casterLevel))
                                casterLevel = 0;
                            for (int i = 1; i <= classData.Level; i++)
                            {
                                if (skipLevelList == null || !skipLevelList.Contains(i))
                                {
                                    casterLevel++;
                                }
                            }
                            spellbookCasterLevels[spellbook] = casterLevel;
                        }
                    }

                    // simulate the caster level
                    foreach (Spellbook spellbook in new List<Spellbook>(spellbookCasterLevels.Keys))
                    {
                        int casterLevel = spellbookCasterLevels[spellbook];
                        int maxLevel = spellbook.Blueprint.SpellsPerDay.Levels.Length - 1;
                        if (RestrictCasterLevelToCharacterLevel.IsAvailable())
                            casterLevel = Math.Min(casterLevel, unit.Progression.CharacterLevel);
                        casterLevel = Math.Max(0, Math.Min(maxLevel, casterLevel + spellbook.Blueprint.CasterLevelModifier));
                        spellbookCasterLevels[spellbook] = casterLevel;
                        Core.Debug($"   - Recalculated spellbook: {spellbook.Blueprint.Name}, target level: {casterLevel}, current level: {spellbook.CasterLevel}");
                    }

                    // check and increase the caster level of the spellbooks
                    StateReplacer stateReplacer = new StateReplacer(state);
                    foreach (Spellbook spellbook in spellbookCasterLevels.Keys)
                    {
                        BlueprintCharacterClass characterClass = spellbook.Blueprint.GetOwnerClass(unit);
                        stateReplacer.Replace(characterClass, unit.Progression.GetClassLevel(characterClass));

                        // We need __instance.Apply(state, unit) to always lead to an increase in the caster level
                        SkipLevelsForSpellProgression skipLevels = characterClass.GetComponent<SkipLevelsForSpellProgression>();
                        int[] backup = skipLevels?.Levels;
                        if (skipLevels != null)
                            skipLevels.Levels = new int[0];

                        int casterLevel = spellbookCasterLevels[spellbook];
                        while (spellbook.CasterLevel < casterLevel)
                        {
                            Core.Debug($"   - Applying caster level: {spellbook.CasterLevel} => {casterLevel}");
                            __instance.Apply(state, unit);
                        }

                        if (skipLevels != null)
                            skipLevels.Levels = backup;
                    }
                    stateReplacer.Restore();

                    Core.Mod.LockedPatchedMethods.Remove(__originalMethod);
                }
            }
        }
    }
}
