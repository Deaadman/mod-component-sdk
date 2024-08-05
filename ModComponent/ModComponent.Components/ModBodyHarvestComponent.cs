using ModComponent.SDK.Components;
using UnityEngine;

namespace ModComponent.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modbodyharvestcomponent")]
    public class ModBodyHarvestComponent : ModGenericComponent
    {
        [Tooltip("Allows carrying like a rabbit carcass.")]
        public bool CanCarry;

        [Tooltip("Sound effect for harvesting.")]
        public DataSoundAsset HarvestAudio;

        [Tooltip("Prefab for guts.")]
        public DataGearAsset GutPrefab;

        [Tooltip("Quantity of guts obtained per harvest.")]
        public int GutQuantity;

        [Tooltip("Weight of each gut unit (kg).")]
        public float GutWeightKgPerUnit;

        [Tooltip("Prefab for hide.")]
        public DataGearAsset HidePrefab;

        [Tooltip("Quantity of hides obtained per harvest.")]
        public int HideQuantity;

        [Tooltip("Weight of each hide unit (kg).")]
        public float HideWeightKgPerUnit;

        [Tooltip("Prefab for raw meat.")]
        public DataGearAsset MeatPrefab;

        [Tooltip("Minimum meat weight available (kg).")]
        public float MeatAvailableMinKG;

        [Tooltip("Maximum meat weight available (kg).")]
        public float MeatAvailableMaxKG;

        [Tooltip("Over the gut label with a custom string.")]
        public string GutLabelOverride;
        
        [Tooltip("Over the hide label with a custom string.")]
        public string HideLabelOverride;
        
        [Tooltip("Over the meat label with a custom string.")]
        public string MeatLabelOverride;
    }
}