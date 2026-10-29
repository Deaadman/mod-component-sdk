using System.Collections.Generic;
using System.Linq;
using System.Text;
using ModComponent.SDK.Editor.Integrations;

namespace ModComponent.SDK.ItemRarities.Editor
{
    /// <summary>rarities.ir: one [Rarity] section per rarity, listing gear names.</summary>
    public sealed class ItemRaritiesExporter : IntegrationExporter<ItemRaritiesIntegration>
    {
        public const string FileName = "rarities.ir";

        private static readonly string[] Extensions = { ".ir" };

        public override IReadOnlyCollection<string> RootExtensions => Extensions;

        protected override IEnumerable<KeyValuePair<string, string>> Export(Mod mod, ItemRaritiesIntegration integration)
        {
            var byRarity = mod.Items
                .Where(i => i != null)
                .Select(i => (item: i, data: i.GetIntegrationData<ItemRarityData>()))
                .Where(x => x.data != null)
                .GroupBy(x => x.data.Rarity)
                .OrderBy(g => g.Key);

            var sb = new StringBuilder();
            foreach (var group in byRarity)
            {
                sb.Append('[').Append(group.Key).Append("]\n");
                foreach (var (item, _) in group) sb.Append(item.GearName).Append('\n');
                sb.Append('\n');
            }

            if (sb.Length > 0) yield return new KeyValuePair<string, string>(FileName, sb.ToString());
        }
    }
}
