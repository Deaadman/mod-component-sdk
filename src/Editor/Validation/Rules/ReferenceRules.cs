using System.Collections.Generic;
using System.Linq;
using ModComponent.SDK.Editor.Catalog;
using ModComponent.SDK.Modules;
using ModComponent.SDK.References;

namespace ModComponent.SDK.Editor.Validation.Rules
{
    /// <summary>
    /// Every reference on an item should name something that exists: a vanilla name from the catalog,
    /// an item in this project, or (for gear) an item from a required mod. The catalog can lag behind the game,
    /// so unknown names are warnings, not errors.
    /// </summary>
    public sealed class ItemReferencesRule : ItemRule
    {
        public override string Id => "item.references";

        protected override IEnumerable<ValidationIssue> CheckItem(GearItem item, ValidationContext context)
        {
            foreach (var found in ReferenceWalker.Walk(item))
            {
                var id = found.Reference.Id;
                if (found.Reference.IsEmpty) continue;

                switch (found.Reference)
                {
                    case GearRef when id == item.GearName && found.Module is not CookableComponent:
                        yield return Warning("The item refers to itself.", item, found.Path);
                        break;
                    case GearRef when !id.StartsWith(GearItem.GearPrefix):
                        yield return Error($"\"{id}\" isn't a gear name; gear names start with GEAR_.", item, found.Path);
                        break;
                    case GearRef when !context.IsKnownGear(id):
                        // With required mods the name may well come from one of them, so only mention it.
                        yield return context.Mod.RequiredMods.Count == 0
                            ? Warning($"{id} isn't a vanilla item or an item in this project. If it comes from another mod, " +
                                      "add that mod to Required mods.", item, found.Path)
                            : Info($"{id} isn't a vanilla item or an item in this project; check it exists in a required mod.", item, found.Path);
                        break;
                    case SoundRef when !context.Catalog.Contains(CatalogKind.Sound, id):
                        yield return Warning($"{id} isn't a known sound event; the item will be silent there.", item, found.Path);
                        break;
                }
            }

            foreach (var issue in ToolReferences(item, context))
                yield return issue;
        }

        /// <summary>Tool lists must name tools. Only checkable for this project's items.</summary>
        private IEnumerable<ValidationIssue> ToolReferences(GearItem item, ValidationContext context)
        {
            IEnumerable<(string path, GearRef tool)> tools = item.AllModules.SelectMany(m => m switch
            {
                RepairableBehaviour r => r.RequiredTools.Select(t => ("Repairable › Required tools", t)),
                HarvestableBehaviour h => h.RequiredToolNames.Select(t => ("Harvestable › Required tools", t)),
                SharpenableBehaviour s => s.Tools.Select(t => ("Sharpenable › Tools", t)),
                _ => Enumerable.Empty<(string, GearRef)>()
            });

            foreach (var (path, tool) in tools)
                if (context.ModItems.TryGetValue(tool.Id, out var toolItem) && toolItem.Component is not ToolComponent)
                    yield return Error($"{tool.Id} isn't a tool; ModComponent fails to load the item when a tool slot names a non-tool.", item, path);
        }
    }
}
