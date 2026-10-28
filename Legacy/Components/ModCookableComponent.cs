using ModComponent.SDK.Components;
using ModComponent.Utilities;
using UnityEngine;

namespace ModComponent.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modcookablecomponent")]
    [AddComponentMenu("")]
    public class ModCookableComponent : ModGenericComponent
    {
        [Tooltip("Enables cooking/heating.")]
        public bool Cooking = true;

        [Tooltip("Time to cook/heat (minutes).")]
        public int CookingMinutes;

        [Tooltip("Units needed for cooking.")]
        public int CookingUnitsRequired;

        [Tooltip("Water needed for cooking (liters).")]
        public float CookingWaterRequired;

        [Tooltip("Result after cooking.")]
        public DataGearAsset CookingResult;

        [Tooltip("Time until item burns after cooking (minutes).")]
        public int BurntMinutes;

        [Tooltip("Type of cookable item.")]
        public CookableType Type;

        [Tooltip("Cooking sound effect. Defaults if empty.")]
        public DataSoundAsset CookingAudio;

        [Tooltip("Sound for starting to cook. Defaults if empty.")]
        public DataSoundAsset StartCookingAudio;
    }
}