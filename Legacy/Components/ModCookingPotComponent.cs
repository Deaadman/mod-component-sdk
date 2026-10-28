using ModComponent.SDK.Components;
using UnityEngine;

namespace ModComponent.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modcookingpotcomponent")]
    [AddComponentMenu("")]
    public class ModCookingPotComponent : ModGenericComponent
    {
        [Tooltip("Allows cooking of liquids.")]
        public bool CanCookLiquid;

        [Tooltip("Allows cooking of canned food and similar.")]
        public bool CanCookGrub;

        [Tooltip("Allows cooking of meat.")]
        public bool CanCookMeat;

        [Tooltip("Maximum water capacity (liters).")]
        public float Capacity;

        [Tooltip("Template for cooking process.")]
        public DataGearAsset Template;
    }
}