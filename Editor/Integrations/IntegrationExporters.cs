using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ModComponent.SDK.Integrations;
using UnityEditor;

namespace ModComponent.SDK.Editor.Integrations
{
    /// <summary>Writes the files an integration adds to the archive. Found by reflection, one per integration type.</summary>
    public interface IIntegrationExporter
    {
        Type IntegrationType { get; }

        /// <summary>Archive-relative path → file contents.</summary>
        IEnumerable<KeyValuePair<string, string>> Export(Mod mod, ModIntegration integration);
    }

    public static class IntegrationExporters
    {
        private static Dictionary<Type, IIntegrationExporter> _exporters;

        public static IIntegrationExporter For(Type integrationType)
        {
            _exporters ??= TypeCache.GetTypesDerivedFrom<IIntegrationExporter>()
                .Where(t => !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
                .Select(t => (IIntegrationExporter)Activator.CreateInstance(t))
                .ToDictionary(e => e.IntegrationType);
            return _exporters.TryGetValue(integrationType, out var exporter) ? exporter : null;
        }
    }

    /// <summary>rarities.ir: one [Rarity] section per rarity, listing gear names.</summary>
    public sealed class ItemRaritiesExporter : IIntegrationExporter
    {
        public const string FileName = "rarities.ir";

        public Type IntegrationType => typeof(ItemRaritiesIntegration);

        public IEnumerable<KeyValuePair<string, string>> Export(Mod mod, ModIntegration integration)
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
