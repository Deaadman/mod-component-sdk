using UnityEngine;

namespace ModComponent.Behaviours
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modaccelerantbehaviour")]
    [AddComponentMenu("")]
    public class ModAccelerantBehaviour : ModBaseBehaviour
    {
        [Tooltip("Destroys the item after one use.")]
        public bool DestroyedOnUse;

        [Tooltip("Changes how long it takes to start a fire. '+' slows it down, '-' speeds it up.")]
        public float DurationOffset;

        [Tooltip("Modifies success chance. '+' increases, '-' decreases.")]
        public float SuccessModifier;
    }
}