using System;

namespace ModComponent.SDK.Integrations
{
    /// <summary>
    /// Support for another mod that reads extra data from a .modcomponent, enabled per mod.
    /// The editor discovers the matching exporter by type.
    /// </summary>
    [Serializable]
    public abstract class ModIntegration
    {
        public abstract string DisplayName { get; }
    }

    /// <summary>Per-item data for an integration, such as an item's rarity.</summary>
    [Serializable]
    public abstract class ItemIntegrationData
    {
    }

    /// <summary>The Item Rarities mod (rarities.ir).</summary>
    [Serializable]
    public sealed class ItemRaritiesIntegration : ModIntegration
    {
        public override string DisplayName => "Item Rarities";
    }

    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    [Serializable]
    public sealed class ItemRarityData : ItemIntegrationData
    {
        public Rarity Rarity;
    }
}
