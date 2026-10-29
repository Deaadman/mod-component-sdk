using ModComponent.Utilities;
using UnityEngine;

namespace ModComponent.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modliquidcomponent")]
    [AddComponentMenu("")]
    public class ModLiquidComponent : ModGenericComponent
    {
        [Tooltip("Type of liquid contained.")]
        public LiquidType LiquidType;

        [Tooltip("Maximum liquid capacity (liters).")]
        public float LiquidCapacityLiters;

        [Tooltip("Enable to randomize initial liquid amount.")]
        public bool RandomizedQuantity;

        [Tooltip("Initial liquid amount (liters), ignored if randomized.")]
        public float LiquidLiters;
    }
}