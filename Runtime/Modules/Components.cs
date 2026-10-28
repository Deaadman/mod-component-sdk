using System;
using System.Collections.Generic;
using ModComponent.SDK.References;
using UnityEngine;

// Field names and types follow the keys ModComponent 7.0.0 reads in each type's InitializeComponent.
// Fields the loader ignores (Servings, PowderType, Footwear, Explosive, ...) are deliberately absent.
namespace ModComponent.SDK.Modules
{
    [Serializable]
    [Module("ModGenericComponent", Category = "General", Description = "An item with no special use: junk, materials, quest items.")]
    public sealed class GenericComponent : ComponentModule { }

    [Serializable]
    [Module("ModGenericEquippableComponent", DisplayName = "Generic Equippable", Category = "General",
        Description = "An item the player can hold, with custom behaviour supplied by a code mod.")]
    public sealed class GenericEquippableComponent : EquippableComponentModule { }

    [Serializable]
    [Module("ModAmmoComponent", Category = "Weapons", Description = "Ammunition for a vanilla gun type.")]
    public sealed class AmmoComponent : ComponentModule
    {
        [Tooltip("Gun this ammunition fits.")]
        public GunType AmmoForGunType;
    }

    [Serializable]
    [Module("ModBedComponent", Category = "Survival", Description = "A bedroll or sleeping bag.")]
    public sealed class BedComponent : ComponentModule
    {
        [Unit("%/h"), Tooltip("Condition restored per hour of sleep.")]
        public float ConditionGainPerHour;

        [Unit("%/h"), Tooltip("Extra condition per hour from the second hour onwards.")]
        public float AdditionalConditionGainPerHour;

        [Unit("°C"), Tooltip("Warmth bonus while sleeping.")]
        public float WarmthBonusCelsius;

        [Unit("%/h"), Tooltip("Condition the bed loses per hour of use.")]
        public float DegradePerHour;

        [Tooltip("Multiplier on the chance of a bear attack while sleeping.")]
        public float BearAttackModifier = 1f;

        [Tooltip("Multiplier on the chance of a wolf attack while sleeping.")]
        public float WolfAttackModifier = 1f;

        [Tooltip("Played when starting to sleep. Empty uses the sleeping bag sound.")]
        public SoundRef OpenAudio;

        [Tooltip("Played when waking up. Empty uses the sleeping bag sound.")]
        public SoundRef CloseAudio;

        [Tooltip("Direct child shown while the bed is packed.")]
        public ChildRef PackedMesh;

        [Tooltip("Direct child shown while the bed is laid out.")]
        public ChildRef UsableMesh;
    }

    [Serializable]
    [Module("ModBodyHarvestComponent", DisplayName = "Body Harvest", Category = "Survival",
        Description = "A carcass that can be harvested for meat, hide and guts.")]
    public sealed class BodyHarvestComponent : ComponentModule
    {
        [Tooltip("Can be picked up and carried, like a rabbit.")]
        public bool CanCarry;

        [Tooltip("Played while harvesting.")]
        public SoundRef HarvestAudio;

        [Tooltip("Gear produced as guts.")]
        public GearRef GutPrefab;

        [Min(0)] public int GutQuantity;

        [Unit("kg"), Min(0)] public float GutWeightKgPerUnit;

        [Tooltip("Gear produced as hide.")]
        public GearRef HidePrefab;

        [Min(0)] public int HideQuantity;

        [Unit("kg"), Min(0)] public float HideWeightKgPerUnit;

        [Tooltip("Gear produced as meat.")]
        public GearRef MeatPrefab;

        [Unit("kg"), Min(0)] public float MeatAvailableMinKG;

        [Unit("kg"), Min(0)] public float MeatAvailableMaxKG;

        [Tooltip("Localization key replacing the \"Gut\" label. Optional.")]
        public LocalizationKey GutLabelOverride;

        [Tooltip("Localization key replacing the \"Hide\" label. Optional.")]
        public LocalizationKey HideLabelOverride;

        [Tooltip("Localization key replacing the \"Meat\" label. Optional.")]
        public LocalizationKey MeatLabelOverride;
    }

    [Serializable]
    [Module("ModCharcoalComponent", Category = "Tools", Description = "Charcoal used to survey and map the area.")]
    public sealed class CharcoalComponent : ComponentModule
    {
        [Unit("min"), Min(0), Tooltip("In-game minutes one survey takes.")]
        public float SurveyGameMinutes = 15f;

        [Unit("s"), Min(0), Tooltip("Real seconds one survey takes.")]
        public float SurveyRealSeconds = 3f;

        [Unit("h"), Min(0), Tooltip("Extra survey time per skill level.")]
        public float SurveySkillExtendedHours;

        [Tooltip("Looped while surveying.")]
        public SoundRef SurveyLoopAudio;
    }

    [Serializable]
    [Module("ModClothingComponent", Category = "Clothing", Description = "Wearable clothing.")]
    public sealed class ClothingComponent : ComponentModule
    {
        [Tooltip("Body region the clothing is worn on.")]
        public ClothingRegion Region;

        [Tooltip("Innermost layer it can be worn on.")]
        public ClothingLayer MinLayer;

        [Tooltip("Outermost layer it can be worn on.")]
        public ClothingLayer MaxLayer;

        [Tooltip("Sound made when moving.")]
        public ClothingMovementSound MovementSound;

        [Unit("days"), Min(0)] public float DaysToDecayWornOutside;

        [Unit("days"), Min(0)] public float DaysToDecayWornInside;

        [Unit("°C"), Tooltip("Warmth when dry.")]
        public float Warmth;

        [Unit("°C"), Tooltip("Warmth when wet.")]
        public float WarmthWhenWet;

        [Unit("°C"), Tooltip("Windproof bonus.")]
        public float Windproof;

        [Unit("%"), Range(0, 100), Tooltip("Waterproofness, 0–100.")]
        public float Waterproofness;

        [Unit("%"), Tooltip("Protection from attacks.")]
        public float Toughness;

        [Unit("%"), Tooltip("Reduces the sprint bar.")]
        public float SprintBarReduction;

        [Unit("%"), Tooltip("Lowers the chance of a wolf attacking.")]
        public int DecreaseAttackChance;

        [Unit("%"), Tooltip("Raises the chance of a wolf fleeing.")]
        public int IncreaseFleeChance;

        [Unit("h"), Min(0.01f), Tooltip("Hours to dry next to a fire. Must be above 0.")]
        public float HoursToDryNearFire = 1f;

        [Unit("h"), Min(0.01f), Tooltip("Hours to dry without a fire. Must be above 0.")]
        public float HoursToDryWithoutFire = 4f;

        [Unit("h"), Min(0.01f), Tooltip("Hours to freeze when wet. Must be above 0.")]
        public float HoursToFreeze = 1f;

        [Tab("Paper doll"), Tooltip("Addressables name of the paper-doll texture.")]
        public string MainTexture = string.Empty;

        [Tab("Paper doll"), Tooltip("Addressables name of the paper-doll blend texture.")]
        public string BlendTexture = string.Empty;

        [Tab("Paper doll"), Tooltip("Paper-doll draw order. 0 uses the default for the region.")]
        public int DrawLayer;

        [Tab("Advanced"), Tooltip("Fully-qualified type name of a custom implementation from a code mod. Usually empty.")]
        public string ImplementationType = string.Empty;

        [Tab("Advanced"), Tooltip("Addressables name of the first-person arms prefab (male).")]
        public string FirstPersonPrefabMale = string.Empty;

        [Tab("Advanced"), Tooltip("Addressables name of the first-person arms prefab (female).")]
        public string FirstPersonPrefabFemale = string.Empty;

        [Tab("Advanced"), Tooltip("Damage source this clothing fully blocks. Unspecified blocks nothing.")]
        public DamageReason PreventAllDamageFromSource;
    }

    [Serializable]
    [Module("ModCollectibleComponent", Category = "General", Description = "A note or collectible with narrative text.")]
    public sealed class CollectibleComponent : ComponentModule
    {
        [Tooltip("Localization key of the message shown after pickup.")]
        public LocalizationKey HudMessageLocalizationId;

        [Tooltip("Localization key of the narrative text.")]
        public LocalizationKey NarrativeTextLocalizationId;

        public TextAlignment TextAlignment;
    }

    /// <summary>ModCookableComponent. Food inherits every field.</summary>
    [Serializable]
    [Module("ModCookableComponent", Category = "Food", Description = "Something that can be cooked or heated but not eaten directly.")]
    public class CookableComponent : ComponentModule
    {
        [Tab("Cooking"), Tooltip("Can be cooked or heated.")]
        public bool Cooking;

        [Tab("Cooking")]
        public CookableType Type;

        [Tab("Cooking"), Unit("min"), Min(0)]
        public int CookingMinutes = 1;

        [Tab("Cooking"), Unit("min"), Min(0), Tooltip("Minutes after cooking until it burns.")]
        public int BurntMinutes = 1;

        [Tab("Cooking"), Min(1)]
        public int CookingUnitsRequired = 1;

        [Tab("Cooking"), Unit("L"), Min(0)]
        public float CookingWaterRequired;

        [Tab("Cooking"), Tooltip("Gear produced by cooking. Required when Cooking is on.")]
        public GearRef CookingResult;

        [Tab("Cooking"), Tooltip("Empty uses the default for the cookable type.")]
        public SoundRef CookingAudio;

        [Tab("Cooking"), Tooltip("Empty uses the default for the cookable type.")]
        public SoundRef StartCookingAudio;
    }

    [Serializable]
    [Module("ModCookingPotComponent", DisplayName = "Cooking Pot", Category = "Food", Description = "A pot or pan to cook in.")]
    public sealed class CookingPotComponent : ComponentModule
    {
        public bool CanCookLiquid = true;
        public bool CanCookGrub = true;
        public bool CanCookMeat = true;

        [Unit("L"), Min(0)]
        public float Capacity = 1f;

        [Tooltip("Vanilla cooking pot to copy meshes and settings from, e.g. GEAR_CookingPot. Required.")]
        public GearRef Template;
    }

    [Serializable]
    [Module("ModFirstAidComponent", DisplayName = "First Aid", Category = "Survival", Description = "Medicine, bandages and remedies.")]
    public sealed class FirstAidComponent : ComponentModule
    {
        [Tooltip("Localization key shown on the progress bar.")]
        public LocalizationKey ProgressBarMessage;

        [Tooltip("Localization key of the action description.")]
        public LocalizationKey RemedyText;

        [Unit("%")] public int InstantHealing;

        public FirstAidType FirstAidType;

        [Unit("s"), Min(0)] public int TimeToUseSeconds = 3;

        [Min(1)] public int UnitsPerUse = 1;

        public SoundRef UseAudio;
    }

    [Serializable]
    [Module("ModFoodComponent", Category = "Food", Description = "Food or drink.")]
    public sealed class FoodComponent : CookableComponent
    {
        [Unit("days"), Min(0), Tooltip("0 never decays.")]
        public int DaysToDecayOutdoors;

        [Unit("days"), Min(0), Tooltip("0 never decays.")]
        public int DaysToDecayIndoors;

        [Unit("kcal"), Min(0)] public int Calories = 250;

        [Unit("s"), Min(1), Tooltip("Seconds to eat.")]
        public int EatingTime = 1;

        [Tooltip("Played when eating. Setting the packaged sound instead marks the item as packaged.")]
        public SoundRef EatingAudio;

        public SoundRef EatingPackagedAudio;

        [Unit("%"), Tooltip("Positive quenches thirst, negative adds thirst.")]
        public int ThirstEffect;

        [Unit("%"), Range(0, 100), Tooltip("Food poisoning chance above 20% condition.")]
        public int FoodPoisoning;

        [Unit("%"), Range(0, 100), Tooltip("Food poisoning chance below 20% condition.")]
        public int FoodPoisoningLowCondition;

        [Unit("%"), Tooltip("Parasite risk added per unit eaten.")]
        public List<float> ParasiteRiskIncrements = new();

        [Tab("Type")] public bool Natural;
        [Tab("Type")] public bool Raw;
        [Tab("Type")] public bool Drink;
        [Tab("Type")] public bool Meat;
        [Tab("Type")] public bool Fish;
        [Tab("Type")] public bool Canned;

        [Tab("Opening"), Tooltip("Needs to be opened before eating.")] public bool Opening;
        [Tab("Opening")] public bool OpeningWithCanOpener;
        [Tab("Opening")] public bool OpeningWithKnife;
        [Tab("Opening")] public bool OpeningWithHatchet;
        [Tab("Opening")] public bool OpeningWithSmashing;

        [Tab("Effects"), Tooltip("Restores condition while sleeping.")] public bool AffectCondition;
        [Tab("Effects"), Unit("%/h")] public float ConditionRestBonus = 2f;
        [Tab("Effects"), Unit("min")] public float ConditionRestMinutes = 360f;
        [Tab("Effects"), Tooltip("Changes fatigue immediately.")] public bool AffectRest;
        [Tab("Effects"), Unit("%")] public float InstantRestChange;
        [Tab("Effects"), Unit("min")] public int RestFactorMinutes = 60;
        [Tab("Effects"), Tooltip("Warms the player immediately.")] public bool AffectCold;
        [Tab("Effects"), Unit("%")] public float InstantColdChange = 20f;
        [Tab("Effects"), Unit("min")] public int ColdFactorMinutes = 60;
        [Tab("Effects")] public bool ContainsAlcohol;
        [Tab("Effects"), Unit("%")] public float AlcoholPercentage;
        [Tab("Effects"), Unit("min")] public float AlcoholUptakeMinutes = 45f;
        [Tab("Effects"), Unit("mg")] public int VitaminC;
    }

    [Serializable]
    [Module("ModLiquidComponent", Category = "Survival", Description = "A container of water or kerosene.")]
    public sealed class LiquidComponent : ComponentModule
    {
        public LiquidType LiquidType;

        [Unit("L"), Min(0)] public float LiquidCapacityLiters = 1f;

        [Tooltip("Start with a random amount instead of the amount below.")]
        public bool RandomizedQuantity;

        [Unit("L"), Min(0), Tooltip("Starting amount. Clamped to the capacity.")]
        public float LiquidLiters;
    }

    [Serializable]
    [Module("ModPowderComponent", Category = "Survival",
        Description = "A container of gunpowder. ModComponent 7.0.0 always treats the powder as gunpowder.")]
    public sealed class PowderComponent : ComponentModule
    {
        [Unit("kg"), Min(0)] public float CapacityKG = 1f;

        [Unit("%"), Range(0, 100), Tooltip("Chance the container is found full.")]
        public float ChanceFull = 100f;
    }

    [Serializable]
    [Module("ModPurificationComponent", Category = "Survival", Description = "Water purification tablets.")]
    public sealed class PurificationComponent : ComponentModule
    {
        [Unit("L"), Min(0)] public float LitersPurify = 1f;

        [Unit("s"), Min(0)] public float ProgressBarDurationSeconds = 1f;

        [Tooltip("Localization key shown on the progress bar.")]
        public LocalizationKey ProgressBarLocalizationID;

        public SoundRef PurifyAudio;
    }

    [Serializable]
    [Module("ModRandomItemComponent", DisplayName = "Random Item", Category = "Spawning",
        Description = "Turns into one of several items, chosen at random.")]
    public sealed class RandomItemComponent : ComponentModule
    {
        public List<GearRef> ItemNames = new();
    }

    [Serializable]
    [Module("ModRandomWeightedItemComponent", DisplayName = "Random Weighted Item", Category = "Spawning",
        Description = "Turns into one of several items, chosen by weight.")]
    public sealed class RandomWeightedItemComponent : ComponentModule
    {
        [SplitArrays("ItemNames", "ItemWeights")]
        public List<GearCount> Items = new();
    }

    [Serializable]
    [Module("ModResearchComponent", Category = "Survival", Description = "A book that trains a skill.")]
    public sealed class ResearchComponent : ComponentModule
    {
        public SkillType SkillType;

        [Unit("h"), Min(1)] public int TimeRequirementHours = 5;

        [Min(0)] public int SkillPoints = 10;

        [Tooltip("Skill level at which reading stops giving benefits.")]
        public int NoBenefitAtSkillLevel = 5;

        public SoundRef ReadAudio;
    }

    [Serializable]
    [Module("ModToolComponent", Category = "Tools", Description = "A tool for crafting, repairing, harvesting or breaking down.")]
    public sealed class ToolComponent : EquippableComponentModule
    {
        [Tooltip("Vanilla cutting tool this behaves like.")]
        public CuttingToolType ToolType;

        public ToolUsage Usage;

        [Unit("%"), Min(0), Tooltip("Condition lost per use.")]
        public float DegradeOnUse;

        [Tooltip("Skill bonus when used.")]
        public int SkillBonus;

        [Tab("Crafting"), Unit("×")] public float CraftingTimeMultiplier = 1f;
        [Tab("Crafting"), Unit("%/h")] public float DegradePerHourCrafting;

        [Tab("Break down")] public bool BreakDown;
        [Tab("Break down"), Unit("×")] public float BreakDownTimeMultiplier = 1f;

        [Tab("Locks")] public bool ForceLocks;
        [Tab("Locks")] public SoundRef ForceLockAudio;

        [Tab("Ice fishing")] public bool IceFishingHole;
        [Tab("Ice fishing"), Unit("%")] public float IceFishingHoleDegradeOnUse;
        [Tab("Ice fishing"), Unit("min")] public int IceFishingHoleMinutes;
        [Tab("Ice fishing")] public SoundRef IceFishingHoleAudio;

        [Tab("Harvesting")] public bool CarcassHarvesting;
        [Tab("Harvesting"), Unit("min/kg")] public int MinutesPerKgMeat;
        [Tab("Harvesting"), Unit("min/kg")] public int MinutesPerKgFrozenMeat;
        [Tab("Harvesting"), Unit("min")] public int MinutesPerHide;
        [Tab("Harvesting"), Unit("min")] public int MinutesPerGut;
        [Tab("Harvesting"), Unit("%/h")] public float DegradePerHourHarvesting;

        [Tab("Struggle")] public bool StruggleBonus;
        [Tab("Struggle"), Unit("×")] public float DamageMultiplier = 1f;
        [Tab("Struggle"), Unit("×")] public float FleeChanceMultiplier = 1f;
        [Tab("Struggle"), Unit("×")] public float TapMultiplier = 1f;
        [Tab("Struggle")] public bool CanPuncture;
        [Tab("Struggle"), Unit("×")] public float BleedoutMultiplier = 1f;
    }
}
