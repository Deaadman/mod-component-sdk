using System.Collections.Generic;
using System.Linq;
using ModComponent.SDK.Editor.Validation;
using UnityEditor;

namespace ModComponent.SDK.ItemRarities.Editor
{
    /// <summary>
    /// With Item Rarities on, every item should have a rarity: one without is left out of rarities.ir, and with
    /// none at all the file isn't written.
    /// </summary>
    public sealed class ItemRaritiesRule : ValidationRule
    {
        public override string Id => "item-rarities.coverage";

        public override IEnumerable<ValidationIssue> Check(ValidationContext context)
        {
            var mod = context.Mod;
            if (!mod.HasIntegration<ItemRaritiesIntegration>()) yield break;

            var items = mod.Items.Where(i => i != null).Distinct().ToList();
            var without = items.Where(i => i.GetIntegrationData<ItemRarityData>() == null).ToList();
            if (items.Count > 0 && without.Count == items.Count)
            {
                yield return Warning("Item Rarities is on, but no item has a rarity, so rarities.ir won't be written.", mod,
                    "Integrations", "Make them all Common", () => { foreach (var item in items) SetCommon(item); });
                yield break;
            }

            foreach (var item in without)
                yield return Info($"{item.GearName} has no rarity, so it isn't listed in rarities.ir.", item, "Item Rarities",
                    "Make it Common", () => SetCommon(item));
        }

        private static void SetCommon(GearItem item)
        {
            Undo.RecordObject(item, "Set rarity");
            item.GetOrAddIntegrationData<ItemRarityData>().Rarity = Rarity.Common;
            EditorUtility.SetDirty(item);
        }
    }
}
