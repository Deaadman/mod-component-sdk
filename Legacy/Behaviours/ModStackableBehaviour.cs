using ModComponent.SDK.Components;
using UnityEngine;

namespace ModComponent.Behaviours
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modstackablebehaviour")]
    [AddComponentMenu("")]
    public class ModStackableBehaviour : ModBaseBehaviour
    {
        [Tooltip("Text for a single unit in a stack.")]
        public string SingleUnitTextId;

        [Tooltip("Text for multiple units in a stack.")]
        public string MultipleUnitTextId;

        [Tooltip("Optional sprite for the stack.")]
        public string StackSprite;

        [Tooltip("Number of units for a full stack.")]
        public int UnitsPerItem;

        [Tooltip("Chance of item being a full stack.")]
        public float ChanceFull;

        [Tooltip("Items compatible for stacking.")]
        public DataGearAsset[] ShareStackWithGear;

        [Tooltip("Item created when the stack is split.")]
        public DataGearAsset InstantiateStackItem;

        [Tooltip("Max condition difference allowed in a stack.")]
        public float StackConditionDifferenceConstraint;
    }
}