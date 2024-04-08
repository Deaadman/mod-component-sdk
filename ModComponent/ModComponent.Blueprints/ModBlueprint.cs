using ModComponent.SDK.Components;
using ModComponent.Utilities;
using UnityEngine;

namespace ModComponent.Blueprints
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modblueprint")]
    [DisallowMultipleComponent]
    public class ModBlueprint : MonoBehaviour
    {
        [Tooltip("Optional blueprint name for error logging.")]
        public string Name;

        [Tooltip("Required items and quantities for crafting.")]
        public GearRequirement[] RequiredGear;

        [Tooltip("Powder ingredients required, including type and amount in kg.")]
        public PowderRequirement[] RequiredPowder;

        [Tooltip("Liquid ingredients required, including type and volume in liters.")]
        public LiquidRequirement[] RequiredLiquid;

        [Tooltip("Mandatory tool for crafting.")]
        public DataGearAsset RequiredTool;

        [Tooltip("Optional tools to improve or substitute crafting.")]
        public DataGearAsset[] OptionalTools;

        [Tooltip("Crafting location requirement.")]
        public WorkbenchType RequiredCraftingLocation;

        [Tooltip("Need a lit fire to craft?")]
        public bool RequiresLitFire;

        [Tooltip("Need light to craft?")]
        public bool RequiresLight;

        [Tooltip("Crafting result item.")]
        public DataGearAsset CraftedResult;

        [Tooltip("Amount produced.")]
        public int CraftedResultCount;

        [Tooltip("Crafting time (minutes).")]
        public int DurationMinutes;

        [Tooltip("Sound effect during crafting.")]
        public DataSoundAsset CraftingAudio;

        [Tooltip("Skill associated with crafting.")]
        public SkillType AppliedSkill;

        [Tooltip("Skill improved upon successful crafting.")]
        public SkillType ImprovedSkill;
    }

    [System.Serializable]
    public class GearRequirement
    {
        [Tooltip("Required gear item name.")]
        public DataGearAsset GearItem;

        [Tooltip("Quantity required.")]
        public int Count;

        [Tooltip("The type of measurement.")]
        public UnitsType Units;
    }

    [System.Serializable]
    public class PowderRequirement
    {
        [Tooltip("Type of powder ingredient.")]
        public DataPowderAsset PowderItem;

        [Tooltip("Amount needed in kilograms.")]
        public int QuantityInKilograms;
    }

    [System.Serializable]
    public class LiquidRequirement
    {
        [Tooltip("Type of liquid ingredient.")]
        public DataLiquidAsset LiquidItem;

        [Tooltip("Volume needed in liters.")]
        public int VolumeInLitres;
    }
}