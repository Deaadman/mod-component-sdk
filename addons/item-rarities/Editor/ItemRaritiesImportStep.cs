using System;
using System.Linq;
using ModComponent.SDK.Editor.Migration;
using UnityEngine;
using V1Definition = ModComponent.SDK.Components.ModDefinition;

namespace ModComponent.SDK.ItemRarities.Editor
{
    /// <summary>Brings a 1.x mod's ModItemRarities across when it's imported.</summary>
    public sealed class ItemRaritiesImportStep : V1ImportStep
    {
        public override void Import(V1Definition definition, Mod mod, ImportReport report)
        {
            var v1 = definition.modItemRarities;
            if (v1 == null) return;
            mod.EnableIntegration<ItemRaritiesIntegration>();
            var tiers = new (GameObject[] prefabs, Rarity rarity)[]
            {
                (v1.Common, Rarity.Common), (v1.Uncommon, Rarity.Uncommon), (v1.Rare, Rarity.Rare),
                (v1.Epic, Rarity.Epic), (v1.Legendary, Rarity.Legendary), (v1.Mythic, Rarity.Mythic)
            };
            foreach (var (prefabs, rarity) in tiers)
            foreach (var prefab in (prefabs ?? Array.Empty<GameObject>()).Where(p => p != null))
            {
                var item = mod.Items.FirstOrDefault(i => i != null && i.Prefab == prefab);
                if (item != null) item.GetOrAddIntegrationData<ItemRarityData>().Rarity = rarity;
                else report.Note($"Rarity for {prefab.name} skipped: it isn't one of the mod's items.");
            }
        }
    }
}
