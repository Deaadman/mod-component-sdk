using ModComponent.SDK.Components;
using UnityEngine;

namespace ModComponent.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modrandomitemcomponent")]
    [AddComponentMenu("")]
    public class ModRandomItemComponent : ModGenericComponent
    {
        [Tooltip("Potential gear items to spawn.")]
        public DataGearAsset[] ItemNames;
    }
}