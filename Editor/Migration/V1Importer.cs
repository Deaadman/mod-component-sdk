using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModComponent.SDK.Editor.Authoring;
using ModComponent.SDK.Editor.Build.Export;
using ModComponent.SDK.Editor.Modules;
using ModComponent.SDK.Integrations;
using ModComponent.SDK.Modules;
using ModComponent.SDK.References;
using UnityEditor;
using UnityEngine;
using V1Blueprint = ModComponent.Blueprints.ModBlueprint;
using V1Definition = ModComponent.SDK.Components.ModDefinition;
using V1Recipe = ModComponent.Blueprints.ModRecipe;

namespace ModComponent.SDK.Editor.Migration
{
    /// <summary>What an import did, or would do in a dry run.</summary>
    public sealed class ImportReport
    {
        public V1Definition Source;
        public Mod Mod;
        public bool DryRun;
        public readonly List<GearItem> Items = new();
        public readonly List<Blueprint> Blueprints = new();
        public readonly List<string> Notes = new();
        public int LocalizationKeys;
        public int Spawns;

        internal void Note(string text) => Notes.Add(text);
    }

    /// <summary>
    /// Reads a mod made with SDK 1.x (a ModDefinition, its prefabs' Mod*Component scripts, localization, spawns and
    /// rarities) into 2.0 assets. Nothing from 1.x is changed or deleted; removing the old scripts from prefabs is
    /// a separate step (<see cref="RemoveLegacyComponents"/>).
    /// </summary>
    public static class V1Importer
    {
        /// <summary>The 1.x assembly name, kept by the Legacy folder so old prefabs still resolve their scripts.</summary>
        public const string LegacyAssembly = "ModComponent.SDK";

        // 1.x field name → 2.0 field name, where they differ.
        private static readonly Dictionary<string, string> RenamedFields = new()
        {
            ["DescriptionLocalizatonId"] = "DescriptionLocalizationId",
            ["RepairRequired"] = "RepairRequiredGear",
            ["RestoreRequired"] = "RestoreRequiredGear",
        };

        // 1.x fields the loader never reads; dropped with a note.
        private static readonly HashSet<string> IgnoredFields = new()
        {
            "Footwear", "Servings", "PowderType", "SuccessModifier", "DurationOffset", "RuinedAfterUse", "ToolKind"
        };

        // 1.x language field → the game's language name.
        private static readonly Dictionary<string, string> Languages = new()
        {
            ["English"] = "English", ["German"] = "German", ["Russian"] = "Russian", ["French"] = "French (France)",
            ["Japanese"] = "Japanese", ["Korean"] = "Korean", ["SimplifiedChinese"] = "Simplified Chinese", ["Swedish"] = "Swedish",
            ["TraditionalChinese"] = "Traditional Chinese", ["Turkish"] = "Turkish", ["Norwegian"] = "Norwegian",
            ["Spanish"] = "Spanish (Spain)", ["PortuguesePortugal"] = "Portuguese (Portugal)", ["PortugueseBrazil"] = "Portuguese (Brazil)",
            ["Dutch"] = "Dutch", ["Finnish"] = "Finnish", ["Italian"] = "Italian", ["Polish"] = "Polish", ["Ukrainian"] = "Ukrainian"
        };

        public static IEnumerable<V1Definition> FindDefinitions() =>
            AssetDatabase.FindAssets("t:" + nameof(V1Definition))
                .Select(g => AssetDatabase.LoadAssetAtPath<V1Definition>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(d => d != null);

        /// <summary>Whether a 2.0 mod already exists for this 1.x definition (matched by id).</summary>
        public static bool IsImported(V1Definition definition) =>
            Validation.ModValidator.FindAllMods().Any(m => string.Equals(m.Id, Mod.ToId(definition.Name), StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Converts the definition. With <paramref name="dryRun"/> the assets are built in memory, reported, then
        /// destroyed; otherwise they are saved under <paramref name="folder"/> (default Assets/Mods/&lt;Id&gt;).
        /// </summary>
        public static ImportReport Import(V1Definition definition, bool dryRun, string folder = null)
        {
            var report = new ImportReport { Source = definition, DryRun = dryRun };
            var id = Mod.ToId(definition.Name);
            if (!Mod.IsValidId(id)) id = "ImportedMod";
            folder ??= $"{AssetFactory.DefaultModsFolder}/{id}";

            var mod = ScriptableObject.CreateInstance<Mod>();
            mod.name = id;
            mod.DisplayName = definition.Name;
            mod.Id = id;
            mod.Author = definition.Author ?? string.Empty;
            mod.Version = string.IsNullOrWhiteSpace(definition.Version) ? "1.0.0" : definition.Version;
            mod.RequiresDLC = definition.RequiresDLC;
            if (definition.RequiredMods != null) mod.RequiredMods.AddRange(definition.RequiredMods.Where(r => !string.IsNullOrWhiteSpace(r)));
            report.Mod = mod;

            var created = new List<UnityEngine.Object> { mod };
            var saved = false;
            try
            {
                var icons = (definition.Icons ?? Array.Empty<Texture2D>()).Where(t => t != null).ToList();
                foreach (var prefab in (definition.Items ?? Array.Empty<GameObject>()).Where(p => p != null))
                {
                    var item = ImportItem(prefab, icons, report);
                    created.Add(item);
                    mod.Items.Add(item);
                    report.Items.Add(item);

                    foreach (var v1 in prefab.GetComponents<V1Blueprint>())
                    {
                        var bp = ImportBlueprint(v1, prefab, report);
                        created.Add(bp);
                        mod.Blueprints.Add(bp);
                        report.Blueprints.Add(bp);
                    }
                }

                foreach (var icon in icons)
                {
                    mod.BundledAssets.Add(icon);
                    report.Note($"{icon.name} doesn't match an item's icon name; it was added to Bundled assets.");
                }

                mod.Localization = ImportLocalization(definition, report);
                if (mod.Localization != null) created.Add(mod.Localization);
                mod.Spawns = ImportSpawns(definition, report);
                if (mod.Spawns != null) created.Add(mod.Spawns);
                ImportRarities(definition, mod, report);

                if (!dryRun && IsImported(definition))
                    report.Note($"A mod with the id {id} already exists, so nothing was written. Delete or rename it to import again.");
                else if (!dryRun)
                {
                    Save(report, folder);
                    saved = true;
                }
                return report;
            }
            finally
            {
                if (!saved)
                    foreach (var o in created.Where(o => o != null)) UnityEngine.Object.DestroyImmediate(o);
            }
        }

        private static GearItem ImportItem(GameObject prefab, List<Texture2D> icons, ImportReport report)
        {
            var item = ScriptableObject.CreateInstance<GearItem>();
            item.name = prefab.name;
            item.GearName = prefab.name;
            item.Prefab = prefab;

            var shortName = item.ShortName;
            item.Icon = Take(icons, GearItem.InventoryIconPrefix + shortName);
            item.CraftingIcon = Take(icons, GearItem.CraftingIconPrefix + shortName);

            foreach (var v1 in prefab.GetComponents<MonoBehaviour>().Where(IsLegacy))
            {
                if (v1 is V1Blueprint) continue;
                var info = ModuleRegistry.All.FirstOrDefault(m => m.ExportName == v1.GetType().Name);
                if (info == null)
                {
                    if (v1.GetType().Name != "ModBaseBehaviour")
                        report.Note($"{prefab.name}: {v1.GetType().Name} has no 2.0 equivalent and was skipped.");
                    continue;
                }

                var module = info.CreateInstance();
                CopyFields(v1, module, $"{prefab.name} › {info.DisplayName}", report, item);
                try
                {
                    item.AddModule(module);
                }
                catch (InvalidOperationException e)
                {
                    report.Note($"{prefab.name}: {e.Message} Skipped {info.DisplayName}.");
                }
            }

            if (item.Component == null) report.Note($"{prefab.name} has no 1.x component; add one before building.");
            return item;
        }

        /// <summary>Copies same-named fields, converting 1.x data assets to names and arrays to lists.</summary>
        private static void CopyFields(object source, ModuleData target, string where, ImportReport report, GearItem item)
        {
            var targetFields = ModuleSerializer.GetExportedFields(target.GetType()).ToDictionary(f => f.Name);
            foreach (var field in source.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = field.GetValue(source);
                if (IgnoredFields.Contains(field.Name))
                {
                    if (value != null && !Equals(value, Activator.CreateInstance(field.FieldType.IsValueType ? field.FieldType : typeof(object))))
                        report.Note($"{where}: dropped {field.Name}; ModComponent 7.0.0 ignores it.");
                    continue;
                }

                var name = RenamedFields.TryGetValue(field.Name, out var renamed) ? renamed : field.Name;
                if (!targetFields.TryGetValue(name, out var targetField))
                {
                    if (target is RandomWeightedItemComponent weighted && field.Name == "ItemNames")
                    {
                        var names = ((IEnumerable)value)?.Cast<object>().Select(NameOf).ToList() ?? new List<string>();
                        var weights = source.GetType().GetField("ItemWeights")?.GetValue(source) as int[] ?? Array.Empty<int>();
                        weighted.Items = names.Select((n, i) => new GearCount(n, i < weights.Length ? weights[i] : 1)).ToList();
                    }
                    else if (field.Name != "ItemWeights")
                    {
                        report.Note($"{where}: {field.Name} has no 2.0 field and was skipped.");
                    }

                    continue;
                }

                if (field.Name == "EquippedModelPrefab" && value is UnityEngine.Object model && model != null)
                {
                    // 1.x pointed at a data asset; 2.0 names the prefab, which then has to be in the bundle.
                    var prefabPath = AssetDatabase.FindAssets(model.name + " t:GameObject").Select(AssetDatabase.GUIDToAssetPath)
                        .FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p) == model.name);
                    var equipped = prefabPath != null ? AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) : null;
                    if (equipped != null && !report.Mod.BundledAssets.Contains(equipped)) report.Mod.BundledAssets.Add(equipped);
                    else if (equipped == null) report.Note($"{where}: add the equipped model prefab {model.name} to Bundled assets; it wasn't found.");
                }

                if (value is Texture texture)
                {
                    // Paper-doll textures are referenced by name in 2.0 and must be bundled.
                    var mod = report.Mod;
                    if (!mod.BundledAssets.Contains(texture)) mod.BundledAssets.Add(texture);
                }

                if (TryConvert(value, targetField.FieldType, out var converted))
                    targetField.SetValue(target, converted);
                else
                    report.Note($"{where}: couldn't convert {field.Name} ({value}); left at the 2.0 default.");
            }
        }

        private static bool TryConvert(object value, Type target, out object result)
        {
            result = null;
            if (value == null || value is UnityEngine.Object o && o == null)
            {
                if (target == typeof(GearCount)) return false;
                result = target.IsValueType ? Activator.CreateInstance(target)
                    : target == typeof(string) ? string.Empty
                    : typeof(IList).IsAssignableFrom(target) ? Activator.CreateInstance(target)
                    : null;
                return true;
            }

            if (typeof(IReference).IsAssignableFrom(target))
            {
                result = Activator.CreateInstance(target, NameOf(value));
                return true;
            }

            if (target == typeof(string)) { result = NameOf(value); return true; }
            if (target.IsEnum)
            {
                if (!Enum.TryParse(target, value.ToString(), true, out var parsed)) return false;
                result = parsed;
                return true;
            }

            if (target == typeof(int) || target == typeof(float) || target == typeof(bool))
            {
                result = Convert.ChangeType(value, target, System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }

            if (target == typeof(Vector3) && value is Vector3) { result = value; return true; }

            if (target.IsGenericType && target.GetGenericTypeDefinition() == typeof(List<>) && value is IEnumerable sequence)
            {
                var element = target.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(target);
                foreach (var entry in sequence)
                {
                    if (element == typeof(GearCount))
                    {
                        var pair = ToGearCount(entry);
                        if (pair != null) list.Add(pair);
                    }
                    else if (TryConvert(entry, element, out var converted)) list.Add(converted);
                }

                result = list;
                return true;
            }

            if (target == typeof(GearCount))
            {
                result = ToGearCount(value);
                return result != null;
            }

            return false;
        }

        /// <summary>1.x paired entries (Yields, Materials, RepairRequired...) hold one data asset and one int.</summary>
        private static GearCount ToGearCount(object entry)
        {
            if (entry == null) return null;
            var fields = entry.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            var item = fields.FirstOrDefault(f => typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType));
            var count = fields.FirstOrDefault(f => f.FieldType == typeof(int));
            if (item == null) return null;
            return new GearCount(NameOf(item.GetValue(entry)), count != null ? (int)count.GetValue(entry) : 1);
        }

        private static string NameOf(object value) => value switch
        {
            null => string.Empty,
            UnityEngine.Object o when o == null => string.Empty,
            UnityEngine.Object o => o.name,
            _ => value.ToString()
        };

        private static Blueprint ImportBlueprint(V1Blueprint v1, GameObject prefab, ImportReport report)
        {
            var bp = ScriptableObject.CreateInstance<Blueprint>();
            bp.name = "BP_" + prefab.name.Replace(GearItem.GearPrefix, string.Empty) + (v1 is V1Recipe ? "_Recipe" : string.Empty);
            bp.Name = v1.Name ?? string.Empty;
            foreach (var g in v1.RequiredGear ?? Array.Empty<ModComponent.Blueprints.GearRequirement>())
                if (g != null)
                    bp.RequiredGear.Add(new Blueprint.GearRequirement
                    {
                        Item = NameOf(g.GearItem), Count = g.Count,
                        Units = string.Equals(g.Units.ToString(), "Kilograms", StringComparison.OrdinalIgnoreCase) ? RequirementUnits.Kilograms : RequirementUnits.Count
                    });
            foreach (var p in v1.RequiredPowder ?? Array.Empty<ModComponent.Blueprints.PowderRequirement>())
                if (p != null) bp.RequiredPowder.Add(new Blueprint.PowderRequirement { Powder = NameOf(p.PowderItem), QuantityInKilograms = p.QuantityInKilograms });
            foreach (var l in v1.RequiredLiquid ?? Array.Empty<ModComponent.Blueprints.LiquidRequirement>())
                if (l != null) bp.RequiredLiquid.Add(new Blueprint.LiquidRequirement { Liquid = NameOf(l.LiquidItem), VolumeInLitres = l.VolumeInLitres });
            bp.RequiredTool = NameOf(v1.RequiredTool);
            bp.OptionalTools = (v1.OptionalTools ?? Array.Empty<ModComponent.SDK.Components.DataGearAsset>()).Where(t => t != null).Select(t => new GearRef(t.name)).ToList();
            Enum.TryParse(v1.RequiredCraftingLocation.ToString(), true, out bp.RequiredCraftingLocation);
            bp.RequiresLitFire = v1.RequiresLitFire;
            bp.RequiresLight = v1.RequiresLight;
            bp.CraftedResult = NameOf(v1.CraftedResult);
            bp.CraftedResultCount = Math.Max(1, v1.CraftedResultCount);
            bp.DurationMinutes = v1.DurationMinutes;
            bp.CraftingAudio = NameOf(v1.CraftingAudio);
            Enum.TryParse(v1.AppliedSkill.ToString(), true, out bp.AppliedSkill);
            Enum.TryParse(v1.ImprovedSkill.ToString(), true, out bp.ImprovedSkill);

            if (v1 is V1Recipe recipe)
            {
                bp.IsCookingRecipe = true;
                bp.RecipeName = recipe.RecipeName ?? string.Empty;
                bp.RecipeDescription = recipe.RecipeDescription ?? string.Empty;
                bp.RequiredSkillLevel = Math.Max(1, recipe.RequiredSkillLevel);
                bp.AllowedCookingPots = (recipe.AllowedCookingPots ?? Array.Empty<ModComponent.SDK.Components.DataGearAsset>())
                    .Where(t => t != null).Select(t => new GearRef(t.name)).ToList();
            }

            if (bp.CraftedResult.IsEmpty)
            {
                bp.CraftedResult = prefab.name;
                report.Note($"{bp.name}: no crafted result was set; assumed {prefab.name}, the prefab it was on.");
            }

            return bp;
        }

        private static LocalizationTable ImportLocalization(V1Definition definition, ImportReport report)
        {
            var table = ScriptableObject.CreateInstance<LocalizationTable>();
            table.name = definition.Name + " Localization";
            var v1 = definition.modLocalization;
            if (v1?.localizationEntries == null) return table;

            foreach (var entry in v1.localizationEntries.Where(e => e != null && !string.IsNullOrWhiteSpace(e.localizationKey)))
            {
                var row = table.GetOrAdd(entry.localizationKey.Trim());
                foreach (var (field, language) in Languages)
                {
                    var text = entry.languages?.GetType().GetField(field)?.GetValue(entry.languages) as string;
                    if (!string.IsNullOrEmpty(text)) row.Set(language, text);
                }
            }

            report.LocalizationKeys = table.Entries.Count;
            return table;
        }

        private static SpawnTable ImportSpawns(V1Definition definition, ImportReport report)
        {
            var table = ScriptableObject.CreateInstance<SpawnTable>();
            table.name = definition.Name + " Spawns";
            var v1 = definition.modGearSpawns;
            if (v1 == null) return table;

            foreach (var scene in v1.sceneGearSpawnEntries.Where(s => s?.sceneName != null))
            {
                var group = new SpawnTable.SceneGroup { Scene = scene.sceneName.name };
                foreach (var spawn in scene.sceneItemSpawns.Where(s => s?.itemName != null))
                    group.Spawns.Add(new SpawnTable.SceneSpawn
                    {
                        Item = spawn.itemName.name, Position = spawn.position, Rotation = spawn.rotation, Chance = spawn.spawnChance
                    });
                table.Scenes.Add(group);
                report.Spawns += group.Spawns.Count;
            }

            foreach (var loot in v1.lootTableGearSpawnEntries.Where(s => s?.lootTableName != null))
            {
                var group = new SpawnTable.LootTableGroup { LootTable = loot.lootTableName.name };
                foreach (var entry in loot.lootTableSpawns.Where(s => s?.itemName != null))
                    group.Entries.Add(new SpawnTable.LootEntry { Item = entry.itemName.name, Weight = Math.Max(1, entry.weight) });
                table.LootTables.Add(group);
                report.Spawns += group.Entries.Count;
            }

            return table;
        }

        private static void ImportRarities(V1Definition definition, Mod mod, ImportReport report)
        {
            var v1 = definition.modItemRarities;
            if (v1 == null) return;
            mod.EnableIntegration<ItemRaritiesIntegration>();
            var tiers = new (GameObject[] prefabs, Rarity rarity)[]
            {
                (v1.Common, Rarity.Common), (v1.Uncommon, Rarity.Uncommon), (v1.Rare, Rarity.Rare),
                (v1.Epic, Rarity.Epic), (v1.Legendary, Rarity.Legendary), (v1.Mythic, Rarity.Mythic)
            };
            foreach (var (prefabs, rarity) in tiers)
            foreach (var prefab in (prefabs ?? Array.Empty<GameObject>()).Where(p => p != null))
            {
                var item = mod.Items.FirstOrDefault(i => i.Prefab == prefab);
                if (item != null) item.GetOrAddIntegrationData<ItemRarityData>().Rarity = rarity;
                else report.Note($"Rarity for {prefab.name} skipped: it isn't one of the mod's items.");
            }
        }

        private static void Save(ImportReport report, string folder)
        {
            AssetFactory.EnsureFolder(folder);
            var items = AssetFactory.EnsureFolder(folder + "/Items");
            var blueprints = AssetFactory.EnsureFolder(folder + "/Blueprints");
            var mod = report.Mod;

            foreach (var item in report.Items) AssetDatabase.CreateAsset(item, AssetDatabase.GenerateUniqueAssetPath($"{items}/{item.name}.asset"));
            foreach (var bp in report.Blueprints) AssetDatabase.CreateAsset(bp, AssetDatabase.GenerateUniqueAssetPath($"{blueprints}/{bp.name}.asset"));
            AssetDatabase.CreateAsset(mod.Localization, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{mod.Id}Localization.asset"));
            AssetDatabase.CreateAsset(mod.Spawns, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{mod.Id}Spawns.asset"));
            AssetDatabase.CreateAsset(mod, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{mod.Id}.asset"));
            AssetDatabase.SaveAssets();
        }

        private static Texture2D Take(List<Texture2D> icons, string name)
        {
            var icon = icons.FirstOrDefault(i => string.Equals(i.name, name, StringComparison.OrdinalIgnoreCase));
            if (icon != null) icons.Remove(icon);
            return icon;
        }

        public static bool IsLegacy(Component component) =>
            component != null && component.GetType().Assembly.GetName().Name == LegacyAssembly;

        /// <summary>Whether the prefab still carries 1.x scripts, which would show as missing scripts in game.</summary>
        public static bool HasLegacyComponents(GameObject prefab) =>
            prefab != null && prefab.GetComponentsInChildren<MonoBehaviour>(true).Any(IsLegacy);

        /// <summary>Removes every 1.x script from the prefab asset. The prefab is otherwise unchanged.</summary>
        public static int RemoveLegacyComponents(GameObject prefab)
        {
            var path = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path)) return 0;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // Components on nested prefab instances belong to the nested prefab and can't be removed here.
                var legacy = root.GetComponentsInChildren<MonoBehaviour>(true)
                    .Where(c => IsLegacy(c) && !PrefabUtility.IsPartOfPrefabInstance(c)).ToList();
                foreach (var component in legacy) UnityEngine.Object.DestroyImmediate(component);
                if (legacy.Count > 0) PrefabUtility.SaveAsPrefabAsset(root, path);

                var nested = root.GetComponentsInChildren<MonoBehaviour>(true).Where(IsLegacy).Select(c => c.gameObject.name).Distinct().ToList();
                if (nested.Count > 0)
                    Debug.LogWarning($"{prefab.name}: 1.x scripts on nested prefabs ({string.Join(", ", nested)}) weren't removed. " +
                                     "Open those prefabs and remove them there.", prefab);
                return legacy.Count;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
