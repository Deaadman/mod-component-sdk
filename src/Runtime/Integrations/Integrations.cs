using System;

namespace ModComponent.SDK.Integrations
{
    /// <summary>
    /// Support for another mod that reads extra data from a .modcomponent, enabled per mod. Add-on packages
    /// subclass it; the editor finds every subclass, shows it as a toggle, and draws its serialized fields.
    /// </summary>
    [Serializable]
    public abstract class ModIntegration
    {
        public abstract string DisplayName { get; }

        /// <summary>One sentence shown as the toggle's tooltip, e.g. what file it writes.</summary>
        public virtual string Description => string.Empty;
    }

    /// <summary>
    /// Per-item data for an integration, such as an item's rarity. Mark subclasses with
    /// <see cref="ItemIntegrationDataAttribute"/> so items of mods that enable the integration show a section for it.
    /// </summary>
    [Serializable]
    public abstract class ItemIntegrationData
    {
    }

    /// <summary>Ties an <see cref="ItemIntegrationData"/> type to the integration that uses it.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ItemIntegrationDataAttribute : Attribute
    {
        public ItemIntegrationDataAttribute(Type integration) => Integration = integration;

        public Type Integration { get; }

        /// <summary>The item inspector's toggle that adds or removes the data, e.g. "Has a rarity".</summary>
        public string Label { get; set; }
    }
}
