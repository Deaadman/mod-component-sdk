using ModComponent.SDK.Components;
using UnityEngine;

namespace ModComponent.Behaviours
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modfirestarterbehaviour")]
    [AddComponentMenu("")]
    public class ModFireStarterBehaviour : ModBaseBehaviour
    {
        [Tooltip("Destroyed after use?")]
        public bool DestroyedOnUse;

        [Tooltip("Total uses available.")]
        public float NumberOfUses;

        [Tooltip("Sound played when used.")]
        public DataSoundAsset OnUseSoundEvent;

        [Tooltip("Needs sunlight to function?")]
        public bool RequiresSunLight;

        [Tooltip("Condition drops to 0% after use.")]
        public bool RuinedAfterUse;

        [Tooltip("Time to ignite tinder (seconds).")]
        public float SecondsToIgniteTinder;

        [Tooltip("Time to ignite torch (seconds).")]
        public float SecondsToIgniteTorch;

        [Tooltip("Modifies success chance ('+' increases, '-' decreases).")]
        public float SuccessModifier;
    }
}