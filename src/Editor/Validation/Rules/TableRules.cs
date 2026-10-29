using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ModComponent.SDK.Editor.Catalog;
using ModComponent.SDK.Modules;
using ModComponent.SDK.References;
using UnityEditor;
using UnityEngine;

namespace ModComponent.SDK.Editor.Validation.Rules
{
    public sealed class LocalizationRule : ValidationRule
    {
        public override string Id => "localization";

        public override IEnumerable<ValidationIssue> Check(ValidationContext context)
        {
            var mod = context.Mod;
            var table = mod.Localization;
            var used = UsedKeys(mod).ToList();

            if (table == null)
            {
                if (used.Count > 0)
                    yield return Warning("Items use localization keys, but the mod has no localization table.", mod, "Localization",
                        "Create table", () => CreateTable(mod));
                yield break;
            }

            var languages = new HashSet<string>(context.Catalog.Languages);
            foreach (var entry in table.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    yield return Error("A row has no key.", table);
                    continue;
                }

                if (string.IsNullOrEmpty(entry.Get(LocalizationTable.DefaultLanguage)))
                    yield return Warning($"{entry.Key} has no English text, which every other language falls back to.", table, entry.Key);

                foreach (var t in entry.Translations.Where(t => !languages.Contains(t.Language)))
                    yield return Error($"\"{t.Language}\" isn't a language name the game uses, so the text is never shown.", table, entry.Key);
            }

            foreach (var group in table.Entries.Where(e => !string.IsNullOrEmpty(e.Key)).GroupBy(e => e.Key).Where(g => g.Count() > 1))
                yield return Error($"{group.Key} appears {group.Count()} times.", table, group.Key);

            foreach (var (key, item, path) in used.Where(u => !table.Contains(u.key)))
                yield return Warning($"{key} isn't in the localization table, so the game shows the raw key.", item, path,
                    $"Add {key}", () => ModIdentityRule.Edit(table, t => t.GetOrAdd(key)));
        }

        /// <summary>Keys the mod's items and recipes refer to, excluding vanilla GAMEPLAY_ keys used on purpose.</summary>
        public static IEnumerable<(string key, Object asset, string path)> UsedKeys(Mod mod)
        {
            foreach (var item in mod.Items.Where(i => i != null).Distinct())
            foreach (var found in ReferenceWalker.Walk(item).Where(r => r.Reference is LocalizationKey && !r.Reference.IsEmpty))
                yield return (found.Reference.Id, item, found.Path);

            foreach (var bp in mod.Blueprints.Where(b => b != null && b.IsCookingRecipe))
            {
                if (!bp.RecipeName.IsEmpty) yield return (bp.RecipeName.Id, bp, "Recipe name");
                if (!bp.RecipeDescription.IsEmpty) yield return (bp.RecipeDescription.Id, bp, "Recipe description");
            }
        }

        private static void CreateTable(Mod mod)
        {
            var folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(mod)) ?? "Assets";
            var table = ScriptableObject.CreateInstance<LocalizationTable>();
            AssetDatabase.CreateAsset(table, AssetDatabase.GenerateUniqueAssetPath($"{folder}/Localization.asset"));
            ModIdentityRule.Edit(mod, m => m.Localization = table);
            foreach (var (key, _, _) in UsedKeys(mod).Distinct()) table.GetOrAdd(key);
            EditorUtility.SetDirty(table);
        }
    }

    public sealed class BlueprintRule : ValidationRule
    {
        public override string Id => "blueprint";

        public override IEnumerable<ValidationIssue> Check(ValidationContext context)
        {
            foreach (var bp in context.Mod.Blueprints.Where(b => b != null).Distinct())
            {
                if (bp.CraftedResult.IsEmpty)
                    yield return Error("Set the crafted result.", bp, "Crafted result");
                else if (!context.IsKnownGear(bp.CraftedResult.Id))
                    yield return Warning($"{bp.CraftedResult.Id} isn't a vanilla item or an item in this project.", bp, "Crafted result");

                if (bp.CraftedResultCount < 1)
                    yield return Error("Crafted result count must be at least 1.", bp, "Crafted result count");

                if (bp.RequiredGear.Count == 0 && bp.RequiredPowder.Count == 0 && bp.RequiredLiquid.Count == 0)
                    yield return Error("Add at least one ingredient.", bp, "Required gear");

                for (var i = 0; i < bp.RequiredGear.Count; i++)
                {
                    var g = bp.RequiredGear[i];
                    var path = $"Required gear[{i}]";
                    if (g == null || g.Item.IsEmpty) { yield return Error("An ingredient has no item.", bp, path); continue; }
                    if (!context.IsKnownGear(g.Item.Id))
                        yield return Warning($"{g.Item.Id} isn't a vanilla item or an item in this project.", bp, path);

                    if (bp.IsCookingRecipe)
                    {
                        if (g.Units == RequirementUnits.Count && g.Count < 1)
                            yield return Error("Count must be at least 1.", bp, path);
                        if (g.Units == RequirementUnits.Kilograms && g.Quantity <= 0)
                            yield return Error("Quantity must be above 0 kg.", bp, path);
                    }
                    else if (g.Count < 1)
                    {
                        yield return Error("Blueprints need a count of at least 1 for every ingredient, even when measured in kilograms.", bp, path);
                    }
                }

                for (var i = 0; i < bp.RequiredLiquid.Count; i++)
                {
                    var liquid = bp.RequiredLiquid[i].Liquid;
                    if (liquid.IsEmpty)
                        yield return Error("Pick a liquid type, such as LIQUID_Kerosene.", bp, $"Required liquid[{i}]");
                    else if (!context.Catalog.Contains(CatalogKind.Liquid, liquid.Id))
                        yield return Warning($"{liquid.Id} isn't a known liquid type.", bp, $"Required liquid[{i}]");
                }

                for (var i = 0; i < bp.RequiredPowder.Count; i++)
                {
                    var powder = bp.RequiredPowder[i].Powder;
                    if (powder.IsEmpty)
                        yield return Error("Pick a powder type, such as POWDER_Gunpowder.", bp, $"Required powder[{i}]");
                    else if (!context.Catalog.Contains(CatalogKind.Powder, powder.Id))
                        yield return Warning($"{powder.Id} isn't a known powder type.", bp, $"Required powder[{i}]");
                }

                if (bp.IsCookingRecipe)
                {
                    if (bp.RecipeName.IsEmpty)
                        yield return Error("A recipe needs a name key.", bp, "Recipe name");
                    if (bp.RequiredSkillLevel < 1)
                        yield return Error("Required skill level must be at least 1.", bp, "Required skill level");
                    if (context.ModItems.TryGetValue(bp.CraftedResult.Id, out var result)
                        && (result.Component is not CookableComponent cookable || cookable.CookingResult.IsEmpty))
                        yield return Error($"Recipes take their icon from the result's cooked item, and {result.GearName} has no cooking result.",
                            bp, "Crafted result");
                }
            }
        }
    }

    public sealed class SpawnRule : ValidationRule
    {
        /// <summary>GearSpawner only accepts word characters in names; anything else stops the rest of the pack loading.</summary>
        private static readonly Regex Word = new(@"^\w+$");

        public override string Id => "spawns";

        public override IEnumerable<ValidationIssue> Check(ValidationContext context)
        {
            var table = context.Mod.Spawns;
            if (table == null) yield break;

            for (var g = 0; g < table.Scenes.Count; g++)
            {
                var group = table.Scenes[g];
                var where = group.Scene.IsEmpty ? $"Scene group {g + 1}" : group.Scene.Id;
                if (group.Scene.IsEmpty) { yield return Error("Pick a scene.", table, where); continue; }
                if (!Word.IsMatch(group.Scene.Id))
                    yield return Error($"Scene \"{group.Scene.Id}\" may only use letters, digits and '_'.", table, where);
                else if (!context.Catalog.Contains(CatalogKind.Scene, group.Scene.Id))
                    yield return Warning($"{group.Scene.Id} isn't a known scene name.", table, where);
                if (!string.IsNullOrWhiteSpace(group.Tag) && !Word.IsMatch(group.Tag.Trim()))
                    yield return Error($"Tag \"{group.Tag}\" may only use letters, digits and '_'.", table, where);

                if (group.Spawns.Any(s => s != null && !s.Item.IsEmpty && s.Chance <= 0))
                    yield return Warning("A spawn has a 0% chance, so it never appears.", table, where);

                foreach (var issue in CheckItems(group.Spawns.Select(s => s?.Item ?? default), table, where, context))
                    yield return issue;
            }

            for (var g = 0; g < table.LootTables.Count; g++)
            {
                var group = table.LootTables[g];
                var where = group.LootTable.IsEmpty ? $"Loot table {g + 1}" : group.LootTable.Id;
                if (group.LootTable.IsEmpty) { yield return Error("Pick a loot table.", table, where); continue; }
                if (!Word.IsMatch(group.LootTable.Id))
                    yield return Error($"Loot table \"{group.LootTable.Id}\" may only use letters, digits and '_'.", table, where);
                else if (!context.Catalog.Contains(CatalogKind.LootTable, group.LootTable.Id))
                    yield return Warning($"{group.LootTable.Id} isn't a known loot table.", table, where);
                if (group.Entries.Any(e => e != null && e.Weight < 1))
                    yield return Error("Loot weights must be whole numbers of at least 1.", table, where);

                foreach (var issue in CheckItems(group.Entries.Select(e => e?.Item ?? default), table, where, context))
                    yield return issue;
            }
        }

        private IEnumerable<ValidationIssue> CheckItems(IEnumerable<GearRef> items, SpawnTable table, string where, ValidationContext context)
        {
            foreach (var item in items)
            {
                if (item.IsEmpty) { yield return Error("A row has no item.", table, where); continue; }
                if (!Word.IsMatch(item.Id))
                    yield return Error($"Item \"{item.Id}\" may only use letters, digits and '_'.", table, where);
                else if (!context.IsKnownGear(item.Id) && !context.IsKnownGear(GearItem.GearPrefix + item.Id))
                    yield return Warning($"{item.Id} isn't a vanilla item or an item in this project.", table, where);
            }
        }
    }
}
