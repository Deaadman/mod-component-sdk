using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ModComponent.SDK.Editor.Catalog;
using ModComponent.SDK.Editor.Modules;
using ModComponent.SDK.Modules;
using ModComponent.SDK.References;
using UnityEditor;
using UnityEngine;

namespace ModComponent.SDK.Editor.Validation.Rules
{
    /// <summary>Base for rules that look at each item in the mod.</summary>
    public abstract class ItemRule : ValidationRule
    {
        public sealed override IEnumerable<ValidationIssue> Check(ValidationContext context) =>
            context.Mod.Items.Where(i => i != null).Distinct().SelectMany(i => CheckItem(i, context));

        protected abstract IEnumerable<ValidationIssue> CheckItem(GearItem item, ValidationContext context);
    }

    public sealed class GearNameRule : ItemRule
    {
        /// <summary>GEAR_ then word characters only: GearSpawner and the console both rely on \w names.</summary>
        public static readonly Regex Pattern = new(@"^GEAR_\w+$");

        public override string Id => "item.gear-name";

        protected override IEnumerable<ValidationIssue> CheckItem(GearItem item, ValidationContext context)
        {
            var name = item.GearName ?? string.Empty;
            if (!Pattern.IsMatch(name))
            {
                yield return Error($"Gear name \"{name}\" must be GEAR_ followed by letters, digits or '_'.", item, "Gear name");
                yield break;
            }

            if (context.Catalog.Contains(CatalogKind.Gear, name))
                yield return Error($"{name} is a vanilla item. Pick a name the game doesn't already use.", item, "Gear name");
        }
    }

    public sealed class ItemComponentRule : ItemRule
    {
        public override string Id => "item.component";

        protected override IEnumerable<ValidationIssue> CheckItem(GearItem item, ValidationContext context)
        {
            if (item.Component == null)
                yield return Error("The item has no component. ModComponent stops mapping every later item when one has none.", item, "Component");

            foreach (var module in item.AllModules)
                if (ModuleRegistry.Get(module) == null)
                    yield return Error($"{module.GetType().Name} has no [Module] attribute, so it can't be exported.", item);

            foreach (var group in item.Behaviours.Where(b => b != null).GroupBy(b => b.GetType()).Where(g => g.Count() > 1))
                yield return Error($"{ModuleRegistry.Get(group.Key)?.DisplayName ?? group.Key.Name} is on the item more than once.", item);

            var behaviours = item.Behaviours.Where(b => b != null).ToList();
            foreach (var behaviour in behaviours)
            {
                var info = ModuleRegistry.Get(behaviour);
                if (info == null) continue;
                foreach (var other in behaviours.Where(o => o != behaviour && info.Conflicts.Contains(o.GetType())))
                    if (behaviours.IndexOf(behaviour) < behaviours.IndexOf(other))
                        yield return Error($"{info.DisplayName} and {ModuleRegistry.Get(other)?.DisplayName} can't be combined; " +
                                           "ModComponent only loads one fire behaviour per item.", item);
            }
        }
    }

    public sealed class ItemPrefabRule : ItemRule
    {
        public override string Id => "item.prefab";

        protected override IEnumerable<ValidationIssue> CheckItem(GearItem item, ValidationContext context)
        {
            var prefab = item.Prefab;
            if (prefab == null)
            {
                yield return Error("No model prefab is assigned.", item, "Model prefab");
                yield break;
            }

            if (PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.NotAPrefab)
                yield return Error($"{prefab.name} isn't a prefab asset.", item, "Model prefab");

            if (prefab.name != item.GearName)
            {
                var path = AssetDatabase.GetAssetPath(prefab);
                yield return Error($"The prefab is named {prefab.name}. ModComponent finds items by prefab name, so it must be {item.GearName}.",
                    item, "Model prefab", string.IsNullOrEmpty(path) ? null : $"Rename prefab to {item.GearName}",
                    string.IsNullOrEmpty(path) ? null : () => AssetDatabase.RenameAsset(path, item.GearName));
            }

            if (prefab.GetComponentInChildren<Collider>(true) == null)
                yield return Warning("The prefab has no collider, so the item can't be picked up in game.", item, "Model prefab");

            if (prefab.GetComponent<Rigidbody>() != null)
                yield return Warning("The prefab root has a Rigidbody. The game adds its own physics; remove it.", item, "Model prefab");

            if (prefab.GetComponent<MeshRenderer>() != null || prefab.GetComponent<SkinnedMeshRenderer>() != null)
                yield return Warning("The mesh is on the prefab root. Put it on a child so the inspect view and model switching work.",
                    item, "Model prefab");

            foreach (var found in ChildReferences(item))
                if (!found.Reference.IsEmpty && prefab.transform.Find(found.Reference.Id) == null)
                    yield return Error($"\"{found.Reference.Id}\" isn't a direct child of {prefab.name}. ModComponent only looks one level down.",
                        item, found.Path);
        }

        private static IEnumerable<FoundReference> ChildReferences(GearItem item) =>
            ReferenceWalker.Walk(item).Where(r => r.Reference is ChildRef);
    }

    public sealed class ItemIconRule : ItemRule
    {
        public override string Id => "item.icons";

        protected override IEnumerable<ValidationIssue> CheckItem(GearItem item, ValidationContext context)
        {
            if (item.Icon == null)
                yield return Warning("No inventory icon. The item shows a blank square in the inventory.", item, "Inventory icon");
            else if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(item.Icon)) is TextureImporter importer
                     && importer.textureCompression != TextureImporterCompression.Uncompressed)
                yield return Warning("The inventory icon is compressed, which blurs it in game.", item, "Inventory icon",
                    "Make uncompressed", () =>
                    {
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        importer.SaveAndReimport();
                    });

            var craftedBy = context.Mod.Blueprints.Where(b => b != null && !b.IsCookingRecipe && b.CraftedResult.Id == item.GearName).ToList();
            if (craftedBy.Count > 0 && item.CraftingIcon == null)
                yield return Error($"{craftedBy[0].name} crafts this item, so it needs a crafting icon ({item.CraftingIconAddress}).",
                    item, "Crafting icon");
        }
    }
}
