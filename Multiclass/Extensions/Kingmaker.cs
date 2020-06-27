using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UI.LevelUp.Phase;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using System;
using System.Linq;
using static ModMaker.Utils.ReflectionCache;

namespace Multiclass.Extensions
{
    public static class Kingmaker
    {
        public static void AddClassLevel_NotCharacterLevel(this UnitProgressionData instance, BlueprintCharacterClass characterClass)
        {
            //instance.SureClassData(characterClass).Level++;
            GetMethodDel<UnitProgressionData, Func<UnitProgressionData, BlueprintCharacterClass, ClassData>>
                ("SureClassData")(instance, characterClass).Level++;
            //instance.CharacterLevel++;
            instance.Classes.Sort();
            instance.Features.AddFact(characterClass.Progression, null);
            instance.Owner.OnGainClassLevel(characterClass);
            //int[] bonuses = BlueprintRoot.Instance.Progression.XPTable.Bonuses;
            //int val = bonuses[Math.Min(bonuses.Length - 1, instance.CharacterLevel)];
            //instance.Experience = Math.Max(instance.Experience, val);
        }

        public static void Apply_NoStatsAndHitPoints(this ApplyClassMechanics instance, LevelUpState state, UnitDescriptor unit)
        {
            if (state.SelectedClass != null)
            {
                ClassData classData = unit.Progression.GetClassData(state.SelectedClass);
                if (classData != null)
                {
                    //GetMethodDel<ApplyClassMechanics, Action<ApplyClassMechanics, LevelUpState, ClassData, UnitDescriptor>>
                    //    ("ApplyBaseStats")(null, state, classData, unit);
                    //GetMethodDel<ApplyClassMechanics, Action<ApplyClassMechanics, LevelUpState, ClassData, UnitDescriptor>>
                    //    ("ApplyHitPoints")(null, state, classData, unit);
                    GetMethodDel<ApplyClassMechanics, Action<ApplyClassMechanics, ClassData, UnitDescriptor>>
                        ("ApplyClassSkills")(null, classData, unit);
                    GetMethodDel<ApplyClassMechanics, Action<ApplyClassMechanics, LevelUpState, UnitDescriptor>>
                        ("ApplyProgressions")(null, state, unit);
                }
            }
        }

        public static BlueprintCharacterClass GetOwnerClass(this BlueprintSpellbook spellbook, UnitDescriptor unit)
        {
            return unit.Progression.Classes.FirstOrDefault(item => item.Spellbook == spellbook)?.CharacterClass;
        }

        public static ClassData GetOwnerClassData(this BlueprintSpellbook spellbook, UnitDescriptor unit)
        {
            return unit.Progression.Classes.FirstOrDefault(item => item.Spellbook == spellbook);
        }

        public static BlueprintCharacterClass GetSourceClass(this BlueprintFeature feature, UnitDescriptor unit)
        {
            return unit.Progression.Features.Enumerable.FirstOrDefault(item => item.Blueprint == feature)?.GetSourceClass();
        }
        
        public static bool IsChildProgressionOf(this BlueprintProgression progression, UnitDescriptor unit, BlueprintCharacterClass characterClass)
        {
            ClassData classData = unit.Progression.GetClassData(characterClass);
            if (classData != null)
            {
                if (progression.Classes.Contains(characterClass) &&
                    !progression.Archetypes.Intersect(characterClass.Archetypes).Any())
                    return true;
                if (progression.Archetypes.Intersect(unit.Progression.GetClassData(characterClass).Archetypes).Any())
                    return true;
            }
            return false;
        }

        public static bool IsManualPlayerUnit(this LevelUpController controller, bool allowPet = false, bool allowAutoCommit = false, bool allowPregen = false)
        {
            return controller != null &&
                controller.Unit.IsPlayerFaction &&
                (allowPet || !controller.Unit.IsPet) &&
                (allowAutoCommit || !controller.AutoCommit) &&
                (allowPregen || !controller.State.IsPreGen());
        }

        public static bool IsCharGen(this LevelUpState state)
        {
            return state.Mode == LevelUpState.CharBuildMode.CharGen;
        }

        public static bool IsLevelUp(this LevelUpState state)
        {
            return state.Mode == LevelUpState.CharBuildMode.LevelUp;
        }

        public static bool IsPreGen(this LevelUpState state)
        {
            return state.Mode == LevelUpState.CharBuildMode.PreGen;
        }

        public static void SetSpellbook(this CharBPhaseSpells instance, BlueprintCharacterClass characterClass)
        {
            LevelUpState state = Game.Instance.UI.CharacterBuildController.LevelUpController.State;
            BlueprintCharacterClass selectedClass = state.SelectedClass;
            state.SelectedClass = characterClass;
            GetMethodDel<CharBPhaseSpells, Action<CharBPhaseSpells>>("SetupSpellBookView")(instance);
            state.SelectedClass = selectedClass;
        }

        //public static ClassData SureClassData(this UnitProgressionData progression, BlueprintCharacterClass characterClass)
        //{
        //    ClassData classData = progression.Classes.FirstOrDefault((ClassData cd) => cd.CharacterClass == characterClass);
        //    if (classData == null)
        //    {
        //        classData = new ClassData(characterClass);
        //        progression.Classes.Add(classData);
        //    }
        //    return classData;
        //}
    }
}
