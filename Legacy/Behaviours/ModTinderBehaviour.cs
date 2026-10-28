using UnityEngine;

namespace ModComponent.Behaviours
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modtinderbehaviour")]
    [AddComponentMenu("")]
    public class ModTinderBehaviour : ModBaseBehaviour
    {
        [Tooltip("Adjusts fire starting time ('+' for slower, '-' for faster).")]
        public float DurationOffset;

        [Tooltip("Changes success chance ('+' increases, '-' decreases).")]
        public float SuccessModifier;
    }
}