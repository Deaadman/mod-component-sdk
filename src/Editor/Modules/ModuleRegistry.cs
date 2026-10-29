using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ModComponent.SDK.Modules;
using UnityEditor;

namespace ModComponent.SDK.Editor.Modules
{
    /// <summary>Everything the editor knows about one module type, read from its attributes.</summary>
    public sealed class ModuleInfo
    {
        internal ModuleInfo(Type type, ModuleAttribute attribute, ModuleKind kind)
        {
            Type = type;
            Kind = kind;
            ExportName = attribute.ExportName;
            DisplayName = string.IsNullOrEmpty(attribute.DisplayName) ? DefaultDisplayName(type) : attribute.DisplayName;
            Category = attribute.Category ?? string.Empty;
            Description = attribute.Description ?? string.Empty;
            Conflicts = type.GetCustomAttribute<ConflictsWithAttribute>()?.Modules ?? Array.Empty<Type>();
        }

        public Type Type { get; }
        public ModuleKind Kind { get; }
        public string ExportName { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public string Description { get; }
        public IReadOnlyList<Type> Conflicts { get; }

        public ModuleData CreateInstance() => (ModuleData)Activator.CreateInstance(Type);

        private static string DefaultDisplayName(Type type)
        {
            var name = Regex.Replace(type.Name, "(Component|Behaviour|Modification)$", string.Empty);
            return Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");
        }
    }

    /// <summary>Discovers every concrete module type, including ones added by other assemblies.</summary>
    public static class ModuleRegistry
    {
        private static IReadOnlyList<ModuleInfo> _all;
        private static Dictionary<Type, ModuleInfo> _byType;

        public static IReadOnlyList<ModuleInfo> All
        {
            get
            {
                EnsureLoaded();
                return _all;
            }
        }

        public static IEnumerable<ModuleInfo> OfKind(ModuleKind kind) => All.Where(m => m.Kind == kind);

        public static ModuleInfo Get(Type type)
        {
            EnsureLoaded();
            return type != null && _byType.TryGetValue(type, out var info) ? info : null;
        }

        public static ModuleInfo Get(ModuleData module) => module == null ? null : Get(module.GetType());

        /// <summary>Module types that cannot be added because of what is already on the item, with the reason.</summary>
        public static string GetBlockReason(ModuleInfo candidate, IEnumerable<ModuleData> existing)
        {
            var present = existing.Where(m => m != null).ToList();
            if (candidate.Kind != ModuleKind.Component && present.Any(m => m.GetType() == candidate.Type))
                return "already on this item";

            foreach (var module in present)
            {
                var info = Get(module);
                if (info == null) continue;
                if (candidate.Conflicts.Contains(info.Type) || info.Conflicts.Contains(candidate.Type))
                    return $"conflicts with {info.DisplayName}";
            }

            return null;
        }

        private static void EnsureLoaded()
        {
            if (_all != null) return;

            var list = new List<ModuleInfo>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<ModuleData>())
            {
                if (type.IsAbstract || type.IsGenericTypeDefinition) continue;
                var attribute = type.GetCustomAttribute<ModuleAttribute>(false);
                if (attribute == null) continue;
                list.Add(new ModuleInfo(type, attribute, KindOf(type)));
            }

            _all = list.OrderBy(m => m.Kind).ThenBy(m => m.Category).ThenBy(m => m.DisplayName).ToArray();
            _byType = _all.ToDictionary(m => m.Type);
        }

        private static ModuleKind KindOf(Type type)
        {
            if (typeof(ComponentModule).IsAssignableFrom(type)) return ModuleKind.Component;
            if (typeof(BehaviourModule).IsAssignableFrom(type)) return ModuleKind.Behaviour;
            return ModuleKind.Modification;
        }
    }
}
