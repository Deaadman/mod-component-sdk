using ModComponent.SDK.Components;
using ModComponent.Utilities;
using UnityEngine;

namespace ModComponent.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modfirstaidcomponent")]
    [AddComponentMenu("")]
    public class ModFirstAidComponent : ModGenericComponent
    {
        [Tooltip("Progress bar message ID during use.")]
        public string ProgressBarMessage;

        [Tooltip("Action description message ID.")]
        public string RemedyText;

        [Tooltip("Health restored immediately upon use.")]
        public int InstantHealing;

        [Tooltip("First aid action type.")]
        public FirstAidType FirstAidType;

        [Tooltip("Usage time (seconds).")]
        public int TimeToUseSeconds;

        [Tooltip("Item quantity needed per use.")]
        public int UnitsPerUse;

        [Tooltip("Sound effect for using item.")]
        public DataSoundAsset UseAudio;
    }
}