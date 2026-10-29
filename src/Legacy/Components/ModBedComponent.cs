using ModComponent.SDK.Components;
using UnityEngine;

namespace ModComponent.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modbedcomponent")]
    [AddComponentMenu("")]
    public class ModBedComponent : ModGenericComponent
    {
        [Tooltip("Base condition restored per hour of sleep.")]
        public float ConditionGainPerHour;

        [Tooltip("Extra condition restored from the second hour onwards.")]
        public float AdditionalConditionGainPerHour;

        [Tooltip("Bed's warmth bonus in Celsius.")]
        public float WarmthBonusCelsius;

        [Tooltip("Bed condition lost per hour of use.")]
        public float DegradePerHour;

        [Tooltip("Changes bear encounter chance during sleep.")]
        public float BearAttackModifier;

        [Tooltip("Changes wolf encounter chance during sleep.")]
        public float WolfAttackModifier;

        [Tooltip("Sound for starting to sleep. Defaults used if empty.")]
        public DataSoundAsset OpenAudio;

        [Tooltip("Sound for waking up. Defaults used if empty.")]
        public DataSoundAsset CloseAudio;

        [Tooltip("Mesh for the bed in packed state.")]
        public GameObject PackedMesh;

        [Tooltip("Mesh for the bed in usable state.")]
        public GameObject UsableMesh;
    }
}
