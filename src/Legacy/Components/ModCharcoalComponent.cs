using ModComponent.SDK.Components;
using UnityEngine;

namespace ModComponent.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modcharcoalcomponent")]
    [AddComponentMenu("")]
    public class ModCharcoalComponent : ModGenericComponent
    {
        [Tooltip("Time taken in game minutes to complete a survey.")]
        public float SurveyGameMinutes;

        [Tooltip("Time taken in real seconds to complete a survey.")]
        public float SurveyRealSeconds;

        [Tooltip("Additional survey time in hours based on skill level.")]
        public float SurveySkillExtendedHours;

        [Tooltip("Sound played during surveying.")]
        public DataSoundAsset SurveyLoopAudio;
    }
}