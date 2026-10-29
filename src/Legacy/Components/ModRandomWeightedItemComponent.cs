using ModComponent.SDK.Components;
using UnityEngine;

namespace ModComponent.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modrandomweighteditemcomponent")]
    [AddComponentMenu("")]
    public class ModRandomWeightedItemComponent : ModGenericComponent
    {
        [Tooltip("Potential gear items to spawn.")]
        public DataGearAsset[] ItemNames;

        [Tooltip("Weights determining spawn likelihood for each item.")]
        public int[] ItemWeights;
    }
}