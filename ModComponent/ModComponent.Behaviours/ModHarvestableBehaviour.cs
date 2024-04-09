using ModComponent.SDK.Components;
using UnityEngine;

namespace ModComponent.Behaviours
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modharvestablebehaviour")]
    public class ModHarvestableBehaviour : ModBaseBehaviour
    {
        [Tooltip("Sound effect during harvesting.")]
        public DataSoundAsset Audio;

        [Tooltip("Time to harvest (minutes).")]
        public int Minutes;

        [Tooltip("An array of multiple yields.")]
        public Yields[] Yields;

        [Tooltip("Tools required for harvesting. None means by hand.")]
        public DataGearAsset[] RequiredToolNames;
    }

    [System.Serializable]
    public class Yields
    {
        [Tooltip("Quantities of each item yielded.")]
        public int YieldCounts;

        [Tooltip("Names of items yielded.")]
        public DataGearAsset YieldNames;
    }
}