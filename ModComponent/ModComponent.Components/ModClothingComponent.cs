using ModComponent.Utilities;
using UnityEngine;

namespace ModComponent.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modclothingcomponent")]
    public class ModClothingComponent : ModGenericComponent
    {
        [Tooltip("Wearable region (e.g., Head, Torso).")]
        public ClothingRegion Region;

        [Tooltip("Innermost wearable layer.")]
        public ClothingLayer MinLayer;

        [Tooltip("Outermost wearable layer.")]
        public ClothingLayer MaxLayer;

        [Tooltip("Sound made by movement.")]
        public ClothingMovementSound MovementSound;

        [Tooltip("Type of footwear.")]
        public FootwearType Footwear;

        [Tooltip("Decay time outside (days).")]
        public float DaysToDecayWornOutside;

        [Tooltip("Decay time inside (days).")]
        public float DaysToDecayWornInside;

        [Tooltip("Warmth bonus when dry (°C).")]
        public float Warmth;

        [Tooltip("Warmth bonus when wet (°C).")]
        public float WarmthWhenWet;

        [Tooltip("Windproof bonus when wet (°C).")]
        public float Windproof;

        [Tooltip("Waterproofness level.")]
        public float Waterproofness;

        [Tooltip("Damage reduction (%) from certain attacks.")]
        public float Toughness;

        [Tooltip("Reduction in sprint stamina (%).")]
        public float SprintBarReduction;

        [Tooltip("Reduces wolf attack chance (%).")]
        public int DecreaseAttackChance;

        [Tooltip("Increases wolf flee chance (%).")]
        public int IncreaseFleeChance;

        [Tooltip("Drying time near fire (hours).")]
        public float HoursToDryNearFire;

        [Tooltip("Drying time without fire (hours).")]
        public float HoursToDryWithoutFire;

        [Tooltip("Freezing time (hours).")]
        public float HoursToFreeze;

        [Tooltip("Main texture for paper doll view.")]
        public Texture2D MainTexture;

        [Tooltip("Blend texture for paper doll view.")]
        public Texture2D BlendTexture;

        [Tooltip("Drawing order layer.")]
        public int DrawLayer;

        [Tooltip("Custom logic implementation type.")]
        public string ImplementationType;
        
        [Tooltip("Prevent damage from the selected damage source.")]
        public DamageSource PreventAllDamageFromSource;
        
        [Tooltip("The prefab to load for first person clothing on arms on a male.")]
        public string FirstPersonPrefabMale;
        
        [Tooltip("The prefab to load for first person clothing on arms on a female.")]
        public string FirstPersonPrefabFemale;
    }
}