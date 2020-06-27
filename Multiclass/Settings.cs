using ModMaker.Utils;
using System.Collections.Generic;
using UnityModManagerNet;

namespace Multiclass
{
    public class Settings : UnityModManager.ModSettings
    {
        public bool toggleMulticlass = false;
        public HashSet<string> selectedMulticlassSet = new HashSet<string>();
        public HashSet<string> selectedCharGenMulticlassSet = new HashSet<string>();
        public SerializableDictionary<string, HashSet<string>> selectedCompanionMulticlassSet = new SerializableDictionary<string, HashSet<string>>();

        public bool toggleTakeHighestHitDie = true;
        public bool toggleTakeHighestSkillPoints = true;
        public bool toggleTakeHighestBAB = true;
        public bool toggleTakeHighestSaveByRecalculation = true;
        public bool toggleTakeHighestSaveByAlways = false;

        public bool toggleRecalculateCasterLevelOnLevelingUp = true;
        public bool toggleRestrictCasterLevelToCharacterLevel = true;
        public bool toggleRestrictCasterLevelToCharacterLevelTemporary = false;
        public bool toggleRestrictClassLevelForPrerequisitesToCharacterLevel = true;

        public bool toggleFixFavoredClassHP = false;
        public bool toggleAlwaysReceiveFavoredClassHPExceptPrestige = true;
        public bool toggleAlwaysReceiveFavoredClassHP = false;

        public bool toggleLockCharacterLevel = false;
        public bool toggleIgnoreAlignmentRestriction = false;
        public bool toggleIgnoreAttributeStatCapChargen = false;
        public bool toggleIgnoreAttributePointsRemainingChargen = false;
        public bool toggleIgnoreAttributePointsRemaining = false;
        public bool toggleIgnoreSkillCap = false;
        public bool toggleIgnoreSkillPointsRemaining = false;

        public bool toggleIgnorePrerequisites = false;
        public HashSet<string> ignoredPrerequisiteSet = new HashSet<string>();

        public bool toggleIgnoreSpellbookAlignmentRestriction = false;
        public bool toggleIgnoreAbilityCasterCheckers = false;
        public HashSet<string> ignoredAbilityCasterCheckerSet = new HashSet<string>();
        public bool toggleIgnoreActivatableAbilityRestrictions = false;
        public HashSet<string> ignoredActivatableAbilityRestrictionSet = new HashSet<string>();
        public bool toggleIgnoreEquipmentRestrictions = false;
        public HashSet<string> ignoredEquipmentRestrictionSet = new HashSet<string>();
        public bool toggleIgnoreBuildingRestrictions = false;
        public HashSet<string> ignoredBuildingRestrictionSet = new HashSet<string>();
    }
}
