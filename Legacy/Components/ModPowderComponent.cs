using ModComponent.Utilities;
using UnityEngine;

namespace ModComponent.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modpowdercomponent")]
    [AddComponentMenu("")]
    public class ModPowderComponent : ModGenericComponent
    {
        [Tooltip("Type of powder contained.")]
        public PowderType PowderType;

        [Tooltip("Maximum capacity in kilograms.")]
        public float CapacityKG;

        [Tooltip("Chance of finding this container full (%).")]
        [Range(0, 100)]
        public float ChanceFull;
    }
}