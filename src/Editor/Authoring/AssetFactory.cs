using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModComponent.SDK.Integrations;
using ModComponent.SDK.Modules;
using UnityEditor;
using UnityEngine;

namespace ModComponent.SDK.Editor.Authoring
{
    /// <summary>Creates mods, items and blueprints as assets, wired together. Used by the wizards and the Hub.</summary>
    public static class AssetFactory
    {
        public const string DefaultModsFolder = "Assets/Mods";

        public sealed class ModSettings
        {
            public string DisplayName = "New Mod";
            public string Id;
            public string Author = string.Empty;
            public string Version = "1.0.0";
            public string Folder;

            /// <summary><see cref="ModIntegration"/> types to enable, such as an add-on's.</summary>
            public List<Type> Integrations = new();
        }

        public sealed class ItemSettings
        {
            public Mod Mod;
            public string GearName;
            public string DisplayName;
            public string Description;

            /// <summary>An FBX, model or prefab to put under the item's root. Optional.</summary>
            public GameObject Model;

            public ItemTemplate Template = ItemTemplates.All[0];
        }

        /// <summary>
        /// Creates a mod folder with the Mod asset, a localization table, a spawn table and Items/ and
        /// Blueprints/ folders.
        /// </summary>
        public static Mod CreateMod(ModSettings settings)
        {
            var id = string.IsNullOrEmpty(settings.Id) ? Mod.ToId(settings.DisplayName) : settings.Id;
            if (!Mod.IsValidId(id)) throw new ArgumentException($"\"{id}\" isn't a valid mod id.");

            var folder = EnsureFolder(string.IsNullOrEmpty(settings.Folder) ? $"{DefaultModsFolder}/{id}" : settings.Folder);
            EnsureFolder($"{folder}/Items");
            EnsureFolder($"{folder}/Blueprints");

            var localization = ScriptableObject.CreateInstance<LocalizationTable>();
            AssetDatabase.CreateAsset(localization, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{id}Localization.asset"));

            var spawns = ScriptableObject.CreateInstance<SpawnTable>();
            AssetDatabase.CreateAsset(spawns, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{id}Spawns.asset"));

            var mod = ScriptableObject.CreateInstance<Mod>();
            mod.DisplayName = settings.DisplayName;
            mod.Id = id;
            mod.Author = settings.Author ?? string.Empty;
            mod.Version = settings.Version;
            mod.Localization = localization;
            mod.Spawns = spawns;
            foreach (var integration in settings.Integrations ?? new List<Type>()) mod.EnableIntegration(integration);
            AssetDatabase.CreateAsset(mod, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{id}.asset"));

            AssetDatabase.SaveAssets();
            return mod;
        }

        /// <summary>
        /// Creates a GEAR_ prefab (root with a collider, the model as a child), the GearItem with the template's
        /// modules, and English name and description keys, then adds the item to the mod.
        /// </summary>
        public static GearItem CreateItem(ItemSettings settings)
        {
            if (settings.Mod == null) throw new ArgumentNullException(nameof(settings.Mod));
            var gearName = NormalizeGearName(settings.GearName);
            if (settings.Mod.FindItem(gearName) != null) throw new ArgumentException($"{settings.Mod.DisplayName} already has {gearName}.");

            var folder = EnsureFolder($"{ModFolder(settings.Mod)}/Items");
            var shortName = gearName.Substring(GearItem.GearPrefix.Length);

            // The prefab must be named exactly like the gear, so an existing file is an error rather than "GEAR_X 1".
            var prefabPath = $"{folder}/{gearName}.prefab";
            if (AssetDatabase.LoadMainAssetAtPath(prefabPath) != null)
                throw new ArgumentException($"{prefabPath} already exists. Delete or rename it first, or pick another gear name.");
            var prefab = CreateItemPrefab(gearName, settings.Model, prefabPath);

            var item = ScriptableObject.CreateInstance<GearItem>();
            item.GearName = gearName;
            item.Prefab = prefab;
            (settings.Template ?? ItemTemplates.All[0]).Apply(item);

            var nameKey = $"GAMEPLAY_{shortName}";
            var descriptionKey = $"GAMEPLAY_{shortName}Description";
            item.Component.DisplayNameLocalizationId = nameKey;
            item.Component.DescriptionLocalizationId = descriptionKey;
            if (settings.Model != null && prefab.transform.childCount > 0)
                item.Component.NormalModel = prefab.transform.GetChild(0).name;

            AssetDatabase.CreateAsset(item, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{gearName}.asset"));

            var table = settings.Mod.Localization;
            if (table != null)
            {
                Undo.RecordObject(table, "Add localization");
                table.Set(nameKey, LocalizationTable.DefaultLanguage, string.IsNullOrWhiteSpace(settings.DisplayName) ? Nicify(shortName) : settings.DisplayName);
                if (!string.IsNullOrWhiteSpace(settings.Description))
                    table.Set(descriptionKey, LocalizationTable.DefaultLanguage, settings.Description);
                else
                    table.GetOrAdd(descriptionKey);
                EditorUtility.SetDirty(table);
            }

            Undo.RecordObject(settings.Mod, "Add item");
            settings.Mod.Items.Add(item);
            EditorUtility.SetDirty(settings.Mod);
            AssetDatabase.SaveAssets();
            return item;
        }

        public static Blueprint CreateBlueprint(Mod mod, string name, GearItem result = null)
        {
            var folder = EnsureFolder($"{ModFolder(mod)}/Blueprints");
            var bp = ScriptableObject.CreateInstance<Blueprint>();
            if (result != null) bp.CraftedResult = result.GearName;
            AssetDatabase.CreateAsset(bp, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}.asset"));
            Undo.RecordObject(mod, "Add blueprint");
            mod.Blueprints.Add(bp);
            EditorUtility.SetDirty(mod);
            AssetDatabase.SaveAssets();
            return bp;
        }

        public static LocalizationTable CreateLocalizationTable(Mod mod)
        {
            var table = ScriptableObject.CreateInstance<LocalizationTable>();
            AssetDatabase.CreateAsset(table, AssetDatabase.GenerateUniqueAssetPath($"{ModFolder(mod)}/{mod.Id}Localization.asset"));
            Undo.RecordObject(mod, "Add localization table");
            mod.Localization = table;
            EditorUtility.SetDirty(mod);
            return table;
        }

        public static SpawnTable CreateSpawnTable(Mod mod)
        {
            var table = ScriptableObject.CreateInstance<SpawnTable>();
            AssetDatabase.CreateAsset(table, AssetDatabase.GenerateUniqueAssetPath($"{ModFolder(mod)}/{mod.Id}Spawns.asset"));
            Undo.RecordObject(mod, "Add spawn table");
            mod.Spawns = table;
            EditorUtility.SetDirty(mod);
            return table;
        }

        /// <summary>Builds a prefab the item rules accept: a GEAR_ root with a box collider and the model as a child.</summary>
        public static GameObject CreateItemPrefab(string gearName, GameObject model, string path)
        {
            var root = new GameObject(gearName);
            try
            {
                if (model != null)
                {
                    var child = PrefabUtility.IsPartOfPrefabAsset(model) || PrefabUtility.GetPrefabAssetType(model) == PrefabAssetType.Model
                        ? (GameObject)PrefabUtility.InstantiatePrefab(model)
                        : UnityEngine.Object.Instantiate(model);
                    child.name = model.name;
                    child.transform.SetParent(root.transform, false);
                    foreach (var body in child.GetComponentsInChildren<Rigidbody>()) UnityEngine.Object.DestroyImmediate(body);
                }

                var collider = root.AddComponent<BoxCollider>();
                var renderers = root.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    var bounds = renderers[0].bounds;
                    foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
                    collider.center = root.transform.InverseTransformPoint(bounds.center);
                    collider.size = bounds.size;
                }
                else
                {
                    collider.size = Vector3.one * 0.1f;
                }

                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>"Wool Hat" → "GEAR_WoolHat"; keeps an existing prefix; drops anything that isn't a word character.</summary>
        public static string NormalizeGearName(string name)
        {
            var trimmed = (name ?? string.Empty).Trim();
            if (trimmed.StartsWith(GearItem.GearPrefix, StringComparison.Ordinal)) trimmed = trimmed.Substring(GearItem.GearPrefix.Length);
            var clean = new string(trimmed.Where(c => c < 128 && (char.IsLetterOrDigit(c) || c == '_')).ToArray());
            if (clean.Length == 0) throw new ArgumentException("The gear name needs at least one letter or digit.");
            return GearItem.GearPrefix + char.ToUpperInvariant(clean[0]) + clean.Substring(1);
        }

        public static string ModFolder(Mod mod)
        {
            var path = AssetDatabase.GetAssetPath(mod);
            return string.IsNullOrEmpty(path) ? DefaultModsFolder : Path.GetDirectoryName(path)!.Replace('\\', '/');
        }

        public static string EnsureFolder(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return path;
            var parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
            return path;
        }

        private static string Nicify(string shortName) => ObjectNames.NicifyVariableName(shortName);
    }
}
