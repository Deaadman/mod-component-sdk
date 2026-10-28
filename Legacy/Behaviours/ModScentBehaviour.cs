using ModComponent.Utilities;
using UnityEngine;

namespace ModComponent.Behaviours
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modscentbehaviour")]
    [AddComponentMenu("")]
    public class ModScentBehaviour : ModBaseBehaviour
    {
        [Tooltip("Type of scent and its effect on wildlife.")]
        public ScentCategory ScentCategory;
    }
}