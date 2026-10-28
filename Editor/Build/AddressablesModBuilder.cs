using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace ModComponent.SDK.Editor.Build
{
    /// <summary>
    /// Builds one mod's assets into AssetBundles plus a JSON catalog in Addressables' legacy layout, which
    /// ModComponent parses. Everything changed in the project's Addressables settings is put back afterwards,
    /// even if the build fails, and the temporary group and profile variable are removed.
    /// </summary>
    public static class AddressablesModBuilder
    {
        internal const string GroupPrefix = "ModComponentSDK_";
        private const string PathVariable = "ModComponentSDK.BuildPath";

        public sealed class Result
        {
            public readonly List<string> BundlePaths = new();
            public string CatalogPath;
            public string Error;
            public bool Succeeded => string.IsNullOrEmpty(Error);
        }

        /// <summary>One asset to put in the bundle, and the address ModComponent will load it by.</summary>
        public readonly struct Entry
        {
            public Entry(UnityEngine.Object asset, string address)
            {
                Asset = asset;
                Address = address;
            }

            public UnityEngine.Object Asset { get; }
            public string Address { get; }
        }

        /// <summary>Item prefabs, icons and the mod's extra bundled assets, with their addresses.</summary>
        public static List<Entry> CollectEntries(Mod mod)
        {
            var entries = new List<Entry>();
            foreach (var item in mod.Items.Where(i => i != null))
            {
                if (item.Prefab != null) entries.Add(new Entry(item.Prefab, item.GearName));
                if (item.Icon != null) entries.Add(new Entry(item.Icon, item.InventoryIconAddress));
                if (item.CraftingIcon != null) entries.Add(new Entry(item.CraftingIcon, item.CraftingIconAddress));
            }

            foreach (var asset in mod.BundledAssets.Where(a => a != null))
                entries.Add(new Entry(asset, asset.name));

            return entries;
        }

        /// <param name="outputDirectory">A scratch folder under <see cref="ModBuildPipeline.StagingRoot"/>; it is emptied first.</param>
        internal static Result Build(Mod mod, string outputDirectory)
        {
            var result = new Result();
            var full = Path.GetFullPath(outputDirectory);
            if (!full.StartsWith(ModBuildPipeline.StagingRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Bundles are built in a scratch folder under {ModBuildPipeline.StagingRoot}.", nameof(outputDirectory));

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            // Start clean so a stale catalog or bundle from an earlier build can't be picked up.
            if (Directory.Exists(full)) Directory.Delete(full, true);
            Directory.CreateDirectory(full);
            outputDirectory = full;

            // A group left by a build that never reached its cleanup (editor crash, killed process).
            foreach (var leftover in settings.groups.Where(g => g != null && g.Name.StartsWith(GroupPrefix, StringComparison.Ordinal)).ToList())
            {
                Debug.LogWarning($"Removing Addressables group {leftover.Name} left behind by an interrupted mod build. " +
                                 "Assets it held are no longer addressable; re-add any that belonged to your own groups.");
                settings.RemoveGroup(leftover);
            }

            var snapshot = Snapshot.Take(settings);
            AddressableAssetGroup group = null;
            string variableId = null;

            try
            {
                var id = mod.Id;
                variableId = settings.profileSettings.CreateValue(PathVariable, outputDirectory);
                settings.profileSettings.SetValue(settings.activeProfileId, PathVariable, outputDirectory.Replace('\\', '/'));

                group = settings.CreateGroup(GroupPrefix + id, false, false, false, null, typeof(BundledAssetGroupSchema));
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                // Bundle names must be unique across every installed mod, so they carry the mod id and no hash.
                schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.NoHash;
                schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
                schema.IncludeInBuild = true;
                schema.BuildPath.SetVariableByName(settings, PathVariable);
                schema.LoadPath.SetVariableByName(settings, PathVariable);

                foreach (var other in settings.groups.Where(g => g != null && g != group))
                {
                    var otherSchema = other.GetSchema<BundledAssetGroupSchema>();
                    if (otherSchema != null) otherSchema.IncludeInBuild = false;
                }

                foreach (var entry in CollectEntries(mod))
                {
                    var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(entry.Asset));
                    if (string.IsNullOrEmpty(guid))
                        throw new InvalidOperationException($"{entry.Asset.name} is not a saved asset, so it cannot be bundled.");
                    snapshot.RememberEntry(settings, guid);
                    var addressable = settings.CreateOrMoveEntry(guid, group, false, false);
                    addressable.address = entry.Address;
                }

                settings.OverridePlayerVersion = id;
                settings.EnableJsonCatalog = true;
                settings.BuildRemoteCatalog = true;
                settings.RemoteCatalogBuildPath.SetVariableByName(settings, PathVariable);
                settings.RemoteCatalogLoadPath.SetVariableByName(settings, PathVariable);
                settings.BuiltInBundleNaming = BuiltInBundleNaming.Custom;
                settings.BuiltInBundleCustomNaming = id;
                settings.MonoScriptBundleNaming = MonoScriptBundleNaming.Custom;
                settings.MonoScriptBundleCustomNaming = id;

                AddressableAssetSettings.BuildPlayerContent(out var build);
                if (!string.IsNullOrEmpty(build.Error))
                {
                    result.Error = build.Error;
                    return result;
                }

                result.BundlePaths.AddRange(build.AssetBundleBuildResults
                    .Select(b => b.FilePath)
                    .Where(File.Exists)
                    .Distinct(StringComparer.OrdinalIgnoreCase));
                // The remote catalog is written as catalog_<OverridePlayerVersion>.json in the remote catalog build path.
                result.CatalogPath = Path.Combine(outputDirectory, $"catalog_{id}.json");
                if (string.IsNullOrEmpty(result.CatalogPath) || !File.Exists(result.CatalogPath))
                    result.Error = "Addressables did not write a JSON catalog.";
                else if (result.BundlePaths.Count == 0)
                    result.Error = "Addressables built no bundles.";
            }
            catch (Exception e)
            {
                result.Error = e.Message;
                Debug.LogException(e);
            }
            finally
            {
                snapshot.Restore(settings);
                if (group != null) settings.RemoveGroup(group);
                if (variableId != null) settings.profileSettings.RemoveValue(variableId);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            return result;
        }

        /// <summary>The Addressables settings this builder touches, captured so they can be put back exactly.</summary>
        private sealed class Snapshot
        {
            private string _playerVersion;
            private bool _jsonCatalog;
            private bool _remoteCatalog;
            private string _remoteCatalogBuildPath;
            private string _remoteCatalogLoadPath;
            private BuiltInBundleNaming _builtInNaming;
            private string _builtInCustom;
            private MonoScriptBundleNaming _monoNaming;
            private string _monoCustom;
            private readonly Dictionary<BundledAssetGroupSchema, bool> _includeInBuild = new();
            private readonly Dictionary<string, (string groupGuid, string address)> _entries = new();
            private readonly HashSet<string> _newEntries = new();

            public static Snapshot Take(AddressableAssetSettings settings)
            {
                var s = new Snapshot
                {
                    _playerVersion = settings.OverridePlayerVersion,
                    _jsonCatalog = settings.EnableJsonCatalog,
                    _remoteCatalog = settings.BuildRemoteCatalog,
                    _remoteCatalogBuildPath = settings.RemoteCatalogBuildPath.Id,
                    _remoteCatalogLoadPath = settings.RemoteCatalogLoadPath.Id,
                    _builtInNaming = settings.BuiltInBundleNaming,
                    _builtInCustom = settings.BuiltInBundleCustomNaming,
                    _monoNaming = settings.MonoScriptBundleNaming,
                    _monoCustom = settings.MonoScriptBundleCustomNaming,
                };
                foreach (var group in settings.groups.Where(g => g != null))
                {
                    var schema = group.GetSchema<BundledAssetGroupSchema>();
                    if (schema != null) s._includeInBuild[schema] = schema.IncludeInBuild;
                }

                return s;
            }

            /// <summary>Records which group an asset was in before the builder moves it.</summary>
            public void RememberEntry(AddressableAssetSettings settings, string guid)
            {
                if (_entries.ContainsKey(guid) || _newEntries.Contains(guid)) return;
                var existing = settings.FindAssetEntry(guid);
                if (existing == null) _newEntries.Add(guid);
                else _entries[guid] = (existing.parentGroup.Guid, existing.address);
            }

            public void Restore(AddressableAssetSettings settings)
            {
                foreach (var (guid, (groupGuid, address)) in _entries)
                {
                    var group = settings.FindGroup(g => g != null && g.Guid == groupGuid);
                    if (group == null) continue;
                    var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                    entry.address = address;
                }

                foreach (var guid in _newEntries) settings.RemoveAssetEntry(guid, false);

                foreach (var (schema, include) in _includeInBuild)
                    if (schema != null) schema.IncludeInBuild = include;

                settings.OverridePlayerVersion = _playerVersion;
                settings.EnableJsonCatalog = _jsonCatalog;
                settings.BuildRemoteCatalog = _remoteCatalog;
                settings.RemoteCatalogBuildPath.SetVariableById(settings, _remoteCatalogBuildPath);
                settings.RemoteCatalogLoadPath.SetVariableById(settings, _remoteCatalogLoadPath);
                settings.BuiltInBundleNaming = _builtInNaming;
                settings.BuiltInBundleCustomNaming = _builtInCustom;
                settings.MonoScriptBundleNaming = _monoNaming;
                settings.MonoScriptBundleCustomNaming = _monoCustom;
            }
        }
    }
}
