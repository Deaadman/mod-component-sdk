using ModComponent.SDK.Components;
using UnityEngine;

namespace ModComponent.Behaviours
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modrepairablebehaviour")]
    public class ModRepairableBehaviour : ModBaseBehaviour
    {
        [Tooltip("Sound effect during repair.")]
        public DataSoundAsset Audio;

        [Tooltip("Time to repair (minutes).")]
        public int Minutes;

        [Tooltip("Condition restored by repair.")]
        public int Condition;

        [Tooltip("Tools required for repair. None means tool-free.")]
        public DataGearAsset[] RequiredTools;

        [Tooltip("An array of multiple materials.")]
        public Materials[] Materials;
    }

    [System.Serializable]
    public class Materials
    {
        [Tooltip("Materials needed for repair.")]
        public DataGearAsset MaterialNames;

        [Tooltip("Quantity of each material needed.")]
        public int MaterialCounts;
    }
}