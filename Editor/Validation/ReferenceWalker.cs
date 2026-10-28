using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ModComponent.SDK.Editor.Build.Export;
using ModComponent.SDK.Editor.Modules;
using ModComponent.SDK.Modules;
using ModComponent.SDK.References;

namespace ModComponent.SDK.Editor.Validation
{
    /// <summary>A reference found on a module, with a readable path such as "Repairable › Materials[1]".</summary>
    public readonly struct FoundReference
    {
        public FoundReference(ModuleData module, string path, IReference reference, FieldInfo field)
        {
            Module = module;
            Path = path;
            Reference = reference;
            Field = field;
        }

        public ModuleData Module { get; }
        public string Path { get; }
        public IReference Reference { get; }
        public FieldInfo Field { get; }
    }

    public static class ReferenceWalker
    {
        /// <summary>Every reference on the item's modules, including ones inside lists and <see cref="GearCount"/> entries.</summary>
        public static IEnumerable<FoundReference> Walk(GearItem item)
        {
            foreach (var module in item.AllModules)
            {
                var moduleName = ModuleRegistry.Get(module)?.DisplayName ?? module.GetType().Name;
                foreach (var field in ModuleSerializer.GetExportedFields(module.GetType()))
                {
                    var path = $"{moduleName} › {field.Name}";
                    var value = field.GetValue(module);
                    switch (value)
                    {
                        case IReference reference:
                            yield return new FoundReference(module, path, reference, field);
                            break;
                        case IList list:
                            for (var i = 0; i < list.Count; i++)
                            {
                                if (list[i] is IReference r) yield return new FoundReference(module, $"{path}[{i}]", r, field);
                                else if (list[i] is GearCount pair) yield return new FoundReference(module, $"{path}[{i}]", pair.Item, field);
                            }

                            break;
                    }
                }
            }
        }
    }
}
