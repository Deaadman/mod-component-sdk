using ModComponent.SDK.Components;
using UnityEngine;

namespace ModComponent.Behaviours
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modsharpenablebehaviour")]
    [AddComponentMenu("")]
    public class ModSharpenableBehaviour : ModBaseBehaviour
    {
        [Tooltip("Sharpening sound effect. Default used if empty.")]
        public DataSoundAsset Audio;

        [Tooltip("Sharpening time with minimum skill (minutes).")]
        public int MinutesMin;

        [Tooltip("Sharpening time with maximum skill (minutes).")]
        public int MinutesMax;

        [Tooltip("Condition restored at minimum skill.")]
        public float ConditionMin;

        [Tooltip("Condition restored at maximum skill.")]
        public float ConditionMax;

        [Tooltip("Tools for sharpening. No tool needed if empty.")]
        public DataGearAsset[] Tools;
    }
}