using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModComponent.SDK.Integrations;
using UnityEditor;

namespace ModComponent.SDK.Editor.Integrations
{
    /// <summary>
    /// Writes the files an integration adds to the archive. Subclasses are found automatically, one per integration
    /// type, so an add-on package only has to define one.
    /// </summary>
    public abstract class IntegrationExporter
    {
        public abstract Type IntegrationType { get; }

        /// <summary>
        /// Extensions this exporter writes at the archive root, such as ".ir". ModComponent skips files it doesn't
        /// read, so only those extensions are allowed there; the packager refuses any it would load.
        /// </summary>
        public virtual IReadOnlyCollection<string> RootExtensions => Array.Empty<string>();

        /// <summary>Archive-relative path → file contents.</summary>
        public abstract IEnumerable<KeyValuePair<string, string>> Export(Mod mod, ModIntegration integration);
    }

    public abstract class IntegrationExporter<T> : IntegrationExporter where T : ModIntegration
    {
        public sealed override Type IntegrationType => typeof(T);

        public sealed override IEnumerable<KeyValuePair<string, string>> Export(Mod mod, ModIntegration integration) =>
            Export(mod, (T)integration);

        protected abstract IEnumerable<KeyValuePair<string, string>> Export(Mod mod, T integration);
    }

    /// <summary>An <see cref="ItemIntegrationData"/> type and how the item inspector labels it.</summary>
    public sealed class ItemDataInfo
    {
        internal ItemDataInfo(Type type, string label)
        {
            Type = type;
            Label = string.IsNullOrEmpty(label) ? "Include" : label;
        }

        public Type Type { get; }
        public string Label { get; }
    }

    /// <summary>Everything the editor knows about one integration type, from whichever package defines it.</summary>
    public sealed class ModIntegrationInfo
    {
        internal ModIntegrationInfo(Type type, IReadOnlyList<ItemDataInfo> itemData, IntegrationExporter exporter)
        {
            Type = type;
            var sample = (ModIntegration)Activator.CreateInstance(type);
            DisplayName = sample.DisplayName;
            Description = sample.Description ?? string.Empty;
            ItemData = itemData;
            Exporter = exporter;
        }

        public Type Type { get; }
        public string DisplayName { get; }
        public string Description { get; }

        /// <summary>Per-item data types, each drawn as a section on items of mods that enable the integration.</summary>
        public IReadOnlyList<ItemDataInfo> ItemData { get; }

        /// <summary>Null when no exporter is installed, so the integration's data isn't written.</summary>
        public IntegrationExporter Exporter { get; }
    }

    /// <summary>Discovers every concrete <see cref="ModIntegration"/>, its item data and its exporter.</summary>
    public static class IntegrationRegistry
    {
        private static IReadOnlyList<ModIntegrationInfo> _all;

        public static IReadOnlyList<ModIntegrationInfo> All => _all ??= Load();

        public static ModIntegrationInfo Get(Type type) => All.FirstOrDefault(i => i.Type == type);

        public static ModIntegrationInfo Get(ModIntegration integration) => integration == null ? null : Get(integration.GetType());

        /// <summary>Root extensions the mod's enabled integrations write, e.g. ".ir" for Item Rarities.</summary>
        public static IReadOnlyCollection<string> RootExtensions(Mod mod) => mod.Integrations
            .Select(Get)
            .Where(i => i?.Exporter != null)
            .SelectMany(i => i.Exporter.RootExtensions)
            .Select(e => e.ToLowerInvariant())
            .Distinct()
            .ToList();

        private static IReadOnlyList<ModIntegrationInfo> Load()
        {
            var exporters = TypeCache.GetTypesDerivedFrom<IntegrationExporter>()
                .Where(t => !t.IsAbstract && !t.ContainsGenericParameters && t.GetConstructor(Type.EmptyTypes) != null)
                .Select(t => (IntegrationExporter)Activator.CreateInstance(t))
                .GroupBy(e => e.IntegrationType)
                .ToDictionary(g => g.Key, g => g.First());

            var itemData = TypeCache.GetTypesDerivedFrom<ItemIntegrationData>()
                .Where(t => !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
                .Select(t => (type: t, attribute: t.GetCustomAttribute<ItemIntegrationDataAttribute>(false)))
                .Where(x => x.attribute?.Integration != null)
                .ToLookup(x => x.attribute.Integration, x => new ItemDataInfo(x.type, x.attribute.Label));

            return TypeCache.GetTypesDerivedFrom<ModIntegration>()
                .Where(t => !t.IsAbstract && !t.ContainsGenericParameters && t.GetConstructor(Type.EmptyTypes) != null)
                .Select(t => new ModIntegrationInfo(t, itemData[t].OrderBy(d => d.Type.Name, StringComparer.Ordinal).ToList(),
                    exporters.TryGetValue(t, out var exporter) ? exporter : null))
                .OrderBy(i => i.DisplayName, StringComparer.Ordinal)
                .ToList();
        }
    }
}
