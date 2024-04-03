using ModComponent.SDK.Components;
using ModComponent.Utilities;
using UnityEngine;

namespace ModComponent.Behaviours
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modmillablebehaviour")]
    public class ModMillableBehaviour : ModBaseBehaviour
    {
        [Tooltip("Time to repair (minutes).")]
        public int RepairDurationMinutes;

        [Tooltip("An array of multiple repair required gear")]
        public RepairRequired[] RepairRequired;

        [Tooltip("Can be fixed from ruined state?")]
        public bool CanRestoreFromWornOut;

        [Tooltip("Time to restore (minutes).")]
        public int RecoveryDurationMinutes;

        [Tooltip("An array of multiple restore required gear.")]
        public RestoreRequired[] RestoreRequired;

        [Tooltip("Skill for repair/restoration.")]
        public SkillType Skill;
    }

    [System.Serializable]
    public class RepairRequired
    {
        [Tooltip("Items needed for repair.")]
        public DataGearAsset RepairRequiredGear;

        [Tooltip("Amount of each item needed for repair.")]
        public int RepairRequiredGearUnits;
    }

    [System.Serializable]
    public class RestoreRequired
    {
        [Tooltip("Items needed for restoration.")]
        public DataGearAsset RestoreRequiredGear;

        [Tooltip("Amount of each item needed for restoration.")]
        public int RestoreRequiredGearUnits;
    }
}