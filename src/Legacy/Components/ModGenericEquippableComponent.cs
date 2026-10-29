using ModComponent.SDK.Components;
using UnityEngine;

namespace ModComponent.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modgenericequippablecomponent")]
    [AddComponentMenu("")]
    public class ModGenericEquippableComponent : ModGenericComponent
    {
        [Tooltip("Model used when item is equipped.")]
        public DataGearAsset EquippedModelPrefab;

        [Tooltip("Custom logic type for this item.")]
        public string ImplementationType;

        [Tooltip("Sound effect for equipping.")]
        public DataSoundAsset EquippingAudio;
    }
}