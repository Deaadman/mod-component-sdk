using ModComponent.SDK.Components;
using UnityEngine;

namespace ModComponent.Behaviours
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modevolvebehaviour")]
    [AddComponentMenu("")]
    public class ModEvolveBehaviour : ModBaseBehaviour
    {
        [Tooltip("Target item's name for evolution.")]
        public DataGearAsset TargetItemName;

        [Tooltip("Time in in-game hours to evolve from 0% to 100%.")]
        public int EvolveHours;

        [Tooltip("Evolves only when stored indoors.")]
        public bool IndoorsOnly;
    }
}