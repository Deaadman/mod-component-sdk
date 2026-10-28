using System;
using System.Collections.Generic;
using ModComponent.SDK.References;
using UnityEngine;

// SuccessModifier and DurationOffset are left out of the fire-making behaviours: ModComponent 7.0.0 never applies them.
namespace ModComponent.SDK.Modules
{
    [Serializable]
    [Module("ModAccelerantBehaviour", Category = "Fire", Description = "Helps start fires.")]
    [ConflictsWith(typeof(BurnableBehaviour), typeof(FireStarterBehaviour), typeof(TinderBehaviour))]
    public sealed class AccelerantBehaviour : BehaviourModule
    {
        [Tooltip("Destroy the item after one use.")]
        public bool DestroyedOnUse = true;
    }

    [Serializable]
    [Module("ModBurnableBehaviour", Category = "Fire", Description = "Fuel that can be added to a fire.")]
    [ConflictsWith(typeof(AccelerantBehaviour), typeof(FireStarterBehaviour), typeof(TinderBehaviour))]
    public sealed class BurnableBehaviour : BehaviourModule
    {
        [Unit("min"), Min(0), Tooltip("Burn time added to the fire.")]
        public int BurningMinutes = 60;

        [Unit("min"), Min(0), Tooltip("Minutes the fire must have burned before this can be added.")]
        public float BurningMinutesBeforeAllowedToAdd;

        [Unit("°C"), Tooltip("Fire temperature increase.")]
        public float TempIncrease;
    }

    [Serializable]
    [Module("ModCarryingCapacityBehaviour", DisplayName = "Carrying Capacity", Category = "Survival",
        Description = "Raises carrying capacity while worn or carried.")]
    public sealed class CarryingCapacityBehaviour : BehaviourModule
    {
        [Unit("kg")] public float MaxCarryCapacityKGBuff;
    }

    [Serializable]
    [Module("ModEvolveBehaviour", Category = "Survival", Description = "Turns into another item over time, like drying hides.")]
    public sealed class EvolveBehaviour : BehaviourModule
    {
        [Tooltip("Item this turns into.")]
        public GearRef TargetItemName;

        [Unit("h"), Min(1), Tooltip("In-game hours to evolve fully.")]
        public int EvolveHours = 1;

        [Tooltip("Only evolves while indoors.")]
        public bool IndoorsOnly;
    }

    [Serializable]
    [Module("ModFireStarterBehaviour", DisplayName = "Fire Starter", Category = "Fire", Description = "Matches, flint, lenses and similar.")]
    [ConflictsWith(typeof(AccelerantBehaviour), typeof(BurnableBehaviour), typeof(TinderBehaviour))]
    public sealed class FireStarterBehaviour : BehaviourModule
    {
        [Unit("s"), Min(0)] public float SecondsToIgniteTinder = 5f;

        [Unit("s"), Min(0)] public float SecondsToIgniteTorch = 5f;

        [Min(1)] public float NumberOfUses = 1f;

        public bool RequiresSunLight;

        public bool DestroyedOnUse;

        [Tooltip("Played when used.")]
        public SoundRef OnUseSoundEvent;
    }

    [Serializable]
    [Module("ModHarvestableBehaviour", Category = "Crafting", Description = "Can be broken down into other items.")]
    public sealed class HarvestableBehaviour : BehaviourModule
    {
        public SoundRef Audio;

        [Unit("min"), Min(0)] public int Minutes = 10;

        [SplitArrays("YieldNames", "YieldCounts")]
        public List<GearCount> Yields = new();

        [Tooltip("Tools that can be used. Empty means by hand. Each must be a tool.")]
        public List<GearRef> RequiredToolNames = new();
    }

    [Serializable]
    [Module("ModMillableBehaviour", Category = "Crafting", Description = "Can be repaired or restored at a milling machine.")]
    public sealed class MillableBehaviour : BehaviourModule
    {
        [Unit("min"), Min(1)] public int RepairDurationMinutes = 1;

        [SplitArrays("RepairRequiredGear", "RepairRequiredGearUnits")]
        public List<GearCount> RepairRequiredGear = new();

        [Tooltip("Can be restored from a ruined state.")]
        public bool CanRestoreFromWornOut;

        [Unit("min"), Min(1)] public int RecoveryDurationMinutes = 1;

        [SplitArrays("RestoreRequiredGear", "RestoreRequiredGearUnits")]
        public List<GearCount> RestoreRequiredGear = new();

        public SkillType Skill;
    }

    [Serializable]
    [Module("ModRepairableBehaviour", Category = "Crafting", Description = "Can be repaired with tools and materials.")]
    public sealed class RepairableBehaviour : BehaviourModule
    {
        public SoundRef Audio;

        [Unit("min"), Min(0)] public int Minutes = 30;

        [Unit("%"), Min(0), Tooltip("Condition restored by one repair.")]
        public int Condition = 25;

        [Tooltip("Tools that can be used. Empty means no tool. Each must be a tool.")]
        public List<GearRef> RequiredTools = new();

        [SplitArrays("MaterialNames", "MaterialCounts")]
        public List<GearCount> Materials = new();
    }

    [Serializable]
    [Module("ModScentBehaviour", Category = "Survival", Description = "Gives off a scent wildlife can follow.")]
    public sealed class ScentBehaviour : BehaviourModule
    {
        public ScentCategory ScentCategory;
    }

    [Serializable]
    [Module("ModSharpenableBehaviour", Category = "Crafting", Description = "Can be sharpened with a whetstone.")]
    public sealed class SharpenableBehaviour : BehaviourModule
    {
        [Tooltip("Empty uses the default sound.")]
        public SoundRef Audio;

        [Unit("min"), Min(0), Tooltip("Minutes at the highest skill level.")]
        public int MinutesMin = 10;

        [Unit("min"), Min(0), Tooltip("Minutes at the lowest skill level.")]
        public int MinutesMax = 20;

        [Unit("%"), Min(0), Tooltip("Condition restored at the lowest skill level.")]
        public float ConditionMin = 5f;

        [Unit("%"), Min(0), Tooltip("Condition restored at the highest skill level.")]
        public float ConditionMax = 10f;

        [Tooltip("Tools that can sharpen it. Each must be a tool.")]
        public List<GearRef> Tools = new();
    }

    [Serializable]
    [Module("ModStackableBehaviour", Category = "General", Description = "Stacks in the inventory.")]
    public sealed class StackableBehaviour : BehaviourModule
    {
        [Tooltip("Localization key for one unit. Empty uses the item's display name.")]
        public LocalizationKey SingleUnitTextId;

        [Tooltip("Localization key for several units.")]
        public LocalizationKey MultipleUnitTextId;

        [Tooltip("Optional sprite name for the stack.")]
        public string StackSprite = string.Empty;

        [Min(1)] public int UnitsPerItem = 1;

        [Unit("%"), Range(0, 100), Tooltip("Chance the item is found as a full stack.")]
        public float ChanceFull = 100f;

        [Tooltip("Other stackable items this one stacks with.")]
        public List<GearRef> ShareStackWithGear = new();

        [Tooltip("Item created when the stack is split. Optional.")]
        public GearRef InstantiateStackItem;

        [Tooltip("Largest condition difference allowed within one stack.")]
        public float StackConditionDifferenceConstraint = 0.01f;
    }

    [Serializable]
    [Module("ModTinderBehaviour", Category = "Fire", Description = "Tinder for starting fires.")]
    [ConflictsWith(typeof(AccelerantBehaviour), typeof(BurnableBehaviour), typeof(FireStarterBehaviour))]
    public sealed class TinderBehaviour : BehaviourModule { }
}
