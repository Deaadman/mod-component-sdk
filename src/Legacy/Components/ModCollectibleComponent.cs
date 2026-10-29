using UnityEngine;

using TextAlignment = ModComponent.Utilities.TextAlignment;

namespace ModComponent.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modcollectiblecomponent")]
    [AddComponentMenu("")]
    public class ModCollectibleComponent : ModGenericComponent
    {
        [Tooltip("HUD message ID for post-pickup.")]
        public string HudMessageLocalizationId;

        [Tooltip("ID for item's narrative text.")]
        public string NarrativeTextLocalizationId;

        [Tooltip("Narrative text alignment.")]
        public TextAlignment TextAlignment;
    }
}