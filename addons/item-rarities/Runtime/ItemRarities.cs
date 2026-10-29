using System;
using ModComponent.SDK.Integrations;
using UnityEngine.Scripting.APIUpdating;

namespace ModComponent.SDK.ItemRarities
{
    // These types were part of the SDK until 2.0. [MovedFrom] lets mods saved with them in the SDK's
    // ModComponent.SDK.Integrations namespace load them from here.

    /// <summary>The Item Rarities mod, which reads rarities.ir from a .modcomponent.</summary>
    [Serializable]
    [MovedFrom(false, "ModComponent.SDK.Integrations", "ModComponent.SDK.Runtime")]
    public sealed class ItemRaritiesIntegration : ModIntegration
    {
        public override string DisplayName => "Item Rarities";
        public override string Description => "Write rarities.ir for the Item Rarities mod.";
    }

    [MovedFrom(false, "ModComponent.SDK.Integrations", "ModComponent.SDK.Runtime")]
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    /// <summary>An item's rarity. Items without one aren't listed in rarities.ir.</summary>
    [Serializable]
    [ItemIntegrationData(typeof(ItemRaritiesIntegration), Label = "Has a rarity")]
    [MovedFrom(false, "ModComponent.SDK.Integrations", "ModComponent.SDK.Runtime")]
    public sealed class ItemRarityData : ItemIntegrationData
    {
        public Rarity Rarity;
    }
}
