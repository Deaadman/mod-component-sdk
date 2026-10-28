using System;
using System.Collections.Generic;
using ModComponent.SDK.Modules;
using ModComponent.SDK.References;
using UnityEngine;

namespace ModComponent.SDK
{
    /// <summary>
    /// A crafting blueprint (CraftingRevisions). With <see cref="IsCookingRecipe"/> on it is exported as
    /// a cooking recipe instead, which uses a subset of the fields plus the recipe fields.
    /// </summary>
    [CreateAssetMenu(menuName = "ModComponent SDK/Blueprint", fileName = "BP_NewBlueprint", order = 2)]
    public sealed class Blueprint : ScriptableObject
    {
        [Serializable]
        public sealed class GearRequirement
        {
            public GearRef Item;

            [Tooltip("Whole items needed. Used when Units is Count.")]
            [Min(0)] public int Count = 1;

            [Tooltip("Amount needed. Used when Units is Kilograms.")]
            [Min(0)] public float Quantity;

            public RequirementUnits Units;
        }

        [Serializable]
        public struct PowderRequirement
        {
            public PowderRef Powder;
            [Min(0)] public float QuantityInKilograms;
        }

        [Serializable]
        public struct LiquidRequirement
        {
            public LiquidRef Liquid;
            [Min(0)] public float VolumeInLitres;
        }

        [Tooltip("Shown in ModComponent's logs. Defaults to the asset name.")]
        public string Name = string.Empty;

        public List<GearRequirement> RequiredGear = new();
        public List<PowderRequirement> RequiredPowder = new();
        public List<LiquidRequirement> RequiredLiquid = new();

        [Tooltip("Tool that must be used. Optional.")]
        public GearRef RequiredTool;

        [Tooltip("Tools any one of which can be used.")]
        public List<GearRef> OptionalTools = new();

        public CraftingLocation RequiredCraftingLocation;
        public bool RequiresLitFire;
        public bool RequiresLight;

        [Tooltip("Item crafted. Required.")]
        public GearRef CraftedResult;

        [Min(1)] public int CraftedResultCount = 1;

        [Unit("min"), Min(0)] public int DurationMinutes = 60;

        public SoundRef CraftingAudio;

        public SkillType AppliedSkill;
        public SkillType ImprovedSkill;

        [Header("Cooking recipe")]
        [Tooltip("Export as a cooking recipe. Recipes always use the Cooking skill and need a lit fire.")]
        public bool IsCookingRecipe;

        public LocalizationKey RecipeName;
        public LocalizationKey RecipeDescription;

        [Range(1, 5)] public int RequiredSkillLevel = 1;

        [Tooltip("Cooking pots the recipe can be made in.")]
        public List<GearRef> AllowedCookingPots = new();

        public string EffectiveName => string.IsNullOrEmpty(Name) ? name : Name;
    }
}
