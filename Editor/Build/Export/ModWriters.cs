using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ModComponent.SDK.Editor.Modules;
using ModComponent.SDK.Modules;
using ModComponent.SDK.References;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ModComponent.SDK.Editor.Build.Export
{
    /// <summary>Builds the text of each file in a .modcomponent. No file I/O happens here.</summary>
    public static class ModWriters
    {
        public static string ToJson(JToken token) => token.ToString(Formatting.Indented);

        // ---------------------------------------------------------------- buildinfo.json

        public static JObject BuildInfo(Mod mod) => new()
        {
            ["Name"] = mod.DisplayName ?? string.Empty,
            ["Version"] = mod.Version ?? string.Empty,
            ["Author"] = mod.Author ?? string.Empty,
            // Always an array: the loader's dependency checker throws on a missing Requires.
            ["Requires"] = new JArray(mod.RequiredMods.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim())),
            ["RequiresDLC"] = mod.RequiresDLC
        };

        // ---------------------------------------------------------------- auto-mapped item JSON

        /// <summary>
        /// The auto-mapped file name for an item. The loader looks items up by prefab name with every
        /// "GEAR_" removed, lower-cased.
        /// </summary>
        public static string ItemFileName(GearItem item) =>
            (item.GearName ?? string.Empty).Replace(GearItem.GearPrefix, string.Empty).ToLowerInvariant() + ".json";

        public static JObject Item(GearItem item, ICollection<string> notes = null)
        {
            var json = new JObject();
            foreach (var module in item.AllModules)
            {
                var info = ModuleRegistry.Get(module)
                           ?? throw new InvalidOperationException($"{module.GetType().Name} on {item.GearName} has no [Module] attribute.");
                var obj = ModuleSerializer.Serialize(module);
                foreach (var note in LoaderQuirks.Apply(info.ExportName, obj))
                    notes?.Add($"{item.GearName}: {note}");
                json[info.ExportName] = obj;
            }

            return json;
        }

        // ---------------------------------------------------------------- blueprints and recipes

        public static string BlueprintFileName(Blueprint blueprint) => SafeFileName(blueprint.name).ToLowerInvariant() + ".json";

        public static JObject Blueprint(Blueprint bp) => new()
        {
            ["Name"] = bp.EffectiveName,
            ["RequiredGear"] = GearRequirements(bp),
            ["RequiredPowder"] = Powders(bp),
            ["RequiredLiquid"] = Liquids(bp),
            ["RequiredTool"] = bp.RequiredTool.Id,
            ["OptionalTools"] = Ids(bp.OptionalTools),
            ["RequiredCraftingLocation"] = bp.RequiredCraftingLocation.ToString(),
            ["RequiresLitFire"] = bp.RequiresLitFire,
            ["RequiresLight"] = bp.RequiresLight,
            ["CraftedResult"] = bp.CraftedResult.Id,
            ["CraftedResultCount"] = bp.CraftedResultCount,
            ["DurationMinutes"] = bp.DurationMinutes,
            ["CraftingAudio"] = bp.CraftingAudio.Id,
            ["AppliedSkill"] = bp.AppliedSkill.ToString(),
            ["ImprovedSkill"] = bp.ImprovedSkill.ToString()
        };

        /// <summary>A cooking recipe. Skill, fire and location are fixed by the loader, so they are not written.</summary>
        public static JObject Recipe(Blueprint bp) => new()
        {
            ["RecipeName"] = bp.RecipeName.Id,
            ["RecipeDescription"] = bp.RecipeDescription.Id,
            ["RequiredSkillLevel"] = bp.RequiredSkillLevel,
            ["AllowedCookingPots"] = Ids(bp.AllowedCookingPots),
            ["BlueprintData"] = new JObject
            {
                ["Name"] = bp.EffectiveName,
                ["RequiredGear"] = GearRequirements(bp),
                ["RequiredPowder"] = Powders(bp),
                ["RequiredLiquid"] = Liquids(bp),
                ["CraftedResult"] = bp.CraftedResult.Id,
                ["CraftedResultCount"] = bp.CraftedResultCount,
                ["DurationMinutes"] = bp.DurationMinutes,
                ["CraftingAudio"] = bp.CraftingAudio.Id
            }
        };

        private static JArray GearRequirements(Blueprint bp) => new(bp.RequiredGear.Where(g => g != null).Select(g => new JObject
        {
            ["Item"] = g.Item.Id,
            // Only the field matching the units is set; the loader rejects recipes with both.
            ["Count"] = g.Units == RequirementUnits.Count ? g.Count : 0,
            ["Quantity"] = g.Units == RequirementUnits.Kilograms ? g.Quantity : 0f,
            ["Units"] = g.Units.ToString()
        }));

        private static JArray Powders(Blueprint bp) => new(bp.RequiredPowder.Select(p => new JObject
        {
            ["Powder"] = p.Powder.Id,
            ["QuantityInKilograms"] = p.QuantityInKilograms
        }));

        private static JArray Liquids(Blueprint bp) => new(bp.RequiredLiquid.Select(l => new JObject
        {
            ["Liquid"] = l.Liquid.Id,
            ["VolumeInLitres"] = l.VolumeInLitres
        }));

        private static JArray Ids<T>(IEnumerable<T> refs) where T : IReference =>
            new(refs.Where(r => !r.IsEmpty).Select(r => r.Id));

        // ---------------------------------------------------------------- localization

        /// <summary>
        /// { KEY: { Language: text } }. Empty translations are left out so the loader falls back to English;
        /// keys with no text at all are skipped, since an empty language map fails the whole file.
        /// </summary>
        public static JObject Localization(LocalizationTable table)
        {
            var json = new JObject();
            if (table == null) return json;
            foreach (var entry in table.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Key)) continue;
                var languages = new JObject();
                foreach (var t in entry.Translations.Where(t => !string.IsNullOrWhiteSpace(t.Language) && !string.IsNullOrEmpty(t.Text)))
                    languages[t.Language] = t.Text;
                if (languages.Count > 0) json[entry.Key] = languages;
            }

            return json;
        }

        // ---------------------------------------------------------------- gear spawns

        /// <summary>
        /// GearSpawner's text format. Numbers are always written with '.' and no exponent, whatever the
        /// computer's regional settings, because the loader only accepts -?\d+(\.\d+)?.
        /// </summary>
        public static string GearSpawns(SpawnTable table)
        {
            var sb = new StringBuilder();
            if (table == null) return string.Empty;

            const string defaultTag = "none";
            var currentTag = defaultTag;

            foreach (var group in table.Scenes.Where(g => g != null && !g.Scene.IsEmpty))
            {
                sb.Append("scene=").Append(group.Scene.Id).Append('\n');
                var tag = string.IsNullOrWhiteSpace(group.Tag) ? defaultTag : group.Tag.Trim();
                if (tag != currentTag)
                {
                    sb.Append("tag=").Append(tag).Append('\n');
                    currentTag = tag;
                }

                foreach (var spawn in group.Spawns.Where(s => s != null && !s.Item.IsEmpty))
                {
                    sb.Append("item=").Append(spawn.Item.Id)
                        .Append(" p=").Append(Vector(spawn.Position));
                    if (spawn.Rotation != Vector3.zero) sb.Append(" r=").Append(Vector(spawn.Rotation));
                    sb.Append(" c=").Append(Number(spawn.Chance)).Append('\n');
                }

                sb.Append('\n');
            }

            foreach (var group in table.LootTables.Where(g => g != null && !g.LootTable.IsEmpty))
            {
                sb.Append("loottable=").Append(group.LootTable.Id).Append('\n');
                foreach (var entry in group.Entries.Where(e => e != null && !e.Item.IsEmpty))
                    sb.Append("item=").Append(entry.Item.Id).Append(" w=").Append(entry.Weight.ToString(CultureInfo.InvariantCulture)).Append('\n');
                sb.Append('\n');
            }

            return sb.ToString();
        }

        private static string Vector(Vector3 v) => $"{Number(v.x)},{Number(v.y)},{Number(v.z)}";

        /// <summary>Fixed-point, invariant, at most 4 decimals, never "-0".</summary>
        public static string Number(float value)
        {
            var rounded = Math.Round((double)value, 4, MidpointRounding.AwayFromZero);
            if (rounded == 0) rounded = 0; // drops negative zero
            return rounded.ToString("0.####", CultureInfo.InvariantCulture);
        }

        // ---------------------------------------------------------------- helpers

        public static string SafeFileName(string name)
        {
            var chars = (name ?? string.Empty).Select(c => char.IsLetterOrDigit(c) && c < 128 || c == '-' || c == '_' ? c : '_').ToArray();
            var result = new string(chars).Trim('_');
            return result.Length == 0 ? "unnamed" : result;
        }
    }
}
