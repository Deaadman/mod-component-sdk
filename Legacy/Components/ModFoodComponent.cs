using ModComponent.SDK.Components;
using UnityEngine;

namespace ModComponent.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modfoodcomponent")]
    [AddComponentMenu("")]
    public class ModFoodComponent : ModCookableComponent
    {
        [Tooltip("Outdoor decay time (days); 0 = Never.")]
        public int DaysToDecayOutdoors;

        [Tooltip("Indoor decay time (days); 0 = Never.")]
        public int DaysToDecayIndoors;

        [Tooltip("Total calories.")]
        public int Calories;

        [Tooltip("Number of servings.")]
        public int Servings;

        [Tooltip("Eating time (seconds) per serving.")]
        public int EatingTime;

        [Tooltip("Sound when eating unpackaged/open food.")]
        public DataSoundAsset EatingAudio;

        [Tooltip("Sound when eating packaged food.")]
        public DataSoundAsset EatingPackagedAudio;

        [Tooltip("Thirst change (%); negative = increase, positive = decrease.")]
        public int ThirstEffect;

        [Tooltip("Food poisoning chance (>20% condition).")]
        public int FoodPoisoning;

        [Tooltip("Food poisoning chance (<20% condition).")]
        public int FoodPoisoningLowCondition;

        [Tooltip("Parasite risk per unit eaten (%).")]
        public float[] ParasiteRiskIncrements;

        [Tooltip("Is this natural food (meat/plant)?")]
        public bool Natural;

        [Tooltip("Is this raw food?")]
        public bool Raw;

        [Tooltip("Is this a drink?")]
        public bool Drink;

        [Tooltip("Is this animal meat?")]
        public bool Meat;

        [Tooltip("Is this fish?")]
        public bool Fish;

        [Tooltip("Is this canned food?")]
        public bool Canned;

        [Tooltip("Requires tool for opening?")]
        public bool Opening;

        [Tooltip("Openable with can opener?")]
        public bool OpeningWithCanOpener;

        [Tooltip("Openable with knife?")]
        public bool OpeningWithKnife;

        [Tooltip("Openable with hatchet?")]
        public bool OpeningWithHatchet;

        [Tooltip("Openable by smashing?")]
        public bool OpeningWithSmashing;

        [Tooltip("Affects condition while sleeping?")]
        public bool AffectCondition;

        [Tooltip("Condition restored per hour.")]
        public float ConditionRestBonus;

        [Tooltip("Duration for condition rest bonus (minutes).")]
        public float ConditionRestMinutes;

        [Tooltip("Affects rest immediately?")]
        public bool AffectRest;

        [Tooltip("Instant rest change (%).")]
        public float InstantRestChange;

        [Tooltip("Duration for rest factor (minutes).")]
        public int RestFactorMinutes;

        [Tooltip("Affects cold immediately?")]
        public bool AffectCold;

        [Tooltip("Instant cold change (%).")]
        public float InstantColdChange;

        [Tooltip("Duration for cold factor (minutes).")]
        public int ColdFactorMinutes;

        [Tooltip("Contains alcohol?")]
        public bool ContainsAlcohol;

        [Tooltip("Alcohol weight percentage.")]
        public float AlcoholPercentage;

        [Tooltip("Alcohol absorption time (minutes).")]
        public float AlcoholUptakeMinutes;

        [Tooltip("Vitamin C added on consumption.")]
        public int VitaminC;
    }
}