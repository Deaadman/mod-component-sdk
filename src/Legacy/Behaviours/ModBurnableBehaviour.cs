using UnityEngine;

namespace ModComponent.Behaviours
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modburnablebehaviour")]
    [AddComponentMenu("")]
    public class ModBurnableBehaviour : ModBaseBehaviour
    {
        [Tooltip("Extends fire burn time by this many minutes.")]
        public int BurningMinutes;

        [Tooltip("Minimum fire burn time required to use this item.")]
        public float BurningMinutesBeforeAllowedToAdd;

        [Tooltip("Adjusts fire-starting success rate. '+' increases, '-' decreases.")]
        public float SuccessModifier;

        [Tooltip("Boosts fire temperature.")]
        public float TempIncrease;

        [Tooltip("Modifies time to start a fire. '+' slows, '-' speeds up.")]
        public float DurationOffset;
    }
}