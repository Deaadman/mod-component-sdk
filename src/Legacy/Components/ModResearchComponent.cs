using ModComponent.SDK.Components;
using ModComponent.Utilities;
using UnityEngine;

namespace ModComponent.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modresearchcomponent")]
    [AddComponentMenu("")]
    public class ModResearchComponent : ModGenericComponent
    {
        [Tooltip("Skill improved by research.")]
        public SkillType SkillType;

        [Tooltip("Hours needed to complete research.")]
        public int TimeRequirementHours;

        [Tooltip("Skill points gained upon completion.")]
        public int SkillPoints;

        [Tooltip("Skill level above which no benefits are gained.")]
        public int NoBenefitAtSkillLevel;

        [Tooltip("Sound played while reading.")]
        public DataSoundAsset ReadAudio;
    }
}